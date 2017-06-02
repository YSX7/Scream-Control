using MahApps.Metro.Controls;
using Microsoft.Win32;
using ScreamControl.WCF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace ScreamControl_Client.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : MetroWindow
    {
        private readonly Brush DEFAULT_NORMAL_BRUSH = Brushes.White;
        private readonly Brush DEFAULT_ALERT_GOES_OFF_BRUSH = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffff00"));

        AlarmSystem _as;
        float _actualHeight;
        float _alarmThreshold = 80;
        bool _mousePressed;

        private Hotkey _hotkeyStealth;

        private WcfScServiceHost _WcfHost;

        float AlarmThreshold
        {
            get
            {
                return _alarmThreshold;
            }
            set
            {
                if (_as != null)
                    _as.alarmThreshold = value;
                _alarmThreshold = value;
            }
        }

        delegate float MonitorVolumeCallback();

        public MainWindow()
        {
            InitializeComponent();

            _hotkeyStealth = new Hotkey(Key.S, KeyModifier.Ctrl | KeyModifier.Alt, OnStealthHotkeyHandler);

#if !DEBUG
            Startup.SetAutostart();
#endif
            this.Title += " " + Assembly.GetEntryAssembly().GetName().Version.ToString();

            Trace.TraceInformation("Window Initialized");
        }


        #region Window initialization and things

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _actualHeight = (float)pbVolume.ActualHeight - 3;
            //float startPos = _actualHeight / 100 * _alarmThreshold;

            _as = new AlarmSystem();
            _as._isSoundAlarmEnabled = (bool)tsSoundAlertSwitch.IsChecked;
            _as._isMessageAlarmEnabled = (bool)tsMessageAlertSwitch.IsChecked;
            _as.OnMonitorUpdate += new AlarmSystem.MonitorHandler(OnMonitorUpdate);
            _as.OnVolumeCheck += new AlarmSystem.VolumeCheckHandler(OnVolumeCheck);
            _as.OnUpdateTimerAlarmDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerAlarmDelay);
            _as.OnUpdateTimerOverlayDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerOverlayDelay);
            _as.OnClosed += new AlarmSystem.ClosedSystemHandler(OnAlarmSystemClosed);

         //   LanguageChanged(null, null);

            LoadThresholdPosition(Properties.Settings.Default.Threshold);
            Trace.TraceInformation("Window loaded");

            List<AppSettingsProperty> settingsToSerialize = new List<AppSettingsProperty>();
            foreach (SettingsPropertyValue item in Properties.Settings.Default.PropertyValues)
            {
                
                var listItem = new AppSettingsProperty(item.Name, item.PropertyValue.ToString(), item.Property.PropertyType.FullName);
                settingsToSerialize.Add(listItem);
            }

            this._WcfHost = new WcfScServiceHost(settingsToSerialize);
            this._WcfHost.client.OnControllerConnected += new WcfScServiceHost.HostClient.ControllerConnectionChangedHandler(OnControllerConnected);
            this._WcfHost.client.OnControllerDisconnected += new WcfScServiceHost.HostClient.ControllerConnectionChangedHandler(OnControllerDisconnected);
            this._WcfHost.client.OnSettingReceive += new WcfScServiceHost.HostClient.SettingReceiveHandler(OnSettingReceive);
            lConnectionStatus.Content = "Succesful connected, state: " + _WcfHost.client.temp;
        }

        private void OnControllerDisconnected()
        {
            gControllerBlock.Visibility = Visibility.Hidden;
        }

        private void OnControllerConnected()
        {
            gControllerBlock.Visibility = Visibility.Visible;
        }

        private void OnSettingReceive(AppSettingsProperty setting)
        {
            Properties.Settings.Default[setting.name] = Convert.ChangeType(setting.value, Type.GetType(setting.type));
            if (setting.name == "Threshold")
                LoadThresholdPosition(Convert.ToSingle(setting.value));
        }

        private void OnMonitorUpdate(object sender, AlarmSystem.MonitorArgs args)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                AlarmSystem.MonitorHandler eh = new AlarmSystem.MonitorHandler(OnMonitorUpdate);
                this.Dispatcher.Invoke(eh, new object[] { sender, args });
            }
            else
            {
                pbVolume.Value = args.MeterVolume;
                lVolume.Content = args.LabelVolume;
                if(_WcfHost.client._isControllerConnected)
                    _WcfHost.client.proxy.SendMicInput(args.RawVolume);
            }
        }

        private void OnVolumeCheck(object sender, AlarmSystem.VolumeCheckArgs args)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                AlarmSystem.VolumeCheckHandler eh = new AlarmSystem.VolumeCheckHandler(OnVolumeCheck);
                this.Dispatcher.Invoke(eh, new object[] { sender, args });
            }
            else
            {
                pbVolume.Foreground = args.meterColor;
                if (args.resetLabelColor)
                {
                    lElapsed.Foreground = DEFAULT_NORMAL_BRUSH;
                    lWindowElapsed.Foreground = DEFAULT_NORMAL_BRUSH;
                }
                if (args.resetLabelContent)
                {
                    lElapsed.Content = "";
                    lWindowElapsed.Content = "";
                }
            }
        }

        private void OnUpdateTimerAlarmDelay(object sender, AlarmSystem.TimerDelayArgs args)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                AlarmSystem.TimerDelayHandler eh = new AlarmSystem.TimerDelayHandler(OnUpdateTimerAlarmDelay);
                this.Dispatcher.Invoke(eh, new object[] { sender, args });
            }
            else
            {
                lElapsed.Content = args.ElapsedTimeString;
                if (args.alarmActive)
                {
                    lElapsed.Foreground = DEFAULT_ALERT_GOES_OFF_BRUSH;
                    lElapsed.Content = FindResource("m_DelayElapsedFinish");
                    lWindowElapsed.Foreground = DEFAULT_NORMAL_BRUSH;
                }
            }
        }

        private void OnUpdateTimerOverlayDelay(object sender, AlarmSystem.TimerDelayArgs args)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                AlarmSystem.TimerDelayHandler eh = new AlarmSystem.TimerDelayHandler(OnUpdateTimerOverlayDelay);
                this.Dispatcher.Invoke(eh, new object[] { sender, args });
            }
            else
            {
                lWindowElapsed.Content = args.ElapsedTimeString;
                if (args.alarmActive)
                {
                    lWindowElapsed.Foreground = DEFAULT_ALERT_GOES_OFF_BRUSH;
                    lWindowElapsed.Content = FindResource("m_AlertWindowElapsedFinish");
                }
            }
        }

        private void OnAlarmSystemClosed(object sender)
        {
            Trace.TraceInformation("Alarm System closed");
            this.Close();
        }

        private void wMain_Deactivated(object sender, EventArgs e)
        {
            //Window window = (Window)sender;
            //window.Topmost = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //            Trace.TraceInformation("Window closing at {0}", DateTime.Now);
            //#if !DEBUG
            //            Startup.CheckAutostartEnabled(Assembly.GetExecutingAssembly().GetName().Name);
            //#endif
            //            if (_as.state != AlarmSystem.States.Closed && _as.state != AlarmSystem.States.Closing)
            //                _as.Close();

            //            if (_as.state != AlarmSystem.States.Closed)
            //            {
            //                Trace.TraceWarning("Alarm system is not closed yet. Window closing canceled");
            //                e.Cancel = true;
            //                return;
            //            }
            //            else
            //            {
            //                Trace.TraceInformation("Closing approved");
            Properties.Settings.Default.Save();
            //                _hotkeyStealth.Unregister();
            //                _hotkeyStealth.Dispose();
            //            }
        }

        private void wMain_Closed(object sender, EventArgs e)
        {
            Trace.TraceInformation("Window closed");
#if !DEBUG
            Startup.CheckAutostartEnabled(Assembly.GetExecutingAssembly().GetName().Name);
#endif
            App.Current.Shutdown();
        }

        #endregion

        #region Threshold

        private void LoadThresholdPosition(float thresholdVolume)
        {
            AlarmThreshold = thresholdVolume;
            var y = (_actualHeight / 100) * AlarmThreshold;

            SetThresholdPosition(y);

            lThreshold.Content = AlarmThreshold.ToString("F0");
        }

        private float CalculateThreshold(float p)
        {
            p = p.Clamp(0, _actualHeight);

            SetThresholdPosition(p);

            float vol = (p) / (_actualHeight / 100);
            vol = vol.Clamp(0, 100);

            lThreshold.Content = vol.ToString("F0");

            return vol;
        }

        private void SetThresholdPosition(float m)
        {
            Thickness margin = new Thickness(0, 0, 0, m);
            Thresold.Margin = margin;
            margin.Bottom = m - 5;
            ThresholdHitBox.Margin = margin;

            float mBottom = (float)margin.Bottom;
            margin.Bottom = margin.Bottom.Clamp(0, _actualHeight - 10);
            lThreshold.Margin = margin;
        }

        private void ThresholdHitBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _mousePressed = true;
            _actualHeight = (float)pbVolume.ActualHeight - 3;
        }

        private void wMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mousePressed)
            {
                float y = (float)(_actualHeight - e.GetPosition(GridVolume).Y);
                AlarmThreshold = CalculateThreshold(y);
                lDebug.Content = _actualHeight + " " + y;
            }
        }

        private void wMain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mousePressed = false;
            Properties.Settings.Default.Threshold = AlarmThreshold;
        }

        #endregion

        private void numBoost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue != null && _as != null)
                _as.captureMultiplier = (int)e.NewValue;
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != null && _as != null)
                _as.AlarmVolume = ((float)e.NewValue / 100).Clamp(0, 1);
        }

        private void sliderVolumeSystem_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != null && _as != null)
                _as.SystemVolume = ((float)e.NewValue / 100).Clamp(0, 1);
        }

        private void nudDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue != null && _as != null)
                _as.delayBeforeAlarm = (float)e.NewValue;
        }

        private void nudAlertWindow_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue != null && _as != null)
                _as.delayBeforeOverlay = (float)e.NewValue;
        }

        private void OnStealthHotkeyHandler(Hotkey hotkey)
        {
            if (this.IsVisible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
            }
        }

        private void HideWindow()
        {
            this.Hide();
            this.ShowInTaskbar = false;
        }

        private void ShowWindow()
        {
            this.Show();
            this.ShowInTaskbar = true;
            this.Focus();
        }

        private void wMain_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && Properties.Settings.Default.StealthMode)
            {
                HideWindow();
            }
        }

        private void tsSoundAlertSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            _as._isSoundAlarmEnabled = (bool)tsSoundAlertSwitch.IsChecked;
        }

        private void tsMessageAlertSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            _as._isMessageAlarmEnabled = (bool)tsMessageAlertSwitch.IsChecked;
        }
    }

}
