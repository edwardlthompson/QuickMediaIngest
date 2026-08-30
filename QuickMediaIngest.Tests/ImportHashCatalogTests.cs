#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class ImportHashCatalogTests
    {
        [Fact]
        public async Task RecordSaveLoad_RecognizesAlreadyImportedHash()
        {
            string catalogFile = Path.Combine(Path.GetTempPath(), $"catalog-test-{Guid.NewGuid():N}.json");
            string testFile = Path.Combine(Path.GetTempPath(), $"sample-file-{Guid.NewGuid():N}.jpg");

            try
            {
                await File.WriteAllTextAsync(testFile, "test hash bytes");

                var catalog = new ImportHashCatalog();
                string hash = await catalog.ComputeFileHashAsync(testFile);
                Assert.False(string.IsNullOrEmpty(hash));
                Assert.False(catalog.IsAlreadyImported(hash));

                catalog.RecordImported(hash, @"D:\Photos\Shoot1\sample.jpg");
                Assert.True(catalog.IsAlreadyImported(hash));

                await catalog.SaveAsync(catalogFile);

                var catalog2 = new ImportHashCatalog();
                await catalog2.LoadAsync(catalogFile);
                Assert.True(catalog2.IsAlreadyImported(hash));
            }
            finally
            {
                if (File.Exists(catalogFile)) File.Delete(catalogFile);
                if (File.Exists(testFile)) File.Delete(testFile);
            }
        }
    }
}
