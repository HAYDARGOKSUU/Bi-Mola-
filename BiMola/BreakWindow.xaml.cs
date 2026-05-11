using System;
using System.Windows;
using System.Windows.Threading;

namespace BiMola
{
    public partial class BreakWindow : Window
    {
        private DispatcherTimer? autoSleepTimer;
        private int countdown = 60;

        public bool RequestedExtraTime { get; private set; } = false;

        public BreakWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            autoSleepTimer = new DispatcherTimer();
            autoSleepTimer.Interval = TimeSpan.FromSeconds(1);
            autoSleepTimer.Tick += AutoSleepTimer_Tick;
            autoSleepTimer.Start();

            var storyboard = (System.Windows.Media.Animation.Storyboard)this.Resources["BreatheStoryboard"];
            storyboard.Begin();
        }

        private void AutoSleepTimer_Tick(object? sender, EventArgs e)
        {
            countdown--;
            AutoSleepText.Text = $"{countdown} saniye içinde uyku moduna geçilecek...";

            int cycleTime = (60 - countdown) % 15;
            if (cycleTime < 4)
            {
                BreatheText.Text = "Nefes Al...";
            }
            else if (cycleTime < 7)
            {
                BreatheText.Text = "Tut...";
            }
            else
            {
                BreatheText.Text = "Nefes Ver...";
            }

            if (countdown <= 0)
            {
                autoSleepTimer?.Stop();
                this.DialogResult = true; // Proceed to sleep
                this.Close();
            }
        }

        private void SleepButton_Click(object sender, RoutedEventArgs e)
        {
            autoSleepTimer?.Stop();
            this.DialogResult = true;
            this.Close();
        }

        private void ExtraTimeButton_Click(object sender, RoutedEventArgs e)
        {
            autoSleepTimer?.Stop();
            RequestedExtraTime = true;
            this.DialogResult = false;
            this.Close();
        }
    }
}
