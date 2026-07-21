# WindowsShellExtension Orrery 1.4

Windows Explorer thumbnail provider compiled from BLP Orrery's shared decoding core.

Supported thumbnail formats:

- `.blp` BLP2 textures
- `.dds` standard DDS textures
- `.dds` BioWare/NWN compact DDS textures
- `.plt` Neverwinter Nights layered textures
- `.ico` Windows icon files

## Build

Build with Visual Studio/MSBuild using the `Release` configuration. The project targets x64 because modern Windows Explorer is 64-bit.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' WindowsShellExtension_Orrery.sln /p:Configuration=Release /p:Platform="Any CPU"
```

## Register

Run PowerShell as Administrator:

```powershell
.\scripts\Register-OrreryShellExtension.ps1 -Configuration Release
```

Then restart Explorer, sign out and in, or wait for Explorer to refresh its thumbnail handler cache.

## Unregister

Run PowerShell as Administrator:

```powershell
.\scripts\Unregister-OrreryShellExtension.ps1 -Configuration Release
```

## Notes

The provider implements `IInitializeWithStream` and `IThumbnailProvider`, so Explorer gives it a file stream and receives an alpha-preserving `HBITMAP`. It avoids UI and returns failure silently when a file is unsupported, which is the expected behavior for shell extensions.

## Integrated Deployment

BLP Orrery 2.9 builds this project automatically, embeds the resulting x64 DLL, extracts it to an immutable per-user location, and controls registration from its **Explorer Thumbnails** menu. This is the preferred deployment path and does not require PowerShell, RegAsm, or administrator elevation.

The scripts below remain for legacy standalone development and package testing.
