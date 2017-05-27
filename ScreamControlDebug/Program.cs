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
using System.Linq;

namespace MicrophoneTest
{

    class Program
    {

        readonly string[] YES_VARIANTS = { "yes", "y" };

        class ControllerSubscriber : IWcfScDataTransferServiceCallback
        {
            public void AllConnected()
            {
                return;
            }

            public void SettingsReceive(List<AppSettingsProperty> settings)
            {
                foreach (var item in settings)
                    Console.WriteLine("{0} \t {1} \t {2}", item.name, item.value, item.type);
            }

            public void SettingsReceive(AppSettingsProperty value)
            {
                return;
            }

            public void VolumeReceive(float volume)
            {
                //Console.WriteLine(volume);
                //Console.CursorTop--;
            }
        }

        static void Main(string[] args)
        {
            new Program().Run();
        }

        EventServiceClient proxy;

        public void Run()
        {
            try
            {

           //     AppSettingsProperty hui = new AppSettingsProperty("Threshold", "", typeof(float).);

                Console.WriteLine("Searching for service...");

                DiscoveryClient discoveryClient = new DiscoveryClient(new UdpDiscoveryEndpoint());

                Collection<EndpointDiscoveryMetadata> helloWorldServices = discoveryClient.Find(new FindCriteria(typeof(IWcfScDataTransferService))).Endpoints;

                discoveryClient.Close();

                if (helloWorldServices.Count == 0)
                {
                    Console.WriteLine("No services. Try again? (y/n)");
                    var input = Console.ReadKey().KeyChar.ToString();
                    if (YES_VARIANTS.Any(x => x == input))
                        new Program().Run();
                }
                else
                {
                    Console.WriteLine("Something finded, connecting...");
                    EndpointAddress serviceAddress = helloWorldServices[0].Address;

                    IWcfScDataTransferServiceCallback evnt = new ControllerSubscriber();
                    InstanceContext evntCntx = new InstanceContext(evnt);

                    NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                    binding.ReliableSession.Enabled = true;
                    binding.ReliableSession.Ordered = false;

                    proxy = new EventServiceClient(evntCntx, binding, serviceAddress);

                    string output = proxy.Connect(ConnectionClients.Controller);

                    AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

                    Console.WriteLine(output);

                    string tmp = "";
                    Console.Write(Environment.NewLine + "Enter Alarm Threshold: ");

                    AppSettingsProperty setting = new AppSettingsProperty("Threshold", "", typeof(float).FullName);
                    while (tmp.ToLower() != "exit")
                    {
                        tmp = Console.ReadLine();
                        setting.value = tmp;
                        proxy.SendSettings(setting);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
            }
        }


        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Console.WriteLine("Exiting...");
            if (proxy != null)
            {
                proxy.Disconnect(ConnectionClients.Controller);
            }
        }
    }
}