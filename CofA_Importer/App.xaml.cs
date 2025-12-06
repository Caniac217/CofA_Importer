using Microsoft.Extensions.Configuration;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;


namespace CofA_Importer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            string configPath = ConfigPathHelper.GetWritableConfigDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(configPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var settingsService = new AppSettingsService(config);
            var mainWindow = new MainWindow(settingsService);
            MainWindow = mainWindow;
            mainWindow.Show();

            bool isSettingsValid =
                !string.IsNullOrWhiteSpace(settingsService.TryGetConnectionString()) &&
                !string.IsNullOrWhiteSpace(settingsService.TryGetCofaDirectory());

            if (!isSettingsValid)
            {
                var settingsWindow = new Settings(settingsService)
                {
                    Owner = mainWindow
                };

                bool? result = settingsWindow.ShowDialog();
                if (result != true)
                {
                    Shutdown();
                    return;
                }

                // Reload settings
                config = new ConfigurationBuilder()
                    .SetBasePath(configPath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                settingsService = new AppSettingsService(config);
                mainWindow.InitializeWithSettings(settingsService);
            }

            ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
    }
}
