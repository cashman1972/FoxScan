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
    [Activity(Label = "activity_ReaderBang")]
    public class activity_ReaderBang : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_readerbang);

            Button btnOKReaderBang = FindViewById<Button>(Resource.Id.btnOKReaderBang);

            btnOKReaderBang.Click += delegate
            {
                this.Finish();
                Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
            };
        }
    }
}