## How to use

Download the release archive for your operating system and architecture (`win-x64`,
`linux-x64`, `linux-arm64`, `osx-x64`, or `osx-arm64`) and extract it. The
executables are self-contained, so no .NET installation is required.

Pass a HEIC/HEIF file or a directory containing them. PNG files are written
alongside the originals.

Windows:
```powershell
.\Smoerfugl.ConvertHeifToPng.exe C:\tmp\
```

Linux or macOS:
```sh
./Smoerfugl.ConvertHeifToPng /path/to/photos
```

To run from source, install the .NET 10 SDK and use
`dotnet run --project Smoerfugl.ConvertHeifToPng -- /path/to/photos`.
