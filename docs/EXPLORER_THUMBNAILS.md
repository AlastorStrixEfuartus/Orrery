# Integrated Explorer Thumbnails

BLP Orrery 2.9 owns the deployment and configuration of WindowsShellExtension Orrery 1.4. The provider remains a separate x64 COM assembly at runtime because Windows Explorer must load it in an isolated shell process, but its binary payload, decoder source, versioning, and registration lifecycle are controlled by BLP Orrery.

## Architecture

The desktop viewer is the authoritative source for the BLP, DDS, DXT, ICO, and PLT decoders. The shell-extension project compiles those files as linked sources. Shell-only code contains `IInitializeWithStream`, `IThumbnailProvider`, safe stream ingestion, thumbnail-size selection, alpha-preserving DIB creation, and COM registration compatibility.

The Release build sequence is:

1. Build `WindowsShellExtension_Orrery.dll` for x64 and .NET Framework 4.8.
2. Compile the same decoder sources used by BLP Orrery into the provider.
3. Embed the provider DLL as `BLP_Orrery.Payload.WindowsShellExtension_Orrery.dll` in `BLP_Orrery.exe`.
4. On first status check or enablement, verify the payload and extract it to `%LOCALAPPDATA%\BLP Orrery\ShellExtension\<SHA-256-prefix>`.
5. Register the immutable extracted path for the current user and notify Windows Shell that associations changed.

Explorer therefore never loads the provider from the build or release directory. A running COM Surrogate can keep an old payload loaded without blocking a new Orrery build or provider update.

## Menu Controls

The **Explorer Thumbnails** menu contains:

- A live status row with active formats and update availability.
- **Enable Orrery thumbnails**, the master switch.
- **Use Orrery for**, with independent `.blp`, `.dds`, `.plt`, and `.ico` toggles.
- **Apply / Update Now**, which reapplies the selected state and activates the embedded provider version.
- **Refresh Windows Explorer**, which sends `SHCNE_ASSOCCHANGED` with a synchronous flush.
- **Status Details**, including versions, paths, scope, and legacy machine-install detection.
- **Open Installed Provider Folder** for deployment inspection.

Format and master switches apply immediately. Selecting no formats disables the provider while remembering the preferred format set for the next enablement.

## Registry Scope And Ownership

Integrated registration uses the 64-bit view of `HKCU\Software\Classes`, so it does not require administrator permission. The managed COM class points `InprocServer32` to `mscoree.dll` and records the provider class, assembly identity, runtime version, and immutable `CodeBase` URI.

For each selected extension, Orrery registers the thumbnail category `{E357FCCD-A995-4576-B01F-234630154E96}` under both the extension and `SystemFileAssociations`. Before replacing a per-user handler, it records whether a value existed and its exact value. Disabling the format restores that value only while Orrery still owns the active entry. A handler installed later by another application is left untouched.

If an older machine-wide Orrery handler is present, disabling a format creates an empty per-user override. This suppresses the legacy handler for the current user without deleting administrator-owned keys. Re-enabling replaces the override with the current embedded provider.

## Reliability Limits

The shell provider runs outside the desktop viewer and treats every input as untrusted:

- Input streams are capped at 256 MiB and read in checked 64 KiB chunks.
- BLP and DDS thumbnails choose an appropriate readable mipmap before decoding.
- Selected decode surfaces are capped at 16,777,216 pixels inside Explorer.
- Damaged BLP mipmap table entries are skipped by the shared decoder.
- Header, dimension, offset, size, arithmetic, and short-read failures return `E_FAIL` without UI inside Explorer.
- The generated top-down 32-bit DIB preserves premultiplied alpha.
- Full payload SHA-256 is verified every time an extracted provider is reused.

## Recovery

If thumbnails remain cached after a format change, choose **Refresh Windows Explorer**. Explorer may retain already-generated thumbnail cache entries; reopening the folder or changing the icon size normally causes a new request. A sign-out or Explorer restart is a final fallback, not part of normal installation.

If the bundled provider is missing or corrupt, the menu reports it as unavailable and leaves existing associations unchanged. Replacing BLP Orrery with a complete Release build restores the embedded payload. Disabling the master switch removes Orrery's per-user COM registration and restores or suppresses each managed format safely.

## Source Layout

The obsolete loose-DLL package is not part of the repository or the 2.9 release. Keep the `WindowsShellExtension_Orrery` project itself: BLP Orrery builds that project for x64 and embeds its output. Removing the project would make a clean Orrery build incomplete.
