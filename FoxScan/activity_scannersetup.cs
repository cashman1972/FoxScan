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
    [Activity(Label = "activity_scannersetup")]
    public class activity_scannersetup : Activity
    {
        Database db = new Database();
        //List<FoxStoreInfo> foxStoreInfo = new List<FoxStoreInfo>();
        string dbError = "";
        string serialNoTC20 = Android.OS.Build.Serial;
        string serialNoRFD2000 = "";
        bool wifiConnected = false;
        bool foxSvcConnected = false;
        int scannerID = 0;

        string storeCode = "";
        string storeName = "";

        EventandlerRegistration regHandler;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_scannersetup);

            TextView txtAppVersion = FindViewById<TextView>(Resource.Id.txtSettings_AppVersion);
            TextView txtScannerID = FindViewById<TextView>(Resource.Id.txtSettings_ScannerNum);
            TextView txtWiFi = FindViewById<TextView>(Resource.Id.txtSettings_WiFiStatus);
            TextView txtDateTime = FindViewById<TextView>(Resource.Id.txtSettings_DateTime);
            TextView txtFoxScanSvcAppVersion = FindViewById<TextView>(Resource.Id.txtSettings_ServiceVersion);
            TextView txtSerialTC20 = FindViewById<TextView>(Resource.Id.txtSettings_SerialTC20);
            TextView txtSerialRFD2000 = FindViewById<TextView>(Resource.Id.txtSettings_SerialRFD2000);
            Spinner spnStores = FindViewById <Spinner>(Resource.Id.spinnerSettingsStore);
            Button btnRegister = FindViewById<Button>(Resource.Id.btnSettings_Register);
            Button btnExitSave = FindViewById<Button>(Resource.Id.btnSettings_Exit);

            serialNoRFD2000 = Intent.GetStringExtra("serialRFD2000");

            scannerID = mcTools.GetScannerID();

            if (scannerID > 0)
            {
                txtScannerID.SetTextColor(Android.Graphics.Color.Black);
                txtScannerID.Text = scannerID.ToString();
            }
            else
            {
                txtScannerID.SetTextColor(Android.Graphics.Color.ParseColor("#F75555"));
                txtScannerID.Text = "Not Assigned";
            }

            txtSerialTC20.Text = serialNoTC20;
            if (serialNoRFD2000 == "")
            {
                txtSerialRFD2000.Text = "--------------";
            }
            else
            {
                txtSerialRFD2000.Text = serialNoRFD2000;
            }

            txtAppVersion.Text = "(app version 1." + Constants.CurrentVersion + ")";
            txtDateTime.Text = DateTime.Now.ToLongDateString() + "  " + DateTime.Now.ToShortTimeString();

            NetworkStatusMonitor nm = new NetworkStatusMonitor();
            nm.UpdateNetworkStatus();

            int position = LoadStoreList();  // Load all stores from FoxStoreInfo table into spinner
            if (position > 0)
            {
                spnStores.SetSelection(position);
            }

            if (nm.State == NetworkState.ConnectedWifi)
            {
                wifiConnected = true;
                txtWiFi.SetTextColor(Android.Graphics.Color.Green);
                txtWiFi.Text = "Connected";

                // Display FoxScannerSvc version #

                FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
                txtFoxScanSvcAppVersion.SetTextColor(Android.Graphics.Color.Black);

                try
                {
                    txtFoxScanSvcAppVersion.Text = "1." + foxSql.GetFoxScanSvcVersion();
                    foxSvcConnected = true;
                }
                catch (Exception exSvc)
                {
                    foxSvcConnected = false;
                }

            }
            else
            {
                wifiConnected = false;
                txtWiFi.SetTextColor(Android.Graphics.Color.ParseColor("#F75555"));
                txtWiFi.Text = "Not Connected!";

                txtFoxScanSvcAppVersion.SetTextColor(Android.Graphics.Color.ParseColor("#F75555"));
                txtFoxScanSvcAppVersion.Text = "???";
            }

            btnRegister.Click += BtnRegister_Click;
            btnExitSave.Click += BtnExitSave_Click;
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            Spinner spnStores = FindViewById<Spinner>(Resource.Id.spinnerSettingsStore);

            storeName = spnStores.SelectedItem.ToString();
            storeCode = mcTools.GetStoreCodeFromStoreName(storeName);

            if (wifiConnected)
            {
                if ((serialNoTC20 != "") && (serialNoRFD2000 != ""))
                {

                    Message msg = new Message();
                    regHandler = new EventandlerRegistration(this);
                    msg = regHandler.ObtainMessage();

                    // Display progress bar

                    ProgressDialog progBar = new ProgressDialog(this);

                    progBar.SetCancelable(false);
                    progBar.SetMessage("Attempting scanner registration...");
                    progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
                    progBar.Show();

                    var thread = new System.Threading.Thread(new ThreadStart(delegate
                    {
                        FoxScannerSvc.FoxScannerSvc foxScannerSvc = new FoxScannerSvc.FoxScannerSvc();
                        string registrationResult = foxScannerSvc.RegisterScanner(serialNoTC20, serialNoRFD2000, storeCode);  // {#} / EXISTS / ERROR

                    RunOnUiThread(() =>
                        {
                            progBar.Dismiss();
                        });

                        if ((registrationResult == "ERROR") || (registrationResult == ""))
                        {
                            msg.Arg1 = 2;
                        }
                        else
                        {
                            if (registrationResult == "EXISTS")
                            {
                                msg.Arg1 = 1;
                            }
                            else
                            {
                                if (mcTools.IsNumeric(registrationResult))
                                {
                                    scannerID = Convert.ToInt16(registrationResult);
                                    msg.Arg1 = 0;
                                }
                            }
                        }

                        regHandler.SendMessage(msg);
                    }));

                    thread.Start();
                }
                else
                {
                    string alertMsg = "";

                    if (serialNoTC20 == "")
                    {
                        alertMsg = "Unable to obtain TC20 serial #. Registration cannot be performed.";
                    }
                    else
                    {
                        if (serialNoRFD2000 == "")
                        {
                            alertMsg = "RFID scanner not detected. Unit may be in 'sleep mode'. Exit app, squeeze gun handle for 5 seconds to wakeup unit and try again.";
                        }
                    }

                    var builder = new AlertDialog.Builder(this);
                    dbError = "";
                    builder.SetMessage(alertMsg);
                    builder.SetTitle("Device error");
                    builder.SetIcon(Resource.Drawable.iconWarning64);
                    builder.SetPositiveButton("OK", (s, e2) =>
                    { /* Handle 'OK' click */

                    }
                    );
                    builder.Create().Show();
                }
            }
            else
            {
                // WiFi not connected

                var builder = new AlertDialog.Builder(this);
                dbError = "";
                builder.SetMessage("WiFi not connected. Cannot proceed.");
                builder.SetTitle("Check WiFi connection");
                builder.SetIcon(Resource.Drawable.iconWarning64);
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */

                }
                );
                builder.Create().Show();

            }
        }

        public void RegistrationSuccess()
        {
            bool regComplete = false;

            if (db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxAdminRecord", ref dbError))
            {
                if (db.ExecWriteSQLite(Constants.DBFilename, @"insert into FoxAdminRecord (ScannerID, StoreCode, AdminPW, LastVendorCatImport) 
                values (" + scannerID.ToString() + ",'" + storeCode + "','Fox$RFID','1/1/2000')" , ref dbError ))
                {
                    regComplete = true;
                }
            }

            if (regComplete)
            {
                TextView txtScannerID = FindViewById<TextView>(Resource.Id.txtSettings_ScannerNum);
                txtScannerID.SetTextColor(Android.Graphics.Color.Black);
                txtScannerID.Text = scannerID.ToString();

                var builder = new AlertDialog.Builder(this);
                dbError = "";
                builder.SetMessage("Scanner successfully registered. Scanner ID = " + scannerID.ToString());
                builder.SetTitle("Success");
                builder.SetIcon(Resource.Drawable.iconCheck128);
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */
                    txtScannerID.Text = scannerID.ToString();
                }
                );
                builder.Create().Show();
            }
            else
            {
                var builder = new AlertDialog.Builder(this);
                dbError = "";
                scannerID = 0;
                builder.SetMessage("Scanner registered on server, *but* failed to update registration db record on scanner.");
                builder.SetTitle("Error");
                builder.SetIcon(Resource.Drawable.iconBang64);
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */
                    
                }
                );
                builder.Create().Show();
            }
        }

        public void RegistrationExists()
        {
            var builder = new AlertDialog.Builder(this);
            dbError = "";
            builder.SetMessage("Scanner already registered. Contact system administrator if scanner should be re-registered");
            builder.SetTitle("Already registered");
            builder.SetIcon(Resource.Drawable.iconWarning64);
            builder.SetPositiveButton("OK", (s, e2) =>
            { /* Handle 'OK' click */

            }
            );
            builder.Create().Show();
        }

        public void RegistrationError()
        {
            var builder = new AlertDialog.Builder(this);
            dbError = "";
            builder.SetMessage("An error occurred while attempting to register scanner.");
            builder.SetTitle("Critical error");
            builder.SetIcon(Resource.Drawable.iconBang64);
            builder.SetPositiveButton("OK", (s, e2) =>
            { /* Handle 'OK' click */

            }
            );
            builder.Create().Show();
        }

        private void BtnExitSave_Click(object sender, EventArgs e)
        {
            // Save settings - if scanner has been registered

            if (scannerID > 0)
            {
                Spinner spnStores = FindViewById<Spinner>(Resource.Id.spinnerSettingsStore);

                storeName = spnStores.SelectedItem.ToString();
                storeCode = mcTools.GetStoreCodeFromStoreName(storeName);

                db.ExecWriteSQLite(Constants.DBFilename, "update FoxAdminRecord set StoreCode = '" + storeCode +"' where ScannerID = " + scannerID.ToString(), ref dbError);
            }

            this.Finish();
        }

        private int LoadStoreList()
        {
            Spinner spnStores = FindViewById<Spinner>(Resource.Id.spinnerSettingsStore);
            int storePositionInList = -1;
            storeName = "";

            // Load store list from db

            List<FoxStoreInfo> foxStoreInfo = db.ExecQuery_FoxStoreInfo(Constants.DBFilename, "select * from FoxStoreInfo order by StoreName", ref dbError);

            storeCode = mcTools.GetStoreCodeAssigned();
            if (storeCode != "")
            {
                storeName = mcTools.GetStoreNameFromStoreCode(storeCode);
            }

            if (foxStoreInfo != null)
            {
                if (foxStoreInfo.Count > 0)
                {
                    List<string> tempList = new List<string>();

                    for (int listCT = 0; listCT < foxStoreInfo.Count; listCT++)
                    {
                        tempList.Add(foxStoreInfo[listCT].StoreName);

                        if (foxStoreInfo[listCT].StoreName == storeName)
                        {
                            storePositionInList = listCT;
                        }
                    }

                    var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, tempList);
                    spnStores.Adapter = adapter;
                }
            }

            return storePositionInList;
        }

        //private async void ImportVendorsAndCategories2()
        //{

        //    var thread = new System.Threading.Thread(new ThreadStart(delegate
        //    {
        //        FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
        //        vendors = foxSql.GetVendorList("192.168.100.97");
        //    }));

        //    thread.Start();

        //    while (thread.ThreadState == ThreadState.Running)
        //    {
        //        await Task.Delay(4000);
        //    }

        //    RunOnUiThread(() =>
        //    {
        //        txtdbResult.Text = "One moment please...";
        //    });

        //    VendorCatImportComplete();
        //}



        //////////////////////////////////////////////////////////////////
        // Registration Event Handler Class
        //////////////////////////////////////////////////////////////////

        class EventandlerRegistration : Handler
        {
            private activity_scannersetup activity;

            public EventandlerRegistration(activity_scannersetup activity)
            {
                this.activity = activity;
            }

            public override void HandleMessage(Message msg)
            {
                //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
                switch (msg.Arg1)
                {
                    case 0:
                        // New Registration Success
                        activity.RegistrationSuccess();
                        break;
                    case 1:
                        // Scanner Already Registered
                        activity.RegistrationExists();
                        break;
                    case 2:
                        // Error occurred
                        activity.RegistrationError();
                        break;
                    default:
                        break;
                }
                base.HandleMessage(msg);
            }
        }  // EventandlerRegistration


    }
}