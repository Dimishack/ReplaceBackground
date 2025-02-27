using MessagePack;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace RBAutorun
{
    internal class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo
            (int uAction, int uParam, string lpvParam, int fuWinIni);

        private static void SetWallpaper(string path, int style, int title)
        {
            using (var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true))
            {
                key.SetValue("WallpaperStyle", style.ToString());
                key.SetValue("TileWallpaper", title.ToString());
                SystemParametersInfo(20, 0, path, 0x01 | 0x02);
            }
        }

        async static Task Main()
        {
            var directory = Path.GetDirectoryName(Environment.ProcessPath);
            if (directory is null
                || !File.Exists($"{directory}\\settings.bin")) return;
            Settings setting;
            using (FileStream fs = new($"{directory}\\settings.bin", FileMode.Open))
            {
                setting = await MessagePackSerializer.DeserializeAsync<Settings>(fs);
            }
            if (IsReplaceBackground(ref setting) && Directory.Exists($@"{directory}/Background/{setting.Season}"))
            {
                await Task.Run(() =>
                {
                    var images = Directory.GetFiles($@"{directory}/Background/{setting.Season}");
                    var image = images[Random.Shared.Next(images.Length)];
                    if (image != null)
                        SetWallpaper(image, 10, 0);
                });
            }
            using (FileStream fs = new($"{directory}\\settings.bin", FileMode.Truncate))
            {
                await MessagePackSerializer.SerializeAsync(fs, setting);
            }
        }


        private static bool IsReplaceBackground(ref Settings setting)
        {
            DateOnly datetoday = DateOnly.FromDateTime(DateTime.Today);

            static string GetSeason(int month) => month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Autumn",
                _ => throw new InvalidDataException($"Неизвестное число месяца {month}"),
            };

            switch (setting.Interval)
            {
                case "День":
                    if (setting.DateReplaced <= datetoday)
                    {
                        setting.DateReplaced = datetoday.AddDays(1);
                        setting.Season = GetSeason(datetoday.Month);
                        return true;
                    }
                    break;
                case "Неделя":
                    if (setting.DateReplaced <= datetoday && datetoday.DayOfWeek == DayOfWeek.Monday)
                    {
                        setting.DateReplaced = datetoday.AddDays(7);
                        setting.Season = GetSeason(datetoday.Month);
                        return true;
                    }
                    break;
                case "Месяц":
                    if (datetoday.Month != setting.DateReplaced.Month)
                    {
                        int year = datetoday.Year;
                        int month = datetoday.Month;
                        if (month + 1 > 12)
                            year++;
                        else month++;
                        setting.DateReplaced = new DateOnly(year, month, 1);
                        setting.Season = GetSeason(setting.DateReplaced.Month);
                        return true;
                    }
                    break;
                case "Сезон":
                    if (string.Compare(setting.Season, GetSeason(datetoday.Month)) != 0)
                    {
                        int plusNextSeason = 3 - (datetoday.Month % 3);
                        int year = datetoday.Year;
                        int month = datetoday.Month;
                        if (month + plusNextSeason > 12)
                            year++;
                        else month += plusNextSeason;
                        setting.DateReplaced = new DateOnly(year, month, 1);
                        setting.Season = GetSeason(setting.DateReplaced.Month);
                        return true;
                    }
                    break;
                default:
                    break;
            }
            return false;
        }
    }

    [MessagePackObject]
    public struct Settings
    {
        public Settings() { }

        [Key(0)]
        public DateOnly DateReplaced { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Key(1)]
        public string Interval { get; set; } = "День";
        [Key(2)]
        public string Season { get; set; } = "Winter";

    }
}
