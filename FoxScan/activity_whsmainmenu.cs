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
    [Activity(Label = "activity_whsmainmenu")]
    public class activity_whsmainmenu : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_whsmainmenu);

            Button btnVerifyInvTix = FindViewById<Button>(Resource.Id.btnWHSRFIDVerify);
            Button btnViewSKUDetail = FindViewById<Button>(Resource.Id.btnWHSScanViewDetail);
            Button btnExit = FindViewById<Button>(Resource.Id.btnWHSExit);


            btnVerifyInvTix.Click += BtnVerifyInvTix_Click;
            btnViewSKUDetail.Click += BtnViewSKUDetail_Click;
            btnViewSKUDetail.Enabled = false;

            btnExit.Click += BtnExit_Click;
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void BtnViewSKUDetail_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(activity_whsscanskudetail));
        }

        private void BtnVerifyInvTix_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(activity_invtixscanverifyepc));
            intent.PutExtra("empNextAction", "VERIFY");
            intent.PutExtra("toFrom", "VERIFY");  // = VERIFY
            intent.PutExtra("storeName", "VERIFY");
            StartActivity(intent);
        }
    }
}