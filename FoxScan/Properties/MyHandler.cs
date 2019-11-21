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
        private activity_dbTest _activity;

        public MyHandler(activity_dbTest _activity)
        {
            this._activity = _activity;
        }

        public override void HandleMessage(Message msg)
        {
            //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
            switch (msg.Arg1)
            {
                case 0:
                    // Copying data from server
                    //mainActivity.Exist();
                    //mainActivity.mDialog.Dismiss();
                    break;
                case 1:
                    // Importing data into SQLite
                    //mainActivity.Regist();
                    //mainActivity.mDialog.Dismiss();
                    break;
                case 2:
                    // Import completed successfully
                    //_activity.VendorCatImportComplete();
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