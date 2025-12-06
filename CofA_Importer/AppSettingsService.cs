using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace CofA_Importer
{
    public class AppSettingsService
    {
        private IConfiguration _config;
        private readonly string _configFilePath = ConfigPathHelper.GetWritableConfigFilePath();

        public AppSettingsService()
        {
            string configDir = Path.GetDirectoryName(_configFilePath) ?? throw new InvalidOperationException("Config path is invalid.");

            _config = new ConfigurationBuilder()
                .SetBasePath(configDir)
                .AddJsonFile(Path.GetFileName(_configFilePath), optional: true, reloadOnChange: true)
                .Build();
        }

        public AppSettingsService(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string? TryGetConnectionString()
        {
            return GetConnectionString(false);
        }

        public string? TryGetCofaDirectory()
        {
            return GetCofaDirectory(false);
        }

        public string? TryGetRejectedDirectory() => GetRejectedDirectory(false);

        public string? TryGetLogDirectory() => GetLogDirectory(false);

        public string? GetConnectionString(bool throwIfMissing = true)
        {
            var value = _config.GetConnectionString("ChemCartConnectionString");
            if (throwIfMissing && string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Missing ChemCartConnectionString in appsettings.json.");
            return value;
        }

        public string GetCofaDirectory(bool throwIfMissing = true)
        {
            var value = _config["AppSettings:CofaPath"];
            if (throwIfMissing && string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Missing CofaDirectory in appsettings.json.");
            return value!;
        }

        public string? GetRejectedDirectory(bool throwIfMissing = true)
        {
            var value = _config["AppSettings:RejectedDirectory"];
            if (throwIfMissing && string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Missing RejectedDirectory in appsettings.json.");
            return value;
        }

        public string? GetLogDirectory(bool throwIfMissing = true)
        {
            var value = _config["AppSettings:LogDirectory"];
            if (throwIfMissing && string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("Missing LogDirectory in appsettings.json.");
            return value;
        }

        public bool IsConnectionStringValid()
        {
            var conn = TryGetConnectionString();
            return !string.IsNullOrWhiteSpace(conn);
        }

        public void SaveConnectionString(string connectionString)
        {
            var json = File.ReadAllText(_configFilePath);
            dynamic? jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

            if (jsonObj == null)
                throw new InvalidOperationException("Failed to parse appsettings.json.");

            jsonObj["ConnectionStrings"]["ChemCartConnectionString"] = connectionString;

            string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(_configFilePath, output);
        }

        public bool GetShowOpenLogPrompt()
        {
            var value = _config["AppSettings:ShowOpenLogPrompt"];
            return bool.TryParse(value, out bool result) ? result : true; // default to true
        }

        public void SetShowOpenLogPrompt(bool value)
        {
            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            config.AppSettings ??= new AppSettings();
            config.AppSettings.ShowOpenLogPrompt = value;

            var updatedJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, updatedJson);

            // Reload _config because the app depends on it immediately
            string configDir = Path.GetDirectoryName(_configFilePath)!;
            _config = new ConfigurationBuilder()
                .SetBasePath(configDir)
                .AddJsonFile(Path.GetFileName(_configFilePath), optional: true, reloadOnChange: true)
                .Build();
        }

        // helper class
        private static string GetWritableConfigPath()
        {
            string configFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "INDOFINE", "ICC_CofA_Importer");

            Directory.CreateDirectory(configFolder);

            return Path.Combine(configFolder, "appsettings.json");
        }

        public bool IsFirstRun()
        {
            var value = _config["FirstRunComplete"];
            return string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var result) || !result;
        }

        public void SetFirstRunComplete()
        {
            var configPath = Path.Combine(ConfigPathHelper.GetWritableConfigDirectory(), "appsettings.json");
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement.Clone();

            var dict = root.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

            dict["FirstRunComplete"] = JsonDocument.Parse("\"true\"").RootElement;

            var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, output);
        }

    }
}
