using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace MediaControl
{
    internal class MediaControl
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        private static readonly int KEYEVENTF_KEYUP = 0x0002;

        private const byte MediaPlayPause = (byte)Keys.MediaPlayPause;
        private const byte MediaNextTrack = (byte)Keys.MediaNextTrack;
        private const byte MediaPreviousTrack = (byte)Keys.MediaPreviousTrack;
        private const byte MediaStop = (byte)Keys.MediaStop;

        public static void PlayPause()
        {
            keybd_event(MediaPlayPause, MediaPlayPause, 0, 0);
            keybd_event(MediaPlayPause, MediaPlayPause, KEYEVENTF_KEYUP, 0);
        }
        public static void PrevTrack()
        {
            keybd_event(MediaPreviousTrack, MediaPreviousTrack, 0, 0);
            keybd_event(MediaPreviousTrack, MediaPreviousTrack, KEYEVENTF_KEYUP, 0);
        }
        public static void NextTrack()
        {
            keybd_event(MediaNextTrack, MediaNextTrack, 0, 0);
            keybd_event(MediaNextTrack, MediaNextTrack, KEYEVENTF_KEYUP, 0);
        }
        public static void Stop()
        {
            keybd_event(MediaStop, MediaStop, 0, 0);
            keybd_event(MediaStop, MediaStop, KEYEVENTF_KEYUP, 0);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        private IntPtr _spotifyWindow;
        private string _currentSong;

        public string CurrentSong
        {
            get { return _currentSong; }
            set 
            {
                if (_currentSong != value)
                {
                    _currentSong = value;
                    OnSongChanged?.Invoke(value);
                }
            }
        }

        private bool _connectedToSpotify;

        public bool ConnectedToSpotify
        {
            get { return _connectedToSpotify; }
            set 
            { 
                if (_connectedToSpotify != value)
                {
                    _connectedToSpotify = value;
                }
            }
        }


        public delegate void OnSongChangedHandler(string newSong);
        public OnSongChangedHandler OnSongChanged;

        public delegate void OnConnectionChangedHandler(bool connected);
        public OnConnectionChangedHandler OnConnectionChanged;


        public void Initialize()
        {
            Timer timer = new(0.1f);
            timer.Elapsed += Update;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void Update(object? sender, ElapsedEventArgs e)
        {
            ConnectedToSpotify = _spotifyWindow != IntPtr.Zero && IsWindow(_spotifyWindow);

            if (_spotifyWindow == IntPtr.Zero || !IsWindow(_spotifyWindow))
            {
                var spotifyProcess = Process.GetProcessesByName("Spotify").FirstOrDefault();
                if (spotifyProcess == null)
                    return;

                _spotifyWindow = new MainWindowFinder().FindMainWindow(spotifyProcess.Id);
                if (_spotifyWindow == IntPtr.Zero || !IsWindow(_spotifyWindow))
                    return;
            }

            var len = GetWindowTextLength(_spotifyWindow) * 2;
            var sb = new StringBuilder(len + 1);
            GetWindowText(_spotifyWindow, sb, sb.Capacity);

            var song = sb.ToString();
            CurrentSong = !song.StartsWith("Spotify") ? song : string.Empty;
        }
    }

    internal class MainWindowFinder
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindowType uCmd);
        private delegate bool CallBackPtr(IntPtr hwnd, int lParam);
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(CallBackPtr lpEnumFunc, int lParam);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private enum GetWindowType : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        private IntPtr _bestHandle;
        private int _processId;

        public IntPtr FindMainWindow(int processId)
        {
            _bestHandle = IntPtr.Zero;
            _processId = processId;

            EnumWindows(EnumWindowsThunk, _processId);

            return _bestHandle;
        }

        private bool EnumWindowsThunk(IntPtr hWnd, int processId)
        {
            GetWindowThreadProcessId(hWnd, out var pid);
            if (pid != processId || !IsMainWindow(hWnd))
                return true;
            _bestHandle = hWnd;
            return false;
        }

        private static bool IsMainWindow(IntPtr hWnd)
        {
            if (GetWindow(hWnd, GetWindowType.GW_OWNER) == IntPtr.Zero && IsWindowVisible(hWnd))
                return true;
            return false;
        }
    }
}
