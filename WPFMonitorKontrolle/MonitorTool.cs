namespace WPFMonitorKontrolle
{
    using System.Management;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;

    public class MonitorTool
    {

        public static List<WmiMonitor> GetMonitorNames()
        {
            var list = new List<WmiMonitor>();

            try
            {
                var searcher = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM WmiMonitorID");

                foreach (ManagementObject obj in searcher.Get())
                {
                    list.Add(new WmiMonitor
                    {
                        InstanceName = obj["InstanceName"].ToString(),
                        Manufacturer = Decode((ushort[])obj["ManufacturerName"]),
                        Model = Decode((ushort[])obj["UserFriendlyName"]),
                        Serial = Decode((ushort[])obj["SerialNumberID"]),
                    });
                }

                if (list.Count == 1)
                {
                    list[0].InstanceName = list[0].InstanceName.Replace("DISPLAY", "DISPLAY1");
                }
            }
            catch (Exception ex)
            {
                string errorText = ex.Message;
                throw;
            }

            return list;
        }

        public static List<MonitorInfo> GetMonitors()
        {
            var result = new List<MonitorInfo>();

            var screens = Screen.AllScreens;
            var wmi = GetMonitorNames();

            for (int i = 0; i < screens.Length; i++)
            {
                var screen = screens[i];

                var hMonitor = NativeMethods.GetMonitorHandle(screen);
                uint dpiX = 0, dpiY = 0;
                int hr = NativeMethods.GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out dpiX, out dpiY);

                if (hr != 0)
                {
                    // Fehler beim Abrufen der DPI, Standardwerte verwenden
                    dpiX = 96;
                    dpiY = 96;
                }

                var wmiMonitor = i < wmi.Count ? wmi[i] : null;

                result.Add(new MonitorInfo
                {
                    DisplayName = screen.DeviceName,
                    IsPrimary = screen.Primary,

                    Manufacturer = wmiMonitor?.Manufacturer ?? "Unbekannt",
                    Model = wmiMonitor?.Model ?? "Unbekannt",
                    Serial = wmiMonitor?.Serial ?? "Unbekannt",

                    DpiX = (int)dpiX,
                    DpiY = (int)dpiY,
                    X = screen.Bounds.Left,
                    Y = screen.Bounds.Top,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    PositionDescription = GetPositionDescription(screen)
                });
            }

            return result;
        }

        static string Decode(ushort[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            var chars = data.TakeWhile(c => c != 0).Select(c => (char)c).ToArray();

            return new string(chars);
        }

        static string GetPositionDescription(Screen screen)
        {
            if (screen.Primary)
            {
                return "Primär";
            }

            if (screen.Bounds.Right <= Screen.PrimaryScreen.Bounds.Left)
            {
                return "Links vom Primärmonitor";
            }

            if (screen.Bounds.Left >= Screen.PrimaryScreen.Bounds.Right)
            {
                return "Rechts vom Primärmonitor";
            }

            if (screen.Bounds.Bottom <= Screen.PrimaryScreen.Bounds.Top)
            {
                return "Oberhalb des Primärmonitors";
            }

            if (screen.Bounds.Top >= Screen.PrimaryScreen.Bounds.Bottom)
            {
                return "Unterhalb des Primärmonitors";
            }

            return "Überlappend / Benutzerdefiniert";
        }

        internal static MONITORINFO GetCurrentMonitor(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;

            // Fenster-Mittelpunkt
            var source = PresentationSource.FromVisual(window);
            var dpi = source.CompositionTarget.TransformToDevice;

            var center = window.PointToScreen(new Point(window.ActualWidth / 2, window.ActualHeight / 2));

            var hMonitor = NativeMethods.MonitorFromPoint(new POINT { X = (int)center.X, Y = (int)center.Y }, NativeMethods.MONITORDEFAULTTONEAREST);

            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            NativeMethods.GetMonitorInfo(hMonitor, ref info);

            return info;
        }

    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DisplayDevice
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    public enum MonitorDpiType
    {
        EffectiveDpi = 0,
        AngularDpi = 1,
        RawDpi = 2
    }

    public static class NativeMethods
    {
        public const int MONITORDEFAULTTONEAREST = 2;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

        public static IntPtr GetMonitorHandle(Screen screen)
        {
            return MonitorFromPoint(new Point(screen.Bounds.Left + 1,screen.Bounds.Top + 1), 2);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr MonitorFromPoint(Point pt, uint flags);

        [DllImport("shcore.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromPoint(POINT pt, int flags);

        [DllImport("user32.dll")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO info);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    public class WmiMonitor
    {
        public string InstanceName { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Serial { get; set; }

    }

    public class MonitorInfo
    {
        public string DisplayName { get; set; }
        public bool IsPrimary { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public string Serial { get; set; }

        public int DpiX { get; set; }
        public int DpiY { get; set; }
        public double ScaleFactor => DpiX / 96.0;

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string PositionDescription { get; set; }
    }
}
