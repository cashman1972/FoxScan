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
    public class VendorCount
    {
        public string VendorCode { get; set; }
        public int Quantity { get; set; }
    }
}