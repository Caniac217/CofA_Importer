using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
//using static System.Net.Mime.MediaTypeNames;

namespace CofA_Importer
{
    public partial class MainWindow : Window
    {
        private AppSettingsService _settingsService;
        private string _connectionString;
        private string _cofaDirectory;
        private string _rejectedDirectory;
        private string _logDirectory;
        private string? _logFilePath = string.Empty;

        public MainWindow(AppSettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _connectionString = _settingsService.TryGetConnectionString() ?? string.Empty;
            _cofaDirectory = _settingsService.TryGetCofaDirectory() ?? string.Empty;
            _rejectedDirectory = _settingsService.TryGetRejectedDirectory() ?? string.Empty;
            _logDirectory = _settingsService.TryGetLogDirectory() ?? string.Empty;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!Initialize())
            {
                Application.Current.Shutdown();
            }
        }

        public bool Initialize()
        {
            // Try to load settings without throwing
            _connectionString = _settingsService.TryGetConnectionString() ?? string.Empty;
            _cofaDirectory = _settingsService.TryGetCofaDirectory() ?? string.Empty;
            _logDirectory = _settingsService.TryGetLogDirectory() ?? string.Empty;
            _rejectedDirectory = _settingsService.TryGetRejectedDirectory() ?? string.Empty;

            // If any are missing, show settings window
            if (string.IsNullOrWhiteSpace(_connectionString) ||
                string.IsNullOrWhiteSpace(_cofaDirectory) ||
                string.IsNullOrWhiteSpace(_logDirectory) ||
                string.IsNullOrWhiteSpace(_rejectedDirectory))
            {
                System.Windows.MessageBox.Show(
                    "One or more required settings are missing. Please complete them in the settings screen.",
                    "Settings Required",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning
                );

                var settingsWindow = new Settings(_settingsService);
                settingsWindow.ShowDialog();

                // Reload updated settings using new service (do not reassign readonly field)
                var updatedService = new AppSettingsService();

                _connectionString = updatedService.TryGetConnectionString() ?? string.Empty;
                _cofaDirectory = updatedService.TryGetCofaDirectory() ?? string.Empty;
                _logDirectory = updatedService.TryGetLogDirectory() ?? string.Empty;
                _rejectedDirectory = updatedService.TryGetRejectedDirectory() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(_connectionString) ||
                    string.IsNullOrWhiteSpace(_cofaDirectory) ||
                    string.IsNullOrWhiteSpace(_logDirectory) ||
                    string.IsNullOrWhiteSpace(_rejectedDirectory))
                {
                    MessageBox.Show("Settings are still incomplete. Application will now exit.",
                    "Settings Required",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);
                    return false;
                }
            }

