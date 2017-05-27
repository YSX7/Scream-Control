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

        [OperationContract(IsOneWay = true, Name = "SendSettingsMultiple")]
        void SendSettings(List<AppSettingsProperty> settings);

        [OperationContract(IsOneWay = true, Name = "SendSettingsSingle")]
        void SendSettings(AppSettingsProperty value);

        [OperationContract(IsOneWay = true)]
        void SendMicInput(float volume);

        [OperationContract(IsTerminating = true)]
        void Disconnect(ConnectionClients client);
    }

    public interface IWcfScDataTransferServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void AllConnected();

        [OperationContract(IsOneWay = true, Name = "SettingReceiveMultiple")]
        void SettingsReceive(List <AppSettingsProperty> settings);

        [OperationContract(IsOneWay = true, Name = "SettingReceiveSingle")]
        void SettingsReceive(AppSettingsProperty value);

        [OperationContract(IsOneWay = true)]
        void VolumeReceive(float volume);
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

        public void SendSettings(AppSettingsProperty value)
        {
            base.Channel.SendSettings(value);
        }

        public void Disconnect(ConnectionClients client)
        {
            base.Channel.Disconnect(client);
        }

        public void SendMicInput(float volume)
        {
            base.Channel.SendMicInput(volume);
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    public class WcfScDataTransferService : IWcfScDataTransferService
    {
        static Action m_EventConnected = delegate { };

        IWcfScDataTransferServiceCallback controllerCallback;
        IWcfScDataTransferServiceCallback clientCallback;

        //public void SubscribeAllConnectedEvent()
        //{
        //    IWcfScDataTransferServiceCallback subscriber = OperationContext.Current.GetCallbackChannel<IWcfScDataTransferServiceCallback>();
        //    m_EventConnected += subscriber.AllConnected;
        //}

        public string Connect(ConnectionClients client)
        {
            //throw new FaultException("Something happend");

            if (client == ConnectionClients.Client)
            {
                if (clientCallback != null) return string.Format("Client already connected");
                else
                {
                    clientCallback = OperationContext.Current.GetCallbackChannel<IWcfScDataTransferServiceCallback>();
                    m_EventConnected += clientCallback.AllConnected;
                }
            }
            if (client == ConnectionClients.Controller)
            {
                if (controllerCallback != null) return string.Format("Controller already connected");
                else
                {
                    controllerCallback = OperationContext.Current.GetCallbackChannel<IWcfScDataTransferServiceCallback>();
                }
            }
            if (clientCallback != null && controllerCallback != null)
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

        public void SendSettings(AppSettingsProperty value)
        {
            clientCallback.SettingsReceive(value);
        }

        public void Disconnect(ConnectionClients client)
        {
            if (client == ConnectionClients.Client)
            {
                clientCallback = null;
            }
            if (client == ConnectionClients.Controller)
            {
                controllerCallback = null;
            }
        }

        public void SendMicInput(float volume)
        {
            controllerCallback.VolumeReceive(volume);
        }
    }

    [DataContract]
    public class ListOfSettingsValues
    {
        [DataMember]
        List<AppSettingsProperty> Data { get; set; }
    }

}
