// UI Screen blend + dark outline for shui_hua splash (visible on light and dark water).

Shader "AsGame/Spine/SkeletonGraphic Screen Outline"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[Toggle(_CANVAS_GROUP_COMPATIBLE)] _CanvasGroupCompatible("CanvasGroup Compatible", Int) = 0
		_Color ("Tint", Color) = (1,1,1,1)

		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp ("Stencil Comparison", Float) = 8
		[HideInInspector] _Stencil ("Stencil ID", Float) = 0
		[HideInInspector][Enum(UnityEngine.Rendering.StencilOp)] _StencilOp ("Stencil Operation", Float) = 0
		[HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
		[HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
		[HideInInspector] _ColorMask ("Color Mask", Float) = 15
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

		[HideInInspector] _OutlineWidth("Outline Width", Range(0,8)) = 3.0
		[HideInInspector] _OutlineColor("Outline Color", Color) = (0.45,0.45,0.45,0.55)
		[HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Int) = 392
		[HideInInspector] _ThresholdEnd("Outline Threshold", Range(0,1)) = 0.25
		[HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0,1)) = 0.65
		[HideInInspector][MaterialToggle(_USE8NEIGHBOURHOOD_ON)] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
		[HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0,3)) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Fog { Mode Off }
		ColorMask [_ColorMask]

		Pass
		{
			Name "Outline"
			Blend One OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vertOutlineGraphic
			#pragma fragment fragOutline
			#define SKELETON_GRAPHIC
			#pragma shader_feature _ _USE8NEIGHBOURHOOD_ON
			#include "../../Spine/Runtime/spine-unity/Shaders/Outline/CGIncludes/Spine-Outline-Pass.cginc"
			ENDCG
		}

		Pass
		{
			Name "Normal"
			Blend One OneMinusSrcColor

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
			#pragma shader_feature _ _CANVAS_GROUP_COMPATIBLE
			#pragma multi_compile __ UNITY_UI_ALPHACLIP

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			struct VertexInput {
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput {
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;

			VertexOutput vert (VertexInput IN) {
				VertexOutput OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
				OUT.texcoord = IN.texcoord;
				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw-1.0) * float2(-1,1);
				#endif
				OUT.color = IN.color * float4(_Color.rgb * _Color.a, _Color.a);
				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag (VertexOutput IN) : SV_Target
			{
				half4 texColor = tex2D(_MainTex, IN.texcoord);
				#if defined(_STRAIGHT_ALPHA_INPUT)
				texColor.rgb *= texColor.a;
				#endif
				half4 color = (texColor + _TextureSampleAdd) * IN.color;
				#ifdef _CANVAS_GROUP_COMPATIBLE
				color.rgb *= IN.color.a;
				#endif
				color *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#ifdef UNITY_UI_ALPHACLIP
				clip (color.a - 0.001);
				#endif
				return color;
			}
		ENDCG
		}
	}
}
