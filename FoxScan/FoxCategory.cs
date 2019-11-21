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
    [Table("FoxCategory")]
    public class FoxCategory
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Category { get; set; }
        public string CategoryName { get; set; }
    }
}