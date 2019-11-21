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
    [Activity(Label = "activity_whsscanskudetail")]
    public class activity_whsscanskudetail : Activity
    {

        private static Readers readers;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static RFIDReader Reader;
        private EventHandler eventHandler;
        ConnectionHandler cHandler;
        bool rfidScannerConnected = false;
        private string[] arrEPC = new string[5000];
        private int arrEPCNextIndex = 0;

        ListView lstViewData;
        List<FoxProduct> listProducts = new List<FoxProduct>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_whsscanskudetail);

            lstViewData = FindViewById<ListView>(Resource.Id.listviewscanskudetail);

            //AddDummyListData();   // testing

            DisplayProductList();
        }

        protected override void OnResume()
        {
            base.OnResume();

            //* Need to test unit on wakeup that has been left on the inventory scan screen
            //  to see if we are going to be able to determine RFID scanner is asleep. Can test this on MainActivity

            InitEPCArray();  // Initialize array that will hold EPCs (only) for the purpose of making sure duplicates
            arrEPCNextIndex = 0; // Required. This will be incremented to approp. in UpdateEPCArrayFromDB()

            RefreshView();

            OpenRFIDConnection2();
        }

        private void InitEPCArray()
        {
            for (int i = 0; i < arrEPC.GetUpperBound(0); i++)
            {
                arrEPC[i] = "";
            }
        }

        private void AddDummyListData()
        {
            FoxProduct fp = new FoxProduct();

            fp.FoxSKU = "WWA12345XLS";
            fp.Color = "Sapphire Blue";
            fp.VendorSKU = "..C19000S-SJ545";
            fp.Price = 1999;
            fp.Description = "Blue blouse with stringy things";

            listProducts.Add(fp);

            fp = new FoxProduct();

            fp.FoxSKU = "WWA12502XLSM";
            fp.Color = "Pink Blush";
            fp.VendorSKU = "MJC12500S-SJ645";
            fp.Price = 2999;
            fp.Description = "Pink blouse with stringy things";

            listProducts.Add(fp);
        }

        private void RefreshView()
        {

        }

        public void DisplayProductList()
        {
            listviewadapter_whsscanskudetail adapter = new listviewadapter_whsscanskudetail(this, listProducts);
            lstViewData.Adapter = adapter;
        }


        // =============== RFID READER STUFF ==================

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

                    // Reader.Events.EventStatusNotify += mcRFID_EventStatusNotify;
                    // Reader.Events.EventReadNotify += mcRFID_EventReadNotify;
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


        //////////////////////////////////////////////////////////////////
        // Connection Event Handler Class
        //////////////////////////////////////////////////////////////////

        class ConnectionHandler : Handler
        {
            private activity_whsscanskudetail activity;

            public ConnectionHandler(activity_whsscanskudetail activity)
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

        // ====================================================
    }
}