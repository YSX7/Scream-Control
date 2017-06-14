﻿using MahApps.Metro.Controls;
using Microsoft.Win32;
using ScreamControl;
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

    public partial class ExtendedMetroWindow : MetroWindow
    {
        public bool CloseTrigger
        {
            get { return (bool)GetValue(CloseTriggerProperty); }
            set { SetValue(CloseTriggerProperty, value); }
        }

        public static readonly DependencyProperty CloseTriggerProperty =
            DependencyProperty.Register("CloseTrigger", typeof(bool), typeof(ExtendedMetroWindow), new PropertyMetadata(new PropertyChangedCallback(OnCloseTriggerChanged)));

        private static void OnCloseTriggerChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
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

        private WcfScServiceHost _WcfHost;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            Trace.TraceInformation("Window loaded");

            //TODO: переместить это когда ктот подключается
            List<AppSettingsProperty> settingsToSerialize = new List<AppSettingsProperty>();
            foreach (SettingsPropertyValue item in Properties.Settings.Default.PropertyValues)
            {

                var listItem = new AppSettingsProperty(item.Name, item.PropertyValue.ToString(), item.Property.PropertyType.FullName);
                settingsToSerialize.Add(listItem);
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Trace.TraceInformation("Window closing at {0}", DateTime.Now);
#if !DEBUG
             Startup.CheckAutostartEnabled(Assembly.GetExecutingAssembly().GetName().Name);
#endif
            _hotkeyStealth.Unregister();
            _hotkeyStealth.Dispose();
        }

        private void wMain_Closed(object sender, EventArgs e)
        {
            Trace.TraceInformation("Window closed");
            App.Current.Shutdown();
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

        private void cbLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }
    }
}
