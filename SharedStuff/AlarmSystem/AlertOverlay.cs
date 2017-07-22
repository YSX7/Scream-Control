using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
//using System.Threading.Tasks;
using Overlay.NET;
using Overlay.NET.Common;
using Overlay.NET.Wpf;
using Process.NET;
using Process.NET.Memory;
using Process.NET.Windows;

namespace ScreamControl.Alarms
{
    //TODO: that invoke things scares me, maybe there is another way to update window

    public class AlertOverlay : WpfOverlayPlugin
    {
        readonly TickEngine _tickEngine = new TickEngine();

        Label _label;

        bool _isDisposed;
        bool _isSetup;

        public override void Enable()
        {
            _tickEngine.IsTicking = true;
            base.Enable();
        }

        public override void Disable()
        {
            _tickEngine.IsTicking = false;
            base.Disable();
        }

        public override void Initialize(IWindow targetWindow)
        {
            // Set target window by calling the base method
            base.Initialize(targetWindow);

            OverlayWindow = new OverlayWindow(targetWindow)
            {
                Height = TargetWindow.Height,
                Width = TargetWindow.Width,
                ShowInTaskbar = false,
                Topmost = true
            };
            // OverlayWindow.Deactivated += OverlayWindow_Deactivated;

            // Set up update interval and register events for the tick engine.
            _tickEngine.Interval = TimeSpan.FromMilliseconds(1000 / 60);
            _tickEngine.PreTick += OnPreTick;
            _tickEngine.Tick += OnTick;
        }

        void OnTick(object sender, EventArgs eventArgs)
        {
            // This will only be true if the target window is active
            // (or very recently has been, depends on your update rate)
            if (OverlayWindow.IsVisible)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (OverlayWindow != null)
                        OverlayWindow.Update();
                });
            }
        }

        void OnPreTick(object sender, EventArgs eventArgs)
        {
            // Only want to set them up once.
            if (!_isSetup)
            {
                SetUp();
                _isSetup = true;
            }

            var activated = TargetWindow.IsActivated;
            var visible = OverlayWindow.IsVisible;

            // Ensure window is shown or hidden correctly prior to updating
            //if (!activated && visible)
            //{
            //    OverlayWindow.Hide();
            //}
            //else
            if (activated && !visible)
            {
                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    if (OverlayWindow != null)
                        OverlayWindow.Show();
                });
            }
        }

        public override void Update()
        {
            // Raises the events only when the given interval has
            // passed since the last event, so it is okay to call every frame
            _tickEngine.Pulse();
        }

        // Clear objects
        public override void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (IsEnabled)
            {
                Disable();
            }

                Application.Current.Dispatcher.Invoke((Action)delegate
                {
                    try
                    {
                        OverlayWindow.Hide();
                        OverlayWindow.Close();
                        OverlayWindow = null;
                    }
                    catch(NullReferenceException e)
                    {
                        
                    }
                });

            _tickEngine.Stop();

            base.Dispose();
            _isDisposed = true;
        }

        ~AlertOverlay()
        {
            Dispose();
        }

        void SetUp()
        {
            Application.Current.Dispatcher.Invoke((Action)delegate
            {

                var _grid = new Grid
                {
                    VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                    Width = OverlayWindow.Width,
                    Height = OverlayWindow.Height
                };

                _label = new Label
                {
                    FontSize = 28,
                    Foreground = Brushes.Red,
                    Background = (Brush)Application.Current.FindResource("OverlayLabelBG"),
                    Content = Application.Current.FindResource("m_AlertWindow"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                _grid.Children.Add(_label);
                OverlayWindow.Add(_grid);

            });

        }

        private void OverlayWindow_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Activate();
        }
    }
}
