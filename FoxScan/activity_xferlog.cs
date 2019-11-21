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
    [Activity(Label = "activity_xferlog")]
    public class activity_xferlog : Activity
    {
        private Database db = new Database();
        string dbError = "";
        ListView lstXFerLog;
        List<XFerLog> listXFerLogData = new List<XFerLog>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_xferlog);

            lstXFerLog = FindViewById<ListView>(Resource.Id.listviewxferlog);
            Button btnClose = FindViewById<Button>(Resource.Id.btnxferLogClose);

            btnClose.Click += BtnClose_Click;

            DisplayXFerLog();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void DisplayXFerLog()
        {
            string sql = "select * from XFerLog order by Id desc limit 30";
            
            listXFerLogData = db.ExecQuery_XFerLog(Constants.DBFilename, sql, ref dbError);
            if (dbError == "")
            {
                listviewadapter_xferlog adapter = new listviewadapter_xferlog(this, listXFerLogData);
                lstXFerLog.Adapter = adapter;
            }
            else
            {
                Toast.MakeText((this.ApplicationContext), "Error: " + dbError, ToastLength.Long).Show();
            }
        }

    }
}