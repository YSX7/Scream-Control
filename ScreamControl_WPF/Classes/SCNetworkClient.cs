using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ScreamControl_Client
{

    public class SCNetworkClient
    {
        #region Variables and Stuff

        private readonly string DEFAULT_CONNECT_MESSAGE = "SC_C";

        private int _localPort = 13642;
        private int _destPort = 13641;

        private UdpClient _udpReceiver;
        private UdpClient _udpSender;
        private TcpClient _tcpClient;
        IPEndPoint _receiveIP;
        IPEndPoint _sendIP;

        private bool _connected = false;

        public class ReceivedMessageArgs : EventArgs
        {

            public ReceivedMessageArgs(string message)
            {

            }
        }
        public delegate void MessageReceivedHandler(object sender, ReceivedMessageArgs args);
        public event MessageReceivedHandler OnMessageReceived;

        #endregion

        private class ByteMessage
        {
            public byte[] Data { get; set; }
        }

        public SCNetworkClient()
        {
            _udpReceiver = new UdpClient(_localPort);
            _receiveIP = new IPEndPoint(IPAddress.Any, _localPort);
            this._udpReceiver.BeginReceive(EstablishConnectionCallback, new object());
        }

        private void EstablishConnectionCallback(IAsyncResult ar)
        {
            byte[] bytes = _udpReceiver.EndReceive(ar, ref _receiveIP);
            string message = Encoding.ASCII.GetString(bytes);
            if (message == DEFAULT_CONNECT_MESSAGE)
            {
                this._connected = true;

                ByteMessage settingsToSend = Serialize(Properties.Settings.Default.PropertyValues);

                _udpSender = new UdpClient(_destPort);
                _sendIP = new IPEndPoint(_receiveIP.Address, _destPort);
                _udpSender.Connect(_sendIP);

                //TODO: 1) передать данные о настройках 2) передавать данные с захвата голоса 3) ждать сохранения контроллера
                return;
            }
            else
             this._udpReceiver.BeginReceive(EstablishConnectionCallback, new object());
        }

        private void ReceiveCallback(IAsyncResult ar)
        { 
            byte[] bytes = _udpReceiver.EndReceive(ar, ref _receiveIP);
            string message = Encoding.ASCII.GetString(bytes);

            System.Diagnostics.Debug.WriteLine(_receiveIP.Address.MapToIPv4().ToString());
            System.Diagnostics.Debug.WriteLine(message);
            Properties.Settings.Default
        }

        private static ByteMessage Serialize(object anySerializableObject)
        {
            using (var memoryStream = new MemoryStream())
            {
                (new BinaryFormatter()).Serialize(memoryStream, anySerializableObject);
                return new ByteMessage { Data = memoryStream.ToArray() };
            }
        }


        private void SendCallback(IAsyncResult ar)
        {

        }

    }

}
