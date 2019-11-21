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

    public class ViewHolderScanSummary : Java.Lang.Object
    {
        public TextView txtVendorName { get; set; }
        public TextView txtFoxSKU { get; set; }
        public TextView txtQty { get; set; }
    }

    public class listviewadapter_scansummary : BaseAdapter
    {
        private Activity activity;
        private List<FoxProduct> listFoxProduct;

        public listviewadapter_scansummary(Activity activity, List<FoxProduct> listFoxProduct)
        {
            this.activity = activity;
            this.listFoxProduct = listFoxProduct;
        }

        public override int Count
        {
            get { return listFoxProduct.Count; }
        }
        public override Java.Lang.Object GetItem(int position)
        {
            return null;
        }
        public override long GetItemId(int position)
        {
            return listFoxProduct[position].Id;
        }
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.listview_scansummary, parent, false);
            var txtVendorName = view.FindViewById<TextView>(Resource.Id.txtSumVendorName);
            var txtFoxSKU = view.FindViewById<TextView>(Resource.Id.txtSumFoxSKU);
            var txtQty = view.FindViewById<TextView>(Resource.Id.txtSumQty);

            txtVendorName.Text = listFoxProduct[position].VendorName;
            txtFoxSKU.Text = listFoxProduct[position].FoxSKU;
            txtQty.Text = listFoxProduct[position].Qty.ToString();

            return view;
        }
    }
}