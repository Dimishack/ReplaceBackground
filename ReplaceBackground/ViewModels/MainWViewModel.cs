using MessagePack;
using Microsoft.Win32;
using ReplaceBackground.Infrastructure.Commands;
using ReplaceBackground.Models;
using ReplaceBackground.ViewModels.Base;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ReplaceBackground.ViewModels
{

    class MainWViewModel : ViewModel
    {
        #region Fields...
        const string SETTINGSFILENAME = "settings.bin";
        const string PROGRAMNAME = "ReplaceBackground";

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

                using (var rk = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    try
                    {
                        if (value ^ rk.GetValue(PROGRAMNAME) is not null)
                        {
                            if (value)
                                rk.SetValue(PROGRAMNAME, $"{Environment.CurrentDirectory}\\RBAutorun.exe");
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
        private async void OnLoadedWindowCommandExecuted(Window p)
        {
            Setting? setting;
            if ((setting = await ReadSettings()) is not null)
                Settings = setting;
            SelectedInterval = Settings.Interval;
        }

        #endregion

        #region ClosedWindowCommand - Команда - закрытие окна

        ///<summary>Команда - закрытие окна</summary>
        private ICommand? _closedWindowCommand;

        ///<summary>Команда - закрытие окна</summary>
        public ICommand ClosedWindowCommand => _closedWindowCommand
            ??= new LambdaCommand(OnClosedWindowCommandExecuted);

        ///<summary>Логика выполнения - закрытие окна</summary>
        private async void OnClosedWindowCommandExecuted(object? p)
        {
            //using (var fs = new FileStream(SETTINGSFILENAME, FileMode.Create))
            //{
            //    await MessagePackSerializer.SerializeAsync(fs, Settings).ConfigureAwait(false);
            //}
        }

        #endregion

        #region OpenFolderImagesCommand - Команда - открыть папку с изображениями

        ///<summary>Команда - открыть папку с изображениями</summary>
        private ICommand? _openFolderImagesCommand;

        ///<summary>Команда - открыть папку с изображениями</summary>
        public ICommand OpenFolderImagesCommand => _openFolderImagesCommand
            ??= new LambdaCommand(OnOpenFolderImagesCommandExecuted, CanOpenFolderImagesCommandExecute);

        ///<summary>Проверка возможности выполнения - открыть папку с изображениями</summary>
        private bool CanOpenFolderImagesCommandExecute(object? p) => Directory.Exists("Background");

        ///<summary>Логика выполнения - открыть папку с изображениями</summary>
        private void OnOpenFolderImagesCommandExecuted(object? p) =>
            Process.Start("explorer.exe", $@"{Environment.CurrentDirectory}\Background\");

        #endregion

        #endregion

        public MainWViewModel()
        {
            using (var rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                _isAutorun = rk is not null && rk.GetValue(PROGRAMNAME) is not null;
            }
        }

        #region Methods...

        private async Task<Setting?> ReadSettings()
        {
            try
            {
                var settings = new Setting();
                using (var fs = new FileStream(SETTINGSFILENAME, FileMode.Open))
                    settings = await MessagePackSerializer.DeserializeAsync<Setting>(fs);
                return settings;
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
