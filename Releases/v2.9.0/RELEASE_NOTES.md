# BLP Orrery 2.9.0

BLP Orrery and WindowsShellExtension Orrery now ship as one controlled product. The x64 Explorer thumbnail provider is built from Orrery's authoritative decoder sources, embedded in `BLP_Orrery.exe`, and deployed on demand from the new **Explorer Thumbnails** menu.

## Highlights

- Enable, disable, update, and inspect Explorer thumbnails without PowerShell, RegAsm, administrator elevation, or a separate permanent extension folder.
- Select thumbnail ownership independently for `.blp`, `.dds`, `.plt`, and `.ico` files.
- Install immutable hash-versioned provider payloads under `%LOCALAPPDATA%`, avoiding rebuild and update failures caused by DLLs locked in COM Surrogate.
- Preserve and restore displaced per-user thumbnail handlers, while safely overriding legacy machine-wide Orrery installations for the current user.
- Compile BLP, DDS/DXT, PLT, and ICO decoding from one shared source set so desktop previews and Explorer thumbnails do not drift apart.
- Reject oversized, truncated, corrupt, or arithmetically unsafe shell inputs without opening UI or destabilizing Explorer.
- Add mouse-drag image panning and pointer-anchored wheel zoom to the desktop preview.

## Versions

- BLP Orrery: 2.9.0.0
- WindowsShellExtension Orrery: 1.4.0.0
- Runtime: .NET Framework 4.8 on 64-bit Windows

## Installation

Extract the release and run `BLP_Orrery.exe`. Open **Explorer Thumbnails** in the main menu, select the formats Orrery should handle, and enable the provider. Existing standalone Orrery shell-extension installations can remain in place; the integrated per-user registration takes precedence.

The release archive does not contain a loose shell-extension DLL. The exact provider binary is embedded in `BLP_Orrery.exe`, integrity-checked, and extracted automatically when needed.
