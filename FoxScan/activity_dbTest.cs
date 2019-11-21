using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
//using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FoxScan
{
    [Activity(Label = "activity_dbTest")]
    public class activity_dbTest : Activity
    {

        //MediaPlayer _player;
        Database db = new Database();
        string dbError = "";
        string vendors = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_dbTest);

            Button btnCreateDB = FindViewById<Button>(Resource.Id.btnCreateDB);
            Button btnCreateTableVendors = FindViewById<Button>(Resource.Id.btnCreateTableVendors);
            Button btnCreateTableCats = FindViewById<Button>(Resource.Id.btnCreateTableCats);
            Button btnInsertRecord = FindViewById<Button>(Resource.Id.btnInsertTestRecord);
            Button btnGetLiveVendorCat = FindViewById<Button>(Resource.Id.btnGetLiveVendAndCat);
            Button btnQuery = FindViewById<Button>(Resource.Id.btnTestQuery);
            Button btnDropTable = FindViewById<Button>(Resource.Id.btnDropTable);
            Button btnBeep = FindViewById<Button>(Resource.Id.btnPlayBeep);
            Button btnClose = FindViewById<Button>(Resource.Id.btndbClose);
            TextView txtdbResult = FindViewById<TextView>(Resource.Id.txtdbResult);

            //_player = MediaPlayer.Create(this, Resource.Raw.beep);

            btnCreateTableVendors.Click += delegate
            {
                dbError = "";
                if (db.createTable_Vendors(Constants.DBFilename, ref dbError))
                {
                    Toast.MakeText((this.ApplicationContext), "Success!", ToastLength.Long).Show();
                    if (dbError == "")
                    {
                        txtdbResult.Text = "Table: FoxVendor created successfully.";
                    }
                    else
                    {
                        txtdbResult.Text = dbError;
                    }
                }
                else
                {
                    txtdbResult.Text = "Failed: " + dbError;
                }
            };

            btnCreateTableCats.Click += delegate
            {
                dbError = "";
                if (db.createTable_Categories(Constants.DBFilename, ref dbError))
                {
                    Toast.MakeText((this.ApplicationContext), "Success!", ToastLength.Long).Show();
                    txtdbResult.Text = "Table: FoxCategory created successfully.";
                }
                else
                {
                    txtdbResult.Text = "Failed: " + dbError;
                }
            };

            btnInsertRecord.Click += delegate
            {
                dbError = "";
                if (db.ExecWriteSQLite(Constants.DBFilename, "insert into FoxVendor (VendorCode, VendorName) values ('XXX','Test Vendor')", ref dbError))
                {
                    Toast.MakeText((this.ApplicationContext), "Success!", ToastLength.Long).Show();
                    txtdbResult.Text = "Record inserted successfully.";
                }
                else
                {
                    txtdbResult.Text = "Failed: " + dbError;
                }
            };

            btnGetLiveVendorCat.Click += BtnGetLiveVendorCat_Click;
            btnQuery.Click += BtnQuery_Click;
            btnClose.Click += BtnClose_Click;
            btnDropTable.Click += BtnDropTable_Click;
            btnBeep.Click += BtnBeep_Click;
        }

        private void BtnBeep_Click(object sender, EventArgs e)
        {
            //_player.Start();
        }

        private void BtnDropTable_Click(object sender, EventArgs e)
        {
            db.ExecWriteSQLite(Constants.DBFilename, "drop table FoxAdminRecord", ref dbError);

            var builder = new Android.App.AlertDialog.Builder(this);
            builder.SetMessage("Table dropped.");
            builder.SetPositiveButton("Ok", (s, e2) =>
            {
                // Do nothing
            }
            );
            builder.Create().Show();
        }

        private void BtnQuery_Click(object sender, EventArgs e)
        {
            TextView txtdbResult = FindViewById<TextView>(Resource.Id.txtdbResult);
            List<FoxVendor> vendors = new List<FoxVendor>();
            List<FoxProduct> foxProduct = new List<FoxProduct>();
            List<ReportRecord> rptRecord = new List<ReportRecord>();
            string dbError = "";
            string sql = @"select prod.vendorcode as Code, v.VendorName as Description, count(prod.vendorcode) as Quantity from FoxProduct prod 
                left join FoxVendor v
                on prod.vendorcode = v.vendorcode
                group by prod.vendorcode, v.VendorName order by v.VendorName";

            vendors = db.ExecQuery_FoxVendor(Constants.DBFilename, "select * from FoxVendor order by VendorCode desc", ref dbError);
            foxProduct = db.ExecQuery_FoxProduct(Constants.DBFilename, "select * from FoxProduct", ref dbError);
            rptRecord = db.ExecQuery_ReportRecord(Constants.DBFilename, sql, ref dbError);
            //vendors = db.ExecQuery_FoxVendor(Constants.DBFilename, "select * from FoxVendor order by VendorCode desc LIMIT 1", ref dbError);
            //txtdbResult.Text = "Import complete! - Last vendor record = (" + vendors[0].VendorCode + ", " + vendors[0].VendorName + ")";
            int i = 0;
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void BtnGetLiveVendorCat_Click(object sender, EventArgs e)
        {
            ImportVendorsAndCategories2();
        }



        //private async void ImportVendorsAndCategories()
        //{
        //    Message msg = new Message();
        //    msg = handler.ObtainMessage();

        //    //msg.Arg1 = 0;
        //    //handler.SendMessage(msg);
        //    foxWebSvc.FoxSvcSql foxSql = new foxWebSvc.FoxSvcSql();
        //    vendors = foxSql.GetVendorList("192.168.100.97");

        //    //txtdbResult.Text += "Import complete.";
        //    //progBar.Hide();
        //    //return true;
        //    msg.Arg1 = 2;
        //    //msg.Arg2 = vendors;
        //    handler.SendMessage(msg);
        //}

        private async void ImportVendorsAndCategories2()
        {
            TextView txtdbResult = FindViewById<TextView>(Resource.Id.txtdbResult);
            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetMessage("Getting data from server(7)...");
            progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
                vendors = foxSql.GetVendorList();
            }));

            thread.Start();

            while (thread.ThreadState == ThreadState.Running)
            {
                await Task.Delay(4000);
            }

            RunOnUiThread(() =>
            {
                txtdbResult.Text = "One moment please...";
            });

            progBar.Dismiss();
            VendorCatImportComplete();
        }

        private async void VendorCatImportComplete()
        {
            TextView txtdbResult = FindViewById<TextView>(Resource.Id.txtdbResult);
            txtdbResult.Text = "One moment please...";

            if (vendors == "")
            {
                System.Threading.Thread.Sleep(3000);
            }

            if (vendors == "")
            {
                System.Threading.Thread.Sleep(3000);
            }

            if (vendors == "")
            {
                txtdbResult.Text = "Import failed!";
            }
            else
            {
                ProgressDialog progBar = new ProgressDialog(this);

                progBar.SetCancelable(false);
                progBar.SetMessage("Importing vendor records...");
                progBar.SetProgressStyle(ProgressDialogStyle.Horizontal);
                progBar.Progress = 0;
                progBar.Max = vendors.Split('|').GetUpperBound(0);
                progBar.Show();

                var thread = new System.Threading.Thread(new ThreadStart(delegate
                {
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
                            if ((recCT >= 20) || (i > vendorData.GetUpperBound(0)))
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
                            //txtdbResult.Text = i.ToString();
                        });

                    }  // while (i <= vendorData.GetUpperBound(0))

                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();

                        List<FoxVendor> vendors = new List<FoxVendor>();
                        string dbError = "";
                        vendors = db.ExecQuery_FoxVendor(Constants.DBFilename, "select * from FoxVendor order by VendorCode desc LIMIT 1", ref dbError);

                        txtdbResult.Text = "Import complete! - Last vendor record = (" + vendors[0].VendorCode + ", " + vendors[0].VendorName + ")";

                        vendors = db.ExecQuery_FoxVendor(Constants.DBFilename, "select * from FoxVendor", ref dbError);
                        txtdbResult.Text += " # Recs: " + vendors.Count.ToString();
                    });
                }));

                thread.Start();

                while (thread.ThreadState == ThreadState.Running)
                {
                    await Task.Delay(5000);
                }

                //progBar.Dismiss();
            }
        }
    }
}