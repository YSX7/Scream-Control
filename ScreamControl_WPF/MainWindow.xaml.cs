﻿using MahApps.Metro.Controls;
using Microsoft.Win32;
using Squirrel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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


namespace ScreamControl_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        AlarmSystem _as;

        bool  _mousePressed;
        float _actualHeight;
        float _alarmThreshold = 80;
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

            App.LanguageChanged += LanguageChanged;
            CultureInfo currLang = App.Language;

            cbLang.Items.Clear();
            foreach (var lang in App.Languages)
            {
                ComboBoxItem cboxItem = new ComboBoxItem();
                cboxItem.Content = lang.NativeName;
                cboxItem.Tag = lang;
                cboxItem.IsSelected = lang.Equals(currLang);
                cbLang.Items.Add(cboxItem);
            }

            App.Language = currLang;
           
            //Update
#if !DEBUG
            //Autostart
            //if (!IsStartupItem())
            //{
            //    RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            //    Assembly curAssembly = Assembly.GetExecutingAssembly();
            //    key.SetValue(curAssembly.GetName().Name, curAssembly.Location);
            //}

            Update();
            MessageBox.Show(Assembly.GetEntryAssembly().GetName().Version.ToString());
#endif
        }

        private async void Update()
        {
            using (var mgr = UpdateManager.GitHubUpdateManager("https://github.com/YSXrus/Scream-Control"))
            {
               await mgr.Result.UpdateApp();
            }
        }

        private bool IsStartupItem()
        {
            RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rkApp.GetValue("Scream Control") == null)
                return false;
            else
                return true;
        }

        #region Window initialization and things

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _actualHeight = (float)pbVolume.ActualHeight - 3;
            float startPos = _actualHeight / 100 * _alarmThreshold;

            _as = new AlarmSystem();
            _as.OnMonitorUpdate += new AlarmSystem.MonitorHandler(OnMonitorUpdate);
            _as.OnVolumeCheck += new AlarmSystem.VolumeCheckHandler(OnVolumeCheck);
            _as.OnUpdateTimerAlarmDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerAlarmDelay);
            _as.OnUpdateTimerOverlayDelay += new AlarmSystem.TimerDelayHandler(OnUpdateTimerOverlayDelay);
            _as.OnClosed += new AlarmSystem.ClosedSystemHandler(OnAlarmSystemClosed);

            LanguageChanged(null, null);

            LoadThresholdPosition();
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
                    lElapsed.Foreground = Brushes.Black;
                    lWindowElapsed.Foreground = Brushes.Black;
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
                    lElapsed.Foreground = Brushes.Red;
                    lElapsed.Content = FindResource("m_DelayElapsedFinish");
                    lWindowElapsed.Foreground = Brushes.Black;
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
                    lWindowElapsed.Foreground = Brushes.Red;
                    lWindowElapsed.Content = FindResource("m_AlertWindowElapsedFinish");
                }
            }
        }

        private void OnAlarmSystemClosed(object sender)
        {
            this.Close();
        }

        private void wMain_Deactivated(object sender, EventArgs e)
        {
            //Window window = (Window)sender;
            //window.Topmost = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _as.Close();

            if (_as.state != AlarmSystem.States.Closed)
            {
                e.Cancel = true;
                return;
            }
            else
                Properties.Settings.Default.Save();
        }

        private void wMain_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        #endregion

        #region Threshold

        private void LoadThresholdPosition()
        {
            AlarmThreshold = Properties.Settings.Default.Threshold;
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

        #region Language things

        private void LanguageChanged(Object sender, EventArgs e)
        {
            CultureInfo currLang = App.Language;

            //Отмечаем нужный пункт смены языка как выбранный язык
            foreach (ComboBoxItem i in cbLang.Items)
            {
                CultureInfo ci = i.Tag as CultureInfo;
                i.IsSelected = ci != null && ci.Equals(currLang);
            }
        }

        private void ChangeLanguageClick(object sender, EventArgs e)
        {
            ComboBox cbox = sender as ComboBox;
            ComboBoxItem citem = cbox.SelectedItem as ComboBoxItem;
            if (citem != null)
            {
                CultureInfo lang = citem.Tag as CultureInfo;
                if (lang != null)
                {
                    App.Language = lang;
                }
            }

        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {

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

    }

}