#nullable enable
using QuickMediaIngest.Core.Services;
using Xunit;

namespace QuickMediaIngest.Tests
{
    public class FfmpegTranscoderTests
    {
        [Fact]
        public void BuildArguments_FormatsExpectedParameters()
        {
            var options = new TranscodeOptions
            {
                Enabled = true,
                TargetCodec = "libx265",
                Crf = 24,
                Preset = "medium"
            };

            string args = FfmpegTranscoder.BuildArguments(@"C:\in.mov", @"C:\out.mp4", options);
            Assert.Contains("-c:v libx265", args);
            Assert.Contains("-crf 24", args);
            Assert.Contains("-preset medium", args);
        }
    }
}
