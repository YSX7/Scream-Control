using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            try
            {
                clientCallback = OperationContext.Current.GetCallbackChannel<IHostingClientServiceCallback>();
                m_ConnectionChanged += clientCallback.ConnectionChanged;
                if (controllerCallback != null)
                    m_ConnectionChanged();
            }
            catch(Exception e)
            {
                Trace.TraceInformation("[WCF] {0}", e);
            }
            return OperationContext.Current.Channel.State.ToString();
        }

        void IHostingClientService.Disconnect()
        {
            m_ConnectionChanged();
         //   clientCallback = null;
        }

        void IHostingClientService.SendMicInput(float volume)
        {
            try
            {
                controllerCallback?.VolumeReceive(volume);
            }
            catch (Exception e)
            {
                Trace.TraceInformation("[WCF] {0}", e);
            }
        }

        void IHostingClientService.SendSettings(List<AppSettingsProperty> settings)
        {
            try
            {
                controllerCallback?.SettingsReceive(settings);
            }
            catch (Exception e)
            {
                Trace.TraceInformation("[WCF] {0}", e);
            }
        }
        #endregion

        #region Controller
        void IControllerService.Connect()
        {
            try
            {
                controllerCallback = OperationContext.Current.GetCallbackChannel<IControllerServiceCallback>();
                m_ConnectionChanged += controllerCallback.ConnectionChanged;
                if (clientCallback != null)
                    m_ConnectionChanged();
            }
            catch (Exception e)
            {
                FaultContract fault = new FaultContract();

                fault.Result = false;
                fault.Message = e.Message;

                Trace.TraceError("[WCF] {0}", e);

                throw new FaultException<FaultContract>(fault);
            }
        }

        string IControllerService.DisconnectPrepare()
        {
            try
            {
                m_ConnectionChanged -= controllerCallback.ConnectionChanged;
                m_ConnectionChanged();
                //   OperationContext.Current.Channel.Close();
                controllerCallback = null;
                return "disconnected";
            }
            catch (Exception e)
            {
                FaultContract fault = new FaultContract();

                fault.Result = false;
                fault.Message = e.Message;

                Trace.TraceError("[WCF] {0}", e);

                throw new FaultException<FaultContract>(fault);
            }
        }

        void IControllerService.Disconnect()
        {
            
        }

        void IControllerService.SendSettings(AppSettingsProperty value)
        {
            try
            {
                clientCallback?.SettingsReceiveAndApply(value);
            }
            catch (Exception e)
            {
                FaultContract fault = new FaultContract();

                fault.Result = false;
                fault.Message = e.Message;

                Trace.TraceError("[WCF] {0}", e);

                throw new FaultException<FaultContract>(fault);
            }
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
