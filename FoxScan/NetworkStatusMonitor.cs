using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace FoxScan
{
    public class NetworkStatusMonitor
    {
        // *Note: Use of this class requires you add -> <uses-permission  android:name="android.permission.ACCESS_NETWORK_STATE"/>
        //        to AndoirdManifest.xml

        private NetworkState _state;
        private string _networkName = "";

        public NetworkStatusMonitor()
        {
        }

        public NetworkState State
        {
            get
            {
                UpdateNetworkStatus();

                return _state;
            }
        }

        public string NetworkName
        {
            get
            {
                return _networkName;
            }
        }

        public void UpdateNetworkStatus()
        {
            _state = NetworkState.Unknown;

            // Retrieve the connectivity manager service
            var connectivityManager = (ConnectivityManager)
                Application.Context.GetSystemService(
                    Context.ConnectivityService);

            // Check if the network is connected or connecting.
            // This means that it will be available, 
            // or become available in a few seconds.
            var activeNetworkInfo = connectivityManager.ActiveNetworkInfo;
            if (activeNetworkInfo != null)
            {
                if (activeNetworkInfo.IsConnectedOrConnecting)
                {
                    // Now that we know it's connected, determine if we're on WiFi or something else.
                    _state = activeNetworkInfo.Type == ConnectivityType.Wifi ?
                        NetworkState.ConnectedWifi : NetworkState.ConnectedData;

                }
                else
                {
                    _state = NetworkState.Disconnected;
                }
            }
            else
            {
                _state = NetworkState.Disconnected;
            }
        }


    }

    public enum NetworkState
    {
        Unknown,
        ConnectedWifi,
        ConnectedData,
        Disconnected
    }
}