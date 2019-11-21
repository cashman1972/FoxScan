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
using Android.Views.InputMethods;

namespace FoxScan
{
    [Activity(Label = "activity_getEmployee")]
    public class activity_getEmployee : Activity
    {
        private string empNo = "";
        private string empNameFin = "";
        private string toFrom = "";
        private string storeName = "";
        private string destStoreCode = "";
        private string nextAction = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_getEmployee);

            LinearLayout lRoot = FindViewById<LinearLayout>(Resource.Id.layoutrootemp);
            lRoot.Click += delegate
            {
                DismissKeyboard();
            };

            nextAction = Intent.GetStringExtra("nextAction");
            toFrom = Intent.GetStringExtra("toFrom");
            storeName = Intent.GetStringExtra("storeName");
            destStoreCode = Intent.GetStringExtra("destStoreCode");

            var btnEmpBack = FindViewById<Button>(Resource.Id.btnEmpBack);
            var btnEmpNext = FindViewById<Button>(Resource.Id.btnEmpNext);
            var btnClear = FindViewById<Button>(Resource.Id.btnEmpClear);
            var txtEmpNo = FindViewById<TextView>(Resource.Id.txtEmpNo);
            var txtSearching = FindViewById<TextView>(Resource.Id.txtEmpInstruct);

            Button btn0 = FindViewById<Button>(Resource.Id.btnEmp0);
            Button btn1 = FindViewById<Button>(Resource.Id.btnEmp1);
            Button btn2 = FindViewById<Button>(Resource.Id.btnEmp2);
            Button btn3 = FindViewById<Button>(Resource.Id.btnEmp3);
            Button btn4 = FindViewById<Button>(Resource.Id.btnEmp4);
            Button btn5 = FindViewById<Button>(Resource.Id.btnEmp5);
            Button btn6 = FindViewById<Button>(Resource.Id.btnEmp6);
            Button btn7 = FindViewById<Button>(Resource.Id.btnEmp7);
            Button btn8 = FindViewById<Button>(Resource.Id.btnEmp8);
            Button btn9 = FindViewById<Button>(Resource.Id.btnEmp9);

            btn0.Click += delegate { empNo += "0"; txtEmpNo.Text = empNo; };
            btn1.Click += delegate { empNo += "1"; txtEmpNo.Text = empNo; };
            btn2.Click += delegate { empNo += "2"; txtEmpNo.Text = empNo; };
            btn3.Click += delegate { empNo += "3"; txtEmpNo.Text = empNo; };
            btn4.Click += delegate { empNo += "4"; txtEmpNo.Text = empNo; };
            btn5.Click += delegate { empNo += "5"; txtEmpNo.Text = empNo; };
            btn6.Click += delegate { empNo += "6"; txtEmpNo.Text = empNo; };
            btn7.Click += delegate { empNo += "7"; txtEmpNo.Text = empNo; };
            btn8.Click += delegate { empNo += "8"; txtEmpNo.Text = empNo; };
            btn9.Click += delegate { empNo += "9"; txtEmpNo.Text = empNo; };

            btnClear.Click += delegate { empNo = ""; txtEmpNo.Text = "####"; };

            btnEmpBack.Click += delegate
            {
                this.Finish();
            };

            btnEmpNext.Click += delegate
            {
                txtSearching.Text = "Looking up employee...";
                txtSearching.SetTextColor(Android.Graphics.Color.ParseColor("#FF0033BB"));

                if (empNo == "")
                {
                    Toast.MakeText((this.ApplicationContext), "Enter employee #", ToastLength.Long).Show();
                }
                else
                {
                    // =============================================================================================================================
                    // We need to check wifi. It is possible that user okay'd scanning w/o wifi. If so, capture the empno, but skip verification
                    // =============================================================================================================================

                    NetworkStatusMonitor nm = new NetworkStatusMonitor();
                    nm.UpdateNetworkStatus();

                    if (nm.State == NetworkState.ConnectedWifi)
                    {
                        txtSearching.Visibility = ViewStates.Visible;
                        ThreadPool.QueueUserWorkItem(state =>
                        {
                            string empName = GetEmployeeNameFromNum(empNo);

                            RunOnUiThread(() => VerifyEmpName(empNo, empName));
                        });
                    }
                    else
                    {
                        LaunchNextActivity();
                    }
                }
            };
        } // OnCreate()

        private void VerifyEmpName(string empNum, string empName)
        {
            if (empName != "")
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetMessage(empName + "?");
                builder.SetPositiveButton("Yes", (s, e) =>
                { /* Handle 'YES' click */
                    empNameFin = empName;
                    LaunchNextActivity();
                }
                );
                builder.SetNegativeButton("No", (s, e) =>
                { /* Handle 'NO' click */

                });
                builder.Create().Show();
            }
            else
            {
                Toast.MakeText((this.ApplicationContext), "Invalid employee # entered!", ToastLength.Long).Show();
            }
        }

        private void LaunchNextActivity()
        {
            Intent intent = new Intent();

            if (mcTools.VendorSyncOK())
            {
                if (nextAction == "SCANOPTIONS")
                {
                    intent = new Intent(this, typeof(activity_ScanOptions));
                }

                if (nextAction == "SCANRFID")
                {
                    intent = new Intent(this, typeof(activity_Scan));
                }

                if (nextAction == "SCANBARCODE")
                {
                    intent = new Intent(this, typeof(activity_scanbarcode));
                }
            }
            else
            {
                intent = new Intent(this, typeof(activity_importvendorcat));
            }

            intent.PutExtra("nextAction", nextAction);
            intent.PutExtra("empNo", empNo);
            intent.PutExtra("empName", empNameFin);
            intent.PutExtra("toFrom", toFrom);
            intent.PutExtra("storeName", storeName);
            intent.PutExtra("destStoreCode", destStoreCode);
            StartActivity(intent);
            this.Finish();
        }

        private string GetEmployeeNameFromNum(string empNum)
        {
            FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
            string empName = foxSql.GetStoreEmployeeNameFromNum(empNum);

            return empName;
        }

        private void DismissKeyboard()
        {
            var view = CurrentFocus;
            if (view != null)
            {
                var imm = (InputMethodManager)GetSystemService(InputMethodService);
                imm.HideSoftInputFromWindow(view.WindowToken, 0);
            }
        }
    }
}