<!-- machine-profile status: FILLED -->
<!--
  Machine-specific paths for this workspace, written by the discovery in
  .claude/CLAUDE.md section 0. A FILLED file whose Hostname does not match the
  current computer is treated as stale and re-discovered.
-->

# Machine-specific paths

| Field | Value |
|---|---|
| Hostname | BOL-WS-01 |
| Discovered on | 2026-10-01 |

| What | Path |
|---|---|
| Alibre install root | `C:\Program Files\Alibre Design 29.1.0.29127\` |
| API binaries / TLBs | `C:\Program Files\Alibre Design 29.1.0.29127\Program\` (`AlibreX.dll` 29.1.0.29127, `AlibreX_64.tlb`, `AlibreAddOn.dll` 29.1.0.29127, `AlibreAddOn_64.tlb`; 32-bit `AlibreX.tlb` / `AlibreAddOn.tlb` also present) |
| Headless COM registration | `C:\Program Files\Alibre Design 29.1.0.29127\Program\registerCOMdlls.BAT` (run as admin; `unregisterCOMdlls.BAT` alongside) |
| Executable | `C:\Program Files\Alibre Design 29.1.0.29127\Program\Alibre Design.exe` (FileVersion 29.1.0.29127 → build **29127**) |
| App data | `C:\ProgramData\Alibre Design\29.1.0.29127\` |
| Deploy target | `C:\ProgramData\Alibre AddOns\<AddOnName>\` — **exists** |
| Registry key | `HKLM\SOFTWARE\Alibre Design Add-Ons` — **exists**, 4 registrations (see Notes) |
| IDE | **JetBrains Rider 2026.3** (build 263.5153.35), `C:\Users\DavidBolsover\AppData\Local\Programs\Rider\` (config `%APPDATA%\JetBrains\Rider2026.3`); `rider-cpp` plugin present |
| Visual Studio | Visual Studio Enterprise 2026, 18.8.0 (18.8.12009.203), `C:\Program Files\Microsoft Visual Studio\18\Enterprise` — used only as the MSVC/MFC toolchain provider for C++ |
| MSVC toolset | 14.51.36231 (from VS above) — `atlmfc` **present** (MFC available) |
| Windows SDK | 10.0.22621.0, 10.0.26100.0 |
| MSBuild (C# / .NET Framework) | Rider-bundled: `C:\Users\DavidBolsover\AppData\Local\Programs\Rider\tools\MSBuild\Current\Bin\amd64\MSBuild.exe` (18.7.8) — **verified**: builds `AlibreAddOnManager.slnx` |
| MSBuild (C++ / `.vcxproj`) | `C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe` (18.8.2) — required, the Rider-bundled MSBuild has no `Microsoft\VC` targets |
| .NET SDKs | 8.0.425, 10.0.112, 10.0.302 (`C:\Program Files\dotnet\`) |
| .NET Framework targeting packs | v4.0 – v4.8.1 (v4.8.1 used by `AlibreAddOnManager`) |

## Notes

- **Single Alibre install** (29.1.0.29127), so no side-by-side choice is
  needed for standalone C# work. `C:\ProgramData\Alibre Design\29.0.1.29069\`
  and `29.1.0.29126\` are leftover app data from earlier, since-removed
  versions (29126 was upgraded in place to 29127).
- **Rider is the IDE.** Rider has no MSBuild toolset override set
  (`resharper-host\GlobalSettingsStorage.DotSettings`), so it auto-selects —
  normally the newest found, i.e. the VS 2026 MSBuild (18.8.2) over its bundled
  18.7.8. Either builds the C# manager. For C++ add-ons Rider must use the VS
  MSBuild (Settings → Build, Execution, Deployment → Toolset and Build) because
  only it has the VC/MFC targets. Command-line builds: use the bundled MSBuild
  for C#, the VS MSBuild for `.vcxproj`.
- `C:\Program Files\JetBrains\Rider\` holds only an `r2r\2026.3.0EAP` leftover
  — not a Rider install; ignore it.
- vswhere also lists *SQL Server Management Studio 22* (it ships an MSBuild,
  no MSVC). Ignore it.
- **Existing add-on registrations** under `HKLM\SOFTWARE\Alibre Design Add-Ons`
  (all folders exist):

  | GUID | Folder | `.adc` |
  |---|---|---|
  | `{305297BD-DE8D-4F36-86A4-AA5E69538A69}` | `C:\Program Files\UtilitiesForAlibre` | `UtilitiesForAlibre.adc`, spec v2 |
  | `{378829C4-F122-4617-92E0-E36ADD4F9AA8}` | `C:\Program Files\AlibreExportOpen` | `AlibreExportOpen.adc`, spec v2 |
  | `{37882997-F122-5217-92E0-976ADD4F9AA8}` | `C:\Program Files\AlibreStlStpConverter` | `AlibreStlStpConverter.adc`, spec v2 |
  | `{925B370F-9858-4416-A7D9-1DD8FAAC16A0}` | `C:\ProgramData\Alibre AddOns\AlibreAddOnManager` | this repo's add-on (installed by `deploy/install.ps1`) |

  The AlibreDynamo dev-build registration recorded on 2026-09-27 is gone.
  `C:\ProgramData\Alibre AddOns\` also holds `AlibrePdmApiSample\` (not
  registered) and the manager's `.manager\` state folder.

- **The three third-party add-ons are managed (.NET)** — their `.adc` files use
  `<DLL type="Managed" ...>`, an attribute not documented in `Reference/`.
  They are deployed under `C:\Program Files\`, not
  `C:\ProgramData\Alibre AddOns\`. For `AlibreAddOnManager` this means:
  - registrations can point anywhere, so the scanner must not assume the
    `ProgramData` location;
  - the x64 PE-machine check must accept managed DLLs (AnyCPU IL-only
    assemblies report machine `0x14C` with the ILONLY CLR flag) — now covered
    in `src/AlibreAddOnManager/online-addon-directory.md` §1.1 and §3.2;
  - these are live "unmanaged" test cases for the Installed view — don't
    modify them during testing without asking.
