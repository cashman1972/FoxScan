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
using Android.Util;
using Com.Zebra.Rfid.Api3;
using Java.Util;
using Android.Views.InputMethods;
using System.Threading;
using SQLite;


namespace FoxScan
{
    [Activity(Label = "activity_scanbarcode")]
    public class activity_scanbarcode : Activity
    {
        private static Readers readers;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static RFIDReader Reader;
        private EventHandler eventHandler;
        private string storeCode = mcTools.GetStoreCodeAssigned();

        Database db = new Database();
        private string dbError = "";
        bool rfidScannerConnected = false;
        ConnectionHandlerBarcode cHandler;

        DataRetrievalHandler dataHandler;
        string dataLastSoldResult = "";
        string dataMDResult = "";

        TextView txtVendor;
        TextView txtVendorLastSoldStore;
        TextView txtVendorLastSoldDate;
        TextView txtVendorLastConsolDate;
        TextView txtStyle;
        TextView txtStyleLastSoldStore;
        TextView txtStyleLastSoldDate;
        TextView txtStyleLastSoldPrice;

        // ** BARCODE **
        public static activity_scanbarcode Instance;
        myBroadcastReceiver receiver;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            activity_scanbarcode.Instance = this;

            // Create your application here

            SetContentView(Resource.Layout.layout_scanbarcode);

            txtVendor = FindViewById<TextView>(Resource.Id.txtSkuInfoVendor);
            txtVendorLastSoldStore = FindViewById<TextView>(Resource.Id.txtSkuInfoVendorLastSoldStore);
            txtVendorLastSoldDate = FindViewById<TextView>(Resource.Id.txtSkuInfoVendorLastSoldDate);
            txtVendorLastConsolDate = FindViewById<TextView>(Resource.Id.txtSkuInfoLastConsolDate);
            txtStyle = FindViewById<TextView>(Resource.Id.txtSkuInfoStyle);
            txtStyleLastSoldStore = FindViewById<TextView>(Resource.Id.txtSkuInfoStyleLastSoldStore);
            txtStyleLastSoldDate = FindViewById<TextView>(Resource.Id.txtSkuInfoStyleLastSoldDate);
            txtStyleLastSoldPrice = FindViewById<TextView>(Resource.Id.txtSkuInfoStyleLastSoldPrice);

            Button btnBack = FindViewById<Button>(Resource.Id.btnSkuInfoBack);
            Button btnViewMD = FindViewById<Button>(Resource.Id.btnSkuInfoViewMD);

            btnBack.Click += BtnBack_Click;
            btnViewMD.Click += BtnViewMD_Click;

            InitResultFields();

            receiver = new myBroadcastReceiver();

            //OpenRFIDConnection2();
            CloseRFIDConnection(); // Make sure we are disconnected from RFID reader

            RegisterReceiver(receiver, new IntentFilter(Resources.GetString(Resource.String.activity_intent_filter_action)));
        }

        private void BtnViewMD_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        protected override void OnResume()
        {
            base.OnResume();

            Intent i = new Intent();
            i.SetAction("com.symbol.datawedge.api.ACTION");
            i.PutExtra("com.symbol.datawedge.api.SCANNER_INPUT_PLUGIN", "ENABLE_PLUGIN");
            this.SendBroadcast(i);
        }

