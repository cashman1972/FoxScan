using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FoxScan
{
    [Activity(Label = "activity_importvendorcat")]
    public class activity_importvendorcat : Activity
    {
        Database db = new Database();
        string dbError = "";
        string nextAction = "";
        string empNo = "";
        string empName = "";
        string toFrom = "";
        string storeName = "";
        string destStoreCode = "";

        string vendors = "";
        string categories = "";

        EventandlerSvcCallComplete svcCallHandler;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_importvencat);

            Button btnBack = FindViewById<Button>(Resource.Id.btnSyncBack);
            Button btnDownloadVenCat = FindViewById<Button>(Resource.Id.btnSyncDownload);
            Button btnNextAction = FindViewById<Button>(Resource.Id.btnSyncNextAction);
            ImageView imgImportComplete = FindViewById<ImageView>(Resource.Id.imgSyncComplete);

            nextAction = Intent.GetStringExtra("nextAction");
            empNo = Intent.GetStringExtra("empNo");
            empName = Intent.GetStringExtra("empName");
            toFrom = Intent.GetStringExtra("toFrom");
            storeName = Intent.GetStringExtra("storeName");
            destStoreCode = Intent.GetStringExtra("destStoreCode");

            imgImportComplete.Visibility = ViewStates.Invisible;
            btnNextAction.Visibility = ViewStates.Invisible;

            btnBack.Click += BtnBack_Click;
            btnDownloadVenCat.Click += BtnDownloadVenCat_Click;
            btnNextAction.Click += BtnNextAction_Click;
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            
            this.Finish();
        }

        private void BtnDownloadVenCat_Click(object sender, EventArgs e)
        {
            DownloadVendorCatLists();
        }

        private void BtnNextAction_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent();

            switch (nextAction)
            {
                case "SCANOPTIONS":
                    {
                        intent = new Intent(this, typeof(activity_ScanOptions));
                        break;
                    }
                case "SCANINVENTORY":
                    {
                        intent = new Intent(this, typeof(activity_Scan));
                        break;
                    }
                case "SCANBARCODE":
                    {
                        intent = new Intent(this, typeof(activity_scanbarcode));
                        break;
                    }
                case "VIEWREPORTS":
                    {
                        intent = new Intent(this, typeof(activity_reportsummary));
                        break;
                    }
            }

            intent.PutExtra("nextAction", nextAction);
            intent.PutExtra("empNo", empNo);
            intent.PutExtra("empName", empNo);
            intent.PutExtra("toFrom", toFrom);
            intent.PutExtra("storeName", storeName);
            intent.PutExtra("destStoreCode", destStoreCode);
            StartActivity(intent);
            this.Finish();
        }

        // ==================================================================

        private void DownloadVendorCatLists()
        {
            // ==============================================================
            // Use FoxScannerSvc to get string of vendor codes + names 
            // ==============================================================

            Message msg = new Message();
            svcCallHandler = new EventandlerSvcCallComplete(this);
            msg = svcCallHandler.ObtainMessage();

            ProgressDialog progBar = new ProgressDialog(this);
            progBar.SetCancelable(false);
            progBar.SetTitle("Synchronizing...");
            progBar.SetIcon(Resource.Drawable.iconChill64);
            progBar.SetMessage("Downloading vendor list from server");
            progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                try
                {
                    FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
                    vendors = foxSql.GetVendorList();

                    RunOnUiThread(() =>
                    {
                        progBar.SetMessage("Downloading category list from server");
                    });

                    categories = foxSql.GetCategoryList();

                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();
                    });

                    msg.Arg1 = 0;
                    msg.Arg2 = 1;
                    svcCallHandler.SendMessage(msg);
                }
                catch (Exception ex)
                {

                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();
                    });

                    msg.Arg1 = 1;
                    msg.Arg2 = 1;
                    svcCallHandler.SendMessage(msg);
                }
            }));

            thread.Start();
        } // ImportVendorAndCategories()

        public void ImportVendorCatListsToDB()
        {

            Message msg = new Message();
            svcCallHandler = new EventandlerSvcCallComplete(this);
            msg = svcCallHandler.ObtainMessage();

            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetTitle("Synchronizing...");
            progBar.SetIcon(Resource.Drawable.iconChill64);
            progBar.SetMessage("Importing vendor records...");
            progBar.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progBar.Progress = 0;
            progBar.Max = vendors.Split('|').GetUpperBound(0);
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                // ============================================
                // IMPORT VENDORS
                // ============================================

                string[] vendorData = vendors.Split('|');

                db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxVendor", ref dbError);

                int i = 0;

                while (i <= vendorData.GetUpperBound(0))
                {
                    string qryInsert = "";
                    string[] vendorDetail = new string[2];
                    int recCT = 0;
                    bool chunkOK = true;

                    while (chunkOK)
                    {
                        if (vendorData[i].Trim() != "")
                        {
                            vendorDetail = vendorData[i].Split(',');
                            qryInsert += "insert into FoxVendor (VendorCode, VendorName) values ('" + vendorDetail[0] + "','" + vendorDetail[1] + "'); ";
                        }
                        i++;
                        recCT++;
                        if ((recCT >= 25) || (i > vendorData.GetUpperBound(0)))
                        {
                            chunkOK = false;
                        }
                    }

                    if (qryInsert != "")
                    {
                        if (!db.ExecWriteSQLiteBatch(Constants.DBFilename, qryInsert, ref dbError))
                        {
                                //Toast.MakeText((this.ApplicationContext), "Success!", ToastLength.Long).Show();
                                //txtdbResult.Text += "Failed insert code: " + vendorDetail[0] + "  ";
                        }
                    }

                    RunOnUiThread(() =>
                    {
                        progBar.Progress = i;
                    });

                }  // while (i <= vendorData.GetUpperBound(0))

                // =======================================
                // Import Categories
                // =======================================

                RunOnUiThread(() =>
                {
                    progBar.SetTitle("Synchronizing...");
                    progBar.SetIcon(Resource.Drawable.iconChill64);
                    progBar.SetMessage("Importing category records...");
                    progBar.SetProgressStyle(ProgressDialogStyle.Horizontal);
                    progBar.Progress = 0;
                    progBar.Max = categories.Split('|').GetUpperBound(0);
                });

                string[] categoryData = categories.Split('|');

                db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxCategory", ref dbError);

                i = 0;

                while (i <= categoryData.GetUpperBound(0))
                {
                    string qryInsert = "";
                    string[] categoryDetail = new string[2];
                    int recCT = 0;
                    bool chunkOK = true;

                    while (chunkOK)
                    {
                        if (categoryData[i].Trim() != "")
                        {
                            categoryDetail = categoryData[i].Split(',');
                            qryInsert += "insert into FoxCategory (Category, CategoryName) values ('" + categoryDetail[0] + "','" + categoryDetail[1] + "'); ";
                        }
                        i++;
                        recCT++;
                        if ((recCT >= 26) || (i > categoryData.GetUpperBound(0)))
                        {
                            chunkOK = false;
                        }
                    }

                    if (qryInsert != "")
                    {
                        if (!db.ExecWriteSQLiteBatch(Constants.DBFilename, qryInsert, ref dbError))
                        {
                            //Toast.MakeText((this.ApplicationContext), "Success!", ToastLength.Long).Show();
                            //txtdbResult.Text += "Failed insert code: " + vendorDetail[0] + "  ";
                        }
                    }

                    RunOnUiThread(() =>
                    {
                        progBar.Progress = i;
                    });

                }  // while (i <= vendorData.GetUpperBound(0))

                RunOnUiThread(() =>
                {
                    progBar.Dismiss();
                });

                db.ExecWriteSQLite(Constants.DBFilename, "update FoxAdminRecord set LastVendorCatImport = '" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "'", ref dbError);

                msg.Arg1 = 0;
                msg.Arg2 = 2;
                svcCallHandler.SendMessage(msg);
            }));

            thread.Start();

            //while (thread.ThreadState == ThreadState.Running)
            //{
            //    await Task.Delay(5000);
            //}

            //progBar.Dismiss();
        } // ImportVendorCatListsToDB()


        public void ImportComplete()
        {
            Button btnNextAction = FindViewById<Button>(Resource.Id.btnSyncNextAction);
            Button btnDownloadVenCat = FindViewById<Button>(Resource.Id.btnSyncDownload);
            ImageView imgImportComplete = FindViewById<ImageView>(Resource.Id.imgSyncComplete);

            btnDownloadVenCat.Enabled = false;
            btnNextAction.Text = "CLOSE";

            if (nextAction == "SCANINVENTORY")
            {
                btnNextAction.Text = "PROCEED TO SCANNING";
            }

            if (nextAction == "VIEWREPORTS")
            {
                btnNextAction.Text = "PROCEED TO VIEW DATA";
            }

            imgImportComplete.Visibility = ViewStates.Visible;
            btnNextAction.Visibility = ViewStates.Visible;
        }

    } 

    //////////////////////////////////////////////////////////////////
    // Registration Event Handler Class
    //////////////////////////////////////////////////////////////////

    class EventandlerSvcCallComplete : Handler
    {
        private activity_importvendorcat activity;

        public EventandlerSvcCallComplete(activity_importvendorcat activity)
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
                    if (msg.Arg2 == 1)
                    {
                        activity.ImportVendorCatListsToDB();
                    }
                    if (msg.Arg2 == 2)
                    {
                        activity.ImportComplete();
                    }
                    break;
                case 1:
                    // Error during service call
                    //activity.RegistrationExists();
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