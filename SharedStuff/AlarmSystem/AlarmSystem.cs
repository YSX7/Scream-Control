using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundIn;
using CSCore.SoundOut;
using Process.NET;
using Process.NET.Memory;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ScreamControl.Alarms
{
    public class AlarmSystem
    {

        private bool _isSoundAlertEnabled = false;
        private bool _isOverlayAlertEnabled = false;
        private bool _isControllerMode = false;

        private bool _defaultDeviceChanged = false;

        private bool _isSoundAlertPlaying = false;
        private bool _isOverlayAlertPlaying = false;

        private int _audioVolumeTryCount = 0;
        private int _sessionManagerTryCount = 0;

        public void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            object value = sender.GetType().GetProperty(e.PropertyName).GetValue(sender);
            switch (e.PropertyName)
            {
                case "IsSoundAlertEnabled":
                    _isSoundAlertEnabled = (bool)value;
                    break;
                case "IsOverlayAlertEnabled":
                    _isOverlayAlertEnabled = (bool)value;
                    break;
                case "MicCaptureBoost":
                    _micCaptureBoost = (int)value;
                    break;
                case "AlarmVolume":
                    AlarmVolume = Convert.ToSingle(value);
                    break;
                case "AlarmSystemVolume":
                    SystemVolume = Convert.ToSingle(value);
                    break;
                case "DelayBeforeAlarm":
                    _delayBeforeAlarm = Convert.ToSingle(value);
                    break;
                case "DelayBeforeOverlay":
                    _delayBeforeOverlay = Convert.ToSingle(value);
                    break;
                case "Threshold":
                    _alarmThreshold = Convert.ToSingle(value);
                    break;
                case "MicVolume":
                    if (this._isControllerMode)
                        VolumeCheck(Convert.ToSingle(value));
                    break;
            }
        }

        private readonly Brush VOLUME_OK = new SolidColorBrush(Color.FromArgb(100, 127, 255, 121));
        private readonly Brush VOLUME_HIGH = new SolidColorBrush(Color.FromArgb(100, 255, 121, 121));

        private float _alarmThreshold = 100;
        private int _micCaptureBoost = 100;
        private float _delayBeforeAlarm = 0,
            _delayBeforeOverlay = 0;
        public enum States { Stopped, Running, Stopping, Closing, Closed };
        public States state = States.Stopped;

        private IWaveSource _soundSource;

        private float _alarmVolume;
        private float AlarmVolume
        {
            get
            {
                return _alarmVolume;
            }
            set
            {
                float newValue = (Convert.ToSingle(value) / 100).Clamp(0, 1);
                _alarmVolume = newValue;
                if (_soundOut != null)
                {
                    _soundOut.Volume = newValue;
                }
            }

        }

        private bool _overlayWorking = false;

        private AudioMeterInformation _meter;
        private AlertOverlay _alertOverlay;
        private readonly BackgroundWorker _bgInputListener = new BackgroundWorker();
        private System.Timers.Timer _timerAlarmDelay,
            _timerOverlayShow,
            _timerOverlayUpdate,
            _timerZeroVolumeDelay;
        private WasapiCapture _soundCapture;
        private ISoundOut _soundOut;

        private Role _currentCaptureRole = Role.Communications;

        //private bool _loggingEnabled = false;
        private float _systemVolume;

        private SimpleAudioVolume _systemSimpleAudioVolume;
        private float SystemVolume
        {
            get
            {
                return _systemVolume;
            }
            set
            {
                float newValue = (Convert.ToSingle(value) / 100).Clamp(0, 1);
                _systemVolume = newValue;
                _systemSimpleAudioVolume.MasterVolume = newValue;
            }
        }


        #region Events

        public class MonitorArgs : EventArgs
        {
            private float _volume;

            public MonitorArgs(float volume)
            {
                this._volume = volume;
            }

            public float MicVolume
            {
                get
                {
                    return _volume;
                }
            }

            public int MeterVolume
            {
                get
                {
                    return (int)_volume;
                }
            }

            public string LabelVolume
            {
                get
                {
                    return _volume.ToString("F2");
                }
            }
        }
        public delegate void MonitorHandler(object sender, MonitorArgs args);
        public event MonitorHandler OnMonitorUpdate;

        public class VolumeCheckArgs : EventArgs
        {
            public Brush meterColor;
            public bool resetSoundLabelColor;
            public bool resetSoundLabelContent;
            public bool resetOverlayLabelColor;
            public bool resetOverlayLabelContent;

            public VolumeCheckArgs(Brush meterColor)
            {
                this.meterColor = meterColor;
            }
        }
        public delegate void VolumeCheckHandler(object sender, VolumeCheckArgs args);
        public event VolumeCheckHandler OnVolumeCheck;

        public class TimerDelayArgs : EventArgs
        {
            private DateTime _start;
            public bool alarmActive;

            public TimerDelayArgs(DateTime start)
            {
                this._start = start;
            }

            public TimeSpan ElapsedTime
            {
                get
                {
                    return DateTime.Now - _start;
                }
            }

            public int ElapsedTimeInt
            {
                get
                {
                    return (int)(DateTime.Now - _start).TotalMilliseconds;
                }
            }

            public string ElapsedTimeString
            {
                get
                {
                    return (DateTime.Now - _start).TotalMilliseconds.ToString("0,0");
                }
            }
        }
        public delegate void TimerDelayHandler(object sender, TimerDelayArgs args);
        public event TimerDelayHandler OnUpdateTimerAlarmDelay,
            OnUpdateTimerOverlayDelay;
        private TimerDelayArgs _timerAlarmDelayArgs,
            _timerOverlayDelayArgs;

        public delegate void ClosedSystemHandler(object sender);
        public event ClosedSystemHandler OnClosed;

        #endregion


        public AlarmSystem(bool isController = false,
                           int micCaptureBoost = 100,
                           float delayBeforeAlarm = 0,
                           float delayBeforeOverlay = 0,
                           float alarmVolume = 0,
                           float systemVolume = 0,
                           float threshold = 100,
                           bool isSoundAlertEnabled = false,
                           bool isOverlayAlertEnabled = false)
        {
            //_loggingEnabled = Trace.Listeners.Count > 1;
            try
            {
                Trace.TraceInformation("Alarm system init");
                Trace.Indent();
                state = States.Running;
                this._isControllerMode = isController;

                MMNotificationClient deviceNotification = new MMNotificationClient();
                deviceNotification.DefaultDeviceChanged += DeviceNotification_DefaultDeviceChanged;
                deviceNotification.DeviceAdded += DeviceNotification_DeviceAdded;
                deviceNotification.DeviceRemoved += DeviceNotification_DeviceRemoved;

                GetCapture(isController);
                Trace.TraceInformation("Sound Capture OK");

                _soundSource = GetSoundSource();
                _soundSource = _soundSource.Loop();
                GetOutputSound();
                Trace.TraceInformation("Sound Out OK");


                this._isSoundAlertEnabled = isSoundAlertEnabled;
                this._isOverlayAlertEnabled = isOverlayAlertEnabled;
                this._micCaptureBoost = micCaptureBoost;
                this._delayBeforeAlarm = delayBeforeAlarm;
                this._delayBeforeOverlay = delayBeforeOverlay;
                this.AlarmVolume = alarmVolume;
                this._alarmThreshold = threshold;

                _systemSimpleAudioVolume = GetSimpleAudioVolume();
                this.SystemVolume = systemVolume;

                if (!isController)
                {
                    _bgInputListener.WorkerSupportsCancellation = true;
                    _bgInputListener.DoWork += bgInputListener_DoWork;
                    _bgInputListener.RunWorkerCompleted += bgInputListener_RunWorkerCompleted;

                    _bgInputListener.RunWorkerAsync();
                }
                Trace.TraceInformation("Background worker running");

                #region Timers

                _timerAlarmDelay = new System.Timers.Timer();
                _timerAlarmDelay.Elapsed += (s, args) =>
                {
                    if (_timerAlarmDelayArgs.ElapsedTime.Seconds >= _delayBeforeAlarm)
                    {
                        _timerAlarmDelayArgs.alarmActive = true;

                        _timerAlarmDelay.Stop();
                        PlayAlarm(!_isControllerMode);

                    }

                    OnUpdateTimerAlarmDelay(this, _timerAlarmDelayArgs);
                };

                _timerOverlayShow = new System.Timers.Timer();
                _timerOverlayShow.Elapsed += (s, args) =>
                {
                    if (_timerOverlayDelayArgs.ElapsedTime.Seconds >= _delayBeforeOverlay)
                    {
                        _timerOverlayDelayArgs.alarmActive = true;
                        _timerOverlayShow.Stop();
                        Application.Current.Dispatcher.Invoke((Action)delegate { ShowAlertWindow(!_isControllerMode); });
                    }
                    OnUpdateTimerOverlayDelay(this, _timerOverlayDelayArgs);
                    //if (_timerOverlayShow.Dispatcher.HasShutdownStarted)
                    //    _timerOverlayDelayArgs = null;
                };

                _timerOverlayUpdate = new System.Timers.Timer();
                _timerOverlayUpdate.Interval = 10;
                _timerOverlayUpdate.Elapsed += (s, args) =>
                {
                    _alertOverlay.Update();
                    if (!_overlayWorking)
                    {
                        _timerOverlayUpdate.Stop();
                        if (_alertOverlay != null)
                        {
                            _alertOverlay.Dispose();
                            _alertOverlay = null;
                        }
                    }
                };

                _timerZeroVolumeDelay = new System.Timers.Timer(10000);
                _timerZeroVolumeDelay.AutoReset = false;
                _timerZeroVolumeDelay.Elapsed += (s, args) =>
                {
                    _currentCaptureRole -= 1;
                    if (_currentCaptureRole < 0)
                        _currentCaptureRole = Role.Communications;
                    GetCapture(isController);
                };
                _timerZeroVolumeDelay.Start();
                #endregion

                Trace.TraceInformation("Timers initialized");
                Trace.TraceInformation("Alarm System up and running!");
                Trace.Unindent();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Application.Current.Shutdown();
            }
        }

        private void DeviceNotification_DeviceAdded(object sender, DeviceNotificationEventArgs e)
        {
            _defaultDeviceChanged = true;
        }

        private void DeviceNotification_DeviceRemoved(object sender, DeviceNotificationEventArgs e)
        {
            _defaultDeviceChanged = true;
        }

        private void DeviceNotification_DefaultDeviceChanged(object sender, DefaultDeviceChangedEventArgs e)
        {
            try
            {
                _defaultDeviceChanged = true;
                Trace.TraceInformation("Default device changed. New device: {0}, {1}", e.DataFlow.ToString(), e.Role.ToString());
                switch (e.DataFlow)
                {
                    case DataFlow.Capture:
                        GetCapture(_isControllerMode, e.DeviceId);
                        break;
                    case DataFlow.Render:
                        GetNewSoundOut();
                        break;
                }

            }
            catch (Exception exp)
            {
                Trace.TraceError(exp.Message);
            }
            finally
            {
                _defaultDeviceChanged = false;
            }
        }

        private void GetNewSoundOut()
        {
            _soundOut.Stop();
            _soundOut.WaitForStopped();
            GetOutputSound();
            _systemSimpleAudioVolume = GetSimpleAudioVolume();
            _soundOut.Volume = AlarmVolume;
        }

        public void Close()
        {
            if (!_isControllerMode)
            {
                switch (state)
                {
                    case States.Closed:
                        return;
                    case States.Closing:
                        return;
                    case States.Stopped:
                        break;
                    default:
                        state = States.Stopping;
                        return;
                }
            }
            else
            {
                _soundOut.Dispose();
            }
            Trace.TraceInformation("Alarm System closing");
            state = States.Closing;
            _overlayWorking = false;
            _timerAlarmDelay.Stop();
            _timerOverlayShow.Stop();
            _timerOverlayUpdate.Stop();
            _timerZeroVolumeDelay.Stop();
            if (_alertOverlay != null)
            {
                _alertOverlay.Disable();
                _alertOverlay.Dispose();
            }
            state = States.Closed;
            OnClosed(this);
        }

        private float GetVolumeInfo()
        {
            if (_meter.PeakValue == 0.0)
            {
                _timerZeroVolumeDelay.Start();
            }
            else
                _timerZeroVolumeDelay.Stop();
            return _meter.PeakValue * _micCaptureBoost;
        }

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                WasapiOut was;
                do
                    was = new WasapiOut();
                while (was.Device.DeviceState != DeviceState.Active);
                return was;
            }
            else
            {
                DirectSoundOut dso = new DirectSoundOut();
                return dso;
            }
        }

        private IWaveSource GetSoundSource()
        {
            return CodecFactory.Instance.GetCodec("beep.mp3");
        }

        private void GetOutputSound()
        {
            _soundOut = GetSoundOut();
            _soundOut.Initialize(_soundSource);
        }

        #region Listen to mic input

        private void bgInputListener_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                while (state != States.Stopping)
                {
                    MonitorVolume(null, null);
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Something happened at BG Listener: {0}", ex);
            }
            Trace.TraceInformation("BG Mic Listener trying to close");
            _bgInputListener.CancelAsync();
        }

        private void bgInputListener_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Trace.TraceInformation("Background Microphone Listener closing. State: {0}", state.ToString());
            state = States.Stopping;
            _soundCapture?.Stop();
            _soundCapture?.Dispose();

            _isSoundAlertPlaying = false;
            if (_soundOut.PlaybackState != PlaybackState.Stopped)
            {
                _soundOut.Stop();
            }
            _soundOut.Dispose();
            state = States.Stopped;
            Close();
        }

        private void MonitorVolume(object sender, EventArgs e)
        {
            try
            {
                if (_defaultDeviceChanged) return;
                float volume = GetVolumeInfo();
                volume = volume.Clamp(0, 100);
                VolumeCheck(volume);
                MonitorArgs ma = new MonitorArgs(volume);
                OnMonitorUpdate(this, ma);
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("Something happend at volume monitor: {0}", ex);
            }
        }

        #endregion

        private void VolumeCheck(float volume)
        {
            if (_defaultDeviceChanged) return;
            try
            {
                VolumeCheckArgs vca;
                vca = new VolumeCheckArgs(VOLUME_OK);
                if (volume >= _alarmThreshold)
                {
                    KeepSystemVolume();
                    vca.meterColor = VOLUME_HIGH;
                    if (_isSoundAlertEnabled)
                    {
                        if (_delayBeforeAlarm > 0)
                        {
                            if (!_timerAlarmDelay.Enabled && !_isSoundAlertPlaying)
                            {
                                vca.resetSoundLabelColor = true;
                                _timerAlarmDelayArgs = new TimerDelayArgs(DateTime.Now);
                                _timerAlarmDelay.Start();
                            }
                            //  else return;
                        }
                        else
                            PlayAlarm(!_isControllerMode);
                    }
                    else
                    {
                        if (_isSoundAlertPlaying)
                        {
                            _soundOut.Stop();
                            _isSoundAlertPlaying = false;
                            vca.resetSoundLabelColor = true;
                            vca.resetSoundLabelContent = true;
                        }

                    }
                    if (_isOverlayAlertEnabled)
                    {
                        if (!_timerOverlayShow.Enabled && !_timerOverlayUpdate.Enabled && !_isOverlayAlertPlaying)
                        {
                            vca.resetOverlayLabelColor = true;
                            _timerOverlayDelayArgs = new TimerDelayArgs(DateTime.Now);
                            _timerOverlayShow.Start();
                        }
                    }
                    else
                    {
                        if (_timerOverlayShow.Enabled)
                        {
                            _timerOverlayShow.Stop();
                            vca.resetOverlayLabelColor = true;
                            _isOverlayAlertPlaying = false;
                        }
                        if (_overlayWorking)
                        {
                            vca.resetOverlayLabelContent = true;
                            _overlayWorking = false;
                            _isOverlayAlertPlaying = false;
                        }
                    }
                }
                else
                {
                    //if (_soundOut.PlaybackState == PlaybackState.Playing)
                    //{
                    vca.meterColor = VOLUME_OK;
                    vca.resetSoundLabelContent = true;
                    vca.resetOverlayLabelContent = true;
                    if (_soundOut.PlaybackState == PlaybackState.Playing)
                        _soundOut.Pause();
                    _isSoundAlertPlaying = false;

                    _isOverlayAlertPlaying = false;

                    if (_timerAlarmDelay != null)
                        if (_timerAlarmDelay.Enabled)
                        {
                            _timerAlarmDelay.Stop();
                        }
                    if (_timerOverlayShow.Enabled)
                    {
                        _timerOverlayShow.Stop();
                    }
                    if (_overlayWorking)
                    {
                        _overlayWorking = false;
                    }
                    //}
                }

                OnVolumeCheck(this, vca);
            }
            catch (Exception e)
            {
                Trace.TraceError("Volume check error: " + e);
            }
        }

        public void PlayAlarm(bool playAlarm = true)
        {
            if (_defaultDeviceChanged) return;
            _timerAlarmDelayArgs = new TimerDelayArgs(DateTime.Now);
            _timerAlarmDelayArgs.alarmActive = true;
            OnUpdateTimerAlarmDelay(this, _timerAlarmDelayArgs);
            _isSoundAlertPlaying = true;
            if (!playAlarm) return;

            if (_soundOut.GetType() == typeof(WasapiOut))
                if ((_soundOut as WasapiOut).Device.DeviceState != DeviceState.Active)
                    GetNewSoundOut();

            if (_soundOut.PlaybackState == PlaybackState.Paused) _soundOut.Resume();
            else _soundOut.Play();
        }

        public void ShowAlertWindow(bool playAlarm = true)
        {
            if (_defaultDeviceChanged) return;

            _isOverlayAlertPlaying = true;

            if (!playAlarm)
            {
                _overlayWorking = true;
                return;
            }

            System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcesses();
            var processSharp = new ProcessSharp((int)GetForegroundProcessId(), MemoryType.Remote);
            if (_alertOverlay != null)
            {
                //  _overlayWorking = false;
                if (!_alertOverlay.IsEnabled)
                {
                    _overlayWorking = false;
                }
                return;
            }

            _alertOverlay = new AlertOverlay();

            _alertOverlay.Initialize(processSharp.WindowFactory.MainWindow);
            _alertOverlay.Enable();
            _overlayWorking = true;

            // Do work
            _timerOverlayUpdate.Start();
        }

        private void GetCapture(bool isController, string deviceId = null)
        {
            if (!isController)
            {
                if(_soundCapture!= null)
                {
                    _soundCapture?.Stop();
                    _soundCapture?.Dispose();
                }
                using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
                {
                    //using (MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications))
                    //{
                    MMDevice device;
                    if (deviceId != null)
                        device = enumerator.GetDevice(deviceId);
                    else
                    {
                        device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, _currentCaptureRole);
                    }

                    _meter = AudioMeterInformation.FromDevice(device);
                    _soundCapture = new WasapiCapture(true, AudioClientShareMode.Shared, 250) { Device = device };
                    _soundCapture.Initialize();
                    _soundCapture.Start();
                    //}
                }
            }
        }


        #region Various

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

        #region System volume
        private SimpleAudioVolume GetSimpleAudioVolume()
        {
            try
            {
                Trace.TraceInformation("Getting simple audio volume device...");
                Trace.Indent();
                using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
                {
                    Trace.TraceInformation("Session manager OK");
                    using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                    {
                        Trace.TraceInformation("Session enumerator OK");
                        Trace.TraceInformation("Num. of sessions: {0}", sessionEnumerator.Count);
                        foreach (var session in sessionEnumerator)
                        {
                            var asControl2 = session.QueryInterface<AudioSessionControl2>();
                            if (asControl2 != null)
                            {
                                Trace.TraceInformation("Audio Session Control OK");
                                if (asControl2.Process != null)
                                {
                                    Trace.TraceInformation("Audio Session Control Process OK");
                                    if (asControl2.Process.ProcessName.ToLower().Contains("screamcontrol"))
                                    {
                                        Trace.Unindent();
                                        Trace.TraceInformation("... Simple audio volume OK");
                                        return session.QueryInterface<SimpleAudioVolume>();
                                    }
                                    else
                                        Trace.TraceInformation("No Scream Control in this process, trying another one.");
                                }
                                else
                                {
                                    Trace.TraceInformation("No process for this session control, trying another one.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _audioVolumeTryCount++;
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Trace.Unindent();
                Thread.Sleep(10000);
                if (_audioVolumeTryCount > 5)
                    System.Windows.Forms.Application.Restart();
                else
                    return GetSimpleAudioVolume();
            }
            return null;
        }

        private void KeepSystemVolume()
        {
            try
            {
                _systemSimpleAudioVolume.MasterVolume = _systemVolume;
                _systemSimpleAudioVolume.IsMuted = false;
            }
            catch (CoreAudioAPIException exp)
            {
                Trace.TraceError(exp.Message);
                _systemSimpleAudioVolume = GetSimpleAudioVolume();
            }
        }

        private AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            try
            {
                Trace.TraceInformation("Getting default audio session manager...");
                Trace.Indent();
                using (var enumerator = new MMDeviceEnumerator())
                {
                    Trace.TraceInformation("Device enumerator OK");
                    using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                    {
                        Trace.TraceInformation("Default audio endpoint OK");
                        AudioSessionManager2 sessionManager = null;
                        Thread getSessionThread = new Thread(() => sessionManager = AudioSessionManager2.FromMMDevice(device));
                        getSessionThread.SetApartmentState(ApartmentState.MTA);
                        getSessionThread.Start();
                        getSessionThread.Join();
                        Trace.Unindent();
                        Trace.TraceInformation("... Default audio session manager OK");
                        return sessionManager;
                    }
                }
            }
            catch (Exception e)
            {
                _sessionManagerTryCount++;
                Trace.TraceError(e.Message);
                Trace.TraceError(e.StackTrace);
                Trace.Unindent();
                Thread.Sleep(10000);
                if (_sessionManagerTryCount > 5)
                    System.Windows.Forms.Application.Restart();
                else
                    return GetDefaultAudioSessionManager2(dataFlow);
            }
            return null;
        }
        #endregion
    }
}
