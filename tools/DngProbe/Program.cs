using System;
using System.IO;
using ImageMagick;
using QuickMediaIngest.Core;
using Microsoft.Extensions.Logging.Abstractions;

class P {
  static void Main() {
    string path = Path.Combine(Path.GetTempPath(), "qmi-smoke.heic");
    Console.WriteLine("exists=" + File.Exists(path) + " len=" + new FileInfo(path).Length);
    try {
      using var img = new MagickImage(path);
      img.Thumbnail(240,240);
      Console.WriteLine("Magick direct: " + img.Width + "x" + img.Height);
    } catch (Exception ex) { Console.WriteLine("Magick FAIL: " + ex.Message); }
    var svc = new ThumbnailService(NullLogger<ThumbnailService>.Instance);
    var thumb = svc.GetThumbnail(path);
    Console.WriteLine("ThumbnailService: null=" + (thumb==null) + (thumb!=null ? (" size="+thumb.PixelWidth+"x"+thumb.PixelHeight+" frozen="+thumb.IsFrozen) : ""));
  }
}
