using System.Security;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace HotCornerBarrier
{
    public class Program : ApplicationContext
    {
        private const string AppName = "HotCornerBarrier";
        const string RunAtStartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string HotCornerClipLengthKey = @"Control Panel\Desktop";
        private const string HotCornerClipLengthValue = "MouseCornerClipLength"; 
        private const int DefaultHotCornerClipLength = 6;
        private const string HotClipMenuItemFormat = "Change barrier height (currently {0})";

        private bool _leftBarrierEnabled;
        private bool LeftBarrierEnabled
        {
            get { return _leftBarrierEnabled; }
            set
            {
                if (LeftBarrierMenuItem != null)
                {
                    LeftBarrierMenuItem.Checked = value;
                }
                _leftBarrierEnabled = value;
            }
        }
        private MenuItem LeftBarrierMenuItem { get; set; }

        private bool _rightBarrierEnabled;
        private bool RightBarrierEnabled
        {
            get { return _rightBarrierEnabled; }
            set
            {
                if (RightBarrierMenuItem != null)
                {
                    RightBarrierMenuItem.Checked = value;
                }
                _rightBarrierEnabled = value;
            }
        }
        private MenuItem RightBarrierMenuItem { get; set; }

        private MenuItem StartupMenuItem { get; set; }

        private bool? _runAtStartup;
        private bool RunAtStartup
        {
            get
            {
                if (_runAtStartup == null)
                {
                    try
                    {
                        _runAtStartup = IsSetAtStartup(AppName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (SecurityException)
                    {
                    }
                }
                return _runAtStartup.HasValue && _runAtStartup.Value;
            }
            set
            {
                if (StartupMenuItem != null)
                {
                    StartupMenuItem.Checked = value;
                }
                try
                {
                    SetAtStartup(AppName, value);
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (SecurityException)
                {
                }
            }
        }

        private int _hotCornerClipLength;
        internal int HotCornerClipLength
        {
            get
            {
                if (_hotCornerClipLength > 0)
                {
                    return _hotCornerClipLength;
                }

                var length = 0;
                try
                {
                    var clipKey = Registry.CurrentUser.OpenSubKey(HotCornerClipLengthKey);
                    if (clipKey != null)
                    {
                        var value = clipKey.GetValue(HotCornerClipLengthValue);
                        if (value != null)
                        {
                            Int32.TryParse(value.ToString(), out length);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (SecurityException)
                {
                }
                return _hotCornerClipLength = length > 0 ? length : DefaultHotCornerClipLength;
            }

            set
            {
                if (value == _hotCornerClipLength)
                {
                    return;
                }
                if (value <= 0)
                {
                    value = DefaultHotCornerClipLength;
                }
                var minHeight = ScreenBounds.Min(b => b.Height);
                if (value > minHeight)
                {
                    throw new ArgumentException("Cannot set height greater than " + minHeight);
                }

                try
                {
                    var clipKey = Registry.CurrentUser.OpenSubKey(HotCornerClipLengthKey, true);
                    if (clipKey == null) return;

                    clipKey = Registry.CurrentUser.CreateSubKey(HotCornerClipLengthKey);
                    if (clipKey != null)
                    {
                        clipKey.SetValue(HotCornerClipLengthValue, value);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (SecurityException)
                {
                }
                HotCornerClipLengthMenuItem.Text = String.Format(
                    HotClipMenuItemFormat, value);
                _hotCornerClipLength = value;
            }
        }
        private MenuItem HotCornerClipLengthMenuItem { get; set; }

        private bool IsClipped { get; set; }

        private readonly HookProc _mouseHookCallback;

        private Rectangle[] ScreenBounds { get; set; }

        internal Rectangle MinDimensions
        {
            get
            {
                return new Rectangle
                    {
                        Height = ScreenBounds.Min(b => b.Height),
                        Width = ScreenBounds.Min(b => b.Width)
                    };
            }
        }

// ReSharper disable UnusedAutoPropertyAccessor.Local
        private NotifyIcon TrayIcon { get; set; }
// ReSharper restore UnusedAutoPropertyAccessor.Local

        private IntPtr MouseHookPtr { get; set; }

// ReSharper disable InconsistentNaming
        const int WH_MOUSE_LL = 14;
// ReSharper restore InconsistentNaming
        delegate IntPtr HookProc(int nCode, uint wParam, IntPtr lParam);

        public Program()
        {
            LeftBarrierMenuItem = new MenuItem("Left barrier enabled", (sender, args) => 
                LeftBarrierEnabled = !LeftBarrierMenuItem.Checked);
            LeftBarrierEnabled = true;

            RightBarrierMenuItem = new MenuItem("Right barrier enabled", (sender, args) => 
                RightBarrierEnabled = !RightBarrierMenuItem.Checked);
            RightBarrierEnabled = false;

            StartupMenuItem = new MenuItem("Run at startup", (sender, args) =>
                RunAtStartup = !((MenuItem) sender).Checked);
            RunAtStartup = RunAtStartup;  // Use its side effects.

            try
            {
                HotCornerClipLength = HotCornerClipLength; // Use its side effects.
            }
            catch (ArgumentException)
            {
                HotCornerClipLength = DefaultHotCornerClipLength;
            }
            HotCornerClipLengthMenuItem = new MenuItem(String.Format(
                HotClipMenuItemFormat, HotCornerClipLength), OnClipLengthClicked);

            ThreadExit += OnQuit;
            TrayIcon = new NotifyIcon
                {
                    Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                    ContextMenu = new ContextMenu(new[]
                        {
                            LeftBarrierMenuItem,
                            RightBarrierMenuItem,
                            new MenuItem("-"),
                            StartupMenuItem,
                            HotCornerClipLengthMenuItem,
                            new MenuItem("-"), 
                            new MenuItem("Quit", OnQuit)
                        }),
                    Visible = true
                };

            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            OnDisplaySettingsChanged(null, null);

            _mouseHookCallback = LowLevelMouseProc;

            MouseHookPtr = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookCallback , GetModuleHandle(null), 0);
        }

        private void OnMouseMove(int x, int y)
        {
            var screen = ScreenBounds.Where(r =>
                r.Left <= x && x < r.Right &&
                r.Top <= y && y < r.Bottom).FirstOrDefault();

            var topClip = new Rectangle(
                    screen.Left, screen.Top, screen.Width, HotCornerClipLength);

            if (IsClipped && (
                    topClip.Bottom == y + 1 ||
                    !LeftBarrierEnabled && topClip.Left == x ||
                    !RightBarrierEnabled && x == topClip.Right - 1))
            {
                IsClipped = false;
                Cursor.Clip = Rectangle.Empty;
                Debug.WriteLine("Unclipping.");
            }
            else if (!IsClipped && topClip.Contains(x, y))
            {
                IsClipped = true;
                Cursor.Clip = topClip;
                Debug.WriteLine("Clipping.");
            }
        }

        private void OnClipLengthClicked(object sender, EventArgs args)
        {

            var prompt = new ChangeBarrierHeightForm(this);
            prompt.ShowDialog();
        }

        private static void OnQuit(object sender, EventArgs args)
        {
            Application.Exit();
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs eArgs)
        {
            ScreenBounds = Screen.AllScreens.Select(s => s.Bounds).ToArray();
        }

        private static bool IsSetAtStartup(string appName)
        {
            var startupKey = Registry.LocalMachine.OpenSubKey(RunAtStartupKey, true);
            return startupKey != null && startupKey.GetValue(appName) != null;
        }

        private static void SetAtStartup(string appName, bool enable)
        {

            var startupKey = Registry.LocalMachine.OpenSubKey(RunAtStartupKey, true);
            if (startupKey == null) return;

            if (enable)
            {
                if (startupKey.GetValue(appName) == null)
                {
                    startupKey.SetValue(appName, Application.ExecutablePath);
                    startupKey.Close();
                }
            }
            else
            {
                startupKey.DeleteValue(appName, false);
                startupKey.Close();
            }
        }


        #region Hooks

        IntPtr LowLevelMouseProc(int nCode, uint wParam, IntPtr lParam)
        {
            OnMouseMove(Cursor.Position.X, Cursor.Position.Y);
            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandle(string moduleName);

        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, uint wParam, IntPtr lParam);

        #endregion

        #region IDisposable

        ~Program()
        {
            Dispose(false);
        }

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (MouseHookPtr != IntPtr.Zero)
            {
                UnhookWindowsHookEx(MouseHookPtr);
            }

            Cursor.Clip = Rectangle.Empty;
            
            if (disposing && TrayIcon != null)
            {
                TrayIcon.Dispose();
                TrayIcon = null;
            }
            base.Dispose(disposing);
        }

        #endregion

        static void Main()
        {
            Application.Run(new Program());
        }
    }
}