            return true;
        }

        public bool InitializeWithSettings(AppSettingsService updatedService)
        {
            _settingsService = updatedService;
            _connectionString = _settingsService.TryGetConnectionString() ?? string.Empty;
            _cofaDirectory = _settingsService.TryGetCofaDirectory() ?? string.Empty;
            _logDirectory = _settingsService.TryGetLogDirectory() ?? string.Empty;
            _rejectedDirectory = _settingsService.TryGetRejectedDirectory() ?? string.Empty;
            return true;
        }


        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new Settings(_settingsService)
            {
                Owner = this
            };

            bool? result = settingsWindow.ShowDialog();

            if (result == true)
            {
                // Reload fresh service so config reloads from updated JSON
                _settingsService = new AppSettingsService();

                _connectionString = _settingsService.TryGetConnectionString() ?? "";
                _cofaDirectory = _settingsService.TryGetCofaDirectory() ?? "";
                _logDirectory = _settingsService.TryGetLogDirectory() ?? "";
                _rejectedDirectory = _settingsService.TryGetRejectedDirectory() ?? "";
            }
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void btnBeginImport_Click(object sender, RoutedEventArgs e)
        {
            btnBeginImport.IsEnabled = false;

            // Show first-run dialog BEFORE any import logic
            if (_settingsService.IsFirstRun())
            {
                MessageBox.Show("This is your first time running the import and it may take several minutes to complete. When you're ready, click OK to begin.",
                                "First Run",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                _settingsService.SetFirstRunComplete();
            }

            AppendOutputSafe("Clearing ProductAttachments table...\n");

            CofaImportResult? result = null;

            // Delete existing records before import
            bool deleteSuccess = CofaData.DeleteCofa(_connectionString, AppendOutputSafe);
            if (!deleteSuccess)
            {
                AppendOutputSafe("Error: Failed to clear ProductAttachments. Aborting import.\n");
                btnBeginImport.IsEnabled = true;
                return;
            }

            AppendOutputSafe("Starting CofA import...\n");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();  // ⏱ Start timer

            await Task.Run(() =>
            {
                var cf = new CofaFiles(_connectionString, _cofaDirectory, _rejectedDirectory, _logDirectory, _settingsService);
                result = cf.GetCofaForInsert(_connectionString, AppendOutputSafe);

                _logFilePath = result.LogFilePath;
            });

            stopwatch.Stop();  // ⏱ Stop timer
            AppendOutputSafe($"Import completed in {stopwatch.Elapsed.TotalSeconds:N1} seconds.\n");

            AppendOutputSafe("CofA import is now complete. You may click exit.\n");

            if (result != null &&
                !string.IsNullOrWhiteSpace(result.LogFilePath) &&
                _settingsService.GetShowOpenLogPrompt())
            {
                var prompt = new LogPrompt
                {
                    Owner = this,
                    FileName = result.LogFilePath
                };

                bool? dialogResult = prompt.ShowDialog();

                if (dialogResult == true)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = result.LogFilePath,
                        UseShellExecute = true
                    });
                }

                if (prompt.DontAskAgain)
                {
                    _settingsService.SetShowOpenLogPrompt(false);
                }
            }

            btnBeginImport.IsEnabled = true;
        }



        //private async void btnBeginImport_Click(object sender, RoutedEventArgs e)
        //{
        //    btnBeginImport.IsEnabled = false;
        //    OutputTextBox.Clear();
        //    AppendOutput("Starting COFA Import...");

        //    CofaImportResult? result = null;

        //    try
        //    {
        //        result = await Task.Run(() =>
        //        {
        //            var cf = new CofaFiles(
        //                _connectionString,
        //                _cofaDirectory,
        //                _settingsService.TryGetRejectedDirectory() ?? string.Empty,
        //                _settingsService.TryGetLogDirectory() ?? string.Empty,
        //                _settingsService
        //            );

        //            AppendOutputSafe("DeleteCofa() command will now TRUNCATE tables [Products] and [ProductAttachments]...");
        //            bool deleted = CofaData.DeleteCofa(_connectionString, AppendOutputSafe);
        //            AppendOutputSafe(deleted ? "TRUNCATE tables [Products] and [ProductAttachments] complete." : "DeleteCofa() failed.");

        //            AppendOutputSafe("Trying to import and generate log output...");
        //            var localResult = cf.GetCofaForInsert(_connectionString, AppendOutputSafe);
        //            AppendOutputSafe(!string.IsNullOrWhiteSpace(localResult.LogFilePath) ? "File processing and insert completed." : "Error occurred during file processing.");
        //            AppendOutputSafe("COFA Import completed successfully.");

        //            return localResult;
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        AppendOutputSafe("ERROR: " + ex.Message);
        //    }

        //    // Ensure any queued Dispatcher operations are executed before showing the dialog
        //    await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background);

        //    await Task.Delay(100); // give UI time to render

        //    // Now result is valid and on the UI thread
        //    if (result != null &&
        //        !string.IsNullOrWhiteSpace(result.LogFilePath) &&
        //        _settingsService.GetShowOpenLogPrompt())
        //    {
        //        var prompt = new LogPrompt
        //        {
        //            Owner = this,
        //            FileName = result.LogFilePath
        //        };

        //        bool? dialogResult = prompt.ShowDialog();

        //        if (dialogResult == true)
        //        {
        //            Process.Start(new ProcessStartInfo
        //            {
        //                FileName = result.LogFilePath,
        //                UseShellExecute = true
        //            });
        //        }

        //        if (prompt.DontAskAgain)
        //        {
        //            _settingsService.SetShowOpenLogPrompt(false);
        //        }
        //    }
        //}

        private void AppendOutputSafe(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text + Environment.NewLine);
                OutputTextBox.ScrollToEnd();
            });
        }

        private void AppendOutput(string message)
        {
            OutputTextBox.AppendText(message + Environment.NewLine);
            OutputTextBox.ScrollToEnd();
        }
    }
}
