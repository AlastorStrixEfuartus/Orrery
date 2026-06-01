# BLP Orrery

BLP Orrery is a Windows texture preview and inspection tool for game and modding workflows. It focuses on formats that normal image viewers often do not handle well, especially Blizzard BLP files and BioWare/Neverwinter Nights DDS and PLT textures.

The repository contains two companion projects:

- **BLP Orrery** - a WinForms desktop viewer for opening, inspecting, previewing, zooming, navigating, and exporting texture files.
- **WindowsShellExtension Orrery** - a Windows Explorer thumbnail provider built from the Orrery decoding core.

## Supported Formats

BLP Orrery can open:

- `.blp` Blizzard Picture textures, including DXT-compressed and RAW3 BGRA files
- `.dds` DirectDraw Surface textures
- BioWare/NWN compact `.dds` textures
- `.plt` Neverwinter Nights player texture layer files
- `.tga` Truevision TGA images
- `.ico` Windows icon files
- `.png`
- `.jpg` / `.jpeg`

The Windows Shell Extension provides Explorer thumbnails for:

- `.blp`
- `.dds`
- BioWare/NWN compact `.dds`
- `.plt`
- `.ico`

## BLP Orrery Features

- Fast texture preview with mipmap selection
- Correct DXT handling for detailed BLP and DDS previews
- Drag and drop loading
- Command-line file opening for file association use
- Folder navigation with toolbar buttons and keyboard arrow keys
- Cached and coalesced navigation for smoother rapid browsing
- Transparency preview toggle with configurable matte color
- Alpha mask preview mode
- Zoom in/out buttons and mouse-wheel zoom
- 1:1 actual-size preview mode without resizing the application window
- Save As export to PNG and JPG
- File information panel with format, alpha, resolution, mipmap size, and offset data
- PLT layer rows with selectable layer highlighting
- Optional resize-to-texture mode for 1:1 inspection
- About window with project splash art, supported formats, author information, and changelog

## Shell Extension Package

`WindowsShellExtension_Orrery_Package` contains installer and uninstaller scripts intended for sharing:

- `Install-OrreryThumbnails.cmd`
- `Install-OrreryThumbnails.ps1`
- `Uninstall-OrreryThumbnails.cmd`
- `Uninstall-OrreryThumbnails.ps1`
- `README.txt`

The recipient should extract the package to a permanent folder, run the installer as administrator, and restart Explorer or reopen the folder if thumbnails do not refresh immediately.

The package scripts expect the compiled shell extension DLL to exist in `bin/`. Build the `WindowsShellExtension_Orrery` project in Release mode before packaging binaries for distribution.

## Build

Both projects target .NET Framework 4.8 and are intended for Visual Studio/MSBuild on Windows.

Build BLP Orrery:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' BLP_Orrery\BLP_Orrery.sln /p:Configuration=Release /p:Platform="Any CPU" /p:UseSharedCompilation=false
```

Build the shell extension:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' WindowsShellExtension_Orrery\WindowsShellExtension_Orrery.sln /p:Configuration=Release /p:Platform="Any CPU"
```

## Author

Created by **Alastor Strix'Efuartus**.

Development began in 2022.
