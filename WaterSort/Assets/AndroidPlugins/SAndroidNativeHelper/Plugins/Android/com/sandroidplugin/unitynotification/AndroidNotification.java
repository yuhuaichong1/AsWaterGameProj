package com.sandroidplugin.unitynotification;

import android.app.Activity;
import android.app.NotificationManager;
import android.content.Context;
import android.content.Intent;
import android.app.PendingIntent;
import androidx.core.app.NotificationCompat;
import android.app.NotificationChannel;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import android.os.Build;

public class AndroidNotification 
{
    private static final String CHANNEL_ID = "default_channel";
    private Activity activity;

    public AndroidNotification(Activity unityActivity) 
    {
        this.activity = unityActivity;
    }

    public void ShowNotification(int notificationID, String title, String content, String iconName)
    {
        if(activity == null)
            return;

        CreateNotificationChannel();

        PendingIntent pendingIntent = CreatePendingIntent();

        NotificationCompat.Builder builder = new NotificationCompat.Builder(activity, CHANNEL_ID)
        .setSmallIcon(GetIconResourceId(iconName))
        .setContentTitle(title)
        .setContentText(content)
        .setPriority(NotificationCompat.PRIORITY_DEFAULT)
        .setContentIntent(pendingIntent)
        .setAutoCancel(true);

        NotificationManager notificationManager = (NotificationManager) activity.getSystemService(Context.NOTIFICATION_SERVICE);

        notificationManager.notify(notificationID, builder.build());
    }

    private void CreateNotificationChannel()
    {
        if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O)
        {
            NotificationChannel channel = new NotificationChannel(
                CHANNEL_ID, 
                "Default Channel", 
                NotificationManager.IMPORTANCE_DEFAULT
            );
            NotificationManager notificationManager = activity.getSystemService(NotificationManager.class);
            notificationManager.createNotificationChannel(channel);
        }
    }

    private int GetIconResourceId(String resourceName)
    {
        int iconId = activity.getResources().getIdentifier(resourceName, "drawable", activity.getPackageName());
        if (iconId == 0)
        {
            iconId = android.R.drawable.ic_dialog_info;
        }
        return iconId;
    }

    private Bitmap GetIconBitmap(String resourceName)
    {
        int iconId = GetIconResourceId(resourceName);
        try
        {
            return BitmapFactory.decodeResource(activity.getResources(), iconId);
        }
        catch (Exception e) 
        {
            Drawable defaultDrawable = activity.getResources().getDrawable(android.R.drawable.ic_dialog_info);
            return ((BitmapDrawable) defaultDrawable).getBitmap();
        }
    }

    private PendingIntent CreatePendingIntent()
    {
        Intent intent = activity.getPackageManager().getLaunchIntentForPackage(activity.getPackageName());

        if (intent == null)
        {
            intent = new Intent();
        }

        //intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        
        int flags = PendingIntent.FLAG_UPDATE_CURRENT;
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
        {
            flags |= PendingIntent.FLAG_IMMUTABLE;
        }

        return PendingIntent.getActivity(activity, 0, intent, flags);
    }
}