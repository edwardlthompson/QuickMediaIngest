#nullable enable
using System;
using System.IO;
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class DestinationEncryptionDetectorTests
    {
        [Fact]
        public void DetectEncryption_EmptyPath_ReturnsUnknown()
        {
            var status = DestinationEncryptionDetector.DetectEncryption(null);
            Assert.Equal(VolumeEncryptionStatus.Unknown, status);

            var emptyStatus = DestinationEncryptionDetector.DetectEncryption("   ");
            Assert.Equal(VolumeEncryptionStatus.Unknown, emptyStatus);
        }

        [Fact]
        public void DetectEncryption_SystemDrive_DoesNotThrow()
        {
            string systemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            var status = DestinationEncryptionDetector.DetectEncryption(systemDrive);

            // Should safely return a valid enum without exception
            Assert.True(Enum.IsDefined(typeof(VolumeEncryptionStatus), status));
        }
    }
}
