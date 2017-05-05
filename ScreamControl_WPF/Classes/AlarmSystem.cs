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
using System.Windows.Media;
using System.Windows.Threading;

namespace ScreamControl_Client
{
    class AlarmSystem
    {
        private readonly Brush VOLUME_OK = new SolidColorBrush(Color.FromArgb(100, 127, 255, 121));
        private readonly Brush VOLUME_HIGH = new SolidColorBrush(Color.FromArgb(100, 255, 121, 121));

        public bool enabled = false;

        public float alarmThreshold = 80;
        public int captureMultiplier = 100;
        public float delayBeforeAlarm = 0,
            delayBeforeOverlay = 0;
        public enum States { Stopped, Running, Stopping, Closing, Closed };
        public States state = States.Stopped;


        private float _alarmVolume;
        public float AlarmVolume
        {
            get
            {
                return _alarmVolume;
            }
            set
            {
                _alarmVolume = value;
                if (_soundOut != null)
                {
                    _soundOut.Volume = value;
                }
            }

        }

        private bool _overlayWorking = false;

        private AudioMeterInformation _meter;
        private AlertOverlay _alertOverlay;
        private readonly BackgroundWorker _bgInputListener = new BackgroundWorker();
        private DispatcherTimer _timerAlarmDelay,
            _timerOverlayShow,
            _timerOverlayUpdate;
        private WasapiCapture _soundCapture;
        private ISoundOut _soundOut;

        //private bool _loggingEnabled = false;
        private float _systemVolume;

        private SimpleAudioVolume _systemSimpleAudioVolume;
        public float SystemVolume
        {
            get
            {
                return _systemVolume;
            }
            set
            {
                _systemVolume = value;
                _systemSimpleAudioVolume.MasterVolume = value;
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
            public bool resetLabelColor;
            public bool resetLabelContent;

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


        public AlarmSystem()
        {
            //_loggingEnabled = Trace.Listeners.Count > 1;
            Trace.TraceInformation("Alarm system init");
            Trace.Indent();
            state = States.Running;
            using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
            {
                using (MMDevice device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications))
                {
                    _meter = AudioMeterInformation.FromDevice(device);
                    _soundCapture = new WasapiCapture(true, AudioClientShareMode.Shared, 250) { Device = device };
                    _soundCapture.Initialize();
                    _soundCapture.Start();
                    Trace.TraceInformation("Sound Capture OK");
                }
            }
            IWaveSource soundSource = GetSoundSource();
            soundSource = soundSource.Loop();
            _soundOut = GetSoundOut();
            _soundOut.Initialize(soundSource);
            Trace.TraceInformation("Sound Out OK");

            captureMultiplier = Properties.Settings.Default.Boost;
            delayBeforeAlarm = Properties.Settings.Default.SafeScreamZone;
            delayBeforeOverlay = Properties.Settings.Default.AlertOverlayDelay;
            AlarmVolume = (float)Properties.Settings.Default.Volume / 100;

            _systemSimpleAudioVolume = GetSimpleAudioVolume();

            _bgInputListener.WorkerSupportsCancellation = true;
            _bgInputListener.DoWork += bgInputListener_DoWork;
            _bgInputListener.RunWorkerCompleted += bgInputListener_RunWorkerCompleted;

            _bgInputListener.RunWorkerAsync();
            Trace.TraceInformation("Background worker running");

            #region Timers

            _timerAlarmDelay = new DispatcherTimer();
            _timerAlarmDelay.Tick += (s, args) =>
            {
                if (_timerAlarmDelayArgs.ElapsedTime.Seconds >= delayBeforeAlarm)
                {
                    _timerAlarmDelayArgs.alarmActive = true;

                    _timerAlarmDelay.Stop();
                    _timerOverlayDelayArgs = new TimerDelayArgs(DateTime.Now);
                    PlayAlarm();
                    _timerOverlayShow.Start();
                }

                OnUpdateTimerAlarmDelay(this, _timerAlarmDelayArgs);
                if (_timerAlarmDelay.Dispatcher.HasShutdownStarted)
                    _timerAlarmDelayArgs = null;
            };

            _timerOverlayShow = new DispatcherTimer();
            _timerOverlayShow.Tick += (s, args) =>
            {
                if (_timerOverlayDelayArgs.ElapsedTime.Seconds >= delayBeforeOverlay)
                {
                    _timerOverlayDelayArgs.alarmActive = true;
                    _timerOverlayShow.Stop();
                    ShowAlertWindow();
                }
                OnUpdateTimerOverlayDelay(this, _timerOverlayDelayArgs);
                if (_timerOverlayShow.Dispatcher.HasShutdownStarted)
                    _timerOverlayDelayArgs = null;
            };

            _timerOverlayUpdate = new DispatcherTimer();
            _timerOverlayUpdate.Interval = TimeSpan.FromMilliseconds(10);
            _timerOverlayUpdate.Tick += (s, args) =>
            {
                _alertOverlay.Update();
                if (!_overlayWorking)
                {
                    _timerOverlayUpdate.Stop();
                    _alertOverlay.Disable();
                }
            };
            #endregion

            Trace.TraceInformation("Timers initialized");
            Trace.TraceInformation("Alarm System up and running!");
            Trace.Unindent();
        }

        public void Close()
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
            Trace.TraceInformation("Alarm System closing");
            state = States.Closing;
            //_soundCapture.Stop();
            //_soundCapture.Dispose();
            //_soundOut.Stop();
            //_soundOut.Dispose();
            _overlayWorking = false;
            _timerAlarmDelay.Stop();
            _timerOverlayShow.Stop();
            _timerOverlayUpdate.Stop();
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
            return _meter.PeakValue * captureMultiplier;
        }

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
                return new WasapiOut();
            else
                return new DirectSoundOut();
        }

