namespace QuickMediaIngest.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data.SQLite;
    using System.IO;
    using QuickMediaIngest.Data.Models;

    public class DatabaseService
    {
        private readonly string _dbPath;

        public DatabaseService()
        {
            // Store DB in AppData
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "QuickMediaIngest");
            _dbPath = Path.Combine(appFolder, "database.db");
            
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            string dir = Path.GetDirectoryName(_dbPath);
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

                using (var cmd = new SQLiteCommand(createDevicesTable, connection)) cmd.ExecuteNonQuery();
                using (var cmd = new SQLiteCommand(createWhitelistTable, connection)) cmd.ExecuteNonQuery();
            }
        }

        public DeviceConfig GetDeviceConfig(string id)
        {
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
                            return new DeviceConfig
                            {
                                Id = reader["Id"].ToString(),
                                DeviceName = reader["DeviceName"].ToString(),
                                LastImportDate = reader["LastImportDate"].ToString(),
                                AutoTrigger = Convert.ToInt32(reader["AutoTrigger"]) == 1
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void SaveDeviceConfig(DeviceConfig config)
        {
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
                            list.Add(new WhitelistRule
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                DeviceId = reader["DeviceId"].ToString(),
                                Path = reader["Path"].ToString(),
                                RuleType = reader["Type"].ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public void AddWhitelistRule(WhitelistRule rule)
        {
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
