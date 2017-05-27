using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace ScreamControl.WCF
{
    [ServiceContract(CallbackContract = typeof(IHostingClientServiceCallback), SessionMode = SessionMode.Required)]
    public interface IHostingClientService
    {
        [OperationContract(IsInitiating = true)]
        string Connect();

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        void SendSettings(List<AppSettingsProperty> settings);

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        void SendMicInput(float volume);

        [OperationContract(IsTerminating = true, IsOneWay = true)]
        void Disconnect();
    }

    interface IHostingClientServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void AllConnected();

        [OperationContract(IsOneWay = true)]
        void SettingsReceiveAndApply(AppSettingsProperty value);

        [OperationContract(IsOneWay = true)]
        void VolumeReceive(float volume);
    }
}
