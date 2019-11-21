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
    [Activity(Label = "activity_ScanOptions")]
    public class activity_ScanOptions : Activity
    {
        Database db = new Database();
        private string dbError = "";
        private string storeName = "";
        private string toFrom = "";
        private string invType = "";
        private string destStoreCode = "";
        private string empNo = "";
        private string empName = "";
        private string empNextAction = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_ScanOptions);

            empNo = Intent.GetStringExtra("empNo");
            empName = Intent.GetStringExtra("empName");

            RadioGroup radioGroup = FindViewById<RadioGroup>(Resource.Id.radioGroup1);

            RadioButton rdoScanIN = FindViewById<RadioButton>(Resource.Id.rdoScanFrom);
            RadioButton rdoScanOUT = FindViewById<RadioButton>(Resource.Id.rdoScanTo);
            RadioButton rdoScanCurrentOnHand = FindViewById<RadioButton>(Resource.Id.rdoScanCurrentOnHand);
            RadioButton rdoScanVerifyInvTix = FindViewById<RadioButton>(Resource.Id.rdoScanVerifyBatch);

            TextView txtFromTo = FindViewById<TextView>(Resource.Id.txtFromTo);
            TextView txtEmpName = FindViewById<TextView>(Resource.Id.txtScanOptionsEmpName);

            Button btnScanRFID = FindViewById<Button>(Resource.Id.btnScanRFID);
            Button btnScanBarcode = FindViewById<Button>(Resource.Id.btnScanBarcodes);
            btnScanBarcode.Enabled = false;
            Button btnExit = FindViewById<Button>(Resource.Id.btnExit2);
            Spinner spnStore = FindViewById<Spinner>(Resource.Id.spinnerStore);

            txtEmpName.Text = "Employee scanning: " + empName;

            rdoScanIN.Click += delegate
            {
                txtFromTo.Visibility = ViewStates.Visible;
                spnStore.Visibility = ViewStates.Visible;
                txtFromTo.Text = "Store FROM: ";
                toFrom = "FROM";
                invType = "IN";
            };

            rdoScanOUT.Click += delegate
            {
                txtFromTo.Visibility = ViewStates.Visible;
                spnStore.Visibility = ViewStates.Visible;
                txtFromTo.Text = "Store TO: ";
                toFrom = "TO";
                invType = "OUT";
            };

            rdoScanCurrentOnHand.Click += delegate
            {
                txtFromTo.Visibility = ViewStates.Gone;
                spnStore.Visibility = ViewStates.Gone;
                toFrom = "ONHAND";
                invType = "ONHAND";
            };

            rdoScanVerifyInvTix.Click += delegate
            {
                txtFromTo.Visibility = ViewStates.Gone;
                spnStore.Visibility = ViewStates.Gone;
                toFrom = "VERIFY";
                invType = "VERIFY";
            };

            spnStore.ItemSelected += new EventHandler<AdapterView.ItemSelectedEventArgs>(spnStore_ItemSelected);

            // Load store list from db

            List<FoxStoreInfo> foxStoreInfo = db.ExecQuery_FoxStoreInfo(Constants.DBFilename, "select * from FoxStoreInfo order by StoreName", ref dbError);
            List<string> tempList = new List<string>();

            tempList.Add("{SELECT LOCATION}");

            for (int listCT = 0; listCT < foxStoreInfo.Count; listCT++)
            {
                tempList.Add(foxStoreInfo[listCT].StoreName);
            }

            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, tempList);
            spnStore.Adapter = adapter;

            txtFromTo.Visibility = ViewStates.Gone;
            spnStore.Visibility = ViewStates.Gone;

            btnScanRFID.Click += BtnScanRFID_Click;
            btnScanBarcode.Click += BtnScanBarcode_Click;
            btnExit.Click += BtnExit_Click;
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void BtnScanBarcode_Click(object sender, EventArgs e)
        {
            if (invType != "VERIFY")
            {
                empNextAction = "SCANBARCODE";
                StartActivity(typeof(activity_scanbarcode));
            }
            else
            {
                mcMsgBoxA.ShowMsgWOK(this, "Invalid Action", "The selected option 'Verify Ticket Batch' is available for RFID scanning only.", IconType.Exclamation);
            }
        }

        private void BtnScanRFID_Click(object sender, EventArgs e)
        {

            bool settingsOK = true;

            if (invType == "")
            {
                mcMsgBoxA.ShowMsgWOK(this, "Incomplete selection.", "Select a scanning option first.", IconType.Exclamation);
                settingsOK = false;
            }

            if (((invType == "IN") || (invType == "OUT")) && ((storeName == "") || (storeName == "{SELECT LOCATION}")))
            {
                mcMsgBoxA.ShowMsgWOK(this, "Incomplete selection.", "Select store to scan " + toFrom, IconType.Exclamation);
                settingsOK = false;
            }

            if (settingsOK)
            {
                if (CheckScannerRegistration())
                {

                    if ((invType == "IN") || (invType == "OUT"))
                    {
                        destStoreCode = mcTools.GetStoreCodeFromStoreName(storeName);
                    }
                    else
                    {
                        destStoreCode = mcTools.GetStoreCodeAssigned();  // * It's ok if scanner is incorrectly set to wrong location here because
                    }                                                    // this is only used for the tofromloc reference field in server's tsqlRFIDData. 
                                                                         // Actual storecode field is based on SERVER IDENTITY at time of xFer

                    empNextAction = "SCANRFID";

                    if (invType != "VERIFY")
                    {
                        //var intent = new Intent(this, typeof(activity_getEmployee));
                        var intent = new Intent(this, typeof(activity_Scan));
                        //intent.PutExtra("empNextAction", empNextAction);
                        intent.PutExtra("empNo", empNo);
                        intent.PutExtra("empName", empNo);
                        intent.PutExtra("toFrom", toFrom);
                        intent.PutExtra("storeName", storeName);
                        intent.PutExtra("destStoreCode", destStoreCode);
                        StartActivity(intent);
                    }
                    else
                    {

                        //StartActivity()    // ScanVerify does not require employee # entry - this is for verifying epc codes after printing tickets at WHS
                        var intent = new Intent(this, typeof(activity_invtixscanverifyepc));
                        intent.PutExtra("empNo", empNo);
                        intent.PutExtra("empName", empNo);
                        intent.PutExtra("empNextAction", empNextAction);
                        intent.PutExtra("toFrom", toFrom);  // = VERIFY
                        intent.PutExtra("storeName", storeName);
                        intent.PutExtra("destStoreCode", destStoreCode);
                        StartActivity(intent);
                    }
                   
                }
            }

        }

        private void spnStore_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            Spinner spinner = (Spinner)sender;

            storeName = spinner.GetItemAtPosition(e.Position).ToString();
            //string toast = string.Format("The planet is {0}", spinner.GetItemAtPosition(e.Position));
            //Toast.MakeText(this, toast, ToastLength.Long).Show();
        }

        private bool CheckScannerRegistration()
        {
            int scannerID = mcTools.GetScannerID();

            if (scannerID > 0)
            {
                return true;
            }
            else
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Register Scanner");
                builder.SetIcon(Resource.Drawable.iconWarning64);
                builder.SetMessage("The scanner registration record is missing. Scanner needs to be registered before you can continue.");
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */
                    
                }
                );
                builder.Create().Show();
                return false;
            }
        }
    }
}