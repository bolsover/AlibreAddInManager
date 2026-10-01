# Alibre Add-On Manager

An **Alibre Design add-on** that installs, updates, disables and removes other
add-ons — from an online directory (catalog) or from a `.alibreaddon` package
file. Design: [`src/AlibreAddOnManager/online-addon-directory.md`](src/AlibreAddOnManager/online-addon-directory.md).

## Layout

| Path | What |
|---|---|
| `src/AlibreAddOnManager/` | The add-on: a managed (.NET Framework 4.8, x64) class library loaded by Alibre (`<DLL type="Managed">`). Entry type `AlibreAddOnAssembly.AlibreAddOn`; UI in `Ui/ManagerForm`. |
| `src/AlibreAddOnManager.Helper/` | Small elevated EXE that applies a batch of changes (files under `%ProgramData%\Alibre AddOns`, `HKLM\SOFTWARE\Alibre Design Add-Ons`, state file). Builds into the add-on's `bin\<Config>\`. |
| `src/AlibreAddOnManager.Shared/` | Shared project (`.shproj`) compiled into both — no extra DLL: `.adc` reader, PE/DLL inspection, package validation, registry, scanner, JSON models. |
| `deploy/` | `install.ps1` / `uninstall.ps1` — bootstrap the manager itself (one time). |
| `tools/` | `New-AlibreAddonPackage.ps1` (build a package + catalog entry), `New-ManagerIcon.ps1`. |
| `catalog-sample/` | A working local catalog listing the manager itself. |

## Build

```powershell
& "$env:LOCALAPPDATA\Programs\Rider\tools\MSBuild\Current\Bin\amd64\MSBuild.exe" AlibreAddOnManager.slnx /p:Configuration=Release
```

The Alibre interop assemblies are referenced from
`C:\Program Files\Alibre Design 29.1.0.29127\Program`; override with
`/p:AlibreProgramDir=...` for another version.

## Install the manager (once)

Close Alibre Design, then:

```powershell
powershell -ExecutionPolicy Bypass -File deploy\install.ps1          # UAC prompt
```

Start Alibre Design → **Add-Ons** ribbon → **Add-On Manager → Manage Add-Ons**.
After that the manager can update itself from a catalog or a package.

## Using it

- **Installed** tab — every registered add-on, wherever its registry value points.
  Status is *Tracked* (installed by the manager), *External* (vendor installer,
  hand-registered, or a dev build — can be disabled, files are never deleted),
  *Disabled*, *Broken* or *This add-on*.
- **Available** tab — enter a catalog: an `https://…/index.json` URL, or a local
  folder/file such as `…\catalog-sample`. Plain `http://` is refused.
- Actions are **queued**; **Apply changes** raises one UAC prompt for the batch.
  Anything that replaces or deletes files of a loaded add-on (updates,
  uninstalls, self-update) waits until Alibre Design has exited, then applies.
  Alibre only reads add-ons at startup, so restart it afterwards.

## Publishing an add-on to a catalog

1. Put `addon.json` next to the built `.adc`/DLL/icon (see `src/AlibreAddOnManager/addon.json`).
2. `tools\New-AlibreAddonPackage.ps1 -SourceDir <folder> -OutDir <catalog>\packages`
3. Paste the printed entry into `<catalog>\addons\{GUID}.json` and list the add-on
   in `<catalog>\index.json`. Relative URLs resolve against `index.json`.

Re-packaging changes the SHA-256 (zip timestamps), so update the catalog entry
every time — the manager refuses a package whose hash does not match.

## Not yet implemented

Catalog signature verification and Authenticode display (design §3.3 / phase 3),
prerequisite installation, standalone-tool packages.
