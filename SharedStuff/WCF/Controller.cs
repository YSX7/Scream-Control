using System.Collections.Generic;
using System.ServiceModel;

namespace ScreamControl.WCF
{
    [ServiceContract(CallbackContract = typeof(IControllerServiceCallback), SessionMode = SessionMode.Required)]
    public interface IControllerService
    {
        [OperationContract(IsInitiating = true, IsOneWay = true)]
        [FaultContract(typeof(FaultContract))]
        void Connect();

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        [FaultContract(typeof(FaultContract))]
        void SendSettings(AppSettingsProperty value);

        [OperationContract(IsInitiating = false, IsOneWay = false)]
        [FaultContract(typeof(FaultContract))]
        string DisconnectPrepare();

        [OperationContract(IsTerminating = true, IsInitiating = false, IsOneWay = true)]
        [FaultContract(typeof(FaultContract))]
        void Disconnect();
    }

    public interface IControllerServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void ConnectionChanged();

        [OperationContract(IsOneWay = true)]
        void SettingsReceive(List<AppSettingsProperty> settings);

        [OperationContract(IsOneWay = true)]
        void VolumeReceive(float volume);
    }
}
