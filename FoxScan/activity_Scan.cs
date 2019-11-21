using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
//using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using System.Timers;
using Android.Util;
using Com.Zebra.Rfid.Api3;
using Java.Util;
using Android.Views.InputMethods;
using System.Threading;
using SQLite;


namespace FoxScan
{
    [Activity(Label = "activity_Scan")]
    public class activity_Scan : Activity
    {
        private static Readers readers;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static RFIDReader Reader;
        private EventHandler eventHandler;
        private string dbError = "";
        private bool invUpdated = false;

        bool rfidScannerConnected = false;

        List<FoxProduct> listFoxProducts = new List<FoxProduct>();

        string empNo = "";
        string empName = "";
        string storeName = "";
        string toFrom = "";
        string destStoreCode = "";
        int scanQTY = -999; // = 1 for New IN, -1 for OUT, 0 for Current ON HAND
        private string vcodeFilter = "";
        private string scanType = "";
        private string newConsol = "NEW";

        private string[] arrEPC = new string[5000];
        private int arrEPCNextIndex = 0;

        List<string> listEPCsVerified = new List<string>();

        MyHandler handler;
        ConnectionHandler cHandler;
        EventHandlerEPCLoadComplete epcLoadHandler;
        EventHandlerUploadVerified epcVerifyHandler;
        Database db = new Database();
        System.Timers.Timer timer = new System.Timers.Timer();
        int timerCT = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_Scan);

            /////////////////////////////////////////////////////////////////////////
            // Chck RFID Reader Connected
            /////////////////////////////////////////////////////////////////////////

            // OpenRFIDConnection();

            /////////////////////////////////////////////////////////////////////////

            int mc = 0;
            mc++;

            empNo = Intent.GetStringExtra("empNo");
            empName = Intent.GetStringExtra("empName");
            storeName = Intent.GetStringExtra("storeName");
            toFrom = Intent.GetStringExtra("toFrom");
            destStoreCode = Intent.GetStringExtra("destStoreCode");
            
            if (toFrom == "VERIFY")
            {
                scanType = "VERIFY";
            }
            else
            {
                scanType = "INVENTORY";
            }

            if ((toFrom == "FROM") || (toFrom == "TO"))
            {
                scanQTY = 1;
            }

            if (toFrom == "ONHAND")
            {
                scanQTY = 0;
            }

            
            LinearLayout lRoot = FindViewById<LinearLayout>(Resource.Id.layoutroot);
            //LinearLayout lTitle = FindViewById<LinearLayout>(Resource.Id.layoutScanTitle);
            LinearLayout lVFilter = FindViewById<LinearLayout>(Resource.Id.layoutVendorFilter);
            //LinearLayout lLoadBatch = FindViewById<LinearLayout>(Resource.Id.layoutLoadBatch);
            LinearLayout lNewConsol = FindViewById<LinearLayout>(Resource.Id.layoutNewConsol);

            TextView txtScanTitle = FindViewById<TextView>(Resource.Id.txtScanTitle);
            TextView txtGridHeader1 = FindViewById<TextView>(Resource.Id.txtGridHdr1);
            TextView txtGridHeader2 = FindViewById<TextView>(Resource.Id.txtGridHdr2);
            //TextView txtGridHeaderQty= FindViewById<TextView>(Resource.Id.txtGridHdrQty);
            TextView txtTagCT = FindViewById<TextView>(Resource.Id.txtTagCT);
            EditText txtVCode = FindViewById<EditText>(Resource.Id.txtVCode);
            TextView txtVName = FindViewById<TextView>(Resource.Id.txtVendorName);
            CheckBox chkVendorFilter = FindViewById<CheckBox>(Resource.Id.chkVendorFilter);

            RadioButton rdoNew = FindViewById<RadioButton>(Resource.Id.rdoScanNew);
            RadioButton rdoConsol = FindViewById<RadioButton>(Resource.Id.rdoScanConsol);

