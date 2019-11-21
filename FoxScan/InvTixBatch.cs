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

//using SQLite;

namespace FoxScan
{
    //[Table("FoxProduct")]
    public class InvTixBatch
    {
        //[PrimaryKey, AutoIncrement]
        //public int Id { get; set; }
        public Int32 BatchNo { get; set; }
        public string VendorCode { get; set; }
        public DateTime BatchTime { get; set; }
        public int Qty { get; set; }
        public string Verified { get; set; }
    }
}