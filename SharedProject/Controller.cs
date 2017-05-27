using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;

namespace ScreamControl.WCF
{
    [ServiceContract(CallbackContract = typeof(IControllerServiceCallback), SessionMode = SessionMode.Required)]
    interface IControllerService
    {
        [OperationContract(IsInitiating = true, IsOneWay = true)]
        void Connect();

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        void SendSettings(AppSettingsProperty value);

        [OperationContract(IsTerminating = true, IsOneWay = true)]
        void Disconnect();
    }

    interface IControllerServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void AllConnected();

        [OperationContract(IsOneWay = true)]
        void SettingsReceive(List<AppSettingsProperty> settings);

        [OperationContract(IsOneWay = true)]
        void VolumeReceive(float volume);
    }
}
