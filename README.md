# BLP Orrery

BLP Orrery is a Windows texture preview and inspection tool for game and modding workflows. It focuses on formats that normal image viewers often do not handle well, especially Blizzard BLP files and BioWare/Neverwinter Nights DDS and PLT textures.

The repository contains two integrated projects:

- **BLP Orrery** - a WinForms desktop viewer for opening, inspecting, previewing, zooming, navigating, and exporting texture files.
- **WindowsShellExtension Orrery** - a Windows Explorer thumbnail provider built from the same authoritative decoder sources and embedded into every BLP Orrery build.

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
- Integrated Explorer thumbnail controls with live status, a master enable switch, and independent `.blp`, `.dds`, `.plt`, and `.ico` selection
- Per-user shell-extension installation and updates without PowerShell, RegAsm, administrator elevation, or a permanently unpacked side package

## Integrated Explorer Thumbnails

Open **Explorer Thumbnails** in BLP Orrery's main menu. The menu can enable or disable the provider, choose the formats Orrery owns, apply an embedded provider update, refresh Explorer, display diagnostics, and open the immutable installation folder.

The provider is extracted from the executable to `%LOCALAPPDATA%\BLP Orrery\ShellExtension\<payload-hash>`. Updates use a new hash-versioned directory instead of overwriting a DLL that Explorer may have loaded. Registration is written to the current user's 64-bit class registry, and displaced per-user handlers are restored when a format is disabled. Existing machine-wide Orrery registrations are overridden per user, so old installations do not need to be removed before using the integrated controls.

See [`docs/EXPLORER_THUMBNAILS.md`](docs/EXPLORER_THUMBNAILS.md) for architecture, recovery, and packaging details.

Obsolete standalone shell-extension packages and earlier desktop release archives are not retained. `WindowsShellExtension_Orrery` remains in the repository because it is the required source project for the provider embedded in BLP Orrery 2.9.

## Build

Both projects target .NET Framework 4.8 and are intended for Visual Studio/MSBuild on Windows.

Build BLP Orrery:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' BLP_Orrery\BLP_Orrery.sln /p:Configuration=Release /p:Platform="Any CPU" /p:UseSharedCompilation=false
```

Build the shell extension directly:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' WindowsShellExtension_Orrery\WindowsShellExtension_Orrery.sln /p:Configuration=Release /p:Platform="Any CPU"
```

Building BLP Orrery automatically builds the x64 shell provider and embeds that exact output into `BLP_Orrery.exe`.

## Author

Created by **Alastor Strix'Efuartus**.

Development began in 2022.
