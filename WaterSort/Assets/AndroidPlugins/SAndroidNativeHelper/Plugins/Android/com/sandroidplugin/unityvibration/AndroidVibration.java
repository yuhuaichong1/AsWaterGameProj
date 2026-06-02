package com.sandroidplugin.unityvibration;

import android.app.Activity;
import android.content.Context;
import android.os.Vibrator;
import android.os.VibrationEffect;
import android.os.Build;

public class AndroidVibration
{
    private Activity activity;
    private Vibrator vibrator;

	public AndroidVibration(Activity unityActivity) 
    {
        activity = unityActivity;
        vibrator = (Vibrator) activity.getSystemService(Context.VIBRATOR_SERVICE);
    }

    public void PlayVibration(long milliseconds, int amplitude)
    {
        Vibrate(milliseconds, amplitude);
    }

    public void CancelVibration()
    {
        if (vibrator != null)
        {
            vibrator.cancel();
        }
    }

    private void Vibrate(long milliseconds, int amplitude)
    {
        if(amplitude == -1)
        {
            amplitude = VibrationEffect.DEFAULT_AMPLITUDE;
        }

        if (vibrator == null || !vibrator.hasVibrator())
        {
            return;
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
        {
            VibrationEffect effect = VibrationEffect.createOneShot(milliseconds, amplitude);
            vibrator.vibrate(effect);
        }
        else
        {
            vibrator.vibrate(milliseconds);
        }
    }
}