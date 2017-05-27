using System;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Collections.Generic;

namespace ScreamControl.WCF
{

    public class WcfScServiceHost
    {
        public EventServiceClient proxy;
        public bool IsControllerConnected = false;

        #region Events
        public delegate void ControllerConnectionChangedHandler();
        public delegate void SettingReceiveHandler(AppSettingsProperty setting);
        public event ControllerConnectionChangedHandler OnControllerConnected;
        public event SettingReceiveHandler OnSettingReceive;
        public event ControllerConnectionChangedHandler OnControllerDisconnected;
        #endregion

        private static List<AppSettingsProperty> settingsToSerialize;

        private class MySubscriber : IWcfScDataTransferServiceCallback
        {
            WcfScServiceHost parent;

            public MySubscriber(WcfScServiceHost parent)
            {
                this.parent = parent;
            }
            
            public void AllConnected()
            {
                parent.IsControllerConnected = true;

                parent.proxy.SendSettings(settingsToSerialize);

                parent.OnControllerConnected();
            }

            public void SettingsReceive(List<AppSettingsProperty> settings)
            {
                return;
            }

            public void SettingsReceive(AppSettingsProperty settings)
            {
                parent.OnSettingReceive(settings);
            }

            public void VolumeReceive(float volume)
            {
                return;
            }
        }

        private ServiceHost serviceHost;

        public WcfScServiceHost(List<AppSettingsProperty> settings)
        {
            settingsToSerialize = settings;

            var baseAddress = new UriBuilder("net.tcp", System.Net.Dns.GetHostName(), 13640, "scwcf");

            serviceHost = new ServiceHost(typeof(WcfScDataTransferService));
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
            binding.ReliableSession.Enabled = true;
            binding.ReliableSession.Ordered = false;
            serviceHost.AddServiceEndpoint(typeof(IWcfScDataTransferService), binding, baseAddress.Uri);

            serviceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            serviceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());

            serviceHost.Open();

            EndpointAddress serviceAddress = new EndpointAddress(baseAddress.Uri);

            IWcfScDataTransferServiceCallback evnt = new MySubscriber(this);
            InstanceContext evntCntx = new InstanceContext(evnt);

            proxy = new EventServiceClient(evntCntx, binding, serviceAddress);

            var output = proxy.Connect(ConnectionClients.Client);

            output = "";


        }

        public void Close()
        {
            serviceHost.Close();
        }

    }

}
