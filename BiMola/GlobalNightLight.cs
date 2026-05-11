using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace BiMola
{
    public class GlobalNightLight : Window
    {
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int GWL_EXSTYLE = (-20);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        public GlobalNightLight()
        {
            this.WindowStyle = WindowStyle.None;
            this.AllowsTransparency = true;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.IsHitTestVisible = false;
            this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 255, 175, 45));
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }

        public void SetIntensity(double intensity)
        {
            // Daha yumuşak, Windows Gece Işığına çok benzeyen pastel bir sarı/turuncu ton (Sunset Amber)
            byte alpha = (byte)(intensity * 130);
            this.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(alpha, 255, 175, 45));
        }
    }
}
