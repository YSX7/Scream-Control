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

        private int _localPort = 13642;
        private int _destPort = 13641;

        private UdpClient _udpReceiver;
        private UdpClient _udpSender;
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
        public SCNetworkClient()
        {
            _udpReceiver = new UdpClient(_localPort);
            _receiveIP = new IPEndPoint(IPAddress.Any, _localPort);
            this._udpReceiver.BeginReceive(EstablishConnectionCallback, new object());
        }

        private void EstablishConnectionCallback(IAsyncResult ar)
        {
            //byte[] bytes = _udpReceiver.EndReceive(ar, ref _receiveIP);
            //string message = Encoding.ASCII.GetString(bytes);
            //if(message == StateEnum.Connecting)
            //{
            //    this._connected = true;

            //    _udpSender = new UdpClient(_destPort);
            //    _sendIP = new IPEndPoint(_receiveIP.Address, _destPort);
            //    _udpSender.Connect(_sendIP);

            //    Stream stream = _udp
            //    BinaryFormatter formatter = new BinaryFormatter();
            //    Encoding.ASCII.GetBytes()
            //    byte[] bytes = Encoding.
            //    Properties.Settings.Default.Properties;
            //      _udpSender.BeginSend()
            //    //TODO: 1) передать данные о настройках 2) передавать данные с захвата голоса 3) ждать сохранения контроллера
            //    return;
            //}
            //this._udpReceiver.BeginReceive(EstablishConnectionCallback, new object());
        }

        private void ReceiveCallback(IAsyncResult ar)
        { 
            byte[] bytes = _udpReceiver.EndReceive(ar, ref _receiveIP);
            string message = Encoding.ASCII.GetString(bytes);

            System.Diagnostics.Debug.WriteLine(_receiveIP.Address.MapToIPv4().ToString());
            System.Diagnostics.Debug.WriteLine(message);
        }

        private void SendCallback(IAsyncResult ar)
        {

        }

        private static class StateEnum
        {
            public static string Connecting = "SC_C";
        }
    }

}
