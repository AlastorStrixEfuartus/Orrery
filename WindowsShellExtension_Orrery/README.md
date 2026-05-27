# WindowsShellExtension Orrery

Windows Explorer thumbnail provider forked from the BLP Orrery decoding core.

Supported thumbnail formats:

- `.blp` BLP2 textures
- `.dds` standard DDS textures
- `.dds` BioWare/NWN compact DDS textures
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
