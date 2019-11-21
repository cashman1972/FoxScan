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
    public class ViewHolderXFerLogDet : Java.Lang.Object
    {
        public TextView txtBatchNo { get; set; }
        public TextView txtUnits { get; set; }
        public TextView txtInvType { get; set; }
        public TextView txtEmpNo { get; set; }
        public TextView txtTransferDate { get; set; }
    }

    public class listviewadapter_xferlog : BaseAdapter
    {
        private Activity activity;
        private List<XFerLog> listXFerLog;

        public listviewadapter_xferlog(Activity activity, List<XFerLog> listXFerLog)
        {
            this.activity = activity;
            this.listXFerLog = listXFerLog;
        }

        public override int Count
        {
            get { return listXFerLog.Count; }
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
            var view = convertView ?? activity.LayoutInflater.Inflate(Resource.Layout.listview_xferlog, parent, false);
            var txtBatchNo = view.FindViewById<TextView>(Resource.Id.txtxferLogDetBatchNo);
            var txtUnits = view.FindViewById<TextView>(Resource.Id.txtxferLogDetUnits);
            var txtInvType = view.FindViewById<TextView>(Resource.Id.txtxferLogDetInvType);
            var txtEmpNo = view.FindViewById<TextView>(Resource.Id.txtxferLogDetEmployee);
            var txtTransferDate = view.FindViewById<TextView>(Resource.Id.txtxferLogDetxferTime);

            txtBatchNo.Text = listXFerLog[position].BatchNo;
            txtUnits.Text = listXFerLog[position].Units.ToString();
            txtInvType.Text = listXFerLog[position].InvType;
            txtEmpNo.Text = listXFerLog[position].EmpNo;

            string dateTemp = Convert.ToDateTime(listXFerLog[position].TransferDate).ToShortDateString() + " " + Convert.ToDateTime(listXFerLog[position].TransferDate).ToShortTimeString().Replace(" ","");

            txtTransferDate.Text = dateTemp;

            return view;
        }
    }

}