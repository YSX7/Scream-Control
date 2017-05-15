using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        [ServiceContract]
        public interface IHelloWorldService
        {
            [OperationContract]
            string SayHello(string name);
        }

        public class HelloWorldService : IHelloWorldService
        {
            public string SayHello(string name)
            {
                return string.Format("Hello from WCF service, {0}", name);
            }
        }

        [TestMethod()]
        public void SCNetworkClientTest()
        {
            SCNetworkClient scnet = new SCNetworkClient();

            DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());

            Collection<EndpointDiscoveryMetadata> helloWorldServices = discoveryClient.Find(new FindCriteria(typeof(IHelloWorldService))).Endpoints;

            discoveryClient.Close();

            if (helloWorldServices.Count == 0)
            {
                return;
            }
            else
            {
                EndpointAddress serviceAddress = helloWorldServices[0].Address;

                var binding = new BasicHttpBinding();
                var factory = new ChannelFactory<IHelloWorldService>(binding);
                IHelloWorldService channel =  factory.CreateChannel(serviceAddress);
                string result = channel.SayHello("Pidr");
                Console.WriteLine(result);
            }

        }
    }
}