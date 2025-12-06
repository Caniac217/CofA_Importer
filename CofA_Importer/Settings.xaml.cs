using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Ookii.Dialogs.Wpf;

namespace CofA_Importer
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private AppSettingsService _settings;

        public Settings(AppSettingsService settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadSettings();
            DatabaseComboBox.IsEnabled = false;

            string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            // If config doesn't exist, create a blank template
            if (!File.Exists(configPath))
            {
                var emptyConfig = new
                {
                    ConnectionStrings = new { ChemCartConnectionString = "" },
                    AppSettings = new { CofaPath = "" }
                };
                string json = JsonSerializer.Serialize(emptyConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
            }

            //var settings = new AppSettingsService();
            string connStr = settings.TryGetConnectionString() ?? string.Empty;


            // Pre-fill fields if settings are available
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                // Optionally parse the connection string to populate individual fields
                try
                {
                    var builder = new SqlConnectionStringBuilder(connStr); // now from Microsoft.Data.SqlClient namespace
                    ServerIpTextBox.Text = builder.DataSource;
                    UsernameTextBox.Text = builder.UserID;
                    PasswordBox.Password = builder.Password;
                    DatabaseComboBox.Text = builder.InitialCatalog;
                }
                catch
                {
                    MessageBox.Show("Connection failed. Please check your settings.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            string? cofaPath = settings.TryGetCofaDirectory();
            if (!string.IsNullOrWhiteSpace(cofaPath))
            {
                CofaPathTextBox.Text = cofaPath;
            }
        }

        private void LoadSettings()
        {
            string connStr = _settings.TryGetConnectionString() ?? string.Empty;
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerIpTextBox.Text;
            string user = UsernameTextBox.Text;
            string pass = PasswordBox.Password;

            string connStr = $"Server={server};User ID={user};Password={pass};Initial Catalog=master;Encrypt=False;TrustServerCertificate=True";

            try
            {
                using SqlConnection conn = new SqlConnection(connStr);
                conn.Open();

                DataTable databases = conn.GetSchema("Databases");
                List<string> dbNames = new();

                foreach (DataRow row in databases.Rows)
                {
                    var dbName = row?["database_name"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(dbName))
                    {
                        dbNames.Add(dbName);
                    }
                }
                if (dbNames.Count > 0) 
                {
                    DatabaseComboBox.IsEnabled = true;
                }
                else
                {
                    DatabaseComboBox.IsEnabled = false;
                }
                DatabaseComboBox.ItemsSource = dbNames;
                MessageBox.Show("Connection successful. Databases loaded.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed:\n{ex.Message}");
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerIpTextBox.Text;
            string user = UsernameTextBox.Text;
            string pass = PasswordBox.Password;

            if (!DatabaseComboBox.IsEnabled || DatabaseComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please test the connection and select a database before saving.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDb = DatabaseComboBox.SelectedItem.ToString(); // no warning here

            string cofaPath = CofaPathTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(cofaPath))
            {
                MessageBox.Show("Please select a valid Cofa path.");
                return;
            }

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(selectedDb))
            {
                MessageBox.Show("Please fill in all fields and select a database.");
                return;
            }

            string connectionString = $"Server={server};Initial Catalog={selectedDb};User ID={user};Password={pass};Encrypt=False;TrustServerCertificate=True";

            SaveToAppSettings(connectionString, cofaPath);

            AppSettingsService newSettings = new AppSettingsService();
            var mainWindow = this.Owner as MainWindow;
            mainWindow?.InitializeWithSettings(newSettings);

            MessageBox.Show("Settings saved successfully.");

            DialogResult = true;  // This tells the calling code: "User saved successfully"
            Close();              // This actually closes the Settings window
        }

        private void SaveToAppSettings(string connStr, string cofaPath)
        {
            string configFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "INDOFINE", "ICC_CofA_Importer");

            Directory.CreateDirectory(configFolder);

            string filePath = ConfigPathHelper.GetWritableConfigFilePath();
            string json = File.Exists(filePath) ? File.ReadAllText(filePath) : "{}";

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement.Clone();

            // Step 1: Preserve top-level settings (like FirstRunComplete)
            var preservedTopLevel = new Dictionary<string, JsonElement>();
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name != "ConnectionStrings" && prop.Name != "AppSettings")
                {
                    preservedTopLevel[prop.Name] = prop.Value.Clone();
                }
            }

            // Step 2: Build AppSettings
            var appSettingsDict = new Dictionary<string, object>();

            if (root.TryGetProperty("AppSettings", out var appSettings) && appSettings.ValueKind == JsonValueKind.Object)
            {
                foreach (var item in appSettings.EnumerateObject())
                {
                    switch (item.Value.ValueKind)
                    {
                        case JsonValueKind.String:
                            appSettingsDict[item.Name] = item.Value.GetString() ?? string.Empty;
                            break;
                        case JsonValueKind.Number:
                            appSettingsDict[item.Name] = item.Value.GetDouble();
                            break;
                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            appSettingsDict[item.Name] = item.Value.GetBoolean();
                            break;
                        default:
                            appSettingsDict[item.Name] = item.Value.ToString();
                            break;
                    }
                }
            }

            // Step 3: Overwrite specific values
            appSettingsDict["CofaPath"] = cofaPath;

            if (!appSettingsDict.ContainsKey("RejectedDirectory"))
                appSettingsDict["RejectedDirectory"] = Path.Combine(cofaPath, "Rejected");

            if (!appSettingsDict.ContainsKey("LogDirectory"))
                appSettingsDict["LogDirectory"] = Path.Combine(cofaPath, "Logs");

            if (!appSettingsDict.ContainsKey("ShowOpenLogPrompt"))
                appSettingsDict["ShowOpenLogPrompt"] = true;

            // Step 4: Compose full output
            var fullOutput = new Dictionary<string, object>
            {
                ["ConnectionStrings"] = new { ChemCartConnectionString = connStr },
                ["AppSettings"] = appSettingsDict
            };

            // Re-attach preserved top-level keys
            foreach (var kvp in preservedTopLevel)
            {
                fullOutput[kvp.Key] = kvp.Value.Deserialize<object>()!;
            }

            string updatedJson = JsonSerializer.Serialize(fullOutput, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, updatedJson);
        }

        private void BrowseCofaPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select the COFA directory";
            dialog.UseDescriptionForTitle = true;
            dialog.ShowNewFolderButton = true;

            bool? result = dialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                CofaPathTextBox.Text = dialog.SelectedPath;
            }
        }
    }
}
