using System;
using System.Reflection;
using ImageMagick;
using Xunit;

namespace Smoerfugl.ConvertHeifToPng.Tests
{
    public class MagickVersionTests
    {
        [Fact]
        public void MagickNetVersion_IsAtLeast_14_10_0()
        {
            var asm = typeof(MagickImage).Assembly;
            var version = asm.GetName().Version;
            // Assembly version for Magick.NET 14.10.0 should be at least 14.10.0.0
            var min = new Version(14,10,0,0);
            Assert.True(version >= min, $"Magick.NET assembly version {version} is less than required {min}");
        }
    }
}
