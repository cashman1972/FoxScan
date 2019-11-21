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
    [Table("XFerLog")]
    public class XFerLog
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string BatchNo { get; set; } 
        public int Units { get; set; }
        public string InvType { get; set; }
        public string EmpNo { get; set; } 
        public string TransferDate { get; set; } // *DON'T use DATETIME w/ sqllite!!
    }
}