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
    public class ViewHolderInvTixBatchImport : Java.Lang.Object
    {
        public TextView txtBatchNo { get; set; }
        public TextView txtVCode { get; set; }
        public TextView txtBatchTime { get; set; }
        public TextView txtQty { get; set; }
        public TextView txtVerified { get; set; }
    }

    public class listviewadapter_invtixbatchimport : BaseAdapter
    {
        private Activity activity;
        private List<InvTixBatch> listInvTixBatches;

        public listviewadapter_invtixbatchimport(Activity activity, List<InvTixBatch> listInvTixBatches)
        {
            this.activity = activity;
            this.listInvTixBatches = listInvTixBatches;
        }

        public override int Count
        {
            get { return listInvTixBatches.Count; }
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
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.listview_invtixbatchimport, parent, false);
            var txtBatchNo = view.FindViewById<TextView>(Resource.Id.txtBatchNum);
            var txtVCode = view.FindViewById<TextView>(Resource.Id.txtBatchVCode);
            var txtBatchTime = view.FindViewById<TextView>(Resource.Id.txtBatchTime);
            var txtQty = view.FindViewById<TextView>(Resource.Id.txtBatchQty);
            var txtVerified = view.FindViewById<TextView>(Resource.Id.txtBatchVerified);

            view.Tag = listInvTixBatches[position].BatchNo.ToString();

            txtBatchNo.Text = listInvTixBatches[position].BatchNo.ToString();
            txtVCode.Text = listInvTixBatches[position].VendorCode;

            string dateTemp = Convert.ToDateTime(listInvTixBatches[position].BatchTime).ToShortTimeString();
            txtBatchTime.Text = dateTemp;

            txtQty.Text = listInvTixBatches[position].Qty.ToString();
            txtVerified.Text = listInvTixBatches[position].Verified;

            return view;
        }
    }

}