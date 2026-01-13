//-----------------------------------------------------------------------
// <copyright file="MainWindow.cs" company="Lifeprojects.de">
//     Class: MainWindow
//     Copyright © Lifeprojects.de 2025
// </copyright>
//
// <author>Gerhard Ahrens - Lifeprojects.de</author>
// <email>developer@lifeprojects.de</email>
// <date>13.12.2025 19:20:27</date>
//
// <summary>
// MainWindow mit Minimalfunktionen
// </summary>
//-----------------------------------------------------------------------

namespace WPFMonitorKontrolle
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private IntPtr _lastMonitor;

        public MainWindow()
        {
            this.InitializeComponent();
            WeakEventManager<Window, RoutedEventArgs>.AddHandler(this, "Loaded", this.OnLoaded);
            WeakEventManager<Window, CancelEventArgs>.AddHandler(this, "Closing", this.OnWindowClosing);

            this.WindowTitel = "Minimal WPF Template";

            this.LocationChanged += this.OnLocationChanged;
            this.SizeChanged += (_, _) => this.OnEnsureVisible(this);

            this.DataContext = this;
        }

        private string _WindowTitel;

        public string WindowTitel
        {
            get { return _WindowTitel; }
            set
            {
                if (this._WindowTitel != value)
                {
                    this._WindowTitel = value;
                    this.OnPropertyChanged();
                }
            }
        }


        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            WeakEventManager<Button, RoutedEventArgs>.AddHandler(this.BtnCloseApplication, "Click", this.OnCloseApplication);
        }

        private void OnCloseApplication(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = false;

            MessageBoxResult msgYN = MessageBox.Show("Wollen Sie die Anwendung beenden?", "Beenden", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (msgYN == MessageBoxResult.Yes)
            {
                App.ApplicationExit();
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            var monitor = MonitorTool.GetCurrentMonitor(this);
            var current = NativeMethods.MonitorFromPoint(new POINT { X = monitor.rcMonitor.Left + 1, Y = monitor.rcMonitor.Top + 1 }, NativeMethods.MONITORDEFAULTTONEAREST);

            if (current != _lastMonitor)
            {
                this._lastMonitor = current;
                this.OnMonitorChanged();
                this.OnEnsureVisible(this);
            }
        }

        private void OnMonitorChanged()
        {
            Console.WriteLine("Monitor gewechselt");
            var monitors = MonitorTool.GetMonitors();
        }

        private void OnEnsureVisible(Window window)
        {
            var center = window.PointToScreen(
                new Point(window.Width / 2, window.Height / 2));

            var hMonitor = NativeMethods.MonitorFromPoint(new POINT { X = (int)center.X, Y = (int)center.Y }, NativeMethods.MONITORDEFAULTTONEAREST);

            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            NativeMethods.GetMonitorInfo(hMonitor, ref info);

            var work = info.rcWork;

            window.Left = Math.Max(work.Left, Math.Min(window.Left, work.Right - window.Width));
            window.Top = Math.Max(work.Top, Math.Min(window.Top, work.Bottom - window.Height));
        }


        #region INotifyPropertyChanged implementierung
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler == null)
            {
                return;
            }

            var e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }
        #endregion INotifyPropertyChanged implementierung
    }
}