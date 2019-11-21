using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FoxScan
{
    public static class mcTools
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static DateTime GetMondayDate(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Monday: return date;
                case DayOfWeek.Tuesday: return date.AddDays(-1);
                case DayOfWeek.Wednesday: return date.AddDays(-2);
                case DayOfWeek.Thursday: return date.AddDays(-3);
                case DayOfWeek.Friday: return date.AddDays(-4);
                case DayOfWeek.Saturday: return date.AddDays(-5);
                case DayOfWeek.Sunday: return date.AddDays(-6);
                default: return date;
            }
        }

        public static bool IsInteger(string num)
        {
            int test;

            if (num.Trim() != "")
            {
                return int.TryParse(num, out test);
            }
            else
            {
                return false;
            }
        }

        public static bool IsDecimal(string num)
        {
            decimal test;

            if (num.Trim() != "")
            {
                return decimal.TryParse(num, out test);
            }
            else
            {
                return false;
            }
        }

        public static bool IsNumeric(string num)
        {
            return ((IsInteger(num)) || (IsDecimal(num)));
        }

        public static string StripNum(string num)
        {
            string multiplier = "";

            num = num.Trim().Replace(" ", "");

            if ((num.IndexOf("-") >= 0) || (num.IndexOf("(") >= 0) || (num.IndexOf(")") >= 0))
            {
                multiplier = "-";
            }


            num = num.Replace("-", "");
            num = num.Replace("!", "");
            num = num.Replace("@", "");
            num = num.Replace("#", "");
            num = num.Replace("$", "");
            num = num.Replace("%", "");
            num = num.Replace("^", "");
            num = num.Replace("&", "");
            num = num.Replace("*", "");
            num = num.Replace("(", "");
            num = num.Replace(")", "");
            num = num.Replace(",", "");
            num = num.Replace(":", "");
            num = num.Replace("`", "");
            num = num.Replace("'", "");
            num = num.Replace("*", "");
            num = num.Replace("+", "");
            num = num.Replace("=", "");
            num = num.Replace(",", "");
            num = num.Replace("/", "");
            num = num.Replace("?", "");
            num = num.Replace("[", "");
            num = num.Replace("]", "");
            num = num.Replace("{", "");
            num = num.Replace("}", "");
            num = num.Replace("_", "");
            num = num.Replace(":", "");
            num = num.Replace(";", "");

            if (num == "") { return "0"; }
            return multiplier + num;
        } // StripNum

        public static string CaseProper(string str)
        {
            if (str.Trim() == "")
            {
                return "";
            }
            else
            {
                return str.Substring(0, 1).ToUpper() + str.Substring(1, str.Length - 1).ToLower();
            }
        }

        public static string MonthNameFromDate(DateTime date)
        {
            switch (date.Month)
            {
                case 1:
                    return "January";
                case 2:
                    return "February";
                case 3:
                    return "March";
                case 4:
                    return "April";
                case 5:
                    return "May";
                case 6:
                    return "June";
                case 7:
                    return "July";
                case 8:
                    return "August";
                case 9:
                    return "September";
                case 10:
                    return "October";
                case 11:
                    return "November";
                case 12:
                    return "December";
                default:
                    return "";
            }
        }

        public static string DayNameFromDate(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday:
                    return "Sunday";
                case DayOfWeek.Monday:
                    return "Monday";
                case DayOfWeek.Tuesday:
                    return "Tuesday";
                case DayOfWeek.Wednesday:
                    return "Wednesday";
                case DayOfWeek.Thursday:
                    return "Thursday";
                case DayOfWeek.Friday:
                    return "Friday";
                case DayOfWeek.Saturday:
                    return "Saturday";
                default:
                    return "";
            }
        }




        public static int MonthValueFromAbbrev(string abbrev)
        {
            switch (abbrev.ToUpper())
            {
                case "JAN":
                    return 1;
                case "FEB":
                    return 2;
                case "MAR":
                    return 3;
                case "APR":
                    return 4;
                case "MAY":
                    return 5;
                case "JUN":
                    return 6;
                case "JUL":
                    return 7;
                case "AUG":
                    return 8;
                case "SEP":
                    return 9;
                case "OCT":
                    return 10;
                case "NOV":
                    return 11;
                case "DEC":
                    return 12;
                default:
                    return 0;
            }
        }

        public static string ZeroFill(int num, int numChar)
        {
            string strNum = num.ToString();
            int StartLen = strNum.Length;

            if (StartLen < numChar)
            {
                for (int ct = 1; ct <= (numChar - StartLen); ct++)
                {
                    strNum = "0" + strNum;
                }
            }

            return strNum;
        }

        public static int GetScannerID()
        {
            Database db = new Database();
            string dbError = "";

            List<FoxAdminRecord> lstAdmin = db.GetAdminRecord("FoxScan.db", ref dbError);

            if (lstAdmin != null)
            {
                if (lstAdmin.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return lstAdmin[0].ScannerID;
                }
            }
            else
            {
                return 0;
            }
        }

        public static string GetScannerStoreCode()
        {
            Database db = new Database();
            string dbError = "";

            List<FoxAdminRecord> lstAdmin = db.GetAdminRecord("FoxScan.db", ref dbError);

            if (lstAdmin != null)
            {
                if (lstAdmin.Count == 0)
                {
                    return "";
                }
                else
                {
                    return lstAdmin[0].StoreCode;
                }
            }
            else
            {
                return "";
            }
        }

        public static string GetStoreCodeFromStoreName(string storeName)
        {
            Database db = new Database();
            string dbError = "";

            return db.ExecQuery_Scalar("FoxScan.db", "select StoreCode from FoxStoreInfo where StoreName = '" + storeName + "'", ref dbError);
        }

        public static string GetStoreNameFromStoreCode(string storeCode)
        {
            Database db = new Database();
            string dbError = "";

           
            return db.ExecQuery_Scalar("FoxScan.db", "select StoreName from FoxStoreInfo where StoreCode = '" + storeCode + "'", ref dbError);
        }

        public static string GetStoreCodeAssigned()
        {
            Database db = new Database();
            string dbError = "";

            List<FoxAdminRecord> lstAdmin = db.GetAdminRecord("FoxScan.db", ref dbError);

            if (lstAdmin != null)
            {
                if (lstAdmin.Count == 0)
                {
                    return "";
                }
                else
                {
                    return lstAdmin[0].StoreCode;
                }
            }
            else
            {
                return "";
            }
        }

        public static string GetAsciiFromHex(String hexString, bool substituteHexZero)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    String hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    if ((hs != "00") || ((hs == "00") && (!substituteHexZero)))
                    {
                        uint decval = System.Convert.ToUInt32(hs, 16);
                        char character = System.Convert.ToChar(decval);
                        ascii += character;
                    }
                    else
                    {
                        ascii += " ";
                    }

                }

                return ascii;
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        public static string GetHexFromAscii(string str)
        {
            return string.Join("", str.Select(c => ((int)c).ToString("X2")));
        }

        public static string GetFoxSKUFromEPC(string epc)
        {

            ////////////////////////////////////////////////////////////////////////////////////////////////////
            // Returns the Fox SKU portion (Vendorcode + 5dig style + size) from a tag's FULL (128 bit) EPC
            ////////////////////////////////////////////////////////////////////////////////////////////////////

            if (epc.Length != 32)
            {
                return "";
            }
            else
            {
                string epcPert = epc.Substring(4, 21); // The pertanent part of the epc starts at the 5th char (all FOX epcs start w/ F0C5)
                string vendorCode = GetAsciiFromHex(epcPert.Substring(0, 6), true);
                string style5 = epcPert.Substring(6, 5);

                // * Note [SIZE] -> Since 0 is a valid product size, we will represent a value of NO SIZE w/ "FFFFFFFFFF"

                string size = "";

                if (epcPert.Substring(11, 10) != "FFFFFFFFFF")
                {
                    size = GetAsciiFromHex(epcPert.Substring(11, 10), true);
                }

                return vendorCode + style5 + size.Trim();
            }
        }

        public static string ConvertFoxSKUToEPCSegment(string foxSKU)
        {
            string hexSize = "FFFFFFFFFF";  // default value for "no size"
            string hex = "";

            foxSKU = foxSKU.ToUpper();
            hex = GetHexFromAscii(foxSKU.Substring(0, 3)) + foxSKU.Substring(3, 5);

            if (foxSKU.Length > 8)
            {
                string size = foxSKU.Substring(8, foxSKU.Length - 8);
                hexSize = GetHexFromAscii(size);

                if (hexSize.Length < 10)
                {
                    for (int i = size.Length; hexSize.Length < 10; i++)
                    {
                        hexSize += "0";
                    }
                }
            }

            return hex + hexSize;
        }

        public static DateTime GetLastVendorCatSync()
        {
            Database db = new Database();
            string dbError = "";
            string lastTransfer = db.ExecQuery_Scalar("FoxScan.db", "select LastVendorCatImport from FoxAdminRecord", ref dbError);

            if (lastTransfer != null)
            {
                if (lastTransfer != "")
                {
                    return Convert.ToDateTime(lastTransfer);
                }
                else
                {
                    return Convert.ToDateTime("1/1/2000");
                }
            }
            else
            {
                return Convert.ToDateTime("1/1/2000");
            }
        }

        public static bool VendorSyncOK()
        {
            Database db = new Database();
            string dbError = "";
            List<FoxVendor> lstVendor = new List<FoxVendor>();
            List<FoxCategory> lstCategory = new List<FoxCategory>();

            // 1st check - are vendor + cat tables populated or empty?

            lstVendor = db.ExecQuery_FoxVendor("FoxScan.db", "select * from FoxVendor Limit 100", ref dbError);
            lstCategory = db.ExecQuery_FoxCategory("FoxScan.db", "select * from FoxCategory", ref dbError);

            if ((lstVendor.Count == 0) || (lstCategory.Count == 0))
            {
                return false;
            }
            else
            {
                // If populated, check AdminRecord for last transfer. If date is not today, sync is required

                List<FoxAdminRecord> lstAdminRec = db.GetAdminRecord("FoxScan.db", ref dbError);

                if (lstAdminRec.Count == 0)
                {
                    return false;
                }
                else
                {
                    if (lstAdminRec[0].LastVendorCatImport == null)
                    {
                        return false;
                    }
                    else
                    {
                        return (Convert.ToDateTime(lstAdminRec[0].LastVendorCatImport) >= Convert.ToDateTime(DateTime.Today.ToShortDateString() + " 12:00 AM"));
                    }
                }
            }
        }


        public static string FormatDateMySQL(string dateString)
        {
            DateTime date = Convert.ToDateTime(dateString);
            return date.Year.ToString() + "-" + ZeroFill(date.Month, 2) + "-" + ZeroFill(date.Day, 2);
        }

        public static string FormatDateMySQL(DateTime date)
        {
            return date.Year.ToString() + "-" + ZeroFill(date.Month, 2) + "-" + ZeroFill(date.Day, 2);
        }

        public static string FormatDateMySQL_wTime(DateTime date)
        {
            return date.Year.ToString() + "-" + ZeroFill(date.Month, 2) + "-" + ZeroFill(date.Day, 2) + " " + ZeroFill(date.Hour, 2) + ":" + ZeroFill(date.Minute, 2) + ":" + ZeroFill(date.Second, 2);
        }


    }
}