using Microsoft.Win32;
using ReplaceBackground.Infrastructure.Commands;
using ReplaceBackground.Models;
using ReplaceBackground.ViewModels.Base;
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
        private Window? _currentWindow;
        private string _directory = Environment.CurrentDirectory;
        private readonly JsonSerializerOptions _JSO = new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            WriteIndented = true,
        };

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
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run"))
                {
                    try
                    {
                        if (value ^ Array.IndexOf(rk.GetValueNames(), _currentWindow.Title) != -1)
                        {
                            if (value)
                                rk.SetValue(_currentWindow.Title, Environment.ProcessPath);
                            else
                                rk.DeleteValue(_currentWindow.Title);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось добавить или удалить с автозапуска. Проверьте исходный код." +
                            $"\nОшибка: {ex.Message}");
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

        #endregion

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
            catch (Exception)
            {
                return null;
            }
        }
    }
}
