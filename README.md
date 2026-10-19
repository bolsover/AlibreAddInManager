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
  folder/file such as `D:\catalog`. Plain `http://` is refused.
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


# Alibre Catalog Manager

A standalone Windows desktop tool for **authors and maintainers of an Alibre Design add-on catalog**.
It creates new add-on projects (C# or C++), builds and packages them as `.alibreaddon` files, and
publishes them to a local catalog folder that the Alibre Add-On Manager reads.

The tool never loads AlibreX or talks to Alibre Design over COM. It only reads and writes files
(and runs MSBuild), so it runs whether or not Alibre is running.

## Features

The main window has three tabs..

### Catalog

Browse and edit a local catalog folder.

- **New… / Open… / Reload / Save**: create an empty catalog, open an existing one (a folder or its
  `index.json`), or discard edits and reload. Edits stay in memory until **Save**, and Save writes
  only the files that changed.
- **Add add-on / Add version**: add a package (`.alibreaddon`), or a build output folder that is
  packed on the fly. Each package is validated before it is added. The tool records its SHA-256
  and size, and copies it into `packages/` when you save.
- **Edit details**: select an add-on, or one of its versions, to edit its fields on the right.
  A value is applied on Enter (Ctrl+Enter in a multi-line field) or when you leave the field; Esc
  restores it. Grey fields are read-only.
- **Remove from listing**: removes the add-on from `index.json` but keeps its manifest and
  packages, so you can list it again later.
- **Delete…**: removes the add-on's manifest and local icon. It can also delete package files that
  no other entry uses.
- **Remove version… / Rehash**: drop a single version, or recompute the hash and size after a
  package was rebuilt in place.
- **Check catalog**: checks the catalog the way the manager will consume it. It flags duplicate
  IDs, shared folder names, ID mismatches between the index and a manifest, missing or duplicate
  versions, a stale `latestVersion`, missing `sha256`/`packageUrl`, invalid `dllType`/build ranges
  and, in a deep check, package contents that don't match the manifest. Double-click a problem to
  jump to its add-on.

### New Add-On

Generates a new, empty **Alibre add-on project** in your choice of language: the minimum that
builds, loads into Alibre and can be packaged for the catalog. Each one has one root menu with one
stateless command, and greys the command out below a minimum Alibre build. If the workspace is
**Home**, the generated `AddOnInvoke` entry point also runs the command, because Alibre calls
`AddOnInvoke` when the add-on is launched from the Home window (Utilities → Add-ons…). For other
workspaces it stays empty.

| Language | Project | Add-on DLL | Your code goes in |
|---|---|---|---|
| **C#** | `<Name>.csproj` (.NET Framework 4.8, x64) | managed | `<Name>AddOn.cs` |
| **C++** | `<Name>.vcxproj` (Win32 DLL, x64, static CRT, no MFC/ATL) | native | `<Name>AddOn.cpp` |

Files for every language:

| Generated file | Purpose |
|---|---|
| `<Name>.adc` | Add-on descriptor (identifier GUID, DLL type, workspace, menu text, author) |
| `<Name>.ico` | Generated icon showing the add-on's initials in a chosen colour |
| `addon.json` | Package manifest used by the catalog |
| `deploy\install.ps1`, `deploy\uninstall.ps1` | Optional. Copy the build output to `%ProgramData%\Alibre AddOns\<Name>` and register it under `HKLM\SOFTWARE\Alibre Design Add-Ons` (they elevate themselves if needed) |

C# files:

| Generated file | Purpose |
|---|---|
| `<Name>.csproj` | Project referencing the AlibreX / AlibreAddOn interop assemblies from Alibre's Program folder |
| `AlibreAddOn.cs` | Entry points Alibre calls |
| `<Name>AddOn.cs` | An `IAlibreAddOn` with one stateless command |
| `AlibreVersion.cs` | Minimum Alibre build gate |
| `Properties\AssemblyInfo.cs` | Assembly and file version |

C++ files:

| Generated file | Purpose |
|---|---|
| `<Name>.vcxproj` | Project that `#import`s `AlibreX_64.tlb` and `AlibreAddOn_64.tlb` from Alibre's Program folder |
| `pch.h`, `pch.cpp` | Precompiled header with the `#import`s |
| `AlibreAddOn.cpp` | The exports Alibre calls: `AddOnLoad`, `AddOnInvoke`, `AddOnUnload`, `GetAddOnInterface` |
| `<Name>AddOn.h`, `<Name>AddOn.cpp` | An `IAlibreAddOn` COM object with one stateless command |
| `AlibreVersion.h`, `AlibreVersion.cpp` | Minimum Alibre build gate |

Build C++ projects with MSBuild from Visual Studio or Build Tools (C++ workload). Rider's bundled
MSBuild has no C++ targets. A C++ project has no `AssemblyInfo.cs`, so only `addon.json` carries its
version.

You can also add the new project to an existing `.slnx`. A C++ project is mapped to its x64
platform there. After generation, the project is selected on the Package & Publish tab
automatically.

### Package & Publish

1. Pick an add-on project (`.csproj` or `.vcxproj`). If it has no `addon.json`, one is built from
   its `.adc` and DLL.
2. Optionally change the **version**. The tool updates `addon.json` and, if the project has one,
   `AssemblyInfo.cs` before building.
3. **Build & package**: runs MSBuild, copies `addon.json` into the build output, and packs the
   output folder into a `.alibreaddon`. `*.pdb` files and existing packages are left out.
4. **Publish to catalog**: adds the package to the catalog open on the Catalog tab, either as a
   new add-on or as a new version of an existing one.

MSBuild is found through `vswhere` (Visual Studio or Build Tools), with JetBrains Rider's bundled
MSBuild as a fallback. You can also choose an MSBuild path by hand.

## Catalog layout

```
<catalog>/
├── index.json              # listing: one entry per add-on (id, name, publisher, summary, latestVersion, manifestUrl…)
├── addons/
│   └── {GUID}.json         # per-add-on manifest: folder name, publisher, description, versions[]
└── packages/
    └── <FolderName>-<version>.alibreaddon
```

URLs inside the catalog are resolved relative to `index.json`, the same way the manager resolves
them. Remote (`http(s)`) manifests are listed but can't be edited here. On save, the tool writes
packages first, then manifests, then the index, and deletes files last. This order means the index
never points to a file that hasn't been written yet.

## Requirements

- Windows with the **.NET 10 Desktop Runtime** to run the tool; the **.NET 10 SDK** to build it
  (C# 10)
- **MSBuild** (Visual Studio, Build Tools, or Rider) to build add-on projects. Generated C# add-ons
  still target **.NET Framework 4.8**, the runtime Alibre Design hosts add-ons in, so they need
  the .NET Framework 4.8 targeting pack
- **Alibre Design** installed, only to build generated add-ons (they reference its interop assemblies or type libraries)
- For C++ add-ons: Visual Studio or Build Tools with the **Desktop development with C++** workload
- The sibling repository **`..\AlibreAddInManager`**. This project imports the shared project
  `src\AlibreAddOnManager.Shared\AlibreAddOnManager.Shared.projitems`, which provides the catalog
  and package models, package validation, JSON helpers and Alibre install detection.


The project is an SDK-style `net10.0-windows` WPF app with `LangVersion` 10. The output
is `bin\Release\AlibreCatalogManager.exe` (with its `.dll`, `.deps.json` and
`.runtimeconfig.json`). It is AnyCPU, so on 64-bit Windows it runs as a 64-bit process and
Program Files and the registry are the 64-bit views.

The shared project from `AlibreAddInManager` is also compiled into that repository's .NET
Framework 4.8 add-ons, so changes to it must build on both frameworks.

## Settings

The last opened catalog, the last project and the chosen MSBuild path are stored in
`%APPDATA%\AlibreCatalogManager\settings.json`.

## Project structure

| Folder | Contents |
|---|---|
| `Catalog/` | `CatalogDocument` (in-memory catalog with deferred save), `CatalogValidator`, `PackageBuilder`, `AddOnManifestFactory` (`addon.json` from `.adc`), version comparison |
| `Generation/` | `AddOnGenerator`, `AddOnSpec` and `AddOnLanguage` for new projects, icon writer, `.slnx` editing |
| `Publishing/` | `AddOnProject` (version, output folder), `MsBuildLocator`, `ProjectBuilder` |
| `Templates/` | Embedded `*.template` files with `%%Token%%` placeholders used by the generator: `Common/` (.adc, addon.json, deploy scripts), `ManagedAddOn/` (C#), `NativeAddOn/` (C++) |
| `Ui/` | WPF `MainWindow` and one view per tab (`CatalogView`, `NewAddOnView`, `PublishView`): XAML layout with code-behind. `PropertyEditor` edits the selected add-on and version (`AddOnEditor`, `VersionEditor`) |
| `App.xaml` | Application entry point and shared styles; opens `MainWindow` |
| `AlibreCatalogManager.csproj` | SDK-style project; also holds the tool's version, product name and copyright |
