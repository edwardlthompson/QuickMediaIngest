using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace QuickMediaIngest.Core
{
    public class ThumbnailService
    {
        public BitmapImage? GetThumbnail(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // DelayCreation avoids rendering the full image if a thumbnail is available
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count > 0)
                    {
                        var frame = decoder.Frames[0];
                        return CreateResizedThumbnail(frame);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Thumbnail Error] {filePath}: {ex.Message}");
            }
            return null;
        }

        private BitmapImage CreateResizedThumbnail(BitmapFrame frame)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.DecodePixelWidth = 160; // Shrink immediately in RAM
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            
            // Convert Frame to Stream to bypass lock issues
            var memoryStream = new MemoryStream();
            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(frame);
            encoder.Save(memoryStream);
            memoryStream.Position = 0;

            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();
            bitmap.Freeze(); // Make cross-thread safe for WPF binding
            return bitmap;
        }
    }
}
