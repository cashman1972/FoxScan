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
using Com.Zebra.Rfid.Api3;
using Java.Util;
using Android.Views.InputMethods;
using System.Threading;
using SQLite;
using Android.Text;

namespace FoxScan
{
    [Activity(Label = "activity_findsku")]
    public class activity_findsku : Activity
    {
        private static Readers readers;
        private static IList<ReaderDevice> availableRFIDReaderList;
        private static ReaderDevice readerDevice;
        private static RFIDReader Reader;
        private EventHandler eventHandler;
        private bool rfidScannerFound = false;

        private string searchSKU = "";  // "JLM76008";
        private string epcSearchMask = ""; // "F0C5444156341024D";
        private string epcFull = "";
        private string searchMode = "";

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here

            SetContentView(Resource.Layout.layout_findsku);

            
            EditText edtFindSKU = FindViewById<EditText>(Resource.Id.edtFindSKU);
            TextView txtFindSKUDistance = FindViewById<TextView>(Resource.Id.txtFindSKUDistance);
            Button btnExit = FindViewById<Button>(Resource.Id.btnFindSKUExit);

            txtFindSKUDistance.Text = "(Hold trigger...)";

            edtFindSKU.InputType = InputTypes.TextFlagCapCharacters;

            InitReader();

            btnExit.Click += BtnExit_Click;

        } // OnCreate()

        private void BtnExit_Click(object sender, EventArgs e)
        {
            this.Finish();
        }

        private void InitReader()
        {
            if (readers == null)
            {
                readers = new Readers(this, ENUM_TRANSPORT.ServiceSerial);
            }
            ThreadPool.QueueUserWorkItem(o =>
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
                                    rfidScannerFound = true;

                                }
                            }
                        }
                    }
                }
                catch (InvalidUsageException e)
                {
                    e.PrintStackTrace();
                }
                catch
                (OperationFailureException e)
                {
                    e.PrintStackTrace();
                    //Log.Debug(TAG, "OperationFailureException " + e.VendorMessage);
                }
            });
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
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
        } // OnDestroy()

        private void LocateTagStartingWEPC(string epcStart)
        {
            Reader.Actions.Inventory.Perform();
            //Reader.Actions.TagLocationing.Perform("F0C5444156341024D000000001000012", null, null);
        }

        private string GetEPCSearchMaskFromSKUEntered()
        {
            EditText edtFindSKU = FindViewById<EditText>(Resource.Id.edtFindSKU);

            searchSKU = edtFindSKU.Text;

            if (searchSKU == "")
            {
                return "";
            }
            else
            {
                if (searchSKU.Substring(0, 1) == "9")   // strip leading "9" if entered
                {
                    searchSKU = searchSKU.Substring(1, searchSKU.Length - 1);
                }

                string sm = "F0C5" + mcTools.ConvertFoxSKUToEPCSegment(searchSKU);
                return sm.Substring(0, 24);
            }
        }

        private void NotifyInvalidSearchSKUEntered()
        {
            RunOnUiThread(() =>
            {
                var builder = new AlertDialog.Builder(this);
                string dbError = "";
                builder.SetMessage("Invalid search SKU entered. SKU must contain minimum of the 1st 8 chars. (vendor code + 5 digit style). Size is optional. (examples: ZZZ12345, ZZZ12345XL etc...)");
                builder.SetTitle("Invalid search SKU");
                builder.SetIcon(Resource.Drawable.iconWarning64);
                builder.SetPositiveButton("OK", (s, e2) =>
                { /* Handle 'OK' click */

                }
                );
                builder.Create().Show();
            });
        }

        private void UpdateRadarGraphic(int distance)
        {
            ImageView imgRadar = FindViewById<ImageView>(Resource.Id.imgFindSKU);

            if (distance == 0)
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind0);
                return;
            }

            if ((distance > 0) && (distance <= 8))
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind1);
                return;
            }

            if ((distance > 8) && (distance <= 15))
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind2);
                return;
            }

            if ((distance > 15) && (distance <= 25))
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind3);
                return;
            }

            if ((distance > 25) && (distance <= 40))
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind4);
                return;
            }

            if ((distance > 40) && (distance <= 55))
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind5);
                return;
            }

            if ((distance > 55) && (distance <= 68))
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind6);
                return;
            }

            if (distance > 68)
            {
                imgRadar.SetImageResource(Resource.Drawable.SKUFind7);
                return;
            }
        }

        // ****************************************************************************
        // Implement our own event handler so we can access TAG data in our class

        private void mcRFID_EventReadNotify(object sender, EventReadNotifyEventArgs e)
        {
            TextView txtFindSKUDistance = FindViewById<TextView>(Resource.Id.txtFindSKUDistance);
            TagData[] myTags = Reader.Actions.GetReadTags(100);

            RunOnUiThread(() =>
            {
                if (myTags != null)
                {

                    // Update listview w/ data stored in listFoxProducts

                    for (int index = 0; index < myTags.Length; index++)
                    {

                        if (myTags[index].IsContainsLocationInfo)
                        {
                            int tag = index;
                            int distance = (int)90 - (int)myTags[tag].LocationInfo.RelativeDistance;

                            if (distance < 0) { distance = 0; }

                            if (distance < 90)
                            {
                                txtFindSKUDistance.SetTextColor(Android.Graphics.Color.Black);
                                txtFindSKUDistance.Text = distance.ToString();
                            }
                            else
                            {
                                txtFindSKUDistance.SetTextColor(Android.Graphics.Color.DarkRed);
                                txtFindSKUDistance.Text = "Out of range";
                            }
                            UpdateRadarGraphic((int)myTags[tag].LocationInfo.RelativeDistance);
                        }

                        
                    }
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
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        try
                        {
                            //SetVendorFilter();
                            //Reader.Actions.Inventory.Perform();
                            //Reader.Actions.TagLocationing.Perform("F0C5444156341024D000000001000012", null, null);
                            //Reader.Actions.TagLocationing.Perform("F0C5444156341024D", null, null);
                            epcFull = "";
                            epcSearchMask = GetEPCSearchMaskFromSKUEntered();
                            if (epcSearchMask.Length >= 11)
                            {
                                //LocateTagStartingWEPC(epcSearchMask);
                                Reader.Actions.TagLocationing.Perform(epcSearchMask, null, null);
                            }
                            else
                            {
                                NotifyInvalidSearchSKUEntered();
                            }
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
                            //Reader.Actions.Inventory.Stop();
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
}