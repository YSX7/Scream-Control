using System.Collections.Generic;
using System.ServiceModel;

namespace ScreamControl.WCF
{
    [ServiceContract(CallbackContract = typeof(IControllerServiceCallback), SessionMode = SessionMode.Required)]
    public interface IControllerService
    {
        [OperationContract(IsInitiating = true, IsOneWay = true)]
        void Connect();

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        void SendSettings(AppSettingsProperty value);

        [OperationContract(IsInitiating = false, IsOneWay = false)]
        string DisconnectPrepare();

        [OperationContract(IsTerminating = true, IsInitiating = false, IsOneWay = true)]
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
