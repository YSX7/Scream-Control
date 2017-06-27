using ScreamControl.WCF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ScreamControl.WCF
{
    public class WcfScServiceClient
    {

        public EventServiceHostingClient proxy;
        public bool _isControllerConnected = false;

        #region Events
        public delegate void RequestCurrentSettingsHandler(ref List<AppSettingsProperty> settings);
        public delegate void ControllerConnectionChangedHandler();
        public delegate void SettingReceiveHandler(AppSettingsProperty setting);
        public event RequestCurrentSettingsHandler OnRequestCurrentSettings;
        public event ControllerConnectionChangedHandler OnControllerConnected;
        public event SettingReceiveHandler OnSettingReceive;
        public event ControllerConnectionChangedHandler OnControllerDisconnected;
        #endregion

        public string temp;

        public WcfScServiceClient(string baseAddress, NetTcpBinding binding)
        {
            EndpointAddress serviceAddress = new EndpointAddress(baseAddress);

            IHostingClientServiceCallback evnt = new ClientCallback(this);
            InstanceContext evntCntx = new InstanceContext(evnt);

            proxy = new EventServiceHostingClient(evntCntx, binding, serviceAddress);

            temp = proxy.Connect();
        }

        private class ClientCallback : IHostingClientServiceCallback
        {
            WcfScServiceClient _parent;

            public ClientCallback(WcfScServiceClient parent)
            {
                this._parent = parent;
            }

            public void ConnectionChanged()
            {
                    _parent._isControllerConnected = !_parent._isControllerConnected;

                if (_parent._isControllerConnected)
                {
                    List<AppSettingsProperty> settingsToSerialize = new List<AppSettingsProperty>();
                    _parent.OnRequestCurrentSettings(ref settingsToSerialize);

                    _parent.proxy.SendSettings(settingsToSerialize);

                    _parent.OnControllerConnected();
                }
                else
                {
                    _parent.OnControllerDisconnected();
                }
            }

            public void SettingsReceiveAndApply(AppSettingsProperty value)
            {
                _parent.OnSettingReceive(value);
            }

            //public void VolumeReceive(float volume)
            //{
            //    return;
            //}
        }

    }
}
