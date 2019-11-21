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
    public class ViewHolderScanDet : Java.Lang.Object
    {
        public TextView txtCode { get; set; }
        public TextView txtDescription { get; set; }
        public TextView txtCount { get; set; }
    }

    public class listviewadapter_ReportSummary : BaseAdapter
    {
        private Activity activity;
        private List<ReportRecord> listReportRecord;

        public listviewadapter_ReportSummary(Activity activity, List<ReportRecord> listReportRecord)
        {
            this.activity = activity;
            this.listReportRecord = listReportRecord;
        }

        public override int Count
        {
            get { return listReportRecord.Count; }
        }
        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }
        public override long GetItemId(int position)
        {
            //return listReportRecord[position].Id;
            return 1;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.listview_ReportSummary, parent, false);
            var txtCode = view.FindViewById<TextView>(Resource.Id.txtRptSumCode);
            var txtDescription = view.FindViewById<TextView>(Resource.Id.txtRptSumDescription);
            var txtCount = view.FindViewById<TextView>(Resource.Id.txtRptSumCount);

            //txtFoxSKU.Text = listReportRecord[position].FoxSKU;
            //txtEPCLast4.Text = listReportRecord[position].EPC.Substring(listFoxProduct[position].EPC.Length - 4, 4);

            txtCode.Text = listReportRecord[position].Code;
            txtDescription.Text = listReportRecord[position].Description;
            txtCount.Text = listReportRecord[position].Quantity.ToString();
            txtCount.TextAlignment = TextAlignment.ViewEnd;

            return view;
        }
    }
    
}