using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BiMola
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? timer;
        private DispatcherTimer? realTimeClock;
        private DispatcherTimer? tipTimer;
        private TimeSpan timeLeft;
        private double totalSeconds;
        private bool isPaused = false;
        private System.Windows.Forms.NotifyIcon? notifyIcon;
        private System.Windows.Media.MediaPlayer? ambientPlayer;
        private GlobalNightLight? globalNightLight;

        private string[] tips = new string[] {
            "💡 İpucu: 20-20-20 Kuralı: Her 20 dakikada bir, 6 metre uzağa 20 saniye bakın.",
            "💡 İpucu: Ekran parlaklığını ortama göre ayarlayarak göz yorgunluğunu azaltın.",
            "💡 İpucu: Sık sık göz kırpmayı unutmayın; ekrandayken göz kırpma oranımız düşer.",
            "💡 İpucu: Su içmek bedeninizi canlı tutar. Bir bardak su alın!",
            "💡 İpucu: Dik oturun. Ekranın üst kenarı göz hizanızda olmalı.",
            "💡 İpucu: Odaklanmak için telefonu sessize veya uçak moduna alın."
        };
        private int tipIndex = 0;

        private string[] healthInfos = new string[] {
            "Uzun süre ekrana bakmak miyopi riskini artırır ve göz kuruluğuna yol açar. Bi'Mola ile kendinize dijital sınırlar çizin.",
            "🥕 Göze iyi gelen besinler: Havuç, ıspanak ve somon gibi A vitamini ve Omega-3 içeren gıdalar tüketmeyi ihmal etmeyin.",
            "💧 Göz damlası kullanmak veya düzenli su içmek, ekrana bakarken kuruyan gözlerinizi nemlendirir.",
            "☀️ Doğal gün ışığı almak, vücut saatinizi dengeler ve uyku kalitenizi artırarak odaklanmanıza yardımcı olur.",
            "🧘 Zorlu Mod, iradenizi güçlendirir ve sizi masa başında saatlerce kalmaktan alıkoyarak postürünüzü korur.",
            "👁️ Ekran mesafeniz kol boyu kadar (50-70 cm) olmalı ve ekran göz seviyenizin biraz altında yer almalıdır.",
            "🌙 Gece Işığı (Mavi Işık Filtresi): Gece modunu kullanarak çalışırsanız gözleriniz daha az yorulur ve uykuya dalmanız kolaylaşır!"
        };
        private int healthIndex = 0;

        [DllImport("PowrProf.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

        [DllImport("user32.dll")]
        public static extern void LockWorkStation();

        public MainWindow()
        {
            InitializeComponent();
            
            // Verileri Yükle
            UserDataManager.Load();
            
            // Uygulama kapanırken kaydet
            this.Closing += (s, e) => UserDataManager.Save();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            UpdateUIFromSettings();
            InitializeRealTimeClock();
        }

        private void UpdateUIFromSettings()
        {
            if (SoundToggle != null) SoundToggle.IsChecked = UserDataManager.Current.IsSoundEnabled;
            if (StrictModeToggle != null) StrictModeToggle.IsChecked = UserDataManager.Current.IsHardModeEnabled;
            if (NightLightToggle != null) NightLightToggle.IsChecked = UserDataManager.Current.IsNightLightEnabled;
            if (NightLightSlider != null) NightLightSlider.Value = UserDataManager.Current.NightLightIntensity;
            
            // Gamification UI
            if (LevelText != null) LevelText.Text = $"Seviye {UserDataManager.Current.Level}";
            if (FocusStatsText != null) FocusStatsText.Text = $"{UserDataManager.Current.DailyFocusMinutes} Dk (Bugün)";
        }

        private void InitializeRealTimeClock()
        {
            // Setup Real Time Clock
            realTimeClock = new DispatcherTimer();
            realTimeClock.Interval = TimeSpan.FromSeconds(1);
            realTimeClock.Tick += (s, e) => { CurrentTimeText.Text = DateTime.Now.ToString("HH:mm"); };
            realTimeClock.Start();
            CurrentTimeText.Text = DateTime.Now.ToString("HH:mm");

            // Setup Tip Timer
            tipTimer = new DispatcherTimer();
            tipTimer.Interval = TimeSpan.FromSeconds(10);
            tipTimer.Tick += TipTimer_Tick;
            tipTimer.Start();

            // Setup NotifyIcon for background functionality
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            notifyIcon.Visible = true;
            notifyIcon.Text = "Bi'Mola";
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
            
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Göster", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Çıkış", null, (s, e) => System.Windows.Application.Current.Shutdown());
            notifyIcon.ContextMenuStrip = contextMenu;

            // Setup MediaPlayer for MP3 ambient sounds
            ambientPlayer = new System.Windows.Media.MediaPlayer();
            ambientPlayer.Volume = 0.5; // Default volume
            ambientPlayer.MediaEnded += (s, e) => {
                ambientPlayer.Position = TimeSpan.Zero;
                ambientPlayer.Play();
            };
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
            ambientPlayer?.Stop();
            globalNightLight?.Close();
            base.OnClosed(e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            int hours = 0;
            int minutes = 0;
            int seconds = 0;

            if (!string.IsNullOrWhiteSpace(HoursInput.Text))
                int.TryParse(HoursInput.Text, out hours);
                
            if (!string.IsNullOrWhiteSpace(MinutesInput.Text))
                int.TryParse(MinutesInput.Text, out minutes);

            if (!string.IsNullOrWhiteSpace(SecondsInput.Text))
                int.TryParse(SecondsInput.Text, out seconds);

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                System.Windows.MessageBox.Show("Lütfen geçerli bir süre giriniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StartTimer(new TimeSpan(hours, minutes, seconds));
        }

        private void StartTimer(TimeSpan time)
        {
            timeLeft = time;
            totalSeconds = time.TotalSeconds;
            TimerProgress.Maximum = totalSeconds;
            TimerProgress.Value = totalSeconds;

            InputGrid.Visibility = Visibility.Collapsed;
            TimerGrid.Visibility = Visibility.Visible;
            
            // Set Goal Text
            if (!string.IsNullOrWhiteSpace(GoalInput.Text))
            {
                ActiveGoalText.Text = GoalInput.Text;
            }
            else
            {
                ActiveGoalText.Text = "Odaklanma Modu (Hedef belirtilmedi)";
            }
            
            // Reset Pause state
            isPaused = false;
            PauseButton.Content = "Duraklat";
            PauseButton.Foreground = System.Windows.Media.Brushes.White;

            // Calculate and show Finish Time
            DateTime finishTime = DateTime.Now.Add(time);
            FinishTimeText.Text = finishTime.ToString("HH:mm:ss");

            // Check Strict Mode
            if (StrictModeToggle.IsChecked == true)
            {
                ControlButtonsPanel.Visibility = Visibility.Collapsed;
                StrictWarningText.Visibility = Visibility.Visible;
            }
            else
            {
                ControlButtonsPanel.Visibility = Visibility.Visible;
                StrictWarningText.Visibility = Visibility.Collapsed;
            }

            UpdateTimeLeftText();
            timer?.Start();
        }

        private void TipTimer_Tick(object? sender, EventArgs e)
        {
            tipIndex = (tipIndex + 1) % tips.Length;
            TipText.Text = tips[tipIndex];

            healthIndex = (healthIndex + 1) % healthInfos.Length;
            if (HealthInfoText != null)
            {
                HealthInfoText.Text = healthInfos[healthIndex];
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            timer?.Stop();
            ambientPlayer?.Stop();
            TimerGrid.Visibility = Visibility.Collapsed;
            InputGrid.Visibility = Visibility.Visible;
            
            isPaused = false;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPaused)
            {
                timer?.Start();
                isPaused = false;
                PauseButton.Content = "Duraklat";
                PauseButton.Foreground = System.Windows.Media.Brushes.White;
                
                DateTime finishTime = DateTime.Now.Add(timeLeft);
                FinishTimeText.Text = finishTime.ToString("HH:mm:ss");
            }
            else
            {
                timer?.Stop();
                isPaused = true;
                PauseButton.Content = "Devam Et";
                PauseButton.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF5CDB95"));
                
                FinishTimeText.Text = "Duraklatıldı";
            }
        }

        private void AddTimeButton_Click(object sender, RoutedEventArgs e)
        {
            timeLeft = timeLeft.Add(TimeSpan.FromMinutes(5));
            totalSeconds += 300;
            TimerProgress.Maximum = totalSeconds;
            TimerProgress.Value = timeLeft.TotalSeconds;
            
            if (!isPaused)
            {
                DateTime finishTime = DateTime.Now.Add(timeLeft);
                FinishTimeText.Text = finishTime.ToString("HH:mm:ss");
            }
            
            UpdateTimeLeftText();
        }

        private void GoalTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Content != null)
            {
                GoalInput.Text = btn.Content.ToString();
            }
        }

        private void QuickSelect_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag != null)
            {
                if (int.TryParse(btn.Tag.ToString(), out int totalMinutes))
                {
                    int hours = totalMinutes / 60;
                    int minutes = totalMinutes % 60;
                    
                    HoursInput.Text = hours.ToString();
                    MinutesInput.Text = minutes.ToString();
                    SecondsInput.Text = "0";
                }
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (timeLeft.TotalSeconds > 0)
            {
                timeLeft = timeLeft.Subtract(TimeSpan.FromSeconds(1));
                TimerProgress.Value = timeLeft.TotalSeconds;
                UpdateTimeLeftText();
            }
            else
            {
                timer?.Stop();
                ambientPlayer?.Stop();
                
                // Play true alarm sound if enabled
                if (SoundToggle.IsChecked == true)
                {
                    try 
                    {
                        using (var player = new System.Media.SoundPlayer(@"C:\Windows\Media\Alarm08.wav"))
                        {
                            player.Play();
                        }
                    }
                    catch 
                    { 
                        System.Media.SystemSounds.Exclamation.Play(); 
                    }
                }

                // İlk önce ekranı kilitle (Win + L)
                LockWorkStation();

                // Kullanıcı ekran kilidini açtığında BreakWindow'u görsün
                BreakWindow breakWindow = new BreakWindow();
                bool? result = breakWindow.ShowDialog();

                // XP ve İstatistik Ekleme (Başarıyla tamamlandı!)
                int minutesFocused = (int)(totalSeconds / 60);
                if (minutesFocused > 0)
                {
                    UserDataManager.AddFocusTime(minutesFocused);
                    UpdateUIFromSettings();
                }

                if (breakWindow.RequestedExtraTime)
                {
                    // Ekstra 15 dakika süre ver
                    StartTimer(TimeSpan.FromMinutes(15));
                }
                else 
                {
                    // Hemen veya süre dolduğunda Uyku moduna geç
                    TimerGrid.Visibility = Visibility.Collapsed;
                    InputGrid.Visibility = Visibility.Visible;
                    
                    SetSuspendState(false, true, true);
                }
            }
        }

        private void UpdateTimeLeftText()
        {
            TimeLeftText.Text = timeLeft.ToString(@"hh\:mm\:ss");
        }

        private void PlayRain_Click(object sender, RoutedEventArgs e)
        {
            PlayAmbientSound("rain.mp3");
        }

        private void PlayCafe_Click(object sender, RoutedEventArgs e)
        {
            PlayAmbientSound("cafe.mp3");
        }

        private void PlayFire_Click(object sender, RoutedEventArgs e)
        {
            PlayAmbientSound("fire.mp3");
        }

        private void SoundToggle_Checked(object sender, RoutedEventArgs e) 
        {
            UserDataManager.Current.IsSoundEnabled = true;
            UserDataManager.Save();
        }
        
        private void SoundToggle_Unchecked(object sender, RoutedEventArgs e) 
        {
            UserDataManager.Current.IsSoundEnabled = false;
            UserDataManager.Save();
        }

        private void StrictModeToggle_Checked(object sender, RoutedEventArgs e)
        {
            UserDataManager.Current.IsHardModeEnabled = true;
            UserDataManager.Save();
        }

        private void StrictModeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            UserDataManager.Current.IsHardModeEnabled = false;
            UserDataManager.Save();
        }

        private void PlayAmbientSound(string fileName)
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", fileName);
                if (System.IO.File.Exists(path))
                {
                    ambientPlayer?.Stop();
                    ambientPlayer?.Open(new Uri(path));
                    
                    if (VolumeSlider != null && ambientPlayer != null)
                        ambientPlayer.Volume = VolumeSlider.Value;
                        
                    ambientPlayer?.Play();
                }
                else
                {
                    System.Windows.MessageBox.Show($"Ses dosyası bulunamadı!\nLütfen {fileName} dosyasını şu klasöre ekleyin:\n{System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets")}", "Dosya Eksik", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ses çalınırken bir hata oluştu: {ex.Message}");
            }
        }

        private void StopSound_Click(object sender, RoutedEventArgs e)
        {
            ambientPlayer?.Stop();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ambientPlayer != null)
            {
                ambientPlayer.Volume = e.NewValue;
            }
            if (VolumePercentageText != null)
            {
                VolumePercentageText.Text = $"%{(int)Math.Round(e.NewValue * 100)}";
            }
        }

        private void NightLightToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (NightLightSlider != null)
                NightLightSlider.IsEnabled = true;

            if (globalNightLight == null)
            {
                globalNightLight = new GlobalNightLight();
            }
            
            globalNightLight.Show();
            if (NightLightSlider != null)
                globalNightLight.SetIntensity(NightLightSlider.Value);
                
            UserDataManager.Current.IsNightLightEnabled = true;
            UserDataManager.Save();
        }

        private void NightLightToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (NightLightSlider != null)
                NightLightSlider.IsEnabled = false;

            if (globalNightLight != null)
            {
                globalNightLight.Hide();
            }
            
            UserDataManager.Current.IsNightLightEnabled = false;
            UserDataManager.Save();
        }

        private void NightLightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (globalNightLight != null && globalNightLight.IsVisible)
            {
                globalNightLight.SetIntensity(e.NewValue);
            }
            if (NightLightPercentageText != null)
            {
                NightLightPercentageText.Text = $"%{(int)Math.Round(e.NewValue * 100)}";
            }
            
            // Sadece form tamamen yüklendikten sonra kaydet
            if (this.IsLoaded)
            {
                UserDataManager.Current.NightLightIntensity = e.NewValue;
                UserDataManager.Save();
            }
        }

        private void OpenWindowsNightLight_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ms-settings:nightlight",
                    UseShellExecute = true
                });
            }
            catch
            {
                System.Windows.MessageBox.Show("Windows Gece Işığı ayarları açılamadı.\nİşletim sisteminizin bu sürümü (Windows 7/8 veya eski Windows 10) bu özelliği desteklemiyor olabilir.", "Desteklenmeyen Sürüm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
