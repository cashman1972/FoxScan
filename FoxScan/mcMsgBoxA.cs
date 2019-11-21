using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FoxScan
{
    public enum IconType
    {
        Information,
        Exclamation,
        Critical,
        Checked
    }

    public static class mcMsgBoxA
    {
        public static void ShowMsgWOK(Activity activity, string Title, string MessageText, IconType iconType)
        {
            int icon = Resource.Drawable.iconInfo64;

            switch (iconType)
            {
                case IconType.Information:
                    {
                        icon = Resource.Drawable.iconInfo64;
                        break;
                    }
                case IconType.Checked:
                    {
                        icon = Resource.Drawable.iconCheck64;
                        break;
                    }
                case IconType.Exclamation:
                    {
                        icon = Resource.Drawable.iconWarning64;
                        break;
                    }
                case IconType.Critical:
                    {
                        icon = Resource.Drawable.iconBang64;
                        break;
                    }
            }

            var builder = new Android.App.AlertDialog.Builder(activity);
            if (Title != "")
            {
                builder.SetTitle(Title);
            }

            builder.SetIcon(icon);
            builder.SetMessage(MessageText);
            builder.SetPositiveButton("OK", (s, e2) =>
            {

            }
            );
            builder.Create().Show();
        }
        
    }
}