        private void OpenRFIDConnection2()
        {

            Message msg = new Message();
            cHandler = new ConnectionHandlerBarcode(this);
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

        private string BarcodeStrip9(string barCode)
        {
            if (barCode.Substring(0, 1) == "9")
            {
                barCode = barCode.Substring(1, barCode.Length - 1);
            }
            return barCode;
        }

        public bool BarcodeValid(string barCode)
        {
            if (barCode != "")
            {
                barCode = BarcodeStrip9(barCode);
                if (barCode.Length >= 8)
                {
                    if (!mcTools.IsNumeric(barCode.Substring(0,3)))
                    {
                        if (mcTools.IsNumeric(barCode.Substring(3,5)))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void ScanResult(Intent intent)
        {
            //  Output the scanned barcode on the screen.  Bear in mind older JB devices will use the legacy DW parameters on unbranded devices.  
            String decodedSource = intent.GetStringExtra(Resources.GetString(
                Resource.String.datawedge_intent_key_source));
            String barCode = intent.GetStringExtra(Resources.GetString(
                Resource.String.datawedge_intent_key_data));
            String decodedLabelType = intent.GetStringExtra(Resources.GetString(
                Resource.String.datawedge_intent_key_label_type));

            //TextView scanSourceTxt = FindViewById<TextView> (Resource.Id.txtScanScource);
            //TextView scanDataTxt = FindViewById<TextView> (Resource.Id.txtScanData);
            //TextView scanLabelTypeTxt = FindViewById<TextView> (Resource.Id.txtScanDecoder);
            //scanSourceTxt.Text = "Scan Source: " + decodedSource;
            //scanDataTxt.Text = "Scan Data: " + decodedData;
            //scanLabelTypeTxt.Text = "Scan Decoder: " + decodedLabelType;

            
            if (storeCode != "")
            {

                TextView txtBarcode = FindViewById<TextView>(Resource.Id.txtBarcode);
                barCode = BarcodeStrip9(barCode);
                txtBarcode.Text = barCode;

                if (BarcodeValid(barCode))
                {

                    ///////////////////////////////////
                    // Pull Last Sold / Consol Data
                    ///////////////////////////////////

                    NetworkStatusMonitor nm = new NetworkStatusMonitor();
                    nm.UpdateNetworkStatus();

                    dataLastSoldResult = "";
                    dataMDResult = "";
                    InitResultFields();

                    if (nm.State == NetworkState.ConnectedWifi)
                    {
                        Message msg = new Message();
                        dataHandler = new DataRetrievalHandler(this);
                        msg = dataHandler.ObtainMessage();

                        // Display progress bar

                        ProgressDialog progBar = new ProgressDialog(this);

                        progBar.SetCancelable(false);
                        progBar.SetTitle("Retrieving data");
                        progBar.SetIcon(Resource.Drawable.iconChill64);
                        progBar.SetMessage("One moment please...");
                        progBar.SetProgressStyle(ProgressDialogStyle.Spinner);
                        progBar.Show();

                        var thread = new System.Threading.Thread(new ThreadStart(delegate
                        {
                            try
                            {
                                FoxScannerSvc.FoxScannerSvc foxScannerSvc = new FoxScannerSvc.FoxScannerSvc();
                                dataLastSoldResult = foxScannerSvc.GetLastSoldInfo(barCode);
                                dataMDResult = foxScannerSvc.GetMarkdownsVendor(barCode.Substring(0, 3), storeCode);

                                RunOnUiThread(() =>
                                {
                                    progBar.Dismiss();
                                });
                                msg.Arg1 = 0;
                                dataHandler.SendMessage(msg);
                            }
                            catch (Exception ex)
                            {
                                RunOnUiThread(() =>
                                {
                                    progBar.Dismiss();
                                });
                                msg.Arg1 = 1;
                                dataHandler.SendMessage(msg);
                            }
                        }));

                        thread.Start();
                    }
                    else
                    {
                        var builder = new Android.App.AlertDialog.Builder(this);
                        builder.SetTitle("WiFi NOT Connected!!");
                        builder.SetIcon(Resource.Drawable.iconWarning64);
                        builder.SetMessage("WiFi is not connected. Please connect WiFi and try again.");
                        builder.SetPositiveButton("OK", (s, e2) =>
                        {
                        // Goto transfer data w/ nextActivity = "SCANOPTIONS"

                    }
                        );
                        builder.Create().Show();
                    }
                }
                else
                {
                    var builder = new Android.App.AlertDialog.Builder(this);
                    builder.SetTitle("Invalid Barcode!!");
                    builder.SetIcon(Resource.Drawable.iconWarning64);
                    builder.SetMessage("The barcode scanned is not recognized as a valid Fox's barcode.");
                    builder.SetPositiveButton("OK", (s, e2) =>
                    {
                    // Goto transfer data w/ nextActivity = "SCANOPTIONS"

                }
                    );
                    builder.Create().Show();
                }
            }
            else
            {
                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetTitle("Scanner NOT Registered!");
                builder.SetIcon(Resource.Drawable.iconWarning64);
                builder.SetMessage("The scanner must be registered before this utility can be used.");
                builder.SetPositiveButton("OK", (s, e2) =>
                {
                    // Goto transfer data w/ nextActivity = "SCANOPTIONS"

                }
                );
                builder.Create().Show();
            }
        }

        public void InitResultFields()
        {
            txtVendor.Text = "";
            txtVendorLastSoldStore.Text = "";
            txtVendorLastSoldDate.Text = "";
            txtVendorLastConsolDate.Text = "";
            txtStyle.Text = "";
            txtStyleLastSoldStore.Text = "";
            txtStyleLastSoldDate.Text = "";
            txtStyleLastSoldPrice.Text = "";

            dataLastSoldResult = "";
            dataMDResult = "";
        }

        public void DisplayDataRetrieved()
        {
            if (dataLastSoldResult != "")
            {
                // return format: {Vendor Code}, {Vendor Name}, {Ven. Last Sold Store}, {Ven. Last Sold Date}, {Ven. Last Consol Date}, {Style}, {Style Last Sold Store}, {Style Last Sold Date}, {Style Last Ticket Price}

                string[] dataFields = new string[9];
                dataFields = dataLastSoldResult.Split(',');

                txtVendor.Text = "[" + dataFields[0] + "] - " + dataFields[1];
                txtVendorLastSoldStore.Text = dataFields[2];
                txtVendorLastSoldDate.Text = dataFields[3];
                txtVendorLastConsolDate.Text = dataFields[4];
                txtStyle.Text = dataFields[5];
                txtStyleLastSoldStore.Text = dataFields[6];
                txtStyleLastSoldDate.Text = dataFields[7];
                if (dataFields[8] != "")
                {
                    if (mcTools.IsNumeric(dataFields[8]))
                    {
                        txtStyleLastSoldPrice.Text = "$" + Math.Round(Convert.ToDecimal(dataFields[8]), 2);
                    }
                }
            }
            else
            {
                // No records found

                var builder = new Android.App.AlertDialog.Builder(this);
                builder.SetMessage("No records found for this barcode.");
                builder.SetPositiveButton("OK", (s, e2) =>
                {
                    // Goto transfer data w/ nextActivity = "SCANOPTIONS"

                }
                );
                builder.Create().Show();
            }
        }

        public void DisplayDataRetrievalFailure()
        {

        }

        private void ConfigureReader()
        {
            if (Reader.IsConnected)
            {
                TriggerInfo triggerInfo = new TriggerInfo();
                //triggerInfo.StartTrigger.TriggerType = START_TRIGGER_TYPE.StartTriggerTypeImmediate;
                //triggerInfo.StopTrigger.TriggerType = STOP_TRIGGER_TYPE.StopTriggerTypeImmediate;
                try
                {
                    // receive events from reader

                    //if (eventHandler == null)
                    //    eventHandler = new EventHandler(Reader);

                    //Reader.Events.AddEventsListener(eventHandler);
                    // HH event
                    //Reader.Events.SetHandheldEvent(true);
                    // tag event with tag data
                    //Reader.Events.SetTagReadEvent(true);
                    //Reader.Events.SetAttachTagDataWithReadEvent(false);

                    // set trigger mode as rfid so scanner beam WILL come
                    
                    
                    Reader.Config.SetTriggerMode(ENUM_TRIGGER_MODE.BarcodeMode, false);
                    // set start and stop triggers
                    //Reader.Config.StartTrigger = triggerInfo.StartTrigger;
                    //Reader.Config.StopTrigger = triggerInfo.StopTrigger;

                    //Reader.Events.EventStatusNotify += mcRFID_EventStatusNotify;
                    //Reader.Events.EventReadNotify += mcRFID_EventReadNotify;
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

        // ****************************************************************************
        // Implement our own event handler so we can access TAG data in our class

        private void mcRFID_EventReadNotify(object sender, EventReadNotifyEventArgs e)
        {

            TagData[] myTags = Reader.Actions.GetReadTags(100);

            ListView lstViewData = FindViewById<ListView>(Resource.Id.listviewdata);

            //RunOnUiThread(() =>
            //{
            //    if (myTags != null)
            //    {
            //        for (int index = 0; index < myTags.Length; index++)
            //        {
            //            if (myTags[index].TagID.Substring(0, 4) == "F0C5")
            //            {
            //                if (!EPCExists(myTags[index].TagID))
            //                {
            //                    if (PassFilter(myTags[index].TagID))
            //                    {
            //                        AddEPC(myTags[index].TagID);  //Full Tag EPC Data
            //                        FoxProduct prod = new FoxProduct();
            //                        prod.EPC = myTags[index].TagID;
            //                        prod.FoxSKU = mcTools.GetFoxSKUFromEPC(prod.EPC);
            //                        prod.VendorCode = prod.FoxSKU.Substring(0, 3);
            //                        prod.Category = prod.FoxSKU.Substring(3, 2);
            //                        prod.epcLast6 = Convert.ToInt16(myTags[index].TagID.Substring(26, 6));
            //                        prod.Qty = scanQTY;  // 1 = IN, -1 = OUT, 0 = CURRENT ON HAND
            //                        prod.DateTimeScanned = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
            //                        prod.EmployeeID = empNo;
            //                        prod.ToFromLocation = destStoreCode;
            //                        listFoxProducts.Add(prod);
            //                    }
            //                }
            //            }
            //        }

            //        // Update listview w/ data stored in listFoxProducts

            //        RefreshView();
            //    }
            //});
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
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            Reader.Actions.Inventory.Stop();
                            //SortProductList();
                            //RefreshView();
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


    //  Broadcast receiver to receive our scanned data from Datawedge  
    [BroadcastReceiver(Enabled = true)]
    public class myBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            String action = intent.Action;
            if (action.Equals(activity_scanbarcode.Instance.Resources.GetString(
                Resource.String.activity_intent_filter_action)))
            {
                //  A barcode has been scanned  
                activity_scanbarcode.Instance.RunOnUiThread(() =>
                    activity_scanbarcode.Instance.ScanResult(intent));
            }
        }
    }


    //////////////////////////////////////////////////////////////////
    // Connection Event Handler Class
    //////////////////////////////////////////////////////////////////

    class ConnectionHandlerBarcode : Handler
    {
        private activity_scanbarcode activity;

        public ConnectionHandlerBarcode(activity_scanbarcode activity)
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
                    //activity.LaunchReaderBang();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }  // Handler


    //////////////////////////////////////////////////////////////////
    // Data Retrieval Event Handler Class
    //////////////////////////////////////////////////////////////////

    class DataRetrievalHandler : Handler
    {
        private activity_scanbarcode activity;

        public DataRetrievalHandler(activity_scanbarcode activity)
        {
            this.activity = activity;
        }

        public override void HandleMessage(Message msg)
        {
            //_activity.UpdateProgBar(msg.Arg1, msg.Arg2);
            switch (msg.Arg1)
            {
                case 0:
                    // Data retrieval SUCCESS
                    activity.DisplayDataRetrieved();
                    break;
                case 1:
                    // Data retrieval FAILURE
                    activity.DisplayDataRetrievalFailure();
                    break;
                default:
                    break;
            }
            base.HandleMessage(msg);
        }
    }  // Handler
}