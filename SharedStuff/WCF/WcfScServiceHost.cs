using System;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Collections.Generic;
using System.Configuration;

namespace ScreamControl.WCF.Host
{

    public class WcfScServiceHost
    {
        public HostClient Client { get; set; }

        private ServiceHost _serviceHost;

        public WcfScServiceHost()
        {
            //    this._settingsToSerialize = settings;

            var baseAddress = new UriBuilder("net.tcp", System.Net.Dns.GetHostName(), 13640, "wcf");

            _serviceHost = new ServiceHost(typeof(ServiceClient));
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
            binding.ReliableSession.Enabled = true;
            binding.ReliableSession.Ordered = false;
            _serviceHost.AddServiceEndpoint(typeof(IHostingClientService), binding, baseAddress.Uri + "/client");
            _serviceHost.AddServiceEndpoint(typeof(IControllerService), binding, baseAddress.Uri + "/controller");

            _serviceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            _serviceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());

            _serviceHost.Open();

            Client = new HostClient(baseAddress.Uri + "/client", binding);
        }

        public void Close()
        {
            Client.proxy.Close();
            _serviceHost.Close();
        }

        public class HostClient
        {
            public EventServiceHostingClient proxy;
            public bool _isControllerConnected = false;

            #region Events
            public delegate void RequestCurrentSettingsHandler(ref List<AppSettingsProperty> settings);
            public delegate void ControllerConnectionChangedHandler();
            public delegate void SettingReceiveHandler(AppSettingsProperty setting);
            public event RequestCurrentSettingsHandler OnRequestCurrentSettings;
            public event ControllerConnectionChangedHandler OnControllerConnected;
            public event SettingReceiveHandler OnSettingReceive;
            public event ControllerConnectionChangedHandler OnControllerDisconnected;
            #endregion

            public string temp;

            public HostClient(string baseAddress, NetTcpBinding binding)
            {
                EndpointAddress serviceAddress = new EndpointAddress(baseAddress);

                IHostingClientServiceCallback evnt = new MyCallback(this);
                InstanceContext evntCntx = new InstanceContext(evnt);

                proxy = new EventServiceHostingClient(evntCntx, binding, serviceAddress);

                temp = proxy.Connect();
            }

            private class MyCallback : IHostingClientServiceCallback
            {
                HostClient _parent;

                public MyCallback(HostClient parent)
                {
                    this._parent = parent;
                }

                public void ConnectionChanged()
                {
                    _parent._isControllerConnected = !_parent._isControllerConnected;

                    if (_parent._isControllerConnected)
                    {
                        List<AppSettingsProperty> settingsToSerialize = new List<AppSettingsProperty>();
                        _parent.OnRequestCurrentSettings(ref settingsToSerialize);

                        _parent.proxy.SendSettings(settingsToSerialize);

                        _parent.OnControllerConnected();
                    }
                    else
                    {
                        _parent.OnControllerDisconnected();
                    }
                }

                public void SettingsReceiveAndApply(AppSettingsProperty value)
                {
                    _parent.OnSettingReceive(value);
                }

                public void VolumeReceive(float volume)
                {
                    return;
                }
            }

        }

    }

}
