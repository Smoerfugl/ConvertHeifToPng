// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using ImageMagick;

var path = Path.GetDirectoryName(args.FirstOrDefault());
if (path == null)
{
    Console.WriteLine("No directory path provided - exitting");
    return;
}

var files = Directory.GetFiles(path).Where(d => d.EndsWith(".heic"));

files.AsParallel()
    .ForAll(file =>
    {
        var fileName = Path.GetFileNameWithoutExtension(file);
        var expectedFileName = path + fileName + ".png";
        var s = Stopwatch.StartNew();

        using var image = new MagickImage(file);
        image.Write(expectedFileName);

        s.Stop();
        Console.WriteLine($"- [x] {file} -> {expectedFileName} took {s.Elapsed}");
    });