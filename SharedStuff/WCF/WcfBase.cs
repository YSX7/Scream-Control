using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScreamControl.WCF
{
    #region Various
    public enum ConnectionClients { Client, Controller }

    [DataContract]
    public class AppSettingsProperty
    {
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string value { get; set; }
        [DataMember]
        public string type { get; set; }

        public AppSettingsProperty(string name, string value, string type)
        {
            this.name = name;
            this.value = value;
            this.type = type;
        }
    }
    #endregion

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class ServiceClient : IControllerService, IHostingClientService
    {
        static Action m_ConnectionChanged = delegate { };

        IControllerServiceCallback controllerCallback;
        IHostingClientServiceCallback clientCallback;

        #region Hosting client
        string IHostingClientService.Connect()
        {
            clientCallback = OperationContext.Current.GetCallbackChannel<IHostingClientServiceCallback>();
            m_ConnectionChanged += clientCallback.ConnectionChanged;
            if (controllerCallback != null)
                m_ConnectionChanged();
            return OperationContext.Current.Channel.State.ToString();
        }

        void IHostingClientService.Disconnect()
        {
            m_ConnectionChanged();
         //   clientCallback = null;
        }

        void IHostingClientService.SendMicInput(float volume)
        {
              controllerCallback?.VolumeReceive(volume);
        }

        void IHostingClientService.SendSettings(List<AppSettingsProperty> settings)
        {
            controllerCallback?.SettingsReceive(settings);
        }
        #endregion

        #region Controller
        void IControllerService.Connect()
        {
            controllerCallback = OperationContext.Current.GetCallbackChannel<IControllerServiceCallback>();
            m_ConnectionChanged += controllerCallback.ConnectionChanged;
            if (clientCallback != null)
                m_ConnectionChanged();
        }

        string IControllerService.DisconnectPrepare()
        {
            m_ConnectionChanged -= controllerCallback.ConnectionChanged;
            m_ConnectionChanged();
            //   OperationContext.Current.Channel.Close();
            controllerCallback = null;
            return "disconnected";
        }

        void IControllerService.Disconnect()
        {
            
        }

        void IControllerService.SendSettings(AppSettingsProperty value)
        {
            clientCallback?.SettingsReceiveAndApply(value);
        }
        #endregion
    }

    /// <summary>
    /// WCF service for controller
    /// </summary>
    public class EventServiceController: ClientBase<IControllerService>, IControllerService
    {
        public EventServiceController(InstanceContext context, Binding binding, EndpointAddress address)
            : base(context, binding, address)
        {

        }

        public void Connect()
        {
            base.Channel.Connect();
        }


        public void SendSettings(AppSettingsProperty value)
        {
            base.Channel.SendSettings(value);
        }

        public string DisconnectPrepare()
        {
            return base.Channel.DisconnectPrepare();
        }

        public void Disconnect()
        {
            base.Channel.Disconnect();
        }
    }

    /// <summary>
    /// WCF service for client who hosting wcf
    /// </summary>
    public class EventServiceHostingClient : ClientBase<IHostingClientService>, IHostingClientService
    {
        public EventServiceHostingClient(InstanceContext context, Binding binding, EndpointAddress address)
            : base(context, binding, address)
        {

        }

        public string Connect()
        {
            return base.Channel.Connect();
        }

        public void SendSettings(List<AppSettingsProperty> settings)
        {
            base.Channel.SendSettings(settings);
        }

        public void Disconnect()
        {
            base.Channel.Disconnect();
        }

        public void SendMicInput(float volume)
        {
            base.Channel.SendMicInput(volume);
        }
    }

}
