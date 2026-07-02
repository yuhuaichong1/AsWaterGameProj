Shader "UI/HLSL_StencilMask"
{
    Properties
    {
        _MaskTex ("Mask Texture", 2D) = "white" {}
        _UseMaskTex ("Use Mask Texture", Float) = 1
        _Radius ("Circle Radius", Range(0,1)) = 0.45
        _Feather ("Edge Feather", Range(0,0.5)) = 0.05
        _Cutoff ("Cutoff", Range(0,1)) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Geometry-1" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ColorMask 0
        ZWrite Off
        Cull Off
        Blend Off

        Pass
        {
            Name "MaskWrite"
            Stencil { Ref 1 Comp Always Pass Replace }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MaskTex;
            float _UseMaskTex;
            float _Radius, _Feather, _Cutoff;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float maskA = 1.0;

                if (_UseMaskTex >= 0.5)
                {
                    maskA = tex2D(_MaskTex, i.uv).a;
                }
                else
                {
                    float2 uv = i.uv * 2 - 1;
                    float dist = length(uv);
                    maskA = 1.0 - smoothstep(_Radius - _Feather, _Radius + _Feather, dist);
                }

                if (maskA <= _Cutoff) discard;
                return half4(0,0,0,0);
            }
            ENDHLSL
        }
    }
}