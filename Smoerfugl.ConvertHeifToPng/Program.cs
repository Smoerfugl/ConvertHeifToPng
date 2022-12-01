using System.Diagnostics;
using ImageMagick;

var path = Path.GetDirectoryName(args.FirstOrDefault());
if (path == null)
{
    Console.WriteLine("No directory path provided - exitting");
    return;
}

Console.WriteLine($"Searching {path} for .heic files");
var files = Directory.GetFiles(path)
    .Where(d => d.EndsWith(".heic"))
    .ToList();
Console.WriteLine($"Found {files.Count} files");

files.AsParallel()
    .ForAll(file =>
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var expectedFileName = path + Path.DirectorySeparatorChar + fileName + ".png";
        var s = Stopwatch.StartNew();
        using (var image = new MagickImage(file))
        {
            Console.WriteLine($"- [x] Converting {image.FileName} to {expectedFileName}");
            image.Write(expectedFileName);
        }

        s.Stop();
        Console.WriteLine($"- [x] {file} -> {expectedFileName} took {s.Elapsed}");
    });