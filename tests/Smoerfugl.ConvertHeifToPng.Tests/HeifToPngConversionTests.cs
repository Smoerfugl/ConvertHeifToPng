using System;
using System.Diagnostics;
using System.IO;
using ImageMagick;
using Xunit;

namespace Smoerfugl.ConvertHeifToPng.Tests
{
    public class HeifToPngConversionTests
    {
        [Fact]
        public void ConvertsHeicToPng_and_PreservesDimensions()
        {
            // Arrange
            var temp = Path.Combine(Path.GetTempPath(), "heictest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);

            var heicPath = Path.Combine(temp, "test.heic");
            var goldenPngPath = Path.Combine(temp, "golden.png");
            var outputPngPath = Path.Combine(temp, "test.png");

            try
            {
                // Create a small 16x16 red PNG as the golden image
                using (var img = new MagickImage(MagickColors.Red, 16, 16))
                {
                    img.Write(goldenPngPath);
                }

                // Create a test.heic by copying the PNG bytes to a .heic filename.
                File.Copy(goldenPngPath, heicPath);

                // Act
                // Locate project file by walking up from the test context, then run with `dotnet run` to execute the converter
                string csprojPath = null;
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 12 && dir != null; i++)
                {
                    var candidate = Path.GetFullPath(Path.Combine(dir.FullName, "..", "..", "Smoerfugl.ConvertHeifToPng", "Smoerfugl.ConvertHeifToPng.csproj"));
                    if (File.Exists(candidate))
                    {
                        csprojPath = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }
                Assert.True(!string.IsNullOrEmpty(csprojPath), "Project file not found while walking up from test context");

                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project \"{csprojPath}\" -- \"{temp}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                using (var proc = Process.Start(psi))
                {
                    proc.WaitForExit(30_000);
                    var stdout = proc.StandardOutput.ReadToEnd();
                    var stderr = proc.StandardError.ReadToEnd();
                    Assert.True(proc.ExitCode == 0, $"Converter failed. ExitCode={proc.ExitCode}. Stdout={stdout}. Stderr={stderr}");
                }

                // Assert
                Assert.True(File.Exists(outputPngPath), "Output PNG was not created");

                using (var outImg = new MagickImage(outputPngPath))
                using (var golden = new MagickImage(goldenPngPath))
                {
                    Assert.Equal(golden.Width, outImg.Width);
                    Assert.Equal(golden.Height, outImg.Height);

                    // Simple pixel color check in center
                    int centerX = (int)outImg.Width / 2;
                    int centerY = (int)outImg.Height / 2;
                    var px = outImg.GetPixels().GetPixel(centerX, centerY);
                    var r = px.GetChannel(0);
                    var g = px.GetChannel(1);
                    var b = px.GetChannel(2);
                    Assert.True(r > 200 && g < 50 && b < 50, "Output PNG center pixel is not red as expected");
                }
            }
            finally
            {
                try { Directory.Delete(temp, true); } catch { }
            }
        }
    }
}
