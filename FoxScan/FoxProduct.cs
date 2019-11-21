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
    [Table("FoxProduct")]
    public class FoxProduct
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string EPC { get; set; }
        public string FoxSKU { get; set; }
        public string VendorSKU { get; set; }
        public string VendorCode { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
        public int epcLast6 { get; set; }
        public int Qty { get; set; }    
        public string EmployeeID { get; set; }
        public string DateTimeScanned { get; set; }
        public string ActionWOtherLoc { get; set; }
        public string NewConsol { get; set; }
        public string OtherStoreCode { get; set; }
    }
}