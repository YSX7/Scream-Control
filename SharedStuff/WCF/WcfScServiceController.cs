using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ScreamControl.WCF
{
    public class WcfScServiceController
    {
        private readonly string[] _ignoredProperties = new[] { "CurrentConnectionState", "VolumeBarBrush", "SoundTimerValue", "OverlayTimerValue", "SoundAlertTimerBrush",
                "OverlayAlertTimerBrush", "CloseTrigger", "CurrentConnectionState", "IsControlsBlocked", "MicVolume" };

        private Thread _broadcastSearch;

        public EventServiceController proxy;
        public bool _isClientConnected = false;

        #region Events
        public delegate void ClientConnectionChangedHandler();
        public delegate void VolumeReceivedHandler(float volume);
        public delegate void SettingReceiveHandler(List<AppSettingsProperty> settings);
        public event ClientConnectionChangedHandler OnClientConnected;
        public event SettingReceiveHandler OnSettingReceive;
        public event VolumeReceivedHandler OnVolumeReceive;
        public event ClientConnectionChangedHandler OnConnectionFailed;
        public event ClientConnectionChangedHandler OnClientDisconnected;
        #endregion

        public string temp;
        public WcfScServiceController()
        {
            
        }

        public void Connect()
        {
            //Discover WCF service via broadcasting
            _broadcastSearch = new Thread(() => {
                DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());
                Collection<EndpointDiscoveryMetadata> helloWorldServices = discoveryClient.Find(new FindCriteria(typeof(IControllerService))).Endpoints;
                discoveryClient.Close();

                if (helloWorldServices.Count == 0)
                {
                    OnConnectionFailed();
                }
                else
                {
                    EndpointAddress serviceAddress = helloWorldServices[0].Address;
                    NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                    binding.ReliableSession.Enabled = true;
                    binding.ReliableSession.Ordered = false;

                    IControllerServiceCallback evnt = new ControllerCallback(this);
                    InstanceContext evntCntx = new InstanceContext(evnt);

                    proxy = new EventServiceController(evntCntx, binding, serviceAddress);

                    proxy.Connect();
                }
            });

            _broadcastSearch.Start();      
        }

        public void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_ignoredProperties.Contains(e.PropertyName))
                return;
            PropertyInfo property = sender.GetType().GetProperty(e.PropertyName);
            object value = property.GetValue(sender);
            AppSettingsProperty setting = new AppSettingsProperty(e.PropertyName, value.ToString(), property.PropertyType.FullName);
            if(_isClientConnected)
                this.proxy.SendSettings(setting);
        }

        public void Close()
        {
            if (_broadcastSearch.IsAlive)
                _broadcastSearch.Abort();
            if (proxy == null)
                return;
            proxy.DisconnectPrepare();
            proxy.Close();
            proxy = null;
        }

        private class ControllerCallback : IControllerServiceCallback
        {
            WcfScServiceController _parent;

            public ControllerCallback(WcfScServiceController parent)
            {
                this._parent = parent;
            }

            public void ConnectionChanged()
            {
                _parent._isClientConnected = !_parent._isClientConnected;

                if (_parent._isClientConnected)
                {

                    _parent.OnClientConnected();
                }
                else
                {
                    _parent.OnClientDisconnected();
                }
            }

            public void SettingsReceive(List<AppSettingsProperty> settings)
            {
                _parent.OnSettingReceive(settings);
            }

            public void VolumeReceive(float volume)
            {
                _parent.OnVolumeReceive(volume);
            }
        }
    }
}
