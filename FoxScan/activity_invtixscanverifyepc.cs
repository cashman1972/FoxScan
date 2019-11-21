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
using System.Timers;
using Android.Util;
using Com.Zebra.Rfid.Api3;
using Java.Util;
using Android.Views.InputMethods;
using System.Threading;
using SQLite;

namespace FoxScan
{
    [Activity(Label = "activity_invtixscanverifyepc")]
    public class activity_invtixscanverifyepc : Activity
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

        //string empNo = "";
        //string empName = "";
        //string storeName = "";
        string toFrom = "";
        //string destStoreCode = "";
        //int scanQTY = -999; // = 1 for New IN, -1 for OUT, 0 for Current ON HAND
        //private string vcodeFilter = "";
        private string scanType = "";
        //private string newConsol = "NEW";

        private string[] arrEPC = new string[5000];
        private int arrEPCNextIndex = 0;

        List<string> listEPCsVerified = new List<string>();

        MyHandler handler;
        ConnectionHandlerEPCVerify cHandler;
        EventHandlerEPCVerifyLoadComplete epcLoadHandler;
        EventHandlerUploadVerified epcVerifyHandler;
        Database db = new Database();
        System.Timers.Timer timer = new System.Timers.Timer();
        int timerCT = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_invtixscanverifyepc);

            Button btnImportInvTixBatch = FindViewById<Button>(Resource.Id.btnInvTixVerifyLoadRFIDBatch);
            Button btnBack = FindViewById<Button>(Resource.Id.btnInvTixVerifyBackScan);

            scanType = "VERIFY";
            toFrom = "VERIFY";

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

            btnImportInvTixBatch.Click += BtnImportInvTixBatch_Click;
            btnBack.Click += BtnBack_Click;

            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;

        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerCT++;

            RunOnUiThread(() =>
            {
                if (timerCT >= 4)
                {
                    timer.Stop();
                    timer.Enabled = false;
                    timerCT = 0;
                    if (listEPCsVerified.Count > 0)
                    {
                        UpdateVerifiedEPCs(true);
                    }
                }
            });
        }

        protected override void OnResume()
        {
            base.OnResume();

            //* Need to test unit on wakeup that has been left on the inventory scan screen
            //  to see if we are going to be able to determine RFID scanner is asleep. Can test this on MainActivity

            InitEPCArray();  // Initialize array that will hold EPCs (only) for the purpose of making sure duplicates
            arrEPCNextIndex = 0; // Required. This will be incremented to approp. in UpdateEPCArrayFromDB()

            if (scanType != "VERIFY")
            {
                UpdateEPCArryFromDB(); // Add any stored <FoxProduct> records in db to EPC array and listFoxProducts *Note: RefreshView() called from within
            }
            else
            {
                RefreshView();
            }

            OpenRFIDConnection2();
        }

        protected override void OnPause()
        {
            base.OnPause();

            if (listEPCsVerified.Count > 0)
            {
                UpdateVerifiedEPCs(false);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (listEPCsVerified.Count > 0)
            {
                UpdateVerifiedEPCs(false);
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
        }  // OnDestroy()

        private void BtnImportInvTixBatch_Click(object sender, EventArgs e)
        {

            if (listEPCsVerified.Count > 0)
            {
                UpdateVerifiedEPCs(false);
            }

            var myIntent = new Intent(this, typeof(activity_invtixbatchimport));
            StartActivityForResult(myIntent, 0);
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            // Check if any items exist in db, prompt user about transferring.

            if (listFoxProducts.Count > 0)
            {
                if (listEPCsVerified.Count > 0)
                {
                    UpdateVerifiedEPCs(false);
                }
                this.Finish();
            }
            else
            {
                this.Finish();
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (scanType == "VERIFY")
            {
                if ((resultCode == Result.Ok) || (resultCode == Android.App.Result.Ok))
                {
                    string batchesImported = data.GetStringExtra("batchesImported");

                    //if (batchesImported.Substring(batchesImported.Length-1,1) == ",") { batchesImported = batchesImported.Substring(0, batchesImported.Length - 1); }

                    int lastChar = batchesImported.Length - 1; // necessary as an apparent VSTUDIO bug causes the above commented out line w/ batchesImported.Length - 1 inside Substring to fail

                    if (batchesImported.Substring(lastChar, 1) == ",") { batchesImported = batchesImported.Substring(0, batchesImported.Length - 1); }
                    LoadVerifyTickets(batchesImported);
                }
            }
        }

        private void LoadVerifyTickets(string batchList)
        {
            InitEPCArray();
            listFoxProducts.Clear();
            listEPCsVerified.Clear();

            Message msg = new Message();
            epcLoadHandler = new EventHandlerEPCVerifyLoadComplete(this);
            msg = epcLoadHandler.ObtainMessage();

            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetTitle("Retrieving Ticket Data...");
            progBar.SetIcon(Resource.Drawable.iconChill64);
            progBar.SetMessage("Importing Ticket Info...");
            progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
            progBar.Progress = 0;
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                try
                {
                    FoxScannerSvc.FoxScannerSvc foxSql = new FoxScannerSvc.FoxScannerSvc();
                    string epcList = foxSql.GetRFIDEPCsfromBatchList(batchList);

                    if (epcList != "")
                    {
                        string[] epcArray = epcList.Split(',');

                        for (int i = 0; i <= epcArray.GetUpperBound(0); i++)
                        {
                            if (epcArray[i] != "")
                            {
                                //AddEPC(epcArray[i]);

                                FoxProduct prod = new FoxProduct();
                                prod.EPC = epcArray[i];
                                prod.FoxSKU = mcTools.GetFoxSKUFromEPC(prod.EPC);
                                prod.VendorCode = prod.FoxSKU.Substring(0, 3);
                                prod.Category = prod.FoxSKU.Substring(3, 2);
                                prod.epcLast6 = Convert.ToInt16(epcArray[i].Substring(26, 6));
                                prod.Qty = 1;  // 1 = IN, -1 = OUT, 0 = CURRENT ON HAND

                                listFoxProducts.Add(prod);

                                // Below does not pertain to inv tix verification

                                //prod.ActionWOtherLoc = toFrom;
                                //prod.NewConsol = newConsol;
                                //prod.OtherStoreCode = destStoreCode;
                                //prod.DateTimeScanned = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                                //prod.EmployeeID = empNo;

                            }
                        }
                    }

                    RunOnUiThread(() =>
                    {
                        progBar.Dismiss();
                    });

                    msg.Arg1 = 0;
                    epcLoadHandler.SendMessage(msg);

                }
                catch (Exception ex)
                {
                    msg.Arg1 = 1;
                    epcLoadHandler.SendMessage(msg);
                }
            }));

            thread.Start();
        } // LoadVerifyTickets()

        public void LaunchReaderBang()
        {
            StartActivity(typeof(activity_ReaderBang));
            this.Finish();
        }

        private void OpenRFIDConnection2()
        {

            Message msg = new Message();
            cHandler = new ConnectionHandlerEPCVerify(this);
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
                    int result = string.Compare(x.FoxSKU, y.FoxSKU);
                    if (result == 0)
                        result = Decimal.Compare(x.epcLast6, y.epcLast6);
                    return result;
                });

            }
        }

        private bool PassFilter(string epc)
        {
            return true;
        }

        private void RefreshView()
        {
            TextView txtTagCT = FindViewById<TextView>(Resource.Id.txtInvTixVerifyTagCT);
            ListView lstViewData = FindViewById<ListView>(Resource.Id.lvInvTixVerifyEPCs);

            RunOnUiThread(() =>
            {
                listviewadapter_invtixscanverifyepc adapter = new listviewadapter_invtixscanverifyepc(this, listFoxProducts);
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

        private void UpdateVerifiedEPCs(bool stealthMode)
        {
            Message msg = new Message();
            epcVerifyHandler = new EventHandlerUploadVerified(this);
            msg = epcVerifyHandler.ObtainMessage();

            // Display progress bar

            ProgressDialog progBar = new ProgressDialog(this);

            progBar.SetCancelable(false);
            progBar.SetMessage("Updating Verified Tickets...");
            progBar.SetProgressStyle(ProgressDialogStyle.Horizontal);
            progBar.Progress = 0;
            progBar.Max = listFoxProducts.Count;
            progBar.Show();

            var thread = new System.Threading.Thread(new ThreadStart(delegate
            {
                string epcsVerified = "";

                for (int epcCT = 0; epcCT < listEPCsVerified.Count; epcCT++)
                {
                    if (epcCT > 0) { epcsVerified += ","; }
                    epcsVerified += listEPCsVerified[epcCT];
                }

                FoxScannerSvc.FoxScannerSvc foxScannerSvc = new FoxScannerSvc.FoxScannerSvc();
                string result = foxScannerSvc.UpdateVerifiedEPCs(epcsVerified);

                if (result == "SUCCESS")
                {
                    if (!stealthMode)
                    {
                        listEPCsVerified.Clear();
                    }
                    msg.Arg1 = 0;
                }
                else
                {
                    msg.Arg1 = 1;
                }

                RunOnUiThread(() =>
                {
                    progBar.Dismiss();
                });

                epcVerifyHandler.SendMessage(msg);

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
                                            prod.Qty = 0;  // 1 = IN, -1 = OUT, 0 = CURRENT ON HAND
                                            prod.DateTimeScanned = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                                            prod.EmployeeID = "";
                                            prod.OtherStoreCode = "";
                                            prod.ActionWOtherLoc = toFrom;
                                            prod.NewConsol = "";
                                            viewUpdated = true;
                                            invUpdated = true;
                                            listFoxProducts.Add(prod);
                                        }
                                    }
                                    else
                                    {
                                        // TAG VERIFICATION - REMOVE Tags from listFoxProducts and refresh view

                                        string epc = myTags[index].TagID;

                                        if (listFoxProducts.Exists(foxProd => foxProd.EPC == epc))
                                        {
                                            int indexEPC = listFoxProducts.FindIndex(foxProd => foxProd.EPC == epc);
                                            listFoxProducts.RemoveAt(indexEPC);
                                            listEPCsVerified.Add(epc);
                                            viewUpdated = true;
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
                            //SetVendorFilter();
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



    }  // Class


    //////////////////////////////////////////////////////////////////
    // Connection Event Handler Class
    //////////////////////////////////////////////////////////////////

    class ConnectionHandlerEPCVerify : Handler
    {
        private activity_invtixscanverifyepc activity;

        public ConnectionHandlerEPCVerify(activity_invtixscanverifyepc activity)
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


    // ================= EVENT HANDLERS ======================

    class EventHandlerEPCVerifyLoadComplete : Handler
    {
        private activity_invtixscanverifyepc activity;

        public EventHandlerEPCVerifyLoadComplete(activity_invtixscanverifyepc activity)
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


    class EventHandlerUploadVerified : Handler
    {
        private activity_invtixscanverifyepc activity;

        public EventHandlerUploadVerified(activity_invtixscanverifyepc activity)
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
                    //activity.CallRefreshView();
                    break;
                case 1:
                    // Error during service call
                    //activity.DisplayUpdateVerifiedEPCFailure();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }

}