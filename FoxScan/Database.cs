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
using Android.Util;

using SQLite;

namespace FoxScan
{
    public class Database
    {
        string folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        string logTag = "FoxScan";

        public bool TableExists(string dbFilename, string tableName)
        {
            bool tableExists = false;
            string error = "";

            Database db = new Database();
            string result = db.ExecQuery_Scalar(Constants.DBFilename, "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = '" + tableName + "'", ref error);

            tableExists = (result != "0");

            return tableExists;
        }



        public bool createTable_FoxProduct(string dbFilename, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    //if (connection.Table<FoxProduct>().Count() == 0)
                    //{
                    connection.BeginTransaction();
                    connection.CreateTable<FoxProduct>();
                    connection.Commit();
                    error = "";
                    return true;
                    //}
                    //else
                    //{
                    //    error = "Table already exists.";
                    //    return true;
                    //}
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool createTable_Vendors(string dbFilename, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.CreateTable<FoxVendor>();
                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool createTable_Categories(string dbFilename, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.CreateTable<FoxCategory>();
                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool createTable_FoxAdminRecord(string dbFilename, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.CreateTable<FoxAdminRecord>();
                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool createTable_FoxStoreInfo(string dbFilename, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.CreateTable<FoxStoreInfo>();
                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool createTable_XFerLog(string dbFilename, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.CreateTable<XFerLog>();
                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        //Add or Insert Operation  

        public bool ExecWriteSQLite(string dbFilename, string sqlCommand, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.Execute(sqlCommand);
                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool ExecWriteSQLiteBatch(string dbFilename, string sqlCommand, ref string error)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    string[] sql = sqlCommand.Split(';');
                    connection.BeginTransaction();

                    for (int queryCT = 0; queryCT <= sql.GetUpperBound(0); queryCT++)
                    {
                        if (sql[queryCT].Trim() != "")
                        {
                            connection.Execute(sql[queryCT]);
                        }
                    }

                    connection.Commit();
                    error = "";
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                error = ex.Message;
                return false;
            }
        }

        public bool InsertFoxProduct(string dbFilename, FoxProduct product)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.Insert(product);
                    connection.Commit();
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                return false;
            }
        }

        public string ExecQuery_Scalar(string dbFilename, string sql, ref string dbError)
        {
            // Use for queries that return a single (1 row, 1 single value) string

            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.ExecuteScalar<string>(sql);
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return "";
            }
        }

        public List<FoxProduct> ExecQuery_FoxProduct(string dbFilename, string sql, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<FoxProduct>(sql).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        public List<ReportRecord> ExecQuery_ReportRecord(string dbFilename, string sql, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<ReportRecord>(sql).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        public List<FoxVendor> ExecQuery_FoxVendor(string dbFilename, string sql, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<FoxVendor>(sql).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        public List<FoxCategory> ExecQuery_FoxCategory(string dbFilename, string sql, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<FoxCategory>(sql).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        public List<FoxStoreInfo> ExecQuery_FoxStoreInfo(string dbFilename, string sql, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<FoxStoreInfo>(sql).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        public List<XFerLog> ExecQuery_XFerLog(string dbFilename, string sql, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<XFerLog>(sql).ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        public List<FoxAdminRecord> GetAdminRecord(string dbFilename, ref string dbError)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    dbError = "";
                    return connection.Query<FoxAdminRecord>("select * from FoxAdminRecord").ToList();
                }
            }
            catch (SQLiteException ex)
            {
                dbError = ex.Message.ToString();
                Log.Info(logTag, ex.Message);
                return null;
            }
        }

        //Delete Data Operation  

        public bool removeTable(string dbFilename, FoxProduct product)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.BeginTransaction();
                    connection.Delete(product);
                    connection.Commit();
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                return false;
            }
        }
        //Select Operation  

        public bool selectRecordByID(string dbFilename, int Id)
        {
            try
            {
                using (var connection = new SQLiteConnection(System.IO.Path.Combine(folder, dbFilename)))
                {
                    connection.Query<FoxProduct>("SELECT * FROM FoxProduct Where Id=?", Id);
                    return true;
                }
            }
            catch (SQLiteException ex)
            {
                Log.Info(logTag, ex.Message);
                return false;
            }
        }
    }
}