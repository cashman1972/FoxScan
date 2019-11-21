using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FoxScan
{
    [Activity(Label = "activity_invtixbatchimport")]
    public class activity_invtixbatchimport : Activity
    {
        ListView lstViewData;
        List<InvTixBatch> listBatchDetail = new List<InvTixBatch>();
        List<string> lstSelected = new List<string>();

        EventandlerBatchListComplete svcCallHandler;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_invtixbatchimport);

            Button btnCancel = FindViewById<Button>(Resource.Id.btnBatchCancel);
            Button btnImport = FindViewById<Button>(Resource.Id.btnBatchImport);

            lstViewData = FindViewById<ListView>(Resource.Id.listviewbatches);

            btnCancel.Click += BtnCancel_Click;
            btnImport.Click += BtnImport_Click;

            lstViewData.ItemClick += LstViewData_ItemClick;
            lstSelected.Clear();

            LoadRecentInvTixBatches();
        }

        private void LstViewData_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            TextView txtBatchNo = FindViewById<TextView>(Resource.Id.txtBatchNum);
            TextView txtVendorCode = FindViewById<TextView>(Resource.Id.txtBatchVCode);
            TextView txtBatchTime = FindViewById<TextView>(Resource.Id.txtBatchTime);
            TextView txtBatchQty = FindViewById<TextView>(Resource.Id.txtBatchQty);
            TextView txtBatchVerified = FindViewById<TextView>(Resource.Id.txtBatchVerified);

            int viewIndex = e.Position;

            // Get batchnum of item selected

            string batchNum = e.View.Tag.ToString();
            Android.Graphics.Color rowColor = Android.Graphics.Color.Transparent;

            // Is this batch already selected? Check List<>...

            if (lstSelected.Exists(x => x == batchNum))
            {
                // Unhighlight Row

                rowColor = Android.Graphics.Color.Transparent;

                // Remove batch # from List<>

                int listIndex = lstSelected.FindIndex(x => x == batchNum);
                lstSelected.RemoveAt(listIndex);
            }
            else
            {
                // Highlight Row

                rowColor = Android.Graphics.Color.Aqua;

                // Add item to list<>

                lstSelected.Add(batchNum);
            }

            lstViewData.GetChildAt(viewIndex).SetBackgroundColor(rowColor);
            //txtBatchNo.SetBackgroundColor(rowColor);
            //txtVendorCode.SetBackgroundColor(rowColor);
            //txtBatchTime.SetBackgroundColor(rowColor);
            //txtBatchQty.SetBackgroundColor(rowColor);
            //txtBatchVerified.SetBackgroundColor(rowColor);

        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            string batchesImported = "";

            if (lstSelected.Count > 0)
            {
                for (int i = 0; i < lstSelected.Count; i++)
                {
                    if (lstSelected[i] != "")
                    {
                        batchesImported += lstSelected[i] + ",";
                    }
                }
            }

            Intent myIntent = new Intent(this, typeof(activity_Scan));
            myIntent.PutExtra("batchesImported", batchesImported);
            SetResult(Result.Ok, myIntent);
            Finish();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void LoadRecentInvTixBatches()
        {
            // Perform service call to retrieve batch list

            Message msg = new Message();
            svcCallHandler = new EventandlerBatchListComplete(this);
            msg = svcCallHandler.ObtainMessage();

            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetTitle("Retrieving Batches...");
            progBar.SetIcon(Resource.Drawable.iconChill64);
            progBar.SetMessage("Importing batch list...");
            progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
            progBar.Progress = 0;
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                try
                {
                    FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
                    string batchData = foxSql.GetRecentInvTixBatches();

                    if (batchData == "ERROR")
                    {
                        msg.Arg1 = 1;  // error occurred
                        svcCallHandler.SendMessage(msg);
                    }
                    else
                    {
                        if (batchData != "")
                        {
                            // Load batch data into List<InvTixBatch>

                            string[] batchRecords = batchData.Split('|');
                            int brCT = 0;

                            while (brCT <= batchRecords.GetUpperBound(0))
                            {
                                if (batchRecords[brCT].Trim() != "")
                                {
                                    string[] batchDetail = batchRecords[brCT].Split(',');

                                    InvTixBatch batchRec = new InvTixBatch();

                                    batchRec.BatchNo = Convert.ToInt32(batchDetail[0]);
                                    batchRec.VendorCode = batchDetail[1];
                                    batchRec.Qty = Convert.ToInt32(batchDetail[2]);
                                    batchRec.BatchTime = Convert.ToDateTime(batchDetail[3]);

                                    listBatchDetail.Add(batchRec);
                                }
                                brCT++;
                            }
                        }

                        RunOnUiThread(() =>
                        {
                            progBar.Dismiss();
                        });

                        msg.Arg1 = 0;
                        svcCallHandler.SendMessage(msg);
                    }
                }
                catch (Exception ex)
                {
                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();
                    });

                    msg.Arg1 = 1;  // error occurred
                    svcCallHandler.SendMessage(msg);
                }

            }));

            thread.Start();
        }  // LoadRecentInvTixBatches()

        public void ListBatches()
        {
            listviewadapter_invtixbatchimport adapter = new listviewadapter_invtixbatchimport(this, listBatchDetail);
            lstViewData.Adapter = adapter;
        }

        public void DisplayBatchLoadFailure()
        {
            mcMsgBoxA.ShowMsgWOK(this, "Error", "An error occurred while attempting to retrieve batch list from server.", IconType.Critical);
        }
    }

    //////////////////////////////////////////////////////////////////
    // Registration Event Handler Class
    //////////////////////////////////////////////////////////////////

    class EventandlerBatchListComplete : Handler
    {
        private activity_invtixbatchimport activity;

        public EventandlerBatchListComplete(activity_invtixbatchimport activity)
        {
            this.activity = activity;
        }

        public override void HandleMessage(Message msg)
        {
            //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
            switch (msg.Arg1)
            {
                case 0:
                    // Service call completed successfully
                    activity.ListBatches();
                    break;
                case 1:
                    // Error during service call
                    activity.DisplayBatchLoadFailure();
                    break;
                case 2:
                    // Error occurred
                    //activity.RegistrationError();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }  // EventandlerRegistration

}