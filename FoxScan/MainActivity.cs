using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Util;
using Com.Zebra.Rfid.Api3;
using Java.Util;
using System.Threading;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Distribute;

using SQLite;
using static FoxScan.MainActivity;

namespace FoxScan
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        Database db = new Database();
        private static Readers readers;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static RFIDReader Reader;
        private bool databaseOK = false;
        private string serialNoTC20 = Android.OS.Build.Serial;
        private string serialRFD2000 = "";
        private bool wifiConnected = false;
        bool rfidScannerConnected = false;
        int scannerID = 0;
        string dbError = "";
        //private EventHandler eventHandler;
        EventhandlerGetRegistration regHandler;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource

            //SupportActionBar.Hide();  // If you want to hide app bar
            SetContentView(Resource.Layout.layout_main);

            Distribute.SetEnabledForDebuggableBuild(true);
            AppCenter.Start("7bba33fe-411e-42d9-ba2c-e5cad04df867", typeof(Distribute));

            ImageButton btnScan = FindViewById<ImageButton>(Resource.Id.btnMainScan);
            ImageButton btnReports = FindViewById<ImageButton>(Resource.Id.btnMainReports);
            ImageButton btnFindSKU = FindViewById<ImageButton>(Resource.Id.btnMainFindSKU);

            ImageButton btnLastMarkdown = FindViewById<ImageButton>(Resource.Id.btnMainLastMD);
            ImageButton btnSettings = FindViewById<ImageButton>(Resource.Id.btnMainSettings);
            ImageButton btnWHS = FindViewById<ImageButton>(Resource.Id.btnMainWHS);

            TextView txtAppVersion = FindViewById<TextView>(Resource.Id.txtAppVersion);
            TextView txtDateTime = FindViewById<TextView>(Resource.Id.txtMainDateTime);
            ImageView imgWifiWarning = FindViewById<ImageView>(Resource.Id.imgMainWifiWarning);
            TextView txtWifiWarning = FindViewById<TextView>(Resource.Id.txtMainWifiWarning);

            databaseOK = CreateDBTables();

            txtAppVersion.Text = "(app version: 1." + Constants.CurrentVersion + ")";
            txtDateTime.Text = DateTime.Now.ToLongDateString() + "  " + DateTime.Now.ToShortTimeString();

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 3000;
            timer.Elapsed += Timer_Elapsed;
            timer.Enabled = true;
            timer.Start();

            OpenRFIDConnection();

            btnScan.Click += BtnScan_Click;
            btnReports.Click += BtnReports_Click;
            btnFindSKU.Click += BtnFindSKU_Click;
            btnLastMarkdown.Click += BtnLastMarkdown_Click;
            btnSettings.Click += BtnSettings_Click;
            btnWHS.Click += BtnWHS_Click;

            UpdateWifiStatus();

            if ((databaseOK) && (wifiConnected))
            {
                DisplayScannerRegistration();

                ThreadPool.QueueUserWorkItem(state =>
                {
                    ImportStoreInfo();
                });

                // =======================================================
                // *If an update wipes out Registration record, try to
                //  look it up (by TC20 serial) on S-WHS and do a silent re-reg
                // =======================================================

                if (scannerID == 0)
                {
                    ImportRegistrationRecordFromWHS();
                }
            }
        } // OnCreate()

        private void BtnWHS_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(activity_whsmainmenu));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TextView txtDateTime = FindViewById<TextView>(Resource.Id.txtMainDateTime);
            RunOnUiThread(() =>
            {
                txtDateTime.Text = DateTime.Now.ToLongDateString() + "  " + DateTime.Now.ToShortTimeString();
            }
            );
        }

        protected override void OnResume()
        {
            base.OnResume();

            UpdateWifiStatus();
            DisplayScannerRegistration();
            OpenRFIDConnection();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            try
            {
                if (Reader != null)
                {
                    //Reader.Events.RemoveEventsListener(eventHandler);
                    Reader.Disconnect();
                    //Toast.MakeText(ApplicationContext, "Disconnecting reader", ToastLength.Long).Show();
                    Reader = null;
                    readers.Dispose();
                    readers = null;
                }
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
            }
            catch (Exception e)
            {
                e.StackTrace.ToString();
            }
        } // OnDestroy()

        // **************************************************************************
        // Main Menu ImageView Click Events
        // **************************************************************************

        // == SCAN ==
        private void BtnScan_Click(object sender, EventArgs e)
        {
            if (databaseOK)
            {
                if (rfidScannerConnected)
                {
                    wifiProceed("SCANINVENTORY");
                }
                else
                {
                    StartActivity(typeof(activity_ReaderBang));
                }
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetMessage("Cannot proceed to scanning as a database error has occurred. Please exit app and try again.");
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */

                }
                );
                builder.Create().Show();
            }
        }  // btnScan_Click()

        // == FIND SKU ==

        private void BtnFindSKU_Click(object sender, EventArgs e)
        {
            if (rfidScannerConnected)
            {
                CloseRFIDConnection();
                StartActivity(typeof(activity_findsku));
            }
            else
            {
                StartActivity(typeof(activity_ReaderBang));
            }
        }

        // == LAST MARKDOWN ==

        private void BtnLastMarkdown_Click(object sender, EventArgs e)
        {

            bool debugMode = true;

            if (debugMode)
            {
                //StartActivity(typeof(activity_dbTest));
                StartActivity(typeof(activity_scanbarcode));
            }
            else
            {
                if (!wifiConnected)
                {
                    // Can't proceed without WiFi

                    var builder = new Android.App.AlertDialog.Builder(this);
                    builder.SetTitle("Wifi NOT connected!");
                    builder.SetIcon(Resource.Drawable.iconWarning64);
                    builder.SetMessage("WiFi is not currently connected. A WiFi connection is required in order to lookup markdowns.");
                    builder.SetPositiveButton("Ok", (s, e2) =>
                    {
                    // Do nothing
                    }
                    );
                    builder.Create().Show();
                }
                else
                {
                    // Launch Markdowns activity
                }
            }
        }

        // == VIEW SCAN DATA ==

        private void BtnReports_Click(object sender, EventArgs e)
        {
            if (databaseOK)
            {
                CloseRFIDConnection();
                if (mcTools.VendorSyncOK())
                {
                    StartActivity(typeof(activity_reportsummary));
                }
                else
                {
                    var intent = new Intent(this, typeof(activity_importvendorcat));
                    intent.PutExtra("nextAction", "VIEWREPORTS");
                    StartActivity(intent);
                }
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetMessage("Cannot proceed to reports as a database error has occurred. Please exit app and try again.");
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */

                }
                );
                builder.Create().Show();
            }
        }

        // == SETTINGS ==

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            var intentSettings = new Intent(this, typeof(activity_scannersetup));
            intentSettings.PutExtra("serialRFD2000", serialRFD2000);

            StartActivity(intentSettings);
        }

        // ********************************************************************
        // RFID Connection Methods
        // ********************************************************************

        private void OpenRFIDConnection()
        {
            // Do this in MainActivity to test connection so we can notify user if they need to wake slep up

            if (readers == null)
            {
                readers = new Readers(this, ENUM_TRANSPORT.ServiceSerial);
            }
            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (readers != null && readers.AvailableRFIDReaderList != null)
                    {
                        availableRFIDReaderList = readers.AvailableRFIDReaderList;
                        if (availableRFIDReaderList.Count > 0)
                        {
                            if (Reader == null)
                            {
                                // get first reader from list
                                readerDevice = availableRFIDReaderList[0];
                                Reader = readerDevice.RFIDReader;
                                // Establish connection to the RFID Reader
                                Reader.Connect();
                                if (Reader.IsConnected)
                                {
                                    //Console.Out.WriteLine("Readers connected");
                                    serialRFD2000 = Reader.ReaderCapabilities.SerialNumber;
                                    rfidScannerConnected = true;
                                }
                            }
                            else
                            {
                                rfidScannerConnected = true;
                            }
                        }
                        else
                        {
                            rfidScannerConnected = false;
                        }
                    }
                    else
                    {
                        rfidScannerConnected = false;
                    }
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch
                (OperationFailureException e)
                {
                    e.PrintStackTrace();
                    //Log.Debug(TAG, "OperationFailureException " + e.VendorMessage);
                }
            });
        }

        private void CloseRFIDConnection()
        {
            try
            {
                if (Reader != null)
                {
                    //Reader.Events.RemoveEventsListener(eventHandler);
                    Reader.Disconnect();
                    //Toast.MakeText(ApplicationContext, "Disconnecting reader", ToastLength.Long).Show();
                    Reader = null;
                    readers.Dispose();
                    readers = null;
                }
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
            }
            catch (Exception e)
            {
                e.StackTrace.ToString();
            }
        }

        public void DisplayScannerRegistration()
        {
            TextView txtStoreName = FindViewById<TextView>(Resource.Id.txtMainStoreName);
            TextView txtScannerID = FindViewById<TextView>(Resource.Id.txtScannerID);

            scannerID = mcTools.GetScannerID();

            if (scannerID > 0)
            {
                txtScannerID.SetTextColor(Android.Graphics.Color.ParseColor("#FFFFFFFF"));
                txtScannerID.Text = "Scanner #" + scannerID.ToString();
            }
            else
            {
                txtScannerID.SetTextColor(Android.Graphics.Color.ParseColor("#F75555"));
                txtScannerID.Text = "*SCANNER NOT REGISTERED*";
            }

            string storeCode = mcTools.GetScannerStoreCode();
            if (storeCode != "")
            {
                string storeName = mcTools.GetStoreNameFromStoreCode(storeCode);
                txtStoreName.SetTextColor(Android.Graphics.Color.ParseColor("#04faee"));
                if (storeName != "")
                {
                    txtStoreName.Text = storeName;
                }
                else
                {
                    txtStoreName.Text = storeCode;
                }
            }
            else
            {
                txtStoreName.SetTextColor(Android.Graphics.Color.ParseColor("#F75555"));
                txtStoreName.Text = "*Store not set!";
            }
        }

        private void UpdateWifiStatus()
        {
            ImageView imgWifiWarning = FindViewById<ImageView>(Resource.Id.imgMainWifiWarning);
            TextView txtWifiWarning = FindViewById<TextView>(Resource.Id.txtMainWifiWarning);

            NetworkStatusMonitor nm = new NetworkStatusMonitor();
            nm.UpdateNetworkStatus();

            if (nm.State == NetworkState.ConnectedWifi)
            {
                wifiConnected = true;
                imgWifiWarning.Visibility = ViewStates.Invisible;
                txtWifiWarning.Visibility = ViewStates.Invisible;
            }
            else
            {
                wifiConnected = false;
                imgWifiWarning.Visibility = ViewStates.Visible;
                txtWifiWarning.Visibility = ViewStates.Visible;
            }
        }

        private void ImportStoreInfo()
        {
            FoxScannerSvc.FoxScannerSvc foxScannerSvc = new FoxScannerSvc.FoxScannerSvc();
            string storeData = foxScannerSvc.GetStoreInfoList();

            if (storeData != null)
            {
                if (storeData != "")
                {
                    if (db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxStoreInfo", ref dbError))
                    {
                        string[] storeRecs = storeData.Split('|');
                        string sql = "";

                        for (int storeCT = 0; storeCT <= storeRecs.GetUpperBound(0); storeCT++)
                        {
                            if (storeRecs[storeCT] != "")
                            {
                                string[] storeRecord = storeRecs[storeCT].Split(',');

                                sql += "insert into FoxStoreInfo (StoreName, StoreCode, StoreServerIP) values ('" + storeRecord[0] + "','" + storeRecord[1] + "','" + storeRecord[2] + "'); ";
                            }
                        }

                        db.ExecWriteSQLiteBatch(Constants.DBFilename, sql, ref dbError);
                    }
                }
            }
        }

        private bool CreateDBTables()
        {
            string errorList = "";

            if (db.TableExists(Constants.DBFilename, "FoxProduct"))
            {
                // This syncs table schema to <FoxProduct> class (in case I modify class)
                SQLiteAsyncConnection conn = new SQLiteAsyncConnection(System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), Constants.DBFilename));

                conn.CreateTableAsync<FoxProduct>();
            }
            else
            {
                if (!db.createTable_FoxProduct(Constants.DBFilename, ref dbError))
                {
                    if (dbError != "")
                    {
                        errorList += "<FoxProduct> : " + dbError;
                    }
                }
            }

            if (!db.TableExists(Constants.DBFilename, "FoxVendor"))
            {
                if (!db.createTable_Vendors(Constants.DBFilename, ref dbError))
                {
                    if (dbError != "")
                    {
                        errorList += "<FoxVendor> : " + dbError;
                    }
                }
            }

            if (!db.TableExists(Constants.DBFilename, "FoxCategory"))
            {
                if (!db.createTable_Categories(Constants.DBFilename, ref dbError))
                {
                    if (dbError != "")
                    {
                        errorList += "<FoxCategory> : " + dbError;
                    }
                }
            }

            if (!db.TableExists(Constants.DBFilename, "FoxAdminRecord"))
            {
                if (!db.createTable_FoxAdminRecord(Constants.DBFilename, ref dbError))
                {
                    if (dbError != "")
                    {
                        errorList += "<FoxAdminRecord> : " + dbError;
                    }
                }
            }

            if (!db.TableExists(Constants.DBFilename, "FoxStoreInfo"))
            {
                if (!db.createTable_FoxStoreInfo(Constants.DBFilename, ref dbError))
                {
                    if (dbError != "")
                    {
                        errorList += "<FoxStoreInfo> : " + dbError;
                    }
                }
            }

            if (!db.TableExists(Constants.DBFilename, "XFerLog"))
            {
                if (!db.createTable_XFerLog(Constants.DBFilename, ref dbError))
                {
                    if (dbError != "")
                    {
                        errorList += "<XFerLog> : " + dbError;
                    }
                }
            }

            if (errorList == "")
            {
                return true;
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                dbError = "";
                builder.SetMessage("A database error has occurred (exit app and try again) : " + errorList);
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */

                }
                );
                builder.Create().Show();
                return false;
            }

        } // CreateDBTables()

        private void CallNextAction(string nextAction)
        {
            switch (nextAction)
            {
                case "SCANINVENTORY":
                {
                    CloseRFIDConnection();
                    if (mcTools.GetScannerID() > 0)
                    {
                            //if (mcTools.VendorSyncOK())
                            //{
                            //    StartActivity(typeof(activity_getEmployee));
                            //}
                            //else
                            //{
                            //    var intent = new Intent(this, typeof(activity_importvendorcat));
                            //    intent.PutExtra("nextAction", "SCANINVENTORY");
                            //    StartActivity(intent);
                            //}

                            Intent intent = new Intent();
                            intent = new Intent(this, typeof(activity_getEmployee));
                            intent.PutExtra("nextAction", "SCANOPTIONS");
                            StartActivity(intent);

                        }
                    else
                    {
                        var builder = new Android.App.AlertDialog.Builder(this);
                        builder.SetTitle("Scanner not registered");
                        builder.SetIcon(Resource.Drawable.iconWarning64);
                        builder.SetMessage("Scanner is not registered. Register scanner before continuing");
                        builder.SetPositiveButton("Ok", (s, e2) =>
                        {
                            
                        }
                        );
                    }
                    break;
                }
            }
        }

        private void wifiProceed(string nextAction)
        {
            if (wifiConnected)
            {
                CallNextAction(nextAction);
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetTitle("Wifi NOT connected!");
                builder.SetIcon(Resource.Drawable.iconWarning64);
                builder.SetMessage("WiFi is not currently connected. You will not be able to transfer data from scanner to computer until connection is re-established. It is recommended that you exit this app and connect WiFi before proceeding.");
                builder.SetPositiveButton("Exit app", (s, e2) =>
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                }
                );
                builder.SetNegativeButton("Proceed without WiFi", (s, e2) =>
                {
                    //CallNextAction(nextAction);  // We don't want to call CallNextAction(nextAction) when wifi is down because it may try to launch VendorSync

                    if (nextAction == "SCANINVENTORY")
                    {
                        Intent intent = new Intent();
                        intent = new Intent(this, typeof(activity_getEmployee));
                        intent.PutExtra("nextAction", "SCANOPTIONS");
                        StartActivity(intent);
                    }
                }
                );
                builder.Create().Show();
            }
        }

        private void ImportRegistrationRecordFromWHS()
        {
            // * If FoxAdminRecord got wiped out somehow (most likely due to an update), try to restore the data from the S-WHS server
            if (serialNoTC20 != "") 
            {
                Message msg = new Message();
                regHandler = new EventhandlerGetRegistration(this);
                msg = regHandler.ObtainMessage();

                // Display progress bar

                ProgressDialog progBar = new ProgressDialog(this);

                progBar.SetCancelable(false);
                progBar.SetMessage("Retrieving scanner registration...");
                progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
                progBar.Show();

                var thread = new System.Threading.Thread(new ThreadStart(delegate
                {
                    FoxScannerSvc.FoxScannerSvc foxScannerSvc = new FoxScannerSvc.FoxScannerSvc();
                    string getregistrationResult = foxScannerSvc.GetScannerRegistration(serialNoTC20);  // {ScannerID}, {StoreCode}

                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();
                    });

                    if (getregistrationResult != "")
                    {
                        
                        string[] foxRegData = getregistrationResult.Split(',');
                        scannerID = Convert.ToInt32(foxRegData[0]);
                        string storeCode = foxRegData[1];

                        // Re-Write <FoxAdmin> record
                        db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxAdminRecord", ref dbError);
                        if (db.ExecWriteSQLite(Constants.DBFilename, @"insert into FoxAdminRecord (ScannerID, StoreCode, AdminPW, LastVendorCatImport) 
                        values (" + scannerID.ToString() + ",'" + storeCode + "','Fox$RFID','1/1/2000')", ref dbError))
                        {
                            msg.Arg1 = 0;
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

                    regHandler.SendMessage(msg);
                }));

                thread.Start();
            } // if (serialTC20 != "")
        } // Import()


        //////////////////////////////////////////////////////////////////
        // GetRegistration Event Handler Class
        //////////////////////////////////////////////////////////////////

        class EventhandlerGetRegistration : Handler
        {
            private MainActivity activity;

            public EventhandlerGetRegistration(MainActivity activity)
            {
                this.activity = activity;
            }

            public override void HandleMessage(Message msg)
            {
                //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
                switch (msg.Arg1)
                {
                    case 0:
                        // Get Registration Attempt Complete
                        activity.DisplayScannerRegistration();
                        break;
                    case 1:
                        // Get Reg Failed - Do nothing
                        break;
                    default:
                        break;
                }
                base.HandleMessage(msg);
            }
        }  // EventandlerRegistration

    }
}