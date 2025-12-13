namespace WPFMonitorKontrolle
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Forms;
    using System.Windows.Media;

    using Microsoft.Win32;
    public static class WindowVisibilityBehavior
    {
        public static readonly DependencyProperty KeepWindowVisibleProperty =
            DependencyProperty.RegisterAttached(
                "KeepWindowVisible",
                typeof(bool),
                typeof(WindowVisibilityBehavior),
                new PropertyMetadata(false, OnKeepWindowVisibleChanged));

        public static void SetKeepWindowVisible(Window element, bool value)
        {
            element.SetValue(KeepWindowVisibleProperty, value);
        }

        public static bool GetKeepWindowVisible(Window element)
        {
            return (bool)element.GetValue(KeepWindowVisibleProperty);
        }

        private static void OnKeepWindowVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (d is Window window)
            {
                if ((bool)e.NewValue)
                {
                    window.Loaded += Window_Loaded;
                    window.Closed += Window_Closed;
                }
                else
                {
                    window.Loaded -= Window_Loaded;
                    window.Closed -= Window_Closed;
                }
            }
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = (Window)sender;

            EnsureWindowIsVisible(window);

            SystemEvents.DisplaySettingsChanged += (s, ev) =>
            {
                window.Dispatcher.Invoke(() => EnsureWindowIsVisible(window));
            };
        }

        private static void Window_Closed(object sender, EventArgs e)
        {
            // Wichtig: Das Event muss abgemeldet werden!
            SystemEvents.DisplaySettingsChanged -= (s, ev) => { };
        }

        // ---------------- Sichtbarkeits- und Repositionierungslogik ----------------

        private static void EnsureWindowIsVisible(Window window)
        {
            var dpi = VisualTreeHelper.GetDpi(window);

            double winLeftPx = window.Left * dpi.DpiScaleX;
            double winTopPx = window.Top * dpi.DpiScaleY;
            double winRightPx = winLeftPx + (window.Width * dpi.DpiScaleX);
            double winBottomPx = winTopPx + (window.Height * dpi.DpiScaleY);

            var screens = Screen.AllScreens;

            bool intersectsAnyScreen = screens.Any(s =>
            {
                var area = s.WorkingArea;
                return winRightPx > area.Left &&
                       winLeftPx < area.Right &&
                       winBottomPx > area.Top &&
                       winTopPx < area.Bottom;
            });

            if (intersectsAnyScreen == false)
            {
                var primary = Screen.PrimaryScreen;
                double screenWidth = primary.WorkingArea.Width / dpi.DpiScaleX;
                double screenHeight = primary.WorkingArea.Height / dpi.DpiScaleY;

                window.Left = (screenWidth - window.Width) / 2;
                window.Top = (screenHeight - window.Height) / 2;
            }

            CorrectWindowEdges(window, dpi, intersectsAnyScreen);
        }

        private static void CorrectWindowEdges(Window window, DpiScale dpi, bool intersectsAnyScreen)
        {
            var primary = Screen.PrimaryScreen.WorkingArea;

            double leftLimit = primary.Left / dpi.DpiScaleX;
            double topLimit = primary.Top / dpi.DpiScaleY;
            double rightLimit = primary.Right / dpi.DpiScaleX;
            double bottomLimit = primary.Bottom / dpi.DpiScaleY;

            const double MARGIN = 20;

            if (window.Left + MARGIN > rightLimit)
            {
                window.Left = rightLimit - window.Width;
            }

            if (window.Top + MARGIN > bottomLimit)
                window.Top = bottomLimit - window.Height;

            if (window.Left + window.Width - MARGIN < leftLimit)
            {
                window.Left = leftLimit;
            }

            if (window.Top + window.Height - MARGIN < topLimit)
            {
                window.Top = topLimit;
            }

            if (intersectsAnyScreen == true)
            {
                double screenWidth = primary.Width / dpi.DpiScaleX;
                double screenHeight = primary.Height / dpi.DpiScaleY;

                window.Left = (screenWidth - window.Width) / 2;
                window.Top = (screenHeight - window.Height) / 2;
            }
        }
    }
}
