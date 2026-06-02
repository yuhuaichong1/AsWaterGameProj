package com.sandroidplugin.unitytoast;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.view.Gravity;
import android.view.View;
import android.widget.LinearLayout;
import android.widget.TextView;
import android.widget.Toast;
import com.unity3d.player.UnityPlayer;
import android.content.Context;
import android.graphics.Typeface;

public class AndroidToast
{
	private Activity currentActivity;

	public AndroidToast(Activity activity)
    {
        currentActivity = activity;
	}

	public void ShowToast(String toastText, boolean ifAdaptive, int showDuration, int toastFontSize, String toastFontColor, int toastFontStype, int bgWidth, int bgHeight, String bgColor, int bgAlpha, int outlineWidth, String outlineColor)
	{
        
		LinearLayout layout = new LinearLayout(currentActivity);
        layout.setOrientation(LinearLayout.VERTICAL);
        int paddingLeftRight = 30;
        int paddingTopBottom = 20;
        layout.setPadding(paddingLeftRight, paddingTopBottom, paddingLeftRight, paddingTopBottom);

        if(ifAdaptive)
        {

        }
        else
        {
            layout.setMinimumWidth(bgWidth);
            layout.setMinimumHeight(bgHeight);
        }


        layout.setGravity(Gravity.CENTER);

        GradientDrawable bgDrawable = new GradientDrawable();
        bgDrawable.setColor(Color.parseColor(bgColor));
        bgDrawable.setAlpha(bgAlpha);
        bgDrawable.setCornerRadius(20);
        bgDrawable.setStroke(outlineWidth, Color.parseColor(outlineColor));
        layout.setBackground(bgDrawable);

        TextView tipTv = new TextView(currentActivity);
        tipTv.setText(toastText);
        tipTv.setTextColor(Color.parseColor(toastFontColor));
        switch(toastFontStype)
        {
            case 0:
            tipTv.setTypeface(null, Typeface.NORMAL);
            break;
            case 1:
            tipTv.setTypeface(null, Typeface.BOLD);
            break;
            case 2:
            tipTv.setTypeface(null, Typeface.ITALIC);
            break;
            case 3:
            tipTv.setTypeface(null, Typeface.BOLD | Typeface.ITALIC);
            break;
        }
        tipTv.setTextSize(toastFontSize);
        tipTv.setGravity(Gravity.CENTER);
        tipTv.setSingleLine(false);
        layout.addView(tipTv);

		currentActivity.runOnUiThread(new Runnable()
		{
            @Override
            public void run()
			{
				Toast toast = new Toast(currentActivity);
				toast.setDuration(showDuration);
				toast.setGravity(Gravity.CENTER, 0, 0);
				toast.setView(layout);

				toast.show();
            }
        });
	}
}

        /*
        LinearLayout.LayoutParams layoutParams = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.WRAP_CONTENT,
                LinearLayout.LayoutParams.WRAP_CONTENT
        );
        layout.setLayoutParams(layoutParams);
        */