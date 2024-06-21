using Microsoft.Win32;
using ReplaceBackground.Infrastructure;
using ReplaceBackground.Infrastructure.Commands;
using ReplaceBackground.Models;
using ReplaceBackground.ViewModels.Base;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Windows;
using System.Windows.Input;

namespace ReplaceBackground.ViewModels
{

    class MainWViewModel : ViewModel
    {
        #region Fields...

        const string PROGRAMNAME = "ReplaceBackground";
        private readonly DateTime DATETODAY = DateTime.Today;
        private Window? _currentWindow;
        private string _directory = Environment.CurrentDirectory;
        private readonly JsonSerializerOptions _JSO = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true,
        };

        #endregion

        #region Properties...

        #region IsAutorun : bool - Разрешение на автозапуск

        ///<summary>Разрешение на автозапуск</summary>
        private bool _isAutorun;

        ///<summary>Разрешение на автозапуск</summary>
        public bool IsAutorun
        {
            get => _isAutorun;
            set
            {
                if (!Set(ref _isAutorun, value)) return;

                using (var rk = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    try
                    {
                        if (value ^ Array.IndexOf(rk.GetValueNames(), PROGRAMNAME) != -1)
                        {
                            if (value)
                                rk.SetValue(PROGRAMNAME, Environment.ProcessPath);
                            else
                                rk.DeleteValue(PROGRAMNAME);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex.Message);
                    }
                }
            }
        }

        #endregion

        #region Settings : Setting - Настройки

        ///<summary>Настройки</summary>
        private Setting _settings = new();

        ///<summary>Настройки</summary>
        public Setting Settings { get => _settings; set => Set(ref _settings, value); }

        #endregion

        public IList<string> Intervals { get; } = ["День", "Неделя", "Месяц", "Сезон"];

        #region SelectedInterval : string - Выбранный интервал

        ///<summary>Выбранный интервал</summary>
        private string _selectedInterval = "Месяц";

        ///<summary>Выбранный интервал</summary>
        public string SelectedInterval
        {
            get => _selectedInterval;
            set
            {
                if (!Set(ref _selectedInterval, value)) return;

                Settings.Interval = value;
            }
        }

        #endregion

        #endregion

        #region Commands...

        #region LoadedWindowCommand - Команда - загрузка окна

        ///<summary>Команда - загрузка окна</summary>
        private ICommand? _loadedWindowCommand;

        ///<summary>Команда - загрузка окна</summary>
        public ICommand LoadedWindowCommand => _loadedWindowCommand
            ??= new LambdaCommand<Window>(OnLoadedWindowCommandExecuted);

        ///<summary>Логика выполнения - загрузка окна</summary>
        private void OnLoadedWindowCommandExecuted(Window p)
        {
            _currentWindow = p;
            _directory = Path.GetDirectoryName(Environment.ProcessPath)!;
            Setting? setting;
            if ((setting = ReadSettings()) is not null)
                Settings = setting;
            SelectedInterval = Settings.Interval;
            if (IsReplaceBackground())
            {
                var image = GetImage();
                if (image is null) return;
                User32.SetWallpaper(image, 10, 0);
            }
            if (!Equals(Path.GetDirectoryName(Environment.ProcessPath), Environment.CurrentDirectory))
                p.Close();
        }

        #endregion

        #region ClosedWindowCommand - Команда - закрытие окна

        ///<summary>Команда - закрытие окна</summary>
        private ICommand? _closedWindowCommand;

        ///<summary>Команда - закрытие окна</summary>
        public ICommand ClosedWindowCommand => _closedWindowCommand
            ??= new LambdaCommand(OnClosedWindowCommandExecuted);

        ///<summary>Логика выполнения - закрытие окна</summary>
        private void OnClosedWindowCommandExecuted(object? p)
        {
            using (var fs = new FileStream(@$"{_directory}/settings.json", FileMode.Create))
            {
                JsonSerializer.Serialize(fs, _settings, _JSO);
            }
        }

        #endregion

        #region OpenFolderImagesCommand - Команда - открыть папку с изображениями

        ///<summary>Команда - открыть папку с изображениями</summary>
        private ICommand? _openFolderImagesCommand;

        ///<summary>Команда - открыть папку с изображениями</summary>
        public ICommand OpenFolderImagesCommand => _openFolderImagesCommand
            ??= new LambdaCommand(OnOpenFolderImagesCommandExecuted, CanOpenFolderImagesCommandExecute);

        ///<summary>Проверка возможности выполнения - открыть папку с изображениями</summary>
        private bool CanOpenFolderImagesCommandExecute(object? p) => Directory.Exists(@$"{_directory}/Background");

        ///<summary>Логика выполнения - открыть папку с изображениями</summary>
        private void OnOpenFolderImagesCommandExecuted(object? p) =>
            Process.Start("explorer.exe", $@"{_directory}\Background\");

        #endregion

        #endregion

        public MainWViewModel()
        {
            using (var rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                try
                {
                    _isAutorun = Array.IndexOf(rk.GetValueNames(), PROGRAMNAME) != -1;
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                    _isAutorun = false;
                    throw;
                }
            }
        }

        #region Methods...

        private Setting? ReadSettings()
        {
            try
            {
                var settings = new Setting();
                using (var fs = new FileStream($@"{_directory}/settings.json", FileMode.Open))
                {
                    settings = JsonSerializer.Deserialize<Setting>(fs, _JSO);
                }
                return settings;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return null;
            }
        }

        private bool IsReplaceBackground()
        {
            switch (_selectedInterval)
            {
                case "День":
                    if (_settings.DateReplaced >= DATETODAY)
                    {
                        Settings.DateReplaced = DATETODAY.AddDays(1);
                        Settings.Season = Setting.GetSeason(DATETODAY.Month);
                        return true;
                    }
                    break;
                case "Неделя":
                    if (DATETODAY.DayOfWeek == DayOfWeek.Monday)
                    {
                        Settings.DateReplaced = DATETODAY;
                        Settings.Season = Setting.GetSeason(DATETODAY.Month);
                        return true;
                    }
                    break;
                case "Месяц":
                    if (DATETODAY.Month != _settings.DateReplaced.Month)
                    {
                        Settings.DateReplaced = new DateTime(DATETODAY.Year, DATETODAY.Month + 1, 1);
                        Settings.Season = Setting.GetSeason(_settings.DateReplaced.Month);
                        return true;
                    }
                    break;
                case "Сезон":
                    if (string.Compare(_settings.Season, Setting.GetSeason(DATETODAY.Month)) != 0)
                    {
                        int plusNextSeason = 3 - (DATETODAY.Month % 3);
                        Settings.DateReplaced = new DateTime(DATETODAY.Year, DATETODAY.Month + plusNextSeason, 1);
                        Settings.Season = Setting.GetSeason(_settings.DateReplaced.Month);
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        private string? GetImage()
        {
            try
            {
                var images = Directory.GetFiles($@"{_directory}/Background/{_settings.Season}");
                return images[Random.Shared.Next(images.Length)];
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                return null;
            }
        }

        private static void ShowError(string message, string caption = "ReplaceBackground") =>
            MessageBox.Show($"Error! Message:{message}", caption, MessageBoxButton.OK, MessageBoxImage.Error);

        #endregion
    }
}
