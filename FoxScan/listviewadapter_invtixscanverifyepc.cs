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

    public class ViewHolderInvTixScanVerifyEPC : Java.Lang.Object
    {
        public TextView txtFoxSKU { get; set; }
        public TextView txtEPCLast4 { get; set; }
    }

    public class listviewadapter_invtixscanverifyepc : BaseAdapter
    {
        private Activity activity;
        private List<FoxProduct> listFoxProduct;

        public listviewadapter_invtixscanverifyepc(Activity activity, List<FoxProduct> listFoxProduct)
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
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.listview_invtixscanverifyepc, parent, false);
            var txtFoxSKU = view.FindViewById<TextView>(Resource.Id.txtEPCVerifyDetFoxSKU);
            var txtEPCLast4 = view.FindViewById<TextView>(Resource.Id.txtEPCVerifyDetEPCLast4);

            txtFoxSKU.Text = listFoxProduct[position].FoxSKU;
            txtEPCLast4.Text = listFoxProduct[position].EPC.Substring(listFoxProduct[position].EPC.Length - 4, 4);

            return view;
        }
    }
}