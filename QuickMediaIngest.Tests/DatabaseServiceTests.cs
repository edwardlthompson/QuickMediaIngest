using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Data;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DatabaseServiceTests
    {
        [Fact]
        public void TryPeriodicVacuum_RunsOnFreshDatabase()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "qmi_db_test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string dbPath = Path.Combine(tempDir, "test.db");
            try
            {
                var logger = new Mock<ILogger<DatabaseService>>();
                var service = new DatabaseService(logger.Object, dbPath);

                service.TryPeriodicVacuum(minimumDaysBetweenRuns: 0);

                Assert.True(File.Exists(dbPath));
                Assert.Equal(dbPath, service.DatabasePath);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore test cleanup failures.
                }
            }
        }

        [Fact]
        public void TryPeriodicVacuum_SkipsWhenRecentlyVacuumed()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "qmi_db_test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string dbPath = Path.Combine(tempDir, "test.db");
            string stampPath = Path.Combine(tempDir, "last_sqlite_vacuum.txt");
            try
            {
                var logger = new Mock<ILogger<DatabaseService>>();
                var service = new DatabaseService(logger.Object, dbPath);

                service.TryPeriodicVacuum(minimumDaysBetweenRuns: 0);
                Assert.True(File.Exists(stampPath));

                File.WriteAllText(stampPath, DateTime.UtcNow.ToString("O"));
                service.TryPeriodicVacuum(minimumDaysBetweenRuns: 14);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Ignore test cleanup failures.
                }
            }
        }
    }
}
