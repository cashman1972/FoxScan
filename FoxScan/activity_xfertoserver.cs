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
    [Activity(Label = "activity_xfertoserver")]
    public class activity_xfertoserver : Activity
    {
        Database db = new Database();
        string dbError = "";
        xFerEventHandler xFerHandler;
        string resultxFer = "";
        string nextAction = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_xfertoserver);

            nextAction = Intent.GetStringExtra("nextAction");

            ImageView imgTransferComplete = FindViewById<ImageView>(Resource.Id.imgxFerComplete);
            Button btnBack = FindViewById<Button>(Resource.Id.btnxFerBack);
            Button btnTransfer = FindViewById<Button>(Resource.Id.btnxFerData);

            imgTransferComplete.Visibility = ViewStates.Invisible;

            btnBack.Click += BtnBack_Click;
            btnTransfer.Click += BtnTransfer_Click;
            btnTransfer.Enabled = true;
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void BtnTransfer_Click(object sender, EventArgs e)
        {
            Button btnTransfer = FindViewById<Button>(Resource.Id.btnxFerData);
            btnTransfer.Enabled = false;

            // Check WiFi status first

            NetworkStatusMonitor nm = new NetworkStatusMonitor();
            nm.UpdateNetworkStatus();

            if (nm.State == NetworkState.ConnectedWifi)
            {
                // Transfer Data

                Message msg = new Message();
                xFerHandler = new xFerEventHandler(this);
                msg = xFerHandler.ObtainMessage();

                // Display progress bar

                ProgressDialog progBar = new ProgressDialog(this);

                progBar.SetCancelable(false);
                progBar.SetTitle("Transferring Data");
                progBar.SetIcon(Resource.Drawable.iconChill64);
                progBar.SetMessage("One moment please...");
                progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
                progBar.Show();

                var thread = new System.Threading.Thread(new ThreadStart(delegate
                {
                    int scannerID = mcTools.GetScannerID();

                    // Create data list

                    List<FoxProduct> foxProducts = db.ExecQuery_FoxProduct(Constants.DBFilename, "select * from FoxProduct order by FoxSKU, epcLast6", ref dbError);

                    if (foxProducts == null)
                    {
                        resultxFer = "Failed to create foxProducts export list. Failed to read table.";
                        msg.Arg1 = 1;
                    }
                    else
                    {
                        if (foxProducts.Count > 0)
                        {
                            // Create data string to send

                            RunOnUiThread(() =>
                            {
                                progBar.SetTitle("Transferring Data...");
                                progBar.SetIcon(Resource.Drawable.iconChill64);
                                progBar.SetProgressStyle(ProgressDialogStyle.Horizontal);
                                progBar.Progress = 0;
                                progBar.Max = foxProducts.Count;
                                progBar.SetMessage("Exporting inventory records...");
                            });

                            string scannerData = "";

                            for (int recordCT = 0; recordCT < foxProducts.Count; recordCT++)
                            {
                                // Data to be transferred in format {EmpNo},{EPC},{Qty},{ActionWOtherLoc},{NewConsol},{OtherStoreCode},{DateTimeScanned}|
                                scannerData += foxProducts[recordCT].EmployeeID + ",";
                                scannerData += foxProducts[recordCT].EPC + ",";
                                scannerData += foxProducts[recordCT].Qty.ToString() + ",";
                                scannerData += foxProducts[recordCT].ActionWOtherLoc + ",";
                                scannerData += foxProducts[recordCT].NewConsol + ",";
                                scannerData += foxProducts[recordCT].OtherStoreCode + ",";
                                scannerData += foxProducts[recordCT].DateTimeScanned;

                                if (recordCT < (foxProducts.Count - 1))
                                {
                                    scannerData += "|";
                                }

                                RunOnUiThread(() =>
                                {
                                    progBar.Progress = recordCT;
                                });
                            }

                            RunOnUiThread(() =>
                            {
                                progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
                                progBar.SetMessage("Just a few more seconds...");
                            });

                            FoxScannerSvc.FoxScannerSvc foxScannerSvc = new FoxScannerSvc.FoxScannerSvc();
                            resultxFer = foxScannerSvc.UploadRFIDInvScans(scannerID.ToString(), scannerData, foxProducts.Count);

                            if (resultxFer != "")
                            {
                                string[] resultValues = resultxFer.Split('|');

                                if (resultValues[0] == "[SUCCESS]")
                                {
                                    string batchNo = resultValues[1];
                                    int unitsTransferred = foxProducts.Count;

                                    int unitsIN = Convert.ToInt32(db.ExecQuery_Scalar(Constants.DBFilename, "select count(Qty) from FoxProduct where Qty > 0", ref dbError));
                                    int unitsOUT = Convert.ToInt32(db.ExecQuery_Scalar(Constants.DBFilename, "select count(Qty) from FoxProduct where Qty < 0", ref dbError));
                                    int unitsOH = Convert.ToInt32(db.ExecQuery_Scalar(Constants.DBFilename, "select count(Qty) from FoxProduct where Qty = 0", ref dbError));

                                    // ====================================
                                    // Write batch Info to <xferLog>
                                    // ====================================

                                    string sqlBatch = "";

                                    if (unitsIN > 0)
                                    {
                                        string empNo = db.ExecQuery_Scalar(Constants.DBFilename, "select EmployeeID from FoxProduct where Qty > 0 limit 1", ref dbError);

                                        sqlBatch += @"insert into XFerLog (BatchNo, Units, InvType, EmpNo, TransferDate) values
                                        ('" + batchNo + "'," + unitsIN.ToString() + ",'IN','" + empNo + "','" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "');";
                                    }

                                    if (unitsOUT > 0)
                                    {
                                        string empNo = db.ExecQuery_Scalar(Constants.DBFilename, "select EmployeeID from FoxProduct where Qty < 0 limit 1", ref dbError);

                                        sqlBatch += @"insert into XFerLog (BatchNo, Units, InvType, EmpNo, TransferDate) values
                                        ('" + batchNo + "'," + unitsOUT.ToString() + ",'OUT','" + empNo + "','" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "');";
                                    }

                                    if (unitsOH > 0)
                                    {
                                        string empNo = db.ExecQuery_Scalar(Constants.DBFilename, "select EmployeeID from FoxProduct where Qty = 0 limit 1", ref dbError);

                                        sqlBatch += @"insert into XFerLog (BatchNo, Units, InvType, EmpNo, TransferDate) values
                                        ('" + batchNo + "'," + unitsOH.ToString() + ",'OH','" + empNo + "','" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + "');";
                                    }

                                    db.ExecWriteSQLiteBatch(Constants.DBFilename, sqlBatch, ref dbError);

                                    // =======================================
                                    // Wipeout <FoxProduct>
                                    // =======================================

                                    if (db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxProduct", ref dbError))
                                    {
                                        msg.Arg1 = 0;
                                    }
                                    else
                                    {
                                        // Notify user that data xferred, but needs to be deleted from scanner
                                    }
                                }
                                else
                                {
                                    msg.Arg1 = 1;
                                }
                            }
                            else
                            {
                                msg.Arg1 = 1;
                            }
                        }
                        else
                        {
                            msg.Arg1 = 0;
                        }
                    }

                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();
                    });

                    xFerHandler.SendMessage(msg);
                }));

                thread.Start();
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetTitle("WiFi NOT Connected!!");
                builder.SetIcon(Resource.Drawable.iconWarning64);
                builder.SetMessage("WiFi is not connected. Please connect WiFi and try again.");
                builder.SetPositiveButton("OK", (s, e2) =>
                {
                    // Goto transfer data w/ nextActivity = "SCANOPTIONS"

                }
                );
                builder.Create().Show();
            }
        } // Transfer_Click()

        public void DisplayXFerSuccess()
        {
            ImageView imgTransferComplete = FindViewById<ImageView>(Resource.Id.imgxFerComplete);  
            imgTransferComplete.Visibility = ViewStates.Visible;
        }

        public void DisplayXFerFailure()
        {
            Button btnTransfer = FindViewById<Button>(Resource.Id.btnxFerData);

            var builder = new Android.App.AlertDialog.Builder(this);
            builder.SetTitle("Transfer FAILED!");
            builder.SetIcon(Resource.Drawable.iconWarning24);
            builder.SetMessage("Data transfer failed: " + resultxFer);
            builder.SetPositiveButton("OK", (s, e2) =>
            {

            }
            );
            builder.Create().Show();

            btnTransfer.Enabled = true;
        }

    }

    //////////////////////////////////////////////////////////////////
    // Connection Event Handler Class
    //////////////////////////////////////////////////////////////////

    class xFerEventHandler : Handler
    {
        private activity_xfertoserver activity;

        public xFerEventHandler(activity_xfertoserver activity)
        {
            this.activity = activity;
        }

        public override void HandleMessage(Message msg)
        {
            //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
            switch (msg.Arg1)
            {
                case 0:
                    // Transfer SUCCEEDED
                    activity.DisplayXFerSuccess();
                    break;
                case 1:
                    // Transfer FAILED
                    activity.DisplayXFerFailure();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }  // Handler
}