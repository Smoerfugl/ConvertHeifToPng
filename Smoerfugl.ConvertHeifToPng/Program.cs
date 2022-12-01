using System.Diagnostics;
using ImageMagick;
using Spectre.Console;

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

AnsiConsole.Status()
    .Start($"Converting {files.Count} files",
        ctx =>
        {
            ctx.Spinner(Spinner.Known.Star);
            ctx.SpinnerStyle(Style.Parse("green"));
            var numberOfFiles = files.Count;
            files.AsParallel()
                .WithDegreeOfParallelism(2)
                .ForAll(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var expectedFileName = path + Path.DirectorySeparatorChar + fileName + ".png";
                    var s = Stopwatch.StartNew();
                    using (var image = new MagickImage(file))
                    {
                        image.Write(expectedFileName);
                    }

                    s.Stop();
                    AnsiConsole.MarkupLine($"[green]{file} -> {expectedFileName} took {s.Elapsed}[/]");
                    numberOfFiles--;
                    ctx.Status($"Converting files {numberOfFiles} remaining");
                });
        });