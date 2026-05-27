WindowsShellExtension Orrery
============================

This package installs Explorer thumbnail previews for:

- .blp Blizzard Picture textures
- .dds DirectDraw Surface textures
- BioWare/NWN compact DDS textures
- .ico Windows icon files

Requirements
------------

- 64-bit Windows
- Microsoft .NET Framework 4.x
- Administrator permission during install/uninstall

Install
-------

1. Extract the whole package to a permanent folder.
   Do not register it from a temporary 7z/zip viewer, because Explorer loads the DLL from this exact path.

2. Run Install-OrreryThumbnails.cmd.
   Approve the Windows UAC prompt.

3. Restart Explorer, sign out/in, or reopen the folder if thumbnails do not refresh immediately.

Uninstall
---------

Run Uninstall-OrreryThumbnails.cmd as administrator.

Important
---------

After installation, keep the bin folder and WindowsShellExtension_Orrery.dll in place.
If you move or delete the package folder, uninstall first, then install again from the new location.

The DLL is an unsigned development build. Windows may show normal warnings for unsigned code.

