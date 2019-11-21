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
    public class ViewHolderScanSKUDetail : Java.Lang.Object
    {
        public TextView txtFoxStyle { get; set; }
        public TextView txtVendorSKU { get; set; }
        public TextView txtPrice { get; set; }
        // public TextView txtColor { get; set; }
        // public TextView txtDescription { get; set; }
    }

    public class listviewadapter_whsscanskudetail : BaseAdapter
    {
        private Activity activity;
        private List<FoxProduct> listFoxProduct;

        public listviewadapter_whsscanskudetail(Activity activity, List<FoxProduct> listFoxProduct)
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
            //return listReportRecord[position].Id;
            return 1;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.listview_whsscanskudetail, parent, false);
            var txtFoxStyle = view.FindViewById<TextView>(Resource.Id.txtWHSScanSkuFoxStyle);
            var txtVendorSKU = view.FindViewById<TextView>(Resource.Id.txtWHSScanSkuVendorSKU);
            var txtPrice = view.FindViewById<TextView>(Resource.Id.txtWHSScanSkuPrice);
            // var txtColor = view.FindViewById<TextView>(Resource.Id.txtWHSScanSkuColor);
            // var txtDescription = view.FindViewById<TextView>(Resource.Id.txtWHSScanSkuDescription);

            view.Tag = listFoxProduct[position].FoxSKU.ToString();

            txtFoxStyle.Text = listFoxProduct[position].FoxSKU.ToString();
            txtVendorSKU.Text = listFoxProduct[position].VendorSKU.ToString();
            txtPrice.Text = "$" + listFoxProduct[position].Price.ToString();
            // txtColor.Text = listFoxProduct[position].Color.ToString();
            // txtDescription.Text = listFoxProduct[position].Description.ToString();

            return view;
        }
    }

}