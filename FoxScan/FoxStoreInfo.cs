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
    [Table("FoxStoreInfo")]
    public class FoxStoreInfo
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string StoreName { get; set; }
        public string StoreCode { get; set; }
        public string StoreServerIP { get; set; }
    }
}