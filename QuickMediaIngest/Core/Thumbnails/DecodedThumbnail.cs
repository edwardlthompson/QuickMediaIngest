#nullable enable
namespace QuickMediaIngest.Core
{
    /// <summary>Decoded JPEG thumbnail payload (no WPF types).</summary>
    public sealed record DecodedThumbnail(byte[] JpegBytes, int Width, int Height);
}
