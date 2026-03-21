using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using QuickMediaIngest.Core;
using QuickMediaIngest.Core.Models;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class IngestEngineTests
    {
        [Fact]
        public void ResolveFileName_UsesNamingTemplateAndGroupTitle()
        {
            // Arrange
            var provider = new Mock<IFileProvider>();
            var logger = new Mock<ILogger<IngestEngine>>();
            var engine = new IngestEngine(provider.Object, logger.Object);
            var item = new ImportItem
            {
                FileName = "IMG_1234.JPG",
                DateTaken = new DateTime(2024, 3, 20, 14, 30, 0)
            };
            string targetDir = "C:/Test";
            string namingTemplate = "[YYYY]-[MM]-[DD]_[Original]_[ShootName]";
            string groupTitle = "Shoot 1";

            // Act
            string result = engine.ResolveFileName(item, targetDir, namingTemplate, groupTitle);

            // Assert
            Assert.Contains("2024-03-20", result);
            Assert.Contains("IMG_1234", result);
            Assert.Contains("Shoot 1", result);
        }
    }
}
