using System;
using System.IO;
using System.Linq;
using ImageMagick;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

class P {
  static void Main() {
    string path = Path.Combine(Path.GetTempPath(), "qmi-probe-full.dng");
    Console.WriteLine("File exists: " + File.Exists(path));
    if (!File.Exists(path)) return;
    Console.WriteLine("Size: " + new FileInfo(path).Length);
    try {
      var dirs = ImageMetadataReader.ReadMetadata(path);
      foreach (var d in dirs) {
        foreach (var t in d.Tags.Where(t => t.Name.Contains("Preview", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("Thumbnail", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("JPEG", StringComparison.OrdinalIgnoreCase) || t.Name.Contains("SubImage", StringComparison.OrdinalIgnoreCase))) {
          Console.WriteLine(t.DirectoryName + " | " + t.Name + " = " + t.Description);
        }
      }
    } catch (Exception ex) { Console.WriteLine("Metadata error: " + ex.Message); }
    try {
      using var img = new MagickImage(path);
      Console.WriteLine("Magick: " + img.Width + "x" + img.Height + " format=" + img.Format);
    } catch (Exception ex) { Console.WriteLine("Magick error: " + ex.Message); }
  }
}
