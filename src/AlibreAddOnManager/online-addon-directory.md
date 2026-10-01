# Design: Online Add-On Directory & Installer for Alibre Design

| | |
|---|---|
| Status | Phases 1–2 implemented (see §4, §6) |
| Date | 2026-09-27 |
| Author | David Bolsover |
| Applies to | `AlibreAddOnManager` — a managed Alibre add-on (C#, .NET Framework 4.8, x64) plus an elevated helper EXE |

Sources: `Reference/API Documentation/00_concepts.md` (§ *Registration*,
§ *Checking the Alibre Design Version*, § *Side-by-side Installations*),
`.claude/CLAUDE.md` §3.1, §5, §6, §7, the sample `.adc` files under
`Reference/Addon Samples/`, and the four add-ons already registered on the
development machine (recorded in `.claude/machine.md`).

---

## 1. What "installing an add-on" actually means

Alibre discovers an integrated add-on through exactly three things:

1. A folder containing the `.adc`, `.dll` and `.ico`. The documented
   convention is `C:\ProgramData\Alibre AddOns\<AddOnName>\`, but **Alibre does
   not require it** — see below.
2. A **String value** under `HKLM\SOFTWARE\Alibre Design Add-Ons` whose
   **name = the `.adc` Identifier GUID** and **data = that folder path**.
3. A restart of Alibre — the registry and `.adc` are read only at startup.

The registry value is the **only** locator. Registrations observed in practice
point at `C:\Program Files\<AddOn>\` folders written by vendor installers and
at developers' `bin\Debug` build folders, not just `ProgramData`. The manager
**installs** to `ProgramData\Alibre AddOns` but must **read** add-ons from
wherever their registry value points.

### 1.1 Two DLL flavours

The `.adc` `<DLL>` element comes in two forms:

| `.adc` | Flavour | Notes |
|---|---|---|
| `<DLL loadedWhen="Startup" location="X.dll"/>` (no `type`) | **Native** C++/MFC COM DLL | Documented in `Reference/`; all official samples. Must be x64 and export `AddOnLoad`, `AddOnInvoke`, `AddOnUnload` (and `GetAddOnInterface`). |
| `<DLL type="Managed" loadedWhen="Startup" location="X.dll"/>` | **Managed** .NET assembly | **Not documented in `Reference/`**, but used by every add-on currently registered on the development machine. Exports and bitness rules differ (see 3.2). |

A missing `type` attribute is treated as native. Values of `type` other than
`Managed` are unknown; the manager should treat them as "unrecognised" rather
than guess.

So the manager is a file-plus-one-registry-value installer. **It does not need
AlibreX at all.** That is deliberate: the COM path (`GetActiveObject` /
`new AutomationHook()`) binds to whichever side-by-side version last registered
its types, which is exactly the ambiguity to avoid. Install and uninstall must
never touch COM.

---

## 2. Online directory structure

A **static catalog + hosted binaries** model (like winget / Scoop): no server
code; it can live on GitHub Pages, S3, or any HTTPS host. Authors submit or
update entries by pull request; CI validates the manifests.

```
https://addons.example.org/
├─ catalog/
│  ├─ index.json              # small; fetched on every refresh
│  ├─ index.json.sig          # detached signature over index.json
│  └─ addons/
│     ├─ {GUID}.json          # full manifest: all versions of one add-on
│     └─ {GUID}/icon.png
└─ (packages hosted on GitHub Releases / CDN; referenced by URL)
```

### 2.1 `index.json` — the listing

```json
{
  "schemaVersion": 1,
  "generated": "2026-09-27T12:00:00Z",
  "addons": [
    {
      "id": "{3B769D4E-5222-4066-9608-8B4CF93671DD}",
      "name": "Part Color",
      "publisher": "Bolsover",
      "summary": "Change the active part's colour",
      "latestVersion": "1.2.0",
      "workspaces": ["Part", "SheetMetal"],
      "tags": ["appearance"],
      "manifestUrl": "addons/{3B769D4E-...}.json",
      "iconUrl": "addons/{3B769D4E-...}/icon.png"
    }
  ]
}
```

### 2.2 `addons/{GUID}.json` — per-add-on manifest

```json
{
  "schemaVersion": 1,
  "id": "{3B769D4E-5222-4066-9608-8B4CF93671DD}",
  "name": "Part Color",
  "folderName": "PartColorAddOn",
  "publisher": { "name": "Bolsover", "url": "https://...", "email": "..." },
  "license": "MIT",
  "homepage": "https://github.com/...",
  "description": "Longer markdown description...",
  "versions": [
    {
      "version": "1.2.0",
      "released": "2026-09-01",
      "kind": "integrated",
      "dllType": "native",
      "arch": "x64",
      "minAlibreBuild": 29000,
      "maxAlibreBuild": null,
      "packageUrl": "https://github.com/.../releases/download/v1.2.0/PartColor-1.2.0.alibreaddon",
      "sha256": "9f2c...",
      "size": 184320,
      "prerequisites": ["vcredist-x64-14"],
      "releaseNotes": "..."
    }
  ]
}
```

Key decisions:

- **`id` is the `.adc` GUID.** It is already the unique key Alibre uses in the
  registry, so the catalog introduces no second identity.
- **`folderName`** fixes the deploy folder, so updates replace in place.
- **`minAlibreBuild` / `maxAlibreBuild`** mirror the `IADRoot.Version`
  build-number gate, letting the manager filter or warn before downloading.
- **`kind`** leaves room for standalone C# tools later (`"standalone"` →
  installed to Program Files with a Start-menu shortcut, no registry value).
  v1 supports `integrated` only.
- **`dllType`** — `"native"` or `"managed"`; must agree with the `.adc`
  `<DLL type>` attribute (1.1). **`arch`** — `"x64"` for native; `"x64"` or
  `"anycpu"` for managed.
- **`prerequisites`** — native MFC add-ons linked dynamically need the VC++
  runtime; managed add-ons may need a specific .NET Framework version. The
  manager can detect and offer them; native authors should be encouraged to
  link MFC statically instead.

### 2.3 Package format: `.alibreaddon` (a renamed zip)

```
PartColor-1.2.0.alibreaddon
├─ addon.json            # copy of this version's manifest entry (offline/sideload install)
├─ PartColorAddOn.adc
├─ PartColorAddOn.dll
├─ PartColorAddOn.ico
└─ (any other runtime files)
```

The zip root **is** the deploy folder, so installation is extraction. Because
`addon.json` travels with the package, "Install from file…" (or double-click)
works without the catalog.

---

## 3. Download & install mechanism

```
 UI process (unelevated)                          Elevated helper (same EXE, --apply)
 ───────────────────────                          ──────────────────────────────────
 1. GET index.json (+ .sig) → verify signature
 2. GET {GUID}.json → choose newest version whose
    min/max build fits an installed Alibre
 3. Download package → %LOCALAPPDATA%\AlibreAddOnManager\cache\
 4. Verify sha256 against manifest
 5. Validate package (see 3.2)
 6. Check no "Alibre Design.exe" is running
 7. Write plan.json, launch helper ──runas──►     8. Extract to <folder>.staging
                                                  9. Move old folder → <folder>.bak,
                                                     staging → <folder>
                                                 10. Set registry value GUID = folder
                                                     (64-bit registry view)
                                                 11. Write state file; delete .bak
                                                     (or restore .bak on failure)
 12. Tell user: restart Alibre ◄──────────────────
```

### 3.1 Elevation

Only steps 8–11 need administrator rights. The same EXE relaunches itself with
`Verb = "runas"` and `--apply <plan.json>`, rather than marking the whole app
`requireAdministrator`. Networking, parsing and UI then never run elevated, and
the user gets one UAC prompt per batch of changes.

### 3.2 Package validation (step 5)

| Check | Why |
|---|---|
| `.adc` parses; its `Property name="Identifier"` equals the manifest `id` | A registry-name/`.adc` GUID mismatch makes Alibre silently skip the add-on |
| `DLL location` is relative and resolves inside the folder | Prevents pointing Alibre at an arbitrary DLL |
| `.adc` `<DLL type>` matches manifest `dllType` | A mislabelled package is loaded the wrong way by Alibre |
| **Native:** PE machine = `0x8664` (x64), no CLR header, and exports `AddOnLoad` / `AddOnInvoke` / `AddOnUnload` undecorated | Alibre is 64-bit and will not load a 32-bit DLL; missing exports fail silently at load |
| **Managed:** PE has a CLR header, and is either machine `0x8664` or machine `0x14C` with `ILONLY` set and `32BITREQUIRED` clear (AnyCPU) | An AnyCPU assembly reports `0x14C` yet runs 64-bit — a naive x64 check would wrongly reject it; an x86-only assembly must be rejected |
| No zip entry contains `..` or is rooted | Zip-slip protection |
| `folderName` has no path separators and does not collide with a *different* GUID's folder | Prevents one add-on overwriting another |
| Optional: Authenticode signature on the DLL; show the publisher | The DLL runs in-process inside Alibre with the user's full rights |

### 3.3 Trust

A signed catalog (Ed25519/RSA public key embedded in the manager) plus a
per-package SHA-256 means a compromised download host cannot substitute
binaries. Unsigned DLLs are flagged as "unverified publisher" in the UI.

### 3.4 Detecting installed Alibre versions (no COM)

Scan `C:\Program Files\Alibre Design*\Program\Alibre Design.exe` and read each
file's `FileVersionInfo`. This finds every side-by-side install, which is what
`minAlibreBuild` should be checked against. Because the add-on registry key is
un-versioned, an installed add-on is loaded by *all* of them. If any installed
version is below the minimum, warn rather than block — the add-on's own runtime
version gate still has to handle it.

### 3.5 What is "installed"?

The registry key is the source of truth. Enumerate values under
`HKLM\SOFTWARE\Alibre Design Add-Ons` and, for each, **follow the registered
path wherever it points** — `ProgramData`, `Program Files`, a build folder on
another drive. Never infer the folder from the GUID or the add-on name. Find
the `.adc` in that folder whose Identifier equals the value name (a folder may
hold more than one `.adc`), read it, and match the GUID to the catalog.

States (named so they are not confused with the *Managed* DLL flavour in 1.1):

- **Tracked** — installed by this tool; the state file
  `C:\ProgramData\Alibre AddOns\.manager\installed.json` records version, hash,
  folder and the files written.
- **External** — registered by hand, by a vendor installer, or by a developer
  pointing at a build folder. Show it; allow *Disable* (remove the registry
  value only). **Never delete its files** — the folder may be a vendor
  install owned by an MSI or a developer's working tree. Uninstall for these
  means *Disable* plus, where one exists, a pointer to the vendor uninstaller
  (Windows "Installed apps").
- **Broken** — registry points to a missing folder, or no `.adc` there carries
  the matching Identifier. Offer to remove the registry value.

If a catalog add-on is already present as **External** (e.g. the vendor's own
installer put it in `Program Files`), installing from the catalog would create
a second copy under `ProgramData` competing for the same GUID. Detect this and
offer *take over*: Disable the external registration and install the tracked
copy — leaving the old files in place for the user to remove.

*Disable* (remove the registry value, keep the files) is also useful for users
diagnosing a misbehaving add-on. Record the removed value in the state file so
*Enable* can restore the original path exactly.

### 3.6 Updates

Same pipeline. The registry value is unchanged (same folder path), so an update
is just the folder swap. The swap cannot happen while Alibre is running — the
DLL is locked. Because the manager itself runs inside Alibre (§6), the helper
does not refuse: it waits, showing the queued changes, until every Alibre
process has exited, then applies them. Never kill Alibre; ask the user to save
and close it.

### 3.7 After install

Runtime behaviour inside Alibre cannot be verified from outside it. The final
message should say "restart Alibre Design and check the Add-Ons tab", not claim
success.

---

## 4. Code layout (as built)

```
src/AlibreAddOnManager.Shared/   # shared project (.shproj/.projitems) compiled into both, no extra DLL
├─ AdcFile.cs                    # .adc reader, incl. <DLL type>
├─ AddOnRegistry.cs              # HKLM add-ons key, 64-bit view
├─ AddOnScanner.cs               # Installed view: Tracked / External / Disabled / Broken / Self (3.5)
├─ AlibreInstallDetector.cs      # Program Files scan, build-number parsing (3.4)
├─ Json.cs                       # DataContractJsonSerializer helpers
├─ ManagerPaths.cs               # well-known folders, own GUID, id/folder helpers
├─ Packages.cs                   # package validation (3.2), zip-slip-safe extract, sha256
├─ PeInspector.cs                # PE machine, CLR header / CorFlags, native exports
└─ Models/                       # catalog, package, plan, state and settings contracts (one type per file)
src/AlibreAddOnManager/          # the add-on (class library, x64)
├─ AlibreAddOn.cs                # AlibreAddOnAssembly.AlibreAddOn — managed entry points
├─ AddOnManagerAddOn.cs          # IAlibreAddOn: one menu, stateless modal command
├─ CatalogClient.cs              # https / local catalog, download + sha256
├─ HelperLauncher.cs             # write plan, copy helper to %TEMP%, runas
├─ SettingsStore.cs              # %APPDATA%\AlibreAddOnManager\settings.json
├─ Ui/ManagerForm.cs             # Installed | Available tabs, pending-changes queue
└─ AlibreAddOnManager.adc/.ico, addon.json
src/AlibreAddOnManager.Helper/   # elevated EXE (AnyCPU, Prefer32Bit=false)
├─ Program.cs                    # --apply <plan.json>, elevation check, result summary
├─ WaitForAlibreForm.cs          # waits for Alibre to exit when files are locked
└─ Installer.cs                  # staging, swap, registry, state, rollback
```

### 4.1 .NET Framework notes

- **Target v4.8**, matching `Alibre Design.exe.config`
  (`sku=".NETFramework,Version=v4.8"`) — the add-on runs in Alibre's CLR.
- **Registry view.** Always open the 64-bit view explicitly, whatever the
  process bitness — a 32-bit process is silently redirected to `WOW6432Node`,
  where 64-bit Alibre never looks. The helper also sets
  `<Prefer32Bit>false</Prefer32Bit>`:

  ```csharp
  using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
  using (var key = hklm.CreateSubKey(@"SOFTWARE\Alibre Design Add-Ons"))
      key.SetValue(guid, targetDir, RegistryValueKind.String);
  ```

  (Do not use PowerShell `New-Item -Force` for this key — see CLAUDE.md §5.3.)
- **Zip:** reference `System.IO.Compression` and
  `System.IO.Compression.FileSystem`; extract entry by entry and check that each
  full path starts with the staging folder.
- **JSON:** `DataContractJsonSerializer` (in the framework). Not Newtonsoft /
  System.Text.Json: the add-on shares Alibre's process with other add-ons, and
  a private copy of a common JSON library invites assembly-binding conflicts.
- **Settings:** a JSON file, not `Properties.Settings` — in a hosted DLL that
  would write into Alibre's own `user.config`.
- **Running Alibre:** detect with `Process.GetProcessesByName("Alibre Design")`.

---

## 6. The manager is itself an add-on

The manager is a managed Alibre add-on (`<DLL type="Managed">`, identifier
`{925B370F-9858-4416-A7D9-1DD8FAAC16A0}`, folder
`%ProgramData%\Alibre AddOns\AlibreAddOnManager`), reached from the Add-Ons
ribbon as *Add-On Manager → Manage Add-Ons* in every workspace, Home included.

**Managed add-on contract.** Not documented in `Reference/`; taken from the
four managed add-ons on the development machine and the `AlibreAddOn.dll`
interop assembly. Alibre loads the assembly named by `<DLL location>` and calls
static methods on the type **`AlibreAddOnAssembly.AlibreAddOn`**:
`AddOnLoad(IntPtr hwnd, IAutomationHook hook, IntPtr)`, `AddOnInvoke(...)`,
`AddOnUnload(IntPtr, bool, ref bool, int, int)` and
`GetAddOnInterface() → IAlibreAddOn`. The interop assemblies are AMD64-only,
so the add-on is built x64 and references them with `Private=False` (Alibre has
them loaded; never ship copies).

**Consequences of living inside Alibre:**

- *Alibre is always running when the user acts.* Changes that replace or delete
  files of a registered add-on — updates, uninstalls, and any update of the
  manager itself — are marked `requiresAlibreClosed`; the elevated helper waits
  (with Cancel) until Alibre exits, then applies them. New installs into a fresh
  folder and registry-only changes (Disable, Enable, remove broken registration)
  apply immediately. Either way nothing takes effect until Alibre restarts.
- *Self-update* works because the helper is copied to `%TEMP%` before launch and
  never runs from — or locks — the manager's own folder.
- *No sessions needed.* The command is stateless (flavour 1): it opens a modal
  window owned by Alibre's main window and returns no command object.
  `MenuItemState` returns enabled without touching `Sessions`, which rejects the
  empty identifier the Home window passes.
- *Bootstrapping* needs one out-of-band install: `deploy\install.ps1` copies
  the build output and writes the registry value. `deploy\uninstall.ps1` removes
  only the manager; add-ons it installed stay registered and working.

**Trust boundary.** The add-on (unelevated) downloads, hashes and validates the
package, then writes `plan.json` under `%LOCALAPPDATA%`. The helper (elevated)
re-checks the package SHA-256 against the plan and re-runs validation before
extracting, and is the only code that writes `%ProgramData%\Alibre AddOns`,
`HKLM` and `state.json`.

---

## 7. Phasing

1. **Local only** — registry scan, installed list, disable / enable /
   uninstall, and "Install from file" for a `.alibreaddon`. Proves the
   elevation, validation and registry code with no network involved.
2. **Read-only catalog** — fetch, list, install and update from the online
   directory, with hash verification.
3. **Trust** — catalog signing, Authenticode display, and a CI schema validator
   for catalog-repo pull requests.
4. **Extras** — prerequisites (VC++ runtime), standalone-tool packages.
   (Self-update of the manager falls out of §6 and is already supported.)

Phases 1 and 2 are implemented. Phase 3 is not: the catalog signature is not
verified yet — packages are protected only by the SHA-256 in the manifest, so a
compromised catalog host could still substitute both.
