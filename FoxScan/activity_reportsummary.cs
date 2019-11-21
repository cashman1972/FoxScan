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
    [Activity(Label = "activity_reportsummary")]
    public class activity_reportsummary : Activity
    {

        private string viewMode = "VENDOR";
        private DateTime vcLastUpdate = DateTime.Now.AddDays(-7);
        private Database db = new Database();
        string dbError = "";
        ListView lstViewReport;
        List<ReportRecord> listReportData = new List<ReportRecord>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_ReportSummary);

            db.ExecWriteSQLiteBatch(Constants.DBFilename, "delete from FoxProduct WHERE rowid NOT IN (SELECT min(rowid) FROM FoxProduct GROUP BY EPC);", ref dbError);

            var optVendor = FindViewById<RadioButton>(Resource.Id.rdoReportVendor);
            var optCategory = FindViewById<RadioButton>(Resource.Id.rdoReportCategory);
            Button btnClose = FindViewById<Button>(Resource.Id.btnReportClose);
            Button btnForceVendorSync = FindViewById<Button>(Resource.Id.btnReportVenCatSync);
            Button btnViewxFerHist = FindViewById<Button>(Resource.Id.btnReportXFerHist);
            Button btnDeleteData = FindViewById<Button>(Resource.Id.btnReportDeleteData);

            lstViewReport = FindViewById<ListView>(Resource.Id.listviewrptdata);

            optVendor.Click += OptVendor_Click;
            optCategory.Click += OptCategory_Click;
            btnClose.Click += BtnClose_Click;
            btnForceVendorSync.Click += BtnForceVendorSync_Click;
            btnViewxFerHist.Click += BtnViewxFerHist_Click;
            btnDeleteData.Click += BtnDeleteData_Click;

            DisplayReport();
        }

        private void BtnViewxFerHist_Click(object sender, EventArgs e)
        {
            StartActivity(typeof(activity_xferlog));
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void BtnForceVendorSync_Click(object sender, EventArgs e)
        {
            var intent = new Intent(this, typeof(activity_importvendorcat));
            intent.PutExtra("nextAction", "MANUALUPDATE");
            StartActivity(intent);
        }

        private void BtnDeleteData_Click(object sender, EventArgs e)
        {
            var builder = new Android.App.AlertDialog.Builder(this);
            builder.SetTitle("Delete Scanned Inventory");
            builder.SetIcon(Resource.Drawable.iconWarning64);
            builder.SetMessage("This will delete all scanned inventory that is currently on this scanner (not transferred). This cannot be undone. Are you sure you want to delete?");
            builder.SetPositiveButton("Yes", (s, e2) =>
            {
                db.ExecWriteSQLite(Constants.DBFilename, "delete from FoxProduct", ref dbError);
                DisplayReport();
            }
            );
            builder.SetNegativeButton("No", (s, e2) =>
            {
                // Do nothing
            }
            );
            builder.Create().Show();
        }

        private void OptVendor_Click(object sender, EventArgs e)
        {
            viewMode = "VENDOR";
            DisplayReport();
        }

        private void OptCategory_Click(object sender, EventArgs e)
        {
            viewMode = "CATEGORY";
            DisplayReport();
        }

        private void DisplayReport()
        {
            string sql = "";
            TextView txtTotalUnits = FindViewById<TextView>(Resource.Id.txtReportTotal);

            // *** NOTE: We use count(qty) instead of sum(qty) in the queries below because CURRENT ON HAND quantities write as 0

            if (viewMode == "VENDOR")
            {
                sql = @"select prod.vendorcode as Code, v.VendorName as Description, count(prod.qty) as Quantity from FoxProduct prod 
                left join FoxVendor v
                on prod.vendorcode = v.vendorcode
                group by prod.vendorcode, v.VendorName order by v.VendorName";
            }

            if (viewMode == "CATEGORY")
            {
                sql = @"select prod.Category as Code, c.CategoryName as Description, count(prod.qty) as Quantity from FoxProduct prod 
                left join FoxCategory c
                on prod.Category = c.Category
                group by prod.Category, c.CategoryName order by c.CategoryName";
            }

            string totUnits = db.ExecQuery_Scalar(Constants.DBFilename, "select count(Qty) as NumUnits from FoxProduct", ref dbError);
            if (totUnits != null)
            {
                txtTotalUnits.Text = totUnits;
            }
            else
            {
                txtTotalUnits.Text = "0";
            }

            listReportData = db.ExecQuery_ReportRecord(Constants.DBFilename, sql, ref dbError);
            if (dbError == "")
            {
                listviewadapter_ReportSummary adapter = new listviewadapter_ReportSummary(this, listReportData);
                lstViewReport.Adapter = adapter;
            }
            else
            {
                Toast.MakeText((this.ApplicationContext), "Error: " + dbError, ToastLength.Long).Show();
            }
        }
    }
}