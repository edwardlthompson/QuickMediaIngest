#nullable enable
using System;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using QuickMediaIngest.Core;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class XmpCreatorCopyrightSidecarTests
    {
        [Fact]
        public void WriteXmpSidecarMetadata_CreatesValidXmpSidecarWithCreatorAndCopyright()
        {
            string tempMedia = Path.Combine(Path.GetTempPath(), $"xmp-test-{Guid.NewGuid():N}.cr3");
            string expectedSidecar = Path.ChangeExtension(tempMedia, ".xmp");

            try
            {
                File.WriteAllText(tempMedia, "dummy raw bytes");

                MetadataKeywordWriter.WriteXmpSidecarMetadata(
                    tempMedia,
                    new[] { "landscape", "mountains" },
                    creator: "Jane Doe Photography",
                    copyright: "Copyright (c) 2026 Jane Doe. All rights reserved.",
                    logger: NullLogger.Instance);

                Assert.True(File.Exists(expectedSidecar));
                string xmpContent = File.ReadAllText(expectedSidecar);

                Assert.Contains("Jane Doe Photography", xmpContent);
                Assert.Contains("Copyright (c) 2026 Jane Doe", xmpContent);
                Assert.Contains("landscape", xmpContent);
                Assert.Contains("mountains", xmpContent);
                Assert.Contains("<xmpRights:Marked>True</xmpRights:Marked>", xmpContent);
            }
            finally
            {
                if (File.Exists(tempMedia)) File.Delete(tempMedia);
                if (File.Exists(expectedSidecar)) File.Delete(expectedSidecar);
            }
        }
    }
}