        private IWaveSource GetSoundSource()
        {
            //return any source ... in this example, we'll just play a mp3 file
            return CodecFactory.Instance.GetCodec("beep.mp3");
        }

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
            catch(Exception ex)
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
            _soundCapture.Stop();
            _soundCapture.Dispose();
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
                float volume = GetVolumeInfo();
                volume = volume.Clamp(0, 100);
                if (this.enabled)
                {
                    VolumeCheck(volume);
                }
                MonitorArgs ma = new MonitorArgs(volume);
                OnMonitorUpdate(this, ma);
            }
            catch(Exception ex)
            {
                Trace.TraceInformation("Something happend at volume monitor: {0}", ex);
            }
        }

        private void VolumeCheck(float volume)
        {
            VolumeCheckArgs vca;
            vca = new VolumeCheckArgs(VOLUME_OK);
            if (volume >= alarmThreshold)
            {
                KeepSystemVolume();
                vca.meterColor = VOLUME_HIGH;
                if (delayBeforeAlarm > 0)
                {
                    if (!_timerAlarmDelay.IsEnabled && _soundOut.PlaybackState != PlaybackState.Playing)
                    {
                        vca.resetLabelColor = true;
                        _timerAlarmDelayArgs = new TimerDelayArgs(DateTime.Now);
                        _timerAlarmDelay.Start();
                    }
                    else return;
                }
                else
                    PlayAlarm();
                OnVolumeCheck(this, vca);
            }
            else
            {
                //if (_soundOut.PlaybackState == PlaybackState.Playing)
                //{
                vca.meterColor = VOLUME_OK;
                vca.resetLabelContent = true;
                _soundOut.Pause();

                if (_timerAlarmDelay.IsEnabled)
                {
                    _timerAlarmDelay.Stop();
                }
                if (_timerOverlayShow.IsEnabled)
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

        private void PlayAlarm()
        {
            if (_soundOut.PlaybackState == PlaybackState.Paused) _soundOut.Resume();
            else _soundOut.Play();
        }

        private void ShowAlertWindow()
        {
            _alertOverlay = new AlertOverlay();

            System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcesses();

            var processSharp = new ProcessSharp((int)GetForegroundProcessId(), MemoryType.Remote);

            _alertOverlay.Initialize(processSharp.WindowFactory.MainWindow);
            _alertOverlay.Enable();
            _overlayWorking = true;

            // Do work
            _timerOverlayUpdate.Start();
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
            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        var asControl2 = session.QueryInterface<AudioSessionControl2>();
                        if (asControl2.Process.ProcessName.ToLower().Contains("screamcontrol"))
                        {
                            Trace.TraceInformation("Simple audio volume OK");
                            return session.QueryInterface<SimpleAudioVolume>();
                        }
                    }
                }
            }
            return null;
        }

        private void KeepSystemVolume()
        {
            _systemSimpleAudioVolume.MasterVolume = _systemVolume;
            _systemSimpleAudioVolume.IsMuted = false;
        }

        private AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }
        #endregion
    }
}
