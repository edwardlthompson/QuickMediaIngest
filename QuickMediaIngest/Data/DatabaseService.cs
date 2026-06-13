#nullable enable
namespace QuickMediaIngest.Data
{
    using System;
    using System.Data.SQLite;
    using System.IO;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// SQLite maintenance only. App config, history, and whitelist persist via JSON under AppData.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private readonly string _dbPath;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ILogger<DatabaseService> logger)
            : this(logger, null)
        {
        }

        /// <summary>For production DI and tests with an isolated database path.</summary>
        public DatabaseService(ILogger<DatabaseService> logger, string? databasePath)
        {
            _logger = logger;
            if (!string.IsNullOrWhiteSpace(databasePath))
            {
                _dbPath = databasePath;
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = Path.Combine(appData, "QuickMediaIngest");
                _dbPath = Path.Combine(appFolder, "database.db");
            }

            EnsureDatabaseFile();
        }

        public string DatabasePath => _dbPath;

        private void EnsureDatabaseFile()
        {
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
                _logger.LogInformation("Created SQLite file at {DatabasePath} for periodic maintenance.", _dbPath);
            }
        }

        public void TryPeriodicVacuum(int minimumDaysBetweenRuns = 14)
        {
            try
            {
                string? dir = Path.GetDirectoryName(_dbPath);
                if (string.IsNullOrEmpty(dir))
                {
                    return;
                }

                string stampPath = Path.Combine(dir, "last_sqlite_vacuum.txt");
                if (File.Exists(stampPath))
                {
                    string text = File.ReadAllText(stampPath).Trim();
                    if (DateTime.TryParse(text, out var last) &&
                        (DateTime.UtcNow - last.ToUniversalTime()).TotalDays < minimumDaysBetweenRuns)
                    {
                        return;
                    }
                }

                using (var connection = new SQLiteConnection($"Data Source={_dbPath};Version=3;"))
                {
                    connection.Open();
                    using (var cmd = new SQLiteCommand("VACUUM;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                File.WriteAllText(stampPath, DateTime.UtcNow.ToString("O"));
                _logger.LogInformation("SQLite VACUUM completed at {Path}.", _dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SQLite periodic VACUUM skipped or failed.");
            }
        }
    }
}
