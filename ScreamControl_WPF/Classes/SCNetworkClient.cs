using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ScreamControl_Client
{


    public class SCNetworkClient
    {
        private int _localPort = 13642;
        private int _destPort = 13641;

        private readonly UdpClient _udp;
        IPEndPoint _receiveIP;

        private bool _connected = false;

        public SCNetworkClient()
        {
            _udp = new UdpClient(_localPort);
            _receiveIP = new IPEndPoint(IPAddress.Any, _localPort);
            this._udp.BeginReceive(EstablishConnectionCallback, new object());
        }

        private void EstablishConnectionCallback(IAsyncResult ar)
        {
            byte[] bytes = _udp.EndReceive(ar, ref _receiveIP);
            string message = Encoding.ASCII.GetString(bytes);
            if(message == StateEnum.Connecting)
            {

            }

        }

        private void ReceiveCallback(IAsyncResult ar)
        { 
            byte[] bytes = _udp.EndReceive(ar, ref _receiveIP);
            string message = Encoding.ASCII.GetString(bytes);

            System.Diagnostics.Debug.WriteLine(_receiveIP.Address.MapToIPv4().ToString());
            System.Diagnostics.Debug.WriteLine(message);
        }

        private static class StateEnum
        {
            public static string Connecting = "sc_c";
        }
    }

}
