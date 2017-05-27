using System;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Collections.Generic;

namespace ScreamControl.WCF
{

    public class WcfScServiceHost
    {
        public class HostClient
        {
            public EventServiceHostingClient proxy;
            public bool _isControllerConnected = false;

            private List<AppSettingsProperty> _settingsToSerialize;

            #region Events
            public delegate void ControllerConnectionChangedHandler();
            public delegate void SettingReceiveHandler(AppSettingsProperty setting);
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

                public void AllConnected()
                {
                    _parent._isControllerConnected = true;

                    _parent.proxy.SendSettings(_parent._settingsToSerialize);

                    _parent.OnControllerConnected();
                }

                public void SettingsReceive(List<AppSettingsProperty> settings)
                {
                    return;
                }

                public void SettingsReceiveAndApply(AppSettingsProperty value)
                {
                    throw new NotImplementedException();
                  //  _parent.OnSettingReceive(settings);
                }

                public void VolumeReceive(float volume)
                {
                    return;
                }
            }

        }

        public HostClient client;

        private ServiceHost _serviceHost;

        public WcfScServiceHost(List<AppSettingsProperty> settings)
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

            client = new HostClient(baseAddress.Uri + "/client", binding);
        }

        public void Close()
        {
            _serviceHost.Close();
        }

    }

}
