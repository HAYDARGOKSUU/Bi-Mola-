using System;
using System.IO;
using System.Text.Json;

namespace BiMola
{
    public class UserProfile
    {
        // Temel Ayarlar
        public bool IsNightLightEnabled { get; set; } = false;
        public double NightLightIntensity { get; set; } = 0.5;
        public bool IsSoundEnabled { get; set; } = true;
        public bool IsHardModeEnabled { get; set; } = false;

        // İstatistikler & Gamification (Oyunlaştırma)
        public int TotalFocusMinutes { get; set; } = 0;
        public int DailyFocusMinutes { get; set; } = 0;
        public DateTime LastFocusDate { get; set; } = DateTime.Today;
        
        public int Level { get; set; } = 1;
        public int CurrentXP { get; set; } = 0;
        
        // Rozetler (İleride genişletilecek)
        public int ConsecutiveDays { get; set; } = 0;
    }

    public static class UserDataManager
    {
        private static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BiMola");
        private static readonly string FilePath = Path.Combine(FolderPath, "UserData.json");

        public static UserProfile Current { get; set; } = new UserProfile();

        public static void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    Current = JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile();
                    
                    // Yeni gün kontrolü (Günlük odaklanmayı sıfırla)
                    if (Current.LastFocusDate.Date < DateTime.Today)
                    {
                        if (Current.LastFocusDate.Date == DateTime.Today.AddDays(-1))
                        {
                            Current.ConsecutiveDays++; // Seri devam ediyor
                        }
                        else
                        {
                            Current.ConsecutiveDays = 0; // Seri bozuldu
                        }
                        
                        Current.DailyFocusMinutes = 0;
                        Current.LastFocusDate = DateTime.Today;
                        Save(); // Güncellenmiş tarihi kaydet
                    }
                }
            }
            catch
            {
                Current = new UserProfile();
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(FolderPath))
                {
                    Directory.CreateDirectory(FolderPath);
                }

                string json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Hata durumunda yoksay
            }
        }
        
        public static void AddFocusTime(int minutes)
        {
            Current.TotalFocusMinutes += minutes;
            Current.DailyFocusMinutes += minutes;
            
            // 1 Dakika = 10 XP
            Current.CurrentXP += (minutes * 10);
            
            // Seviye atlama formülü (Örn: Her seviye için (Level * 100) XP gerekir)
            int requiredXp = Current.Level * 100;
            while (Current.CurrentXP >= requiredXp)
            {
                Current.CurrentXP -= requiredXp;
                Current.Level++;
                requiredXp = Current.Level * 100;
            }
            
            Save();
        }
    }
}
