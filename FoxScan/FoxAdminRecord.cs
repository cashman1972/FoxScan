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
    [Table("FoxAdminRecord")]
    public class FoxAdminRecord
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ScannerID { get; set; } = 0;  //default value for unregistered canner
        public string StoreCode { get; set; }
        public string AdminPW { get; set; }
        public string LastVendorCatImport { get; set; } // *DON'T use DATETIME w/ sqllite!!
    }
}