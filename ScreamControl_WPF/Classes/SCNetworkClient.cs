using System;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.ServiceModel.Description;

namespace ScreamControl_Client
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

    public class SCNetworkClient
    {

        public SCNetworkClient()
        {
            var baseAddress = new UriBuilder("http", System.Net.Dns.GetHostName(), 13640, "scwcf");

            ServiceHost serviceHost = new ServiceHost(typeof(HelloWorldService), baseAddress.Uri);
                serviceHost.AddServiceEndpoint(typeof(IHelloWorldService), new BasicHttpBinding(), string.Empty);

                serviceHost.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
                serviceHost.AddServiceEndpoint(new UdpDiscoveryEndpoint());

                serviceHost.Open();
        }

        //#region Variables and Stuff

        //private readonly string DEFAULT_CONNECT_MESSAGE = "SC_C";

        //private int _localPort = 13642;
        //private int _destPort = 13641;

        //private UdpClient _udpReceiver;
        //private UdpClient _udpSender;
        //private TcpClient _tcpClient;
        //IPEndPoint _receiveIP;
        //IPEndPoint _sendIP;

        //private bool _connected = false;

        //public class ReceivedMessageArgs : EventArgs
        //{

        //    public ReceivedMessageArgs(string message)
        //    {

        //    }
        //}
        //public delegate void MessageReceivedHandler(object sender, ReceivedMessageArgs args);
        //public event MessageReceivedHandler OnMessageReceived;

        //#endregion

        //private class ByteMessage
        //{
        //    public byte[] Data { get; set; }
        //}

        //[Serializable]
        //private class AppSettingsProperty
        //{
        //    public string name;
        //    public object value;
        //    public Type type;

        //    public AppSettingsProperty(string name, object value, Type type)
        //    {
        //        this.name = name;
        //        this.value = value;
        //        this.type = type;
        //    }
        //}

        //public SCNetworkClient()
        //{
        //    _udpReceiver = new UdpClient(_localPort);
        //    _receiveIP = new IPEndPoint(IPAddress.Any, _localPort);
        //    this._udpReceiver.BeginReceive(EstablishConnectionCallback, new object());
        //}

        //private void EstablishConnectionCallback(IAsyncResult ar)
        //{
        //    byte[] bytes = _udpReceiver.EndReceive(ar, ref _receiveIP);
        //    string message = Encoding.ASCII.GetString(bytes);
        //    if (message == DEFAULT_CONNECT_MESSAGE)
        //    {
        //        this._connected = true;

        //        List<AppSettingsProperty> settingsToSerialize = new List<AppSettingsProperty>();
        //        foreach(SettingsPropertyValue item in Properties.Settings.Default.PropertyValues)
        //        {
        //            var listItem = new AppSettingsProperty(item.Name, item.PropertyValue, item.Property.PropertyType);
        //            settingsToSerialize.Add(listItem);
        //        }
        //        ByteMessage settingsToSend = Serialize(settingsToSerialize);

        //        _sendIP = new IPEndPoint(_receiveIP.Address, _destPort);
        //        _tcpClient = new TcpClient();
        //        _tcpClient.Connect(_sendIP);

        //        _tcpClient.Client.BeginSend(settingsToSend.Data, 0, settingsToSend.Data.Length, SocketFlags.None, SettingsSended, null);
        //        //    _udpSender = new UdpClient(_destPort);
        //        //TODO: 1) передать данные о настройках 2) передавать данные с захвата голоса 3) ждать сохранения контроллера
        //        return;
        //    }
        //    else
        //        this._udpReceiver.BeginReceive(EstablishConnectionCallback, new object());
        //}

        //private void SettingsSended(IAsyncResult ar)
        //{
        //    _tcpClient.Client.EndSend(ar);

        //}

        //private void ReceiveCallback(IAsyncResult ar)
        //{
        //    byte[] bytes = _udpReceiver.EndReceive(ar, ref _receiveIP);
        //    string message = Encoding.ASCII.GetString(bytes);

        //    System.Diagnostics.Debug.WriteLine(_receiveIP.Address.MapToIPv4().ToString());
        //    System.Diagnostics.Debug.WriteLine(message);
        //    //  Properties.Settings.Default
        //}

        //private static ByteMessage Serialize(object anySerializableObject)
        //{
        //    using (var memoryStream = new MemoryStream())
        //    {
        //        (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
        //        return new ByteMessage { Data = memoryStream.ToArray() };
        //    }
        //}


        //private void SendCallback(IAsyncResult ar)
        //{

        //}


    }

}