            Button btnBackScan = FindViewById<Button>(Resource.Id.btnBackScan);
            Button btnViewReports = FindViewById<Button>(Resource.Id.btnReports);
            Button btnFinishScan = FindViewById<Button>(Resource.Id.btnFinishScan);  // Transfer Data

            rdoNew.Click += RdoNew_Click;
            rdoConsol.Click += RdoConsol_Click;

            if (scanType == "INVENTORY")
            {
                //lTitle.SetBackgroundColor(Android.Graphics.Color.ParseColor("#48C0FA"));
                txtTagCT.SetBackgroundColor(Android.Graphics.Color.ParseColor("#48C0FA"));
                lVFilter.Visibility = ViewStates.Visible;
                //lLoadBatch.Visibility = ViewStates.Gone;
                lNewConsol.Visibility = ViewStates.Visible;
                btnViewReports.Enabled = true;
                btnFinishScan.Enabled = true;

                rdoNew.Checked = true;

                if (toFrom != "ONHAND")
                {
                    txtScanTitle.Text = toFrom + " " + storeName;
                }
                else
                {
                    txtScanTitle.Text = "ON HAND";
                }

            }

            ////////////////////////////////////////////////////////////////////////
            // Hide soft keyboard when user clicks on layout background

            lRoot.Click += delegate
            {
                DismissKeyboard();
            };

            lRoot.RequestFocus();

            ////////////////////////////////////////////////////////////////////////

            chkVendorFilter.Checked = false;
            txtVCode.Visibility = ViewStates.Gone;
            txtVName.Visibility = ViewStates.Gone;


            //Moved to OnResume()
            //InitEPCArray();  // Initialize array that will hold EPCs (only) for the purpose of making sure duplicates
            //UpdateEPCArryFromDB(); // Add any stored <FoxProduct> records in db to EPC array and listFoxProducts

            chkVendorFilter.Click += delegate
            {
                if (chkVendorFilter.Checked)
                {
                    txtVCode.Visibility = ViewStates.Visible;
                    txtVName.Visibility = ViewStates.Visible;
                    vcodeFilter = txtVCode.Text;
                    txtVCode.RequestFocus();
                }
                else
                {
                    vcodeFilter = "";
                    txtVCode.Visibility = ViewStates.Gone;
                    txtVName.Visibility = ViewStates.Gone;
                    DismissKeyboard();
                }
            };

            dbError = "";
            if (!db.createTable_FoxProduct(Constants.DBFilename, ref dbError))
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Database Error!");
                builder.SetIcon(Resource.Drawable.iconBang64);
                builder.SetMessage("A database error has occurred. <FoxProduct> not created. Please exit app and try again.");
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */

                }
                );
                builder.Create().Show();
            }

            btnViewReports.Click += BtnViewReports_Click;
            btnFinishScan.Click += BtnFinishScan_Click;  // transfer data
            btnBackScan.Click += BtnBackScan_Click;

            //timer.Interval = 1000;
            //timer.Elapsed += Timer_Elapsed;

        }

        private void RdoConsol_Click(object sender, EventArgs e)
        {
            newConsol = "CONSOL";
        }

        private void RdoNew_Click(object sender, EventArgs e)
        {
            newConsol = "NEW";
        }

        //private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    timerCT++;

        //    RunOnUiThread(() =>
        //    {
        //        if (timerCT >= 4)
        //        {
        //            timer.Stop();
        //            timer.Enabled = false;
        //            timerCT = 0;
        //            if (listEPCsVerified.Count > 0)
        //            {
        //                UpdateVerifiedEPCs(true);
        //            }
        //        }
        //    });
        //}

        protected override void OnResume()
        {
            base.OnResume();

            //* Need to test unit on wakeup that has been left on the inventory scan screen
            //  to see if we are going to be able to determine RFID scanner is asleep. Can test this on MainActivity

            InitEPCArray();  // Initialize array that will hold EPCs (only) for the purpose of making sure duplicates
            arrEPCNextIndex = 0; // Required. This will be incremented to approp. in UpdateEPCArrayFromDB()
            UpdateEPCArryFromDB(); // Add any stored <FoxProduct> records in db to EPC array and listFoxProducts *Note: RefreshView() called from within
            OpenRFIDConnection2();
        }

        protected override void OnPause()
        {
            base.OnPause();

            //if (listFoxProducts.Count > 0)
            //{
            //    SaveItemsScanned("DONOTHING");
            //}

        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if ((listFoxProducts.Count > 0) && (scanType != "VERIFY"))
            {
                SaveItemsScanned("DONOTHING");
            }

            try
            {
                if (Reader != null)
                {
                    Reader.Events.RemoveEventsListener(eventHandler);
                    Reader.Disconnect();
                    Toast.MakeText(ApplicationContext, "Disconnecting reader", ToastLength.Long).Show();
                    Reader = null;
                    readers.Dispose();
                    readers = null;
                }
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
            }
            catch (Exception e)
            {
                e.StackTrace.ToString();
            }
        }


        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            //if (scanType == "VERIFY")
            //{
            //    if ((resultCode == Result.Ok) || (resultCode == Android.App.Result.Ok))
            //    {
            //        string batchesImported = data.GetStringExtra("batchesImported");

            //        //if (batchesImported.Substring(batchesImported.Length-1,1) == ",") { batchesImported = batchesImported.Substring(0, batchesImported.Length - 1); }

            //        int lastChar = batchesImported.Length - 1; // necessary as an apparent VSTUDIO bug causes the above commented out line w/ batchesImported.Length - 1 inside Substring to fail

            //        if (batchesImported.Substring(lastChar, 1) == ",") { batchesImported = batchesImported.Substring(0, batchesImported.Length - 1); }
            //        LoadVerifyTickets(batchesImported);
            //    }
            //}
        }

        private void BtnBackScan_Click(object sender, EventArgs e)
        {
            // Check if any items exist in db, prompt user about transferring.

            if (listFoxProducts.Count > 0)
            {

                SaveItemsScanned("DONOTHING");

                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetTitle("Don't forget to transfer data!!");
                builder.SetIcon(Resource.Drawable.iconserversync);
                builder.SetMessage("Inventory data (scanned product) exists on this scanner that has not been transferred to your server. Would you like to transfer now?");
                builder.SetPositiveButton("Yes", (s, e2) =>
                {
                // Goto transfer data w/ nextActivity = "SCANOPTIONS"
                GotoDataTransfer("SCANOPTIONS");
                }
                );
                builder.SetNegativeButton("Not now", (s, e2) =>
                {
                    this.Finish();
                }
                );
                builder.Create().Show();

            }
            else
            {
                this.Finish();
            }
        }

        private void BtnFinishScan_Click(object sender, EventArgs e)
        {

            if (listFoxProducts.Count > 0)
            {
                SaveItemsScanned("DATATRANSFER");
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetMessage("There is currently no data on this scanner to transfer. Scan your inventory first.");
                builder.SetPositiveButton("OK", (s, e2) =>
                {
                // Goto transfer data w/ nextActivity = "SCANOPTIONS"

            }
                );
                builder.Create().Show();
            }

        }

        private void BtnViewReports_Click(object sender, EventArgs e)
        {
            if (listFoxProducts.Count > 0)
            {
                if (invUpdated)
                {
                    invUpdated = false;
                    SaveItemsScanned("VIEWREPORTS");
                }
                else
                {
                    StartActivity(typeof(activity_reportsummary));
                }
            }
            else
            {
                StartActivity(typeof(activity_reportsummary));
            }
        }

        private void DismissKeyboard()
        {
            var view = CurrentFocus;
            if (view != null)
            {
                var imm = (InputMethodManager)GetSystemService(InputMethodService);
                imm.HideSoftInputFromWindow(view.WindowToken, 0);
            }
        }

        public void LaunchReaderBang()
        {
            StartActivity(typeof(activity_ReaderBang));
            this.Finish();
        }

        private void OpenRFIDConnection2()
        {

            Message msg = new Message();
            cHandler = new ConnectionHandler(this);
            msg = cHandler.ObtainMessage();

            // Display progress bar

            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetTitle("Connecting to RFID reader");
            progBar.SetIcon(Resource.Drawable.iconChill64);
            progBar.SetMessage("One moment please...");
            progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
            progBar.Show();

            if (readers == null)
            {
                readers = new Readers(this, ENUM_TRANSPORT.ServiceSerial);
            }

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                try
                {
                    if (readers != null && readers.AvailableRFIDReaderList != null)
                    {
                        availableRFIDReaderList = readers.AvailableRFIDReaderList;
                        if (availableRFIDReaderList.Count > 0)
                        {
                            if (Reader == null)
                            {
                                // get first reader from list
                                readerDevice = availableRFIDReaderList[0];
                                Reader = readerDevice.RFIDReader;
                                // Establish connection to the RFID Reader
                                Reader.Connect();
                                if (Reader.IsConnected)
                                {
                                    ConfigureReader();
                                    //Console.Out.WriteLine("Readers connected");
                                    //serialRFD2000 = Reader.ReaderCapabilities.SerialNumber;
                                    rfidScannerConnected = true;
                                    msg.Arg1 = 0;
                                }
                            }
                            else
                            {
                                rfidScannerConnected = true;
                                msg.Arg1 = 0;
                            }
                        }
                        else
                        {
                            rfidScannerConnected = false;
                            msg.Arg1 = 1;
                        }
                    }
                    else
                    {
                        rfidScannerConnected = false;
                        msg.Arg1 = 1;
                    }
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                    msg.Arg1 = 1;
                }
                catch
                (OperationFailureException e)
                {
                    e.PrintStackTrace();
                    msg.Arg1 = 1;
                    //Log.Debug(TAG, "OperationFailureException " + e.VendorMessage);
                }
                RunOnUiThread(() =>
                {
                    progBar.Dismiss();
                });
                cHandler.SendMessage(msg);
            }));
            thread.Start();
        }

        private void CloseRFIDConnection()
        {
            try
            {
                if (Reader != null)
                {
                    //Reader.Events.RemoveEventsListener(eventHandler);
                    Reader.Disconnect();
                    //Toast.MakeText(ApplicationContext, "Disconnecting reader", ToastLength.Long).Show();
                    Reader = null;
                    readers.Dispose();
                    readers = null;
                }
            }
            catch (InvalidUsageException e)
            {
                e.PrintStackTrace();
            }
            catch (OperationFailureException e)
            {
                e.PrintStackTrace();
            }
            catch (Exception e)
            {
                e.StackTrace.ToString();
            }
        }


        private void ConfigureReader()
        {
            if (Reader.IsConnected)
            {
                TriggerInfo triggerInfo = new TriggerInfo();
                triggerInfo.StartTrigger.TriggerType = START_TRIGGER_TYPE.StartTriggerTypeImmediate;
                triggerInfo.StopTrigger.TriggerType = STOP_TRIGGER_TYPE.StopTriggerTypeImmediate;
                try
                {
                    // receive events from reader

                    if (eventHandler == null)
                        eventHandler = new EventHandler(Reader);

                    Reader.Events.AddEventsListener(eventHandler);
                    // HH event
                    Reader.Events.SetHandheldEvent(true);
                    // tag event with tag data
                    Reader.Events.SetTagReadEvent(true);
                    Reader.Events.SetAttachTagDataWithReadEvent(false);

                    // set trigger mode as rfid so scanner beam will not come
                    Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.RfidMode, true);
                    // set start and stop triggers
                    Reader.Config.StartTrigger = triggerInfo.StartTrigger;
                    Reader.Config.StopTrigger = triggerInfo.StopTrigger;

                    Reader.Events.EventStatusNotify += mcRFID_EventStatusNotify;
                    Reader.Events.EventReadNotify += mcRFID_EventReadNotify;
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch (OperationFailureException e)
                {
                    e.PrintStackTrace();
                }
            }
        }

        private void InitEPCArray()
        {
            for (int i = 0; i < arrEPC.GetUpperBound(0); i++)
            {
                arrEPC[i] = "";
            }
        }

        private void UpdateEPCArryFromDB()
        {
            dbError = "";
            listFoxProducts = db.ExecQuery_FoxProduct(Constants.DBFilename, "select * from FoxProduct", ref dbError);

            if (listFoxProducts.Count > 0)
            {
                for (int i = 0; i < listFoxProducts.Count; i++)
                {
                    AddEPC(listFoxProducts[i].EPC);  //Full Tag EPC Data
                }
            }

            RefreshView();
        }

        private bool EPCExists(string epc)
        {
            int i = 0;

            while (arrEPC[i] != "")
            {
                if (arrEPC[i] == epc)
                {
                    return true;
                }
                i++;
            }

            return false;
        }

        private bool EPCExistsInArray(string epc)
        {
            for (int i = 0; i <= arrEPC.GetUpperBound(0); i++)
            {
                if (arrEPC[i] == "") { return false; }
                if (arrEPC[i] == epc) { return true; }
            }

            return false;
        }

        private bool AddEPC(string epc)
        {
            if (!EPCExistsInArray(epc))
            {
                arrEPC[arrEPCNextIndex] = epc;

                arrEPCNextIndex++;
                if (arrEPCNextIndex > arrEPC.GetUpperBound(0))
                {
                    var builder = new Android.Support.V7.App.AlertDialog.Builder(this);
                    builder.SetMessage("Memory limit reached. Please transfer data, then continue scanning.");
                    builder.SetPositiveButton("Ok", (s, e) =>
                    {

                    });
                    builder.Create().Show();
                }
                return true;
            }
            return false;
        }

        private void SortProductList()
        {
            if (listFoxProducts.Count > 0)
            {
                listFoxProducts.Sort((x, y) =>
                {
                    // Sort by SKU -> EPC
                    //int result = string.Compare(x.FoxSKU, y.FoxSKU);
                    //if (result == 0)
                    //    result = Decimal.Compare(x.epcLast6, y.epcLast6);

                    // Sort by Store -> SKU -> EPC
                    int result = string.Compare(x.OtherStoreCode, y.OtherStoreCode);
                    if (result == 0)
                        result = string.Compare(x.FoxSKU, y.FoxSKU);
                    if (result == 0)
                        result = Decimal.Compare(x.epcLast6, y.epcLast6);
                    return result;
                });

            }
        }

        private void SetVendorFilter()
        {
            EditText txtVCode = FindViewById<EditText>(Resource.Id.txtVCode);

            RunOnUiThread(() =>
            {
                vcodeFilter = txtVCode.Text;
            });
        }

        private bool PassFilter(string epc)
        {
            if (vcodeFilter == "")
            {
                return true;
            }
            else
            {
                return (mcTools.GetAsciiFromHex(epc.Substring(4, 6), true) == vcodeFilter.ToUpper());
            }
        }

        private void RefreshView()
        {
            TextView txtTagCT = FindViewById<TextView>(Resource.Id.txtTagCT);
            ListView lstViewData = FindViewById<ListView>(Resource.Id.listviewdata);

            RunOnUiThread(() =>
            {
                listviewadapter_scandetail adapter = new listviewadapter_scandetail(this, listFoxProducts);
                lstViewData.Adapter = adapter;
                txtTagCT.Text = " Qty: " + listFoxProducts.Count.ToString();
            });
        }

        public void CallRefreshView()
        {
            RefreshView();
        }

        public void DisplayEPCVerifyLoadFailure()
        {
            mcMsgBoxA.ShowMsgWOK(this, "Error", "An error occurred while attempting to load inventory tickets data.", IconType.Critical);
        }

        public void ViewReports()
        {
            StartActivity(typeof(activity_reportsummary));
        }

        public void GotoDataTransfer(string nextAction)
        {
            var intent = new Intent(this, typeof(activity_xfertoserver));
            intent.PutExtra("nextAction", nextAction);
            StartActivity(intent);
        }

        public void DisplayUpdateVerifiedEPCFailure()
        {
            //mcMsgBoxA.ShowMsgWOK(this, "Error", "An error occurred while attempting to update the verification status of these tickets.", IconType.Critical);
        }

        private async void SaveItemsScanned(string nextAction)
        {
            // Save all records in FoxProduct List (Don't worry about adding duplicates. We will de-dedupe after import for better performance)

            Message msg = new Message();
            handler = new MyHandler(this);
            msg = handler.ObtainMessage();

            // Display progress bar

            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetMessage("Processing data. One moment please...");
            progBar.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progBar.Progress = 0;
            progBar.Max = listFoxProducts.Count;
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                int i = 0;

                while (i < listFoxProducts.Count)
                {
                    string qryInsert = "";
                    int batchCT = 0;
                    bool chunkOK = true;

                    while (chunkOK)
                    {
                        //Check list item ok for db write
                        if (listFoxProducts[i].EPC != "")
                        {
                            qryInsert += "insert into FoxProduct (EPC, FoxSKU, VendorCode, Category, epcLast6, Qty, EmployeeID, ActionWOtherLoc, NewConsol, OtherStoreCode, DateTimeScanned) values ('" + listFoxProducts[i].EPC +
                            "','" + listFoxProducts[i].FoxSKU + "','" + listFoxProducts[i].VendorCode + "','" + listFoxProducts[i].Category + "'," +
                            listFoxProducts[i].epcLast6 + "," + listFoxProducts[i].Qty.ToString() + ",'" + empNo + "','" + listFoxProducts[i].ActionWOtherLoc + "','" + listFoxProducts[i].NewConsol + "','" + listFoxProducts[i].OtherStoreCode + "','" + listFoxProducts[i].DateTimeScanned + "'); ";
                        }
                        i++;
                        batchCT++;
                        if ((batchCT >= 25) || (i >= listFoxProducts.Count))
                        {
                            chunkOK = false;
                        }
                    }

                    if (qryInsert != "")
                    {
                        dbError = "";
                        if (!db.ExecWriteSQLiteBatch(Constants.DBFilename, qryInsert, ref dbError))
                        {
                            //Toast.MakeText((this.ApplicationContext), "Success!", ToastLength.Long).Show();
                            //txtdbResult.Text += "Failed insert code: " + _arrVendorDetail[0] + "  ";
                        }
                    }

                    RunOnUiThread(() =>
                    {
                        progBar.Progress = i;
                        //txtdbResult.Text = i.ToString();
                    });

                }  // while (i <= _arrVendorData.GetUpperBound(0))

                string err = "";
                db.ExecWriteSQLiteBatch(Constants.DBFilename, "delete from FoxProduct WHERE rowid NOT IN (SELECT min(rowid) FROM FoxProduct GROUP BY EPC);", ref err);

                RunOnUiThread(() =>
                {
                    progBar.Dismiss();

                    List<FoxVendor> vendors = new List<FoxVendor>();
                });

                if (nextAction == "DONOTHING")
                {
                    msg.Arg1 = 0;
                }

                if (nextAction == "VIEWREPORTS")
                {
                    msg.Arg1 = 1;
                }

                if (nextAction == "DATATRANSFER")
                {
                    msg.Arg1 = 2;
                }

                handler.SendMessage(msg);

            }));

            thread.Start();

        }

        // ****************************************************************************
        // Implement our own event handler so we can access TAG data in our class

        private void mcRFID_EventReadNotify(object sender, EventReadNotifyEventArgs e)
        {
            TagData[] myTags = Reader.Actions.GetReadTags(100);

            ListView lstViewData = FindViewById<ListView>(Resource.Id.listviewdata);

            RunOnUiThread(() =>
            {
                bool viewUpdated = false;

                if (myTags != null)
                {
                    for (int index = 0; index < myTags.Length; index++)
                    {
                        if (myTags[index].TagID.Substring(0, 4) == "F0C5") 
                        {
                            if (!EPCExists(myTags[index].TagID))
                            {
                                if (PassFilter(myTags[index].TagID))
                                {
                                    if (scanType != "VERIFY")
                                    {
                                        if (AddEPC(myTags[index].TagID))  //Full Tag EPC Data
                                        {
                                            FoxProduct prod = new FoxProduct();
                                            prod.EPC = myTags[index].TagID;
                                            prod.FoxSKU = mcTools.GetFoxSKUFromEPC(prod.EPC);
                                            prod.VendorCode = prod.FoxSKU.Substring(0, 3);
                                            prod.Category = prod.FoxSKU.Substring(3, 2);
                                            prod.epcLast6 = Convert.ToInt16(myTags[index].TagID.Substring(26, 6));
                                            prod.Qty = scanQTY;  // 1 = IN, -1 = OUT, 0 = CURRENT ON HAND
                                            prod.DateTimeScanned = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                                            prod.EmployeeID = empNo;
                                            prod.OtherStoreCode = destStoreCode;
                                            prod.ActionWOtherLoc = toFrom;
                                            prod.NewConsol = newConsol;
                                            viewUpdated = true;
                                            invUpdated = true;
                                            listFoxProducts.Add(prod);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Update listview w/ data stored in listFoxProducts

                    if (viewUpdated) { RefreshView(); }
                }
            });
        }

        private void mcRFID_EventStatusNotify(object sender, EventStatusNotifyEventArgs e)
        {

            //Log.Debug(appTAG, "Status Notification: " + rfidStatusEvents.StatusEventData.StatusEventType);
            //if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.HandheldTriggerEvent)
            if (e.P0.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.HandheldTriggerEvent)
            {
                //if (e.P0.StatusEventData.StatusEventType.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerPressed)
                if (e.P0.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerPressed)
                {

                    timerCT = 0;
                    timer.Stop();
                    timer.Enabled = false;

                    ThreadPool.QueueUserWorkItem(o =>
                    {

                        try
                        {
                            SetVendorFilter();
                            Reader.Actions.Inventory.Perform();
                        }
                        catch
                        (InvalidUsageException ex)
                        {
                            ex.PrintStackTrace();
                        }
                        catch
                        (OperationFailureException ex)
                        {
                            ex.PrintStackTrace();
                        }
                    });
                }
                if (e.P0.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerReleased)
                {
                    timerCT = 0;
                    timer.Enabled = true;
                    timer.Start();

                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            Reader.Actions.Inventory.Stop();                            
                            SortProductList();
                            RefreshView();
                        }
                        catch
                        (InvalidUsageException ex)
                        {
                            ex.PrintStackTrace();
                        }
                        catch
                        (OperationFailureException ex)
                        {
                            ex.PrintStackTrace();
                        }
                    });
                }
            }


        } // mcRFID_EventStatusNotify

        // ***********************************************************************

    }

    //////////////////////////////////////////////////////////////////
    // Connection Event Handler Class
    //////////////////////////////////////////////////////////////////

    class ConnectionHandler : Handler
    {
        private activity_Scan activity;

        public ConnectionHandler(activity_Scan activity)
        {
            this.activity = activity;
        }

        public override void HandleMessage(Message msg)
        {
            //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
            switch (msg.Arg1)
            {
                case 0:
                    // RFID Reader connected. Do nothing
                    break;
                case 1:
                    // Connection to RFID reader FAILED. Launch BANG
                    activity.LaunchReaderBang();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }  // cHandler


    class EventHandlerEPCLoadComplete : Handler
    {
        private activity_Scan activity;

        public EventHandlerEPCLoadComplete(activity_Scan activity)
        {
            this.activity = activity;
        }

        public override void HandleMessage(Message msg)
        {
            //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
            switch (msg.Arg1)
            {
                case 0:
                    // Service call completed successfully
                    activity.CallRefreshView();
                    break;
                case 1:
                    // Error during service call
                    activity.DisplayEPCVerifyLoadFailure();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }



}
