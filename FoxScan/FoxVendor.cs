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

using SQLite;

namespace FoxScan
{
    [Table("FoxVendor")]
    public class FoxVendor
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string VendorCode { get; set; }
        public string VendorName { get; set; }
    }
}