using System;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Collections.Generic;
using System.Configuration;

namespace ScreamControl.WCF
{

    public class WcfScServiceHost
    {
        public WcfScServiceClient Client { get; set; }

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

            Client = new WcfScServiceClient(baseAddress.Uri + "/client", binding);
        }

        public void Close()
        {
            Client.proxy.Close();
            _serviceHost.Close();
        }

        

    }

}
