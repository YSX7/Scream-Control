using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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

namespace ScreamControl_Control
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        readonly Brush VOLUME_OK = new SolidColorBrush(Color.FromArgb(100, 127, 255, 121));
        readonly Brush VOLUME_HIGH = new SolidColorBrush(Color.FromArgb(100, 255, 121, 121));

        DispatcherTimer timer, timerWindow, timerOverlayUpdate;
        List<DispatcherTimer> _timersCollection;
        DateTime timeStart, timeStartWindow;
        bool _closeGui, _closeBGWorker = false;
        bool _mousePressed = false;
        bool _overlayWorking = false;

        int _captureMultiplier = 100;
        float _actualHeight;
        float _alarmThreshold = 80;

        private readonly BackgroundWorker bgInputListener = new BackgroundWorker();

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
        }

        #region Window initialization and things

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _actualHeight = (float)pbVolume.ActualHeight - 3;
            float startPos = _actualHeight / 100 * _alarmThreshold;
            CalculateThreshold(startPos);

            bgInputListener.DoWork += bgInputListener_DoWork;
            bgInputListener.RunWorkerCompleted += bgInputListener_RunWorkerCompleted;

            bgInputListener.RunWorkerAsync();

            #region Timers definition

            timer = new DispatcherTimer();
            timer.Tick += (s, args) =>
                {
                    lElapsed.Content = (DateTime.Now - timeStart).TotalMilliseconds.ToString("0,0");
                    if ((DateTime.Now - timeStart).Seconds >= nudDuration.Value)
                    {
                        lElapsed.Foreground = Brushes.Red;
                        lElapsed.Content = FindResource("m_DelayElapsedFinish");
                        timer.Stop();
                        lWindowElapsed.Foreground = Brushes.Black;
                        timeStartWindow = DateTime.Now;
                        timerWindow.Start();
                    }
                };

            timerWindow = new DispatcherTimer();
            timerWindow.Tick += (s, args) =>
                {
                    lWindowElapsed.Content = (DateTime.Now - timeStart).TotalMilliseconds.ToString("0,0");
                    if ((DateTime.Now - timeStartWindow).Seconds >= nudAlertWindow.Value)
                    {
                        lWindowElapsed.Foreground = Brushes.Red;
                        lWindowElapsed.Content = FindResource("m_AlertWindowElapsedFinish");
                        timerWindow.Stop();
                    }
                };


            _timersCollection = new List<DispatcherTimer>(new DispatcherTimer[]{timerWindow, timerOverlayUpdate, timer});

            #endregion

            LanguageChanged(null, null);



            LoadThresholdPosition();
        }

        private void wMain_Deactivated(object sender, EventArgs e)
        {
            //Window window = (Window)sender;
            //window.Topmost = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _overlayWorking = false;
            foreach (var t in _timersCollection)
                if(t.IsEnabled)
                  t.Stop();
            _closeBGWorker = true;
            if (!_closeGui)
                e.Cancel = true;
            else
                Properties.Settings.Default.Save();
        }

        private void wMain_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        #endregion

        #region Sound things

        private void bgInputListener_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!_closeBGWorker)
            {
                MonitorVolume(null, null);
                Thread.Sleep(50);
            }
            bgInputListener.CancelAsync();
        }

        private void bgInputListener_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _closeGui = true;
            //_soundCapture.Stop();
            //_soundCapture.Dispose();
            //if (soundOut.PlaybackState != PlaybackState.Stopped)
            //{
            //    soundOut.Stop();
            //    soundOut.Dispose();
            //}

            this.Close();
        }

        private void MonitorVolume(object sender, EventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                // Invoke the event onto the GUI thread
                EventHandler eh = new EventHandler(MonitorVolume);
                this.Dispatcher.Invoke(eh, new object[] { null, null });
            }
            else
            {
                //volume = Clamp(volume, (float)pbVolume.Minimum, (float)pbVolume.Maximum);
                //VolumeAlarm(volume);
                //pbVolume.Value = (int)volume;
                //lVolume.Content = volume.ToString("F2");
            }
        }

        private void VolumeAlarm(float volume)
        {
            if (volume >= _alarmThreshold)
            {
                //pbVolume.Foreground = VOLUME_HIGH;
                //if (nudDuration.Value > 0)
                //{
                //    if (!timer.IsEnabled && soundOut.PlaybackState != PlaybackState.Playing)
                //    {
                //        lElapsed.Foreground = Brushes.Black;
                //        timeStart = DateTime.Now;
                //        timer.Start();
                //    }
                //    else return;
                //}
                //else
                //    PlayAlarm();
            }
            else
            {
                //if (soundOut.PlaybackState == PlaybackState.Playing)
                //{
                //    lElapsed.Content = "";
                //    soundOut.Pause();
                //    pbVolume.Foreground = VOLUME_OK;

                //    if (timer.IsEnabled)
                //    {
                //        timer.Stop();
                //    }
                //    if (timerWindow.IsEnabled)
                //    {
                //        timerWindow.Stop();
                //    }
                //    if (_overlayWorking)
                //    {
                //        _overlayWorking = false;
                //    }
                //}
            }
        }

        #endregion

        #region Threshold

        private void LoadThresholdPosition()
        {
            _alarmThreshold = Properties.Settings.Default.Threshold;
            var y = (_actualHeight / 100) * _alarmThreshold;

            SetThresholdPosition(y);

            lThreshold.Content = _alarmThreshold.ToString("F0");
        }

        private float CalculateThreshold(float p)
        {
            p = Clamp(p, 0, _actualHeight);

            SetThresholdPosition(p);

            float vol = (p) /(_actualHeight / 100) ;
            vol = Clamp(vol, 0, 100);

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
            margin.Bottom = Clamp(mBottom, 0, _actualHeight - 10);
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
                _alarmThreshold = CalculateThreshold(y);
                lDebug.Content = _actualHeight + " " + y;
            }
        }

        private void wMain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _mousePressed = false;
            Properties.Settings.Default.Threshold = _alarmThreshold;
        }

        #endregion

        private void numBoost_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            if(e.NewValue != null)
                _captureMultiplier = (int)e.NewValue;
        }

        private void sliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //if(soundOut != null)
            //    soundOut.Volume = (float)e.NewValue / 100;
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

        #region Various

        private float Clamp(float value, float min = 0, float max = 100)
        {
            if (value > max)
                return max;
            else
                if (value < min)
                    return min;
                else return value;
        }

        private int Clamp(int value, int min = 0, int max = 100)
        {
            if (value > max)
                return max;
            else
                if (value < min)
                    return min;
                else return value;
        }

        // The GetForegroundWindow function returns a handle to the foreground window
        // (the window  with which the user is currently working).
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // The GetWindowThreadProcessId function retrieves the identifier of the thread
        // that created the specified window and, optionally, the identifier of the
        // process that created the window.
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Returns the name of the process owning the foreground window.
        private static uint GetForegroundProcessId()
        {
            IntPtr hwnd = GetForegroundWindow();

            // The foreground window can be NULL in certain circumstances, 
            // such as when a window is losing activation.
            if (hwnd == null)
                return 0;

            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);

            return pid;
        }


        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

    }

}
