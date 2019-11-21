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
    public class MyHandler : Handler
    {
        private activity_Scan activity;

        public MyHandler(activity_Scan activity)
        {
            this.activity = activity;
        }

        public override void HandleMessage(Message msg)
        {
            switch (msg.Arg1)
            {
                case 0:
                    // Do nothing
                    break;
                case 1:
                    // Call activity.ViewReports()
                    activity.ViewReports();
                    break;
                case 2:
                    // Import completed successfully
                    activity.GotoDataTransfer("");
                    break;
                case 99:
                    // Import FAILED!
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }
}