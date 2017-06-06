//using MVVM_Test.ViewModel;
using ScreamControl.WCF;
using ScreamControl_Client.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ScreamControl_Client.ViewModel
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

        public MainModel mainModel;

        #region Constructor
        public MainViewModel()
        {
            #region Commands
            LoadedCommand = new Command(arg => LoadedMethod());
            #endregion

            this.mainModel = new MainModel();

            CurrentLanguage = App.Language;
            Languages = new ObservableCollection<CultureInfo>(App.Languages);
        }
        #endregion

        #region Constants
        private readonly Brush DEFAULT_NORMAL_BRUSH = Brushes.White;
        private readonly Brush DEFAULT_ALERT_GOES_OFF_BRUSH = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffff00"));
        #endregion

        #region Private fields
        WcfScServiceHost _WcfHost;
        #endregion

        #region Properties
        /// <summary>
        /// Get or set Stealth Mode bool
        /// </summary>
        public bool IsStealthMode
        {
            get
            {
                return Properties.Settings.Default.StealthMode;
            }
            set
            {
                Properties.Settings.Default.StealthMode = value;
            }
        }

        public float MicVolume
        {
            get; set;
        }

        /// <summary>
        /// Get or set window visibility, based on IsStealthMode property
        /// </summary>
        public Visibility WindowVisibilityState
        {
            get
            {
                if (IsStealthMode)
                    return Visibility.Hidden;
                else
                    return Visibility.Visible;

            }

            set { WindowVisibilityState = value; }
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
            get { return App.Language; }
            set { App.Language = value; }
        }

        /// <summary>
        /// Get or set available space for threshold value moving
        /// </summary>
        public float MovingHeight {
            private get
            {
                return mainModel.MovingHeight;
            }
            
            set
            {
                float newValue = value - 3;
                mainModel.MovingHeight = value;
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
            get; set;
        }

        /// <summary>
        /// Get or set sound timer value (-1 = Finished)
        /// </summary>
        public int SoundTimerValue
        {
            get; set;
        }

        /// <summary>
        /// Get or set overlay timer value (-1 = Finished)
        /// </summary>
        public int OverlayTimerValue
        {
            get; set;
        }

        /// <summary>
        /// Get or set sound alert timer brush (normal or finished)
        /// </summary>
        public Brush SoundAlertTimerBrush
        {
            get; set;
        }

        /// <summary>
        /// Get or set overlay alert timer brush (normal or finished)
        /// </summary>
        public Brush OverlayAlertTimerBrush
        {
            get; set;
        }

        
        #endregion

        #region Commands
        public ICommand LoadedCommand { get; set; }
        #endregion

        #region Methods

        #region AlarmSystem Events
        private void OnMonitorUpdate(object sender, AlarmSystem.MonitorArgs args)
        {
            this.MicVolume = args.MicVolume;
            if (_WcfHost.client._isControllerConnected)
                _WcfHost.client.proxy.SendMicInput(args.MicVolume);
        }

        private void OnVolumeCheck(object sender, AlarmSystem.VolumeCheckArgs args)
        {
            //if (!this.Dispatcher.CheckAccess())
            //{
            //    AlarmSystem.VolumeCheckHandler eh = new AlarmSystem.VolumeCheckHandler(OnVolumeCheck);
            //    this.Dispatcher.Invoke(eh, new object[] { sender, args });
            //}
            //else
            //{
                VolumeBarBrush = args.meterColor;
                if (args.resetLabelColor)
                {
                    SoundAlertTimerBrush = DEFAULT_NORMAL_BRUSH;
                    OverlayAlertTimerBrush = DEFAULT_NORMAL_BRUSH;
                }
                if (args.resetLabelContent)
                {
                    SoundTimerValue = 0;
                    OverlayTimerValue = 0;
                }
            //}
        }

        private void OnUpdateTimerAlarmDelay(object sender, AlarmSystem.TimerDelayArgs args)
        {
            //if (!this.Dispatcher.CheckAccess())
            //{
            //    AlarmSystem.TimerDelayHandler eh = new AlarmSystem.TimerDelayHandler(OnUpdateTimerAlarmDelay);
            //    this.Dispatcher.Invoke(eh, new object[] { sender, args });
            //}
            //else
            //{
                SoundTimerValue = args.ElapsedTimeInt;
                if (args.alarmActive)
                {
                    SoundAlertTimerBrush = DEFAULT_ALERT_GOES_OFF_BRUSH;
                    //TODO: converter этого значения
                    SoundTimerValue = -1; // === lElapsed.Content = FindResource("m_DelayElapsedFinish");
                    OverlayAlertTimerBrush = DEFAULT_NORMAL_BRUSH;
                }
            //}
        }

        private void OnUpdateTimerOverlayDelay(object sender, AlarmSystem.TimerDelayArgs args)
        {
            //if (!this.Dispatcher.CheckAccess())
            //{
            //    AlarmSystem.TimerDelayHandler eh = new AlarmSystem.TimerDelayHandler(OnUpdateTimerOverlayDelay);
            //    this.Dispatcher.Invoke(eh, new object[] { sender, args });
            //}
            //else
            //{
                OverlayTimerValue = args;
                if (args.alarmActive)
                {
                    lWindowElapsed.Foreground = DEFAULT_ALERT_GOES_OFF_BRUSH;
                    lWindowElapsed.Content = FindResource("m_AlertWindowElapsedFinish");
                }
            //}
        }
        #endregion

        private void LoadedMethod()
        {
            AlarmSystem alarmSystem = new AlarmSystem();
            this.PropertyChanged += alarmSystem.PropertyChanged;
            alarmSystem.OnMonitorUpdate += new AlarmSystem.MonitorHandler(OnMonitorUpdate);
            alarmSystem.OnVolumeCheck += new AlarmSystem.VolumeCheckHandler(OnVolumeCheck);
            alarmSystem.OnUpdateTimerAlarmDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerAlarmDelay);
            alarmSystem.OnUpdateTimerOverlayDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerOverlayDelay);
            alarmSystem.OnClosed += new AlarmSystem.ClosedSystemHandler(OnAlarmSystemClosed);
        }
        #endregion

        #region Helper methods

        #endregion
    }
}
