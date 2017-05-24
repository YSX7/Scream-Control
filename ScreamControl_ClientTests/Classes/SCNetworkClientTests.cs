using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScreamControl.WCF;
using ScreamControl_Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading.Tasks;

namespace ScreamControl_Client.Tests
{
    [TestClass()]
    public class SCNetworkClientTests
    {
    

        [TestMethod()]
        public void SCNetworkClientTest()
        {
            //WcfScServiceHost scnet = new WcfScServiceHost();

            DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());

            Collection<EndpointDiscoveryMetadata> helloWorldServices = discoveryClient.Find(new FindCriteria(typeof(IWcfScDataTransferService))).Endpoints;

            discoveryClient.Close();

            if (helloWorldServices.Count == 0)
            {
                Console.WriteLine("No services");
                return;
            }
            else
            {
                EndpointAddress serviceAddress = helloWorldServices[0].Address;
                IWcfScDataTransferServiceCallback evnt = new MySubscriber();
                InstanceContext evntCntx = new InstanceContext(evnt);

                var binding = new NetTcpBinding();
                var factory = new DuplexChannelFactory<IWcfScDataTransferService>(evntCntx,binding);
                var channel = factory.CreateChannel(serviceAddress);

                string output = channel.Connect(ConnectionClients.Client);
                channel.SubscribeAllConnectedEvent();
               
                //EventServiceClient proxy = new EventServiceClient(evntCntx);
                //proxy.SubscribeAllConnectedEvent();
                //string output = proxy.Connect(ConnectionClients.Client);
                Console.WriteLine(output);

                //string result = channel.SayHello("Unit Test John");
                //Console.WriteLine(result);
            }

        }

        [TestMethod()]
        public void SCNetworkControllerTest()
        {
            //WcfScServiceHost scnet = new WcfScServiceHost();

            DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());

            Collection<EndpointDiscoveryMetadata> helloWorldServices = discoveryClient.Find(new FindCriteria(typeof(IWcfScDataTransferService))).Endpoints;

            discoveryClient.Close();

            if (helloWorldServices.Count == 0)
            {
                Console.WriteLine("No services");
                return;
            }
            else
            {
                IWcfScDataTransferServiceCallback evnt = new MySubscriber();
                InstanceContext evntCntx = new InstanceContext(evnt);
                EventServiceClient proxy = new EventServiceClient(evntCntx);
                proxy.SubscribeAllConnectedEvent();
                string output = proxy.Connect(ConnectionClients.Controller);
                Console.WriteLine(output);

                //EndpointAddress serviceAddress = helloWorldServices[0].Address;

                //var binding = new NetTcpBinding();
                //var factory = new ChannelFactory<IWcfScDataTransferService>(binding);
                //IWcfScDataTransferService channel = factory.CreateChannel(serviceAddress);
                //string result = channel.SayHello("Unit Test John");
                //Console.WriteLine(result);
            }

        }
    }
}