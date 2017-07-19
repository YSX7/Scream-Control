using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace ScreamControl.View
{

    public partial class ExtendedMetroWindow : MetroWindow
    {
        protected static bool _isWindowClosing;

        public bool CloseTrigger
        {
            get { return (bool)GetValue(CloseTriggerProperty); }
            set { SetValue(CloseTriggerProperty, value); }
        }

        public static readonly DependencyProperty CloseTriggerProperty =
            DependencyProperty.Register("CloseTrigger", typeof(bool), typeof(ExtendedMetroWindow), new PropertyMetadata(new PropertyChangedCallback(OnCloseTriggerChanged)));

        private static void OnCloseTriggerChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            // if(!_isWindowClosing)
            if ((bool)e.NewValue == true)
                (dp as MetroWindow).Close();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : ExtendedMetroWindow
    {

        private Hotkey _hotkeyStealth;
        private double _availableHeight;
        private bool _isController;
        private bool _isDebugMode;

        delegate float MonitorVolumeCallback();

        public MainWindow(bool debugMode = false)
        {
            try
            {
                Trace.TraceInformation("Window initializing... ");
                Trace.Indent();

                _isDebugMode = debugMode;
                if (_isDebugMode) Trace.TraceInformation("DEBUG MODE");

                InitializeComponent();

                _isController = ((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title.Split(' ')[1] == "Controller";

                if (!_isController)
                {
                    _hotkeyStealth = new Hotkey(Key.S, KeyModifier.Ctrl | KeyModifier.Alt, OnStealthHotkeyHandler);
                }

                this.Title = ((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title;

                Trace.Unindent();
                Trace.TraceInformation("... OK");
            }
            catch(Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Trace.TraceInformation("Internal window loading...");
            Trace.Indent();

#if !DEBUG
            if(!_isController)
              Startup.SetAutostart(_isDebugMode, this.IsVisible);
#endif
            _availableHeight = GridVolume.ActualHeight;
            SetThresholdContentMargin(slThreshold.Value);

            Trace.Unindent();
            Trace.TraceInformation("... OK");
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _isWindowClosing = true;
            Trace.TraceInformation("Window closing at {0}", DateTime.Now);
            if (!_isController)
            {
                _hotkeyStealth.Unregister();
                _hotkeyStealth.Dispose();
            }
        }

        private void wMain_Closed(object sender, EventArgs e)
        {
#if !DEBUG
            if (!_isController)
                Startup.CheckAutostartEnabled(_isDebugMode, false);
#endif

            Trace.TraceInformation("Window closed");
            Application.Current.Shutdown();
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
            //if (this.WindowState == WindowState.Minimized && Properties.Settings.Default.StealthMode)
            //{
            //    HideWindow();
            //}
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SetThresholdContentMargin(e.NewValue);
        }

        private void wMain_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _availableHeight = GridVolume.ActualHeight;
        }

        private void SetThresholdContentMargin(double sliderValue)
        {
            double newBottom = (_availableHeight / 100 * sliderValue - lThreshold.ActualHeight / 2).Clamp(0, _availableHeight - lThreshold.ActualHeight);
            lThreshold.Margin = new Thickness(0, 0, 0, newBottom);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aw = new AboutWindow();
            aw.ShowDialog();
        }
    }
}
