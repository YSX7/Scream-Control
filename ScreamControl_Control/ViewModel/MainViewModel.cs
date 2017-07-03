using ScreamControl.WCF;
using System;
using ScreamControl.ViewModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;
using ScreamControl.Alarms;
using System.Diagnostics;
using System.Threading;

namespace ScreamControl.Controller.ViewModel
{
    class MainViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members
        protected void RaisePropertyChanged(string p)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(p));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        //public MainModel mainModel;

        #region Constructor
        public MainViewModel()
        {
            LoadedCommand = new Command(arg => LoadedMethod());
            ReconnectCommand = new Command(arg => ReconnectMethod());

            //  this.mainModel = new MainModel();

            CurrentLanguage = App.Language;
            Languages = new ObservableCollection<CultureInfo>(App.Languages);
        }
        #endregion

        #region Constants
        private readonly Brush DEFAULT_NORMAL_BRUSH = Brushes.White;
        private readonly Brush DEFAULT_ALERT_GOES_OFF_BRUSH = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffff00"));
        #endregion

        #region Fields

        WcfScServiceController _WcfController;
        AlarmSystem _alarmSystem;

        private float _micVolume;

        /// <summary>
        /// Current volume brush
        /// </summary>
        private Brush _volumeBarBrush;

        /// <summary>
        /// Window closing trigger
        /// </summary>
        private bool _closeTrigger;

        private int _soundTimerValue = 0;
        private int _overlayTimerValue = 0;

        private Brush _soundAlertTimerBrush;
        private Brush _overlayAlertTimerBrush;

        private ConnectionInfoStates _currentConnectionState;
        #endregion

        #region Properties

        public Command LoadedCommand { get; private set; }
        public Command ReconnectCommand { get; private set; }

        /// <summary>
        /// Get prefix for localizable connection strings
        /// </summary>
        public string ConnectionInfoStringsPrefix
        { get { return "mh_"; } }

        /// <summary>
        /// Get or set Stealth Mode bool
        /// </summary>
        public bool IsStealthMode
        {
            get
            {
                return Properties.Settings.Default.IsStealthMode;
            }
            set
            {
                Properties.Settings.Default.IsStealthMode = value;
                RaisePropertyChanged("IsStealthMode");
            }
        }

        /// <summary>
        /// Get visible window visibilty
        /// </summary>
        public Visibility WindowVisibilityState
        {
            get; set;
        }

        /// <summary>
        /// Get or set available languages
        /// </summary>
        public ObservableCollection<CultureInfo> Languages { get; set; }

        /// <summary>
        /// Get or set selected language
        /// </summary>
        public CultureInfo CurrentLanguage
        {
            get
            {
                return App.Language;
            }
            set
            {
                App.Language = value;
                RaisePropertyChanged("CurrentLanguage");
                RaisePropertyChanged("CurrentConnectionState");
            }
        }

        /// <summary>
        /// Get or set if sound alert enabled
        /// </summary>
        public bool IsSoundAlertEnabled
        {
            get
            {
                return Properties.Settings.Default.IsSoundAlertEnabled;
            }
            set
            {
                Properties.Settings.Default.IsSoundAlertEnabled = value;
                RaisePropertyChanged("IsSoundAlertEnabled");
            }
        }

        /// <summary>
        /// Get or set if overlay alert enabled
        /// </summary>
        public bool IsOverlayAlertEnabled
        {
            get
            {
                return Properties.Settings.Default.IsOverlayAlertEnabled;
            }
            set
            {
                Properties.Settings.Default.IsOverlayAlertEnabled = value;
                RaisePropertyChanged("IsOverlayAlertEnabled");
            }
        }

        /// <summary>
        /// Get or set brush for volume bar
        /// </summary>
        public Brush VolumeBarBrush
        {
            get
            {
                return _volumeBarBrush;
            }
            set
            {
                _volumeBarBrush = value;
                RaisePropertyChanged("VolumeBarBrush");
            }
        }

        /// <summary>
        /// Get or set sound timer value (-1 = Finished)
        /// </summary>
        public int SoundTimerValue
        {
            get
            {
                return _soundTimerValue;
            }
            set
            {
                _soundTimerValue = value;
                RaisePropertyChanged("SoundTimerValue");
            }
        }

        /// <summary>
        /// Get or set overlay timer value (-1 = Finished)
        /// </summary>
        public int OverlayTimerValue
        {
            get
            {
                return _overlayTimerValue;
            }
            set
            {
                _overlayTimerValue = value;
                RaisePropertyChanged("OverlayTimerValue");
            }
        }

        /// <summary>
        /// Get or set sound alert timer brush (normal or finished)
        /// </summary>
        public Brush SoundAlertTimerBrush
        {
            get
            {
                return _soundAlertTimerBrush;
            }
            set
            {
                _soundAlertTimerBrush = value;
                RaisePropertyChanged("SoundAlertTimerBrush");
            }
        }

        /// <summary>
        /// Get or set overlay alert timer brush (normal or finished)
        /// </summary>
        public Brush OverlayAlertTimerBrush
        {
            get
            {
                return _overlayAlertTimerBrush;
            }
            set
            {
                _overlayAlertTimerBrush = value;
                RaisePropertyChanged("OverlayAlertTimerBrush");
            }
        }

        /// <summary>
        /// Get or set trigger when app need to be closed
        /// </summary>
        public bool CloseTrigger
        {
            get
            {
                return _closeTrigger;
            }
            set
            {
                _closeTrigger = value;
                RaisePropertyChanged("CloseTrigger");
            }
        }

        /// <summary>
        /// Get or set current connection info state
        /// </summary>
        public ConnectionInfoStates CurrentConnectionState
        {
            get
            {
                return _currentConnectionState;
            }
            set
            {
                _currentConnectionState = value;
                RaisePropertyChanged("CurrentConnectionState");
                RaisePropertyChanged("IsControlsBlocked");
            }
        }

        /// <summary>
        /// Get if controls need to be blocked
        /// </summary>
        public Visibility IsControlsBlocked
        {
            get
            {
                if (new[] { ConnectionInfoStates.Connected }.Contains(CurrentConnectionState))
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        /// <summary>
        /// Get or set mic capture boost
        /// </summary>
        public int MicCaptureBoost
        {
            get
            {
                return Properties.Settings.Default.MicCaptureBoost;
            }
            set
            {
                Properties.Settings.Default.MicCaptureBoost = value;
                RaisePropertyChanged("MicCaptureBoost");
            }
        }

        /// <summary>
        /// Get or set application alarm volume
        /// </summary>
        public int AlarmVolume
        {
            get
            {
                return Properties.Settings.Default.AlarmVolume;
            }
            set
            {
                Properties.Settings.Default.AlarmVolume = value;
                RaisePropertyChanged("AlarmVolume");
            }
        }

        /// <summary>
        /// Get or set system alarm volume
        /// </summary>
        public int AlarmSystemVolume
        {
            get
            {
                return Properties.Settings.Default.AlarmSystemVolume;
            }
            set
            {
                Properties.Settings.Default.AlarmSystemVolume = value;
                RaisePropertyChanged("AlarmSystemVolume");
            }
        }

        /// <summary>
        /// Get or set delay before sound alarm
        /// </summary>
        public int DelayBeforeAlarm
        {
            get
            {
                return Properties.Settings.Default.DelayBeforeAlarm;
            }
            set
            {
                Properties.Settings.Default.DelayBeforeAlarm = value;
                RaisePropertyChanged("DelayBeforeAlarm");
            }
        }

        /// <summary>
        /// Get or set delay before alarm overlay
        /// </summary>
        public int DelayBeforeOverlay
        {
            get
            {
                return Properties.Settings.Default.DelayBeforeOverlay;
            }
            set
            {
                Properties.Settings.Default.DelayBeforeOverlay = value;
                RaisePropertyChanged("DelayBeforeOverlay");
            }
        }

        /// <summary>
        /// Get captured microphone volume
        /// </summary>
        public float MicVolume
        {
            get
            {
                return _micVolume;
            }
            set
            {
                _micVolume = value;
                RaisePropertyChanged("MicVolume");
            }
        }

        /// <summary>
        /// Get or set threshold (alarms beyond that will be activated)
        /// </summary>
        public float Threshold
        {
            get
            {
                return Properties.Settings.Default.Threshold;
            }
            set
            {
                Properties.Settings.Default.Threshold = value;
                RaisePropertyChanged("Threshold");
            }
        }

        #endregion

        #region Methods

        #region Alarm system
        private void OnVolumeCheck(object sender, AlarmSystem.VolumeCheckArgs args)
        {
            VolumeBarBrush = args.meterColor;
            if (args.resetSoundLabelColor)
            {
                SoundAlertTimerBrush = DEFAULT_NORMAL_BRUSH;
            }
            if (args.resetOverlayLabelColor)
            {
                OverlayAlertTimerBrush = DEFAULT_NORMAL_BRUSH;
            }
            if (args.resetSoundLabelContent)
            {
                SoundTimerValue = 0;
            }
            if (args.resetOverlayLabelContent)
            {
                OverlayTimerValue = 0;
            }
        }

        private void OnUpdateTimerAlarmDelay(object sender, AlarmSystem.TimerDelayArgs args)
        {
            SoundTimerValue = args.ElapsedTimeInt;
            if (args.alarmActive)
            {
                SoundAlertTimerBrush = DEFAULT_ALERT_GOES_OFF_BRUSH;
                SoundTimerValue = -1;
            }
        }

        private void OnUpdateTimerOverlayDelay(object sender, AlarmSystem.TimerDelayArgs args)
        {
            OverlayTimerValue = args.ElapsedTimeInt;
            if (args.alarmActive)
            {
                OverlayAlertTimerBrush = DEFAULT_ALERT_GOES_OFF_BRUSH;
                OverlayTimerValue = -1;
            }
        }

        private void OnAlarmSystemClosed(object sender)
        {
            Trace.TraceInformation("Alarm System closed");
            ClosingMethod(sender, new CancelEventArgs());
        }
        #endregion

        #region WCF

        private void OnClientConnected()
        {
            CurrentConnectionState = ConnectionInfoStates.Connected;
        }

        private void OnControllerDisconnected()
        {
            CurrentConnectionState = ConnectionInfoStates.Disconnected;
        }

        private void OnSettingsReceive(List<AppSettingsProperty> settings)
        {
            foreach (var item in settings)
            {
                if (Type.GetType(item.type) == typeof(CultureInfo))
                    Properties.Settings.Default[item.name] = new CultureInfo(item.value);
                else
                    Properties.Settings.Default[item.name] = Convert.ChangeType(item.value, Type.GetType(item.type));
                RaisePropertyChanged(item.name);
            }
        }

        private void OnVolumeReceive(float volume)
        {
            MicVolume = volume;
        }

        private void OnConnectionFailed()
        {
            CurrentConnectionState = ConnectionInfoStates.Failed;
        }

        #endregion

        private void ReconnectMethod()
        {
            CurrentConnectionState = ConnectionInfoStates.Ready;
            this._WcfController?.Connect();
        }

        private void LoadedMethod()
        {
            CurrentConnectionState = ConnectionInfoStates.Initializing;

            _alarmSystem = new AlarmSystem(true, MicCaptureBoost, DelayBeforeAlarm, DelayBeforeOverlay, AlarmVolume, AlarmSystemVolume, Threshold, IsSoundAlertEnabled, IsOverlayAlertEnabled);
            this.PropertyChanged += _alarmSystem.PropertyChanged;
            _alarmSystem.OnVolumeCheck += new AlarmSystem.VolumeCheckHandler(OnVolumeCheck);
            _alarmSystem.OnUpdateTimerAlarmDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerAlarmDelay);
            _alarmSystem.OnUpdateTimerOverlayDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerOverlayDelay);
            _alarmSystem.OnClosed += new AlarmSystem.ClosedSystemHandler(OnAlarmSystemClosed);

            this._WcfController = new WcfScServiceController();
            this.PropertyChanged += this._WcfController.PropertyChanged;
            this._WcfController.OnClientConnected += new WcfScServiceController.ClientConnectionChangedHandler(OnClientConnected);
            this._WcfController.OnClientDisconnected += new WcfScServiceController.ClientConnectionChangedHandler(OnControllerDisconnected);
            this._WcfController.OnSettingReceive += new WcfScServiceController.SettingReceiveHandler(OnSettingsReceive);
            this._WcfController.OnVolumeReceive += new WcfScServiceController.VolumeReceivedHandler(OnVolumeReceive);
            this._WcfController.OnConnectionFailed += new WcfScServiceController.ClientConnectionChangedHandler(OnConnectionFailed);
            CurrentConnectionState = ConnectionInfoStates.Ready;
            this._WcfController.Connect();
        }



        public void ClosingMethod(object sender, CancelEventArgs e)
        {
            this._WcfController.Close();

            if (CloseTrigger)
                return;
            if (_alarmSystem.state != AlarmSystem.States.Closed && _alarmSystem.state != AlarmSystem.States.Closing)
            {
               Thread alarmSystemCloseDelay = new Thread(() => { _alarmSystem.Close(); });
                alarmSystemCloseDelay.Priority = ThreadPriority.Lowest;
                alarmSystemCloseDelay.Start();
            }

            if (_alarmSystem.state != AlarmSystem.States.Closed)
            {
                Trace.TraceWarning("Alarm system is not closed yet. Window closing canceled");
                e.Cancel = true;
                return;
            }
            else
            {
                Trace.TraceInformation("Closing approved");
                Properties.Settings.Default.Save();
                CloseTrigger = true;
            }
        }

        #endregion


    }
}
