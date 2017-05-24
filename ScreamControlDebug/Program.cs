using ScreamControl.WCF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading;

namespace MicrophoneTest
{

    class Program
    {

        class ControllerSubscriber : IWcfScDataTransferServiceCallback
        {
            public void AllConnected()
            {
                return;
            }

            public void SettingsReceive(List<AppSettingsProperty> settings)
            {
                foreach (var item in settings)
                    Console.WriteLine("{0} :: {1} :: {2}", item.name, item.value, item.type);
            }
        }

        static void Main(string[] args)
        {
            new Program().Run();
        }

        public void Run()
        {
            
            Console.WriteLine("Starting service.");

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

                IWcfScDataTransferServiceCallback evnt = new ControllerSubscriber();
                InstanceContext evntCntx = new InstanceContext(evnt);

                //    var binding = new NetTcpBinding();
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                binding.ReceiveTimeout = TimeSpan.FromSeconds(60);

                EventServiceClient proxy = new EventServiceClient(evntCntx, binding, serviceAddress);

                string output = proxy.Connect(ConnectionClients.Controller);
                //proxy.SubscribeAllConnectedEvent();
                
                Console.WriteLine(output);

                //IWcfScDataTransferServiceCallback evnt = new MySubscriber();
                //InstanceContext evntCntx = new InstanceContext(evnt);
                //EventServiceClient proxy = new EventServiceClient(evntCntx);
                //proxy.SubscribeAllConnectedEvent();
                //string output = proxy.Connect(ConnectionClients.Controller);
              

                //EndpointAddress serviceAddress = helloWorldServices[0].Address;

                //var binding = new NetTcpBinding();
                //var factory = new ChannelFactory<IWcfScDataTransferService>(binding);
                //IWcfScDataTransferService channel = factory.CreateChannel(serviceAddress);
                //string result = channel.SayHello("Unit Test John");
                //Console.WriteLine(result);
            }

            string tmp = "";

            while (tmp.ToLower() != "exit")
            {
                Console.Write("Enter Something: ");
                tmp = Console.ReadLine();
            }

            
        }
    }
}