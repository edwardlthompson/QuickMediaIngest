#nullable enable
namespace QuickMediaIngest.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using Microsoft.Extensions.Logging;
    using QuickMediaIngest.Data.Models;

    public class DatabaseService : IDatabaseService
    {
        private readonly string _dbPath;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
        {
            _logger = logger;
            // Store DB in AppData
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "QuickMediaIngest");
            _dbPath = Path.Combine(appFolder, "database.db");
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            _logger.LogInformation("Initializing database at {DatabasePath}.", _dbPath);
            string? dir = Path.GetDirectoryName(_dbPath);
            if (string.IsNullOrEmpty(dir))
            {
                return;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();

                string createDevicesTable = @"
                    CREATE TABLE IF NOT EXISTS Devices (
                        Id TEXT PRIMARY KEY,
                        DeviceName TEXT,
                        LastImportDate TEXT,
                        AutoTrigger INTEGER DEFAULT 1
                    );";

                string createWhitelistTable = @"
                    CREATE TABLE IF NOT EXISTS Whitelist (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DeviceId TEXT,
                        Path TEXT,
                        Type TEXT, -- Folder, Extension
                        FOREIGN KEY(DeviceId) REFERENCES Devices(Id)
                    );";

                string createWhitelistIndex = @"
                    CREATE INDEX IF NOT EXISTS idx_whitelist_device_path ON Whitelist(DeviceId, Path);";

                // New: ImportHistory table for imported files
                string createImportHistoryTable = @"
                    CREATE TABLE IF NOT EXISTS ImportHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        DeviceId TEXT,
                        Path TEXT,
                        FileName TEXT,
                        FileSize INTEGER,
                        DateImported TEXT,
                        UNIQUE(DeviceId, Path)
                    );";

                string createImportHistoryIndex = @"
                    CREATE INDEX IF NOT EXISTS idx_importhistory_device_path ON ImportHistory(DeviceId, Path);";

                using (var cmd = new SQLiteCommand(createDevicesTable, connection)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createWhitelistTable, connection)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createWhitelistIndex, connection)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createImportHistoryTable, connection)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createImportHistoryIndex, connection)) cmd.ExecuteNonQuery();
            }
        }

        public DeviceConfig? GetDeviceConfig(string id)
        {
            _logger.LogInformation("Loading device config for {DeviceId}.", id);
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string query = "SELECT * FROM Devices WHERE Id = @Id";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new DeviceConfig(
                                reader["Id"].ToString() ?? string.Empty,
                                reader["DeviceName"].ToString() ?? string.Empty,
                                reader["LastImportDate"].ToString() ?? string.Empty,
                                Convert.ToInt32(reader["AutoTrigger"]) == 1
                            );
                        }
                    }
                }
            }
            return null;
        }

        public void SaveDeviceConfig(DeviceConfig config)
        {
            _logger.LogInformation("Saving device config for {DeviceId}.", config.Id);
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string query = @"
                    INSERT OR REPLACE INTO Devices (Id, DeviceName, LastImportDate, AutoTrigger)
                    VALUES (@Id, @DeviceName, @LastImportDate, @AutoTrigger);";

                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", config.Id);
                    cmd.Parameters.AddWithValue("@DeviceName", config.DeviceName);
                    cmd.Parameters.AddWithValue("@LastImportDate", config.LastImportDate);
                    cmd.Parameters.AddWithValue("@AutoTrigger", config.AutoTrigger ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<WhitelistRule> GetWhitelist(string deviceId)
        {
            var list = new List<WhitelistRule>();
            _logger.LogInformation("Loading whitelist rules for {DeviceId}.", deviceId);
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string query = "SELECT * FROM Whitelist WHERE DeviceId = @DeviceId";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new WhitelistRule(
                                Convert.ToInt32(reader["Id"]),
                                reader["DeviceId"].ToString() ?? string.Empty,
                                reader["Path"].ToString() ?? string.Empty,
                                reader["Type"].ToString() ?? string.Empty
                            ));
                        }
                    }
                }
            }
            return list;
        }

        public void AddWhitelistRule(WhitelistRule rule)
        {
            _logger.LogInformation("Adding whitelist rule for {DeviceId} with path {RulePath}.", rule.DeviceId, rule.Path);
            using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
            {
                connection.Open();
                string query = "INSERT INTO Whitelist (DeviceId, Path, Type) VALUES (@DeviceId, @Path, @Type)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@DeviceId", rule.DeviceId);
                    cmd.Parameters.AddWithValue("@Path", rule.Path);
                    cmd.Parameters.AddWithValue("@Type", rule.RuleType);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
