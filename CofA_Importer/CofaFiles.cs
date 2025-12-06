using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace CofA_Importer
{
    public class CofaFiles
    {
        private readonly string _connectionString;
        private readonly string _cofaDirectory;
        private readonly string _rejectedDirectory;
        private readonly string _logDirectory;
        private readonly AppSettingsService _settingsService;

        public CofaFiles(string connectionString, string cofaDirectory, string rejectedDirectory, string logDirectory, AppSettingsService settingsService)
        {
            _connectionString = connectionString;
            _cofaDirectory = cofaDirectory;
            _rejectedDirectory = rejectedDirectory;
            _logDirectory = logDirectory ?? throw new ArgumentNullException(nameof(logDirectory));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public CofaImportResult GetCofaForInsert(string connectionString, Action<string> log)
        {
            var result = new CofaImportResult();
            var logs = new List<string>();
            string? logFilePath = null;

            // Confirm working directory is writable
            if (Directory.Exists(_cofaDirectory))
            {
                try
                {
                    string testFile = Path.Combine(_cofaDirectory, "test.txt");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    log("Write access confirmed.");
                }
                catch (UnauthorizedAccessException)
                {
                    log("Access denied.");
                }
            }

            log("Inserting records into table [ProductAttachments], please wait...");

            Directory.CreateDirectory(_logDirectory);
            Directory.CreateDirectory(Path.Combine(_cofaDirectory, "Logs"));
            Directory.CreateDirectory(Path.Combine(_cofaDirectory, "Rejected"));

            DateTime dt = DateTime.Now;
            int i = 0;

            string logFileName = $"COFA_{dt:yyyyMMdd}.csv";
            logFilePath = Path.Combine(_logDirectory, logFileName);

            var batchBuilder = new StringBuilder();
            int batchSize = 100;

            try
            {
                DirectoryInfo directory = new DirectoryInfo(_cofaDirectory);

                foreach (FileInfo objFile in directory.GetFiles("*.*"))
                {
                    string full = objFile.Name;
                    string filename = Path.Combine(_cofaDirectory, objFile.Name);
                    string rejectPath = Path.Combine(_cofaDirectory, "Rejected", objFile.Name);
                    string fn = Path.GetFileNameWithoutExtension(filename);

                    if (full.Contains("[") && full.Contains("]"))
                    {
                        List<string> parts = fileNameParts(fn, full);

                        CofaData cd = new CofaData(_connectionString)
                        {
                            Sku = parts[0],
                            Lot = parts[1],
                            FileName = full,
                            UploadDateTime = dt
                        };

                        string insertMsg = $"Inserted: SKU={cd.Sku}, Lot={cd.Lot}, File={cd.FileName}";
                        logs.Add(insertMsg);
                        result.Logs.Add(insertMsg);
                        batchBuilder.AppendLine(insertMsg);

                        CofaData.InsertCofa(cd, connectionString);
                    }
                    else
                    {
                        File.Move(filename, rejectPath);
                        string rejectMsg = $"{full} was moved to the rejected directory.";
                        logs.Add(rejectMsg);
                        result.Logs.Add(rejectMsg);
                        batchBuilder.AppendLine(rejectMsg);
                    }

                    i++;

                    if (i % batchSize == 0)
                    {
                        log?.Invoke(batchBuilder.ToString());
                        batchBuilder.Clear();
                    }
                }

                // After loop flush any remaining
                if (batchBuilder.Length > 0)
                {
                    log?.Invoke(batchBuilder.ToString());
                }

                string summary = $"\nTotal Record Count: {i}\n";
                log?.Invoke(summary);
                logs.Add(summary);
                result.Logs.Add(summary);

                // Write CSV log
                logFileName = $"COFA_{dt:yyyyMMdd}.csv";
                logFilePath = Path.Combine(_logDirectory, logFileName);
                logs.Add($"Log file written to: {logFilePath}");
                result.Logs.Add($"Log file written to: {logFilePath}");
                File.WriteAllLines(logFilePath, result.Logs);
                log?.Invoke($"Log file written to: {logFilePath}");
            }
            catch (Exception ex)
            {
                string error = $"ERROR: {ex.Message}";
                log?.Invoke(error);
                logs.Add(error);
                result.Logs.Add(error);

                try
                {
                    // Write partial log in case of failure
                    File.WriteAllLines(logFilePath, result.Logs);
                    log?.Invoke($"Error log file written to: {logFilePath}");
                }
                catch
                {
                    logs.Add("ERROR: " + ex.Message);
                    log?.Invoke("Failed to write error log file.");
                }
                result.LogFilePath = logFilePath;
                return result; // return what we do have, but indicate failure via null path
            }
            result.LogFilePath = logFilePath;
            return result;
        }

        public string splitString(string value, string fullname)
        {
            var lineItem = new List<string>();
            char[] delimiters = new[] { '[', ']' };
            string[] parts = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            lineItem.AddRange(parts);
            return string.Join("|", lineItem) + "|" + fullname;
        }

        public static List<string> fileNameParts(string value, string fullname)
        {
            var retList = new List<string>();
            char[] delimiters = new[] { '[', ']' };
            string[] parts = value.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            retList.AddRange(parts);
            return retList;
        }
    }
}
