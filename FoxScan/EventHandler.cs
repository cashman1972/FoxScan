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

namespace FoxScan
{
    // **********************************************************************************
    // ZEBRA EVENT HANDLER CLASS Below

    // Read/Status Notify handler
    // Implement the RfidEventsLister class to receive event notifications

    public class EventHandler : Java.Lang.Object, IRfidEventsListener
    {

        //TagData[] myTags = null;

        public EventHandler(RFIDReader Reader)
        {
        }
        // Read Event Notification
        public void EventReadNotify(RfidReadEvents e)
        {
            // Recommended to use new method getReadTagsEx for better performance in case of large tag population

            //TagData[] myTags = Reader.Actions.GetReadTags(100);

            //myTags = Reader.Actions.GetReadTags(100);

            //if (myTags != null)
            //{
            //   for (int index = 0; index < myTags.Length; index++)
            //   {
            //Log.Debug(TAG, "Tag ID " + myTags[index].TagID);
            //if (myTags[index].OpCode == ACCESS_OPERATION_CODE.AccessOperationRead && myTags[index].OpStatus == ACCESS_OPERATION_STATUS.AccessSuccess)
            //{
            //    if (myTags[index].MemoryBankData.Length > 0)
            //    {
            //        Log.Debug(TAG, " Mem Bank Data " + myTags[index].MemoryBankData);
            //    }
            //}



            //if (myTags[index].TagID == "305401B5F00134800000432800000032")
            //{
            //    //Reader.Config.DPOState = DYNAMIC_POWER_OPTIMIZATION.Disable;
            //    String tagId = "305401B5F00134800000432800000032";
            //    TagAccess tagAccess = new TagAccess();
            //    TagAccess.ReadAccessParams readAccessParams = new
            //    TagAccess.ReadAccessParams(tagAccess);
            //    TagData readAccessTag;
            //    readAccessParams.AccessPassword = 0;
            //    readAccessParams.Count = 4; // read 4 words
            //    readAccessParams.MemoryBank = MEMORY_BANK.MemoryBankUser;
            //    readAccessParams.Offset = 0; // start reading from word offset 0
            //    readAccessTag = Reader.Actions.TagAccess.ReadWait(tagId, readAccessParams, null);


            //    //Reader.Config.DPOState = DYNAMIC_POWER_OPTIMIZATION.Enable;
            //    int x = 0;
            //}

            //   }
            //}
        }

        // Status Event Notification
        public void EventStatusNotify(RfidStatusEvents rfidStatusEvents)
        {
            //Log.Debug(appTAG, "Status Notification: " + rfidStatusEvents.StatusEventData.StatusEventType);
            //if (rfidStatusEvents.StatusEventData.StatusEventType == STATUS_EVENT_TYPE.HandheldTriggerEvent)
            //{
            //    if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerPressed)
            //    {
            //        ThreadPool.QueueUserWorkItem(o =>
            //        {
            //            try
            //            {
            //                Reader.Actions.Inventory.Perform();
            //            }
            //            catch
            //            (InvalidUsageException e)
            //            {
            //                e.PrintStackTrace();
            //            }
            //            catch
            //            (OperationFailureException e)
            //            {
            //                e.PrintStackTrace();
            //            }
            //        });
            //    }
            //    if (rfidStatusEvents.StatusEventData.HandheldTriggerEventData.HandheldEvent == HANDHELD_TRIGGER_EVENT_TYPE.HandheldTriggerReleased)
            //    {
            //        ThreadPool.QueueUserWorkItem(o =>
            //        {
            //            try
            //            {
            //                Reader.Actions.Inventory.Stop();
            //            }
            //            catch
            //            (InvalidUsageException e)
            //            {
            //                e.PrintStackTrace();
            //            }
            //            catch
            //            (OperationFailureException e)
            //            {
            //                e.PrintStackTrace();
            //            }
            //        });
            //    }
            //}
        }
    }
}