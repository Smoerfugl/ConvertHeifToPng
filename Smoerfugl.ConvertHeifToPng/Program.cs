using System.Diagnostics;
using ImageMagick;
using Spectre.Console;

// Accept either a directory or a single file path as first argument
if (args.Length == 0)
{
    Console.WriteLine("No directory path provided - exiting");
    return;
}

var input = args[0];
string path;
List<string> files;
if (Directory.Exists(input))
{
    path = input;
    Console.WriteLine($"Searching {path} for .heic files");
    files = Directory.GetFiles(path)
        .Where(d => d.EndsWith(".heic", StringComparison.OrdinalIgnoreCase) || d.EndsWith(".heif", StringComparison.OrdinalIgnoreCase))
        .ToList();
}
else if (File.Exists(input))
{
    path = Path.GetDirectoryName(input) ?? ".";
    files = new List<string> { input };
    Console.WriteLine($"Converting single file: {input}");
}
else
{
    Console.WriteLine($"Path not found: {input}");
    return;
}

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