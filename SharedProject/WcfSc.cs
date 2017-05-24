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
    public enum ConnectionClients { Client, Controller }

    [ServiceContract(CallbackContract = typeof(IWcfScDataTransferServiceCallback), SessionMode = SessionMode.Required)]
    public interface IWcfScDataTransferService
    {
        [OperationContract(IsInitiating = true)]
        string Connect(ConnectionClients client);

        //[OperationContract(IsOneWay = true)]
        //void SubscribeAllConnectedEvent();

        [OperationContract(IsOneWay = true)]
        void SendSettings(List<AppSettingsProperty> settings);

        [OperationContract(IsTerminating = true)]
        void Disconnect(ConnectionClients client);
    }

    public interface IWcfScDataTransferServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void AllConnected();

        [OperationContract(IsOneWay = true)]
        void SettingsReceive(List <AppSettingsProperty> settings);
    }

    [DataContract]
    //[KnownType(typeof(List<AppSettingsProperty>))]
    //[KnownType(typeof(Type))]
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

    public class EventServiceClient : ClientBase<IWcfScDataTransferService>, IWcfScDataTransferService
    {
        public EventServiceClient(InstanceContext context, Binding binding, EndpointAddress address)
            : base(context, binding, address)
        {

        }

        public string Connect(ConnectionClients client)
        {
            return base.Channel.Connect(client);
        }


        //public void SubscribeAllConnectedEvent()
        //{
        //    base.Channel.SubscribeAllConnectedEvent();
        //}

        public void SendSettings(List<AppSettingsProperty> settings)
        {
            base.Channel.SendSettings(settings);
        }

        public void Disconnect(ConnectionClients client)
        {
            base.Channel.Disconnect(client);
        }

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class WcfScDataTransferService : IWcfScDataTransferService
    {
        static Action m_EventConnected = delegate { };

        public bool SCclientConnected = false;
        public bool SCcontrollerConnected = false;
        IWcfScDataTransferServiceCallback controllerCallback;

        //public void SubscribeAllConnectedEvent()
        //{
        //    IWcfScDataTransferServiceCallback subscriber = OperationContext.Current.GetCallbackChannel<IWcfScDataTransferServiceCallback>();
        //    m_EventConnected += subscriber.AllConnected;
        //}

        public string Connect(ConnectionClients client)
        {
            //throw new FaultException("Something happend");
            IWcfScDataTransferServiceCallback subscriber = OperationContext.Current.GetCallbackChannel<IWcfScDataTransferServiceCallback>();
            m_EventConnected += subscriber.AllConnected;

            if (client == ConnectionClients.Client)
            {
                if (SCclientConnected) return string.Format("Client already connected");
                else SCclientConnected = true;
            }
            if (client == ConnectionClients.Controller)
            {
                if (controllerCallback != null) return string.Format("Controller already connected");
                else
                {
                    controllerCallback = OperationContext.Current.GetCallbackChannel<IWcfScDataTransferServiceCallback>();
                }
            }
            if (SCclientConnected && controllerCallback!= null)
            {
                m_EventConnected();
                return ("All connected, starting data transfer...");
            }
            else
                return string.Format("You connected, {0} [{1}]", client.ToString(), "placeholder");
        }

        public void SendSettings(List<AppSettingsProperty> settings)
        {
            controllerCallback.SettingsReceive(settings);
        }


        public void Disconnect(ConnectionClients client)
        {
            if (client == ConnectionClients.Client)
            {
                SCclientConnected = false;
            }
            if (client == ConnectionClients.Controller)
            {
                SCcontrollerConnected = false;
            }
        }
    }

    [DataContract]
    public class ListOfSettingsValues
    {
        [DataMember]
        List<AppSettingsProperty> Data { get; set; }
    }

}
