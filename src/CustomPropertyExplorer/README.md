# Custom Property Explorer

A managed (C#, .NET Framework 4.8, x64) Alibre Design add-on, ported from the
three AlibreScript files in `Reference/Addon Samples/CustomPropertyExplorer/`.

It adds two commands under **Add-Ons → Property Explorer**, enabled in Part,
Sheet Metal and Assembly workspaces:

| Command | Ported from | Window |
|---|---|---|
| **Property Explorer** | `PropertyExplorerCustomProperties.py` — a superset of `PropertyExplorer.py`, which adds the custom-property features, so both scripts are covered | Modeless; one per workspace, closes with its workspace |
| **Custom Property Explorer** | `CustomPropertyExplorer.py` | Modal |

## Property Explorer

The left pane is the model tree:

- Configurations, Parameters, Custom Properties
- Part: Faces, Edges, Vertices (all bodies). Assembly: Parts and SubAssemblies,
  nested, each part with its own faces, edges and vertices
- Features, Sketches and 3D Sketches (each with its Figures), Planes, Axes, Points

Collections load the first time they are expanded, so big parts open quickly.
The right pane shows the selected item's properties: design and extended
properties, physical properties, geometry, sketch-figure data, parameter values
and so on. Lengths and angles use the workspace's display units.

**Real-time Sync** (on by default) shows the first item of the Alibre selection
as it changes. Sync pauses for a second after you click in the tree so the
click isn't overwritten. The toolbar also has Refresh, Expand All, Collapse All,
**Custom Props** (look up by name) and **Set Custom Prop**.

## Custom properties

Both windows list the custom properties **A–G**, which were the scripts'
defaults, plus every name you look up or set. Values are editable in the grid.
Alibre only accepts a value for a property that is already defined on the
design or on this PC; otherwise the grid reports the error and keeps the old
value. AlibreX can't enumerate custom properties, which is why the list is
built from names.

## Differences from the scripts

| Script | Add-on |
|---|---|
| Found features, sketches, planes, axes and points by trying names such as `Sketch<1>` … `Sketch<50>` | Lists AlibreX's real collections, so every item appears whatever its name or language |
| `Units.Current = Inches` | Uses the workspace's display units (API values are cm / rad, converted only for display) |
| Face `Area`, `IsRectangle` | Not in AlibreX. Surface type, normal / radius, counts and extents are shown instead |
| Edge `Length` | Exact for straight edges and full circles; other edges show N/A |
| Parameter `ExcelWorkbook/Sheet/Cell` | Not in AlibreX; omitted |
| Python `str()` of AlibreScript objects | Formatted values (points, vectors, enum names, colours as R, G, B) |
| Emoji node prefixes | Plain text, because WinForms renders emoji unreliably (the script had already swapped one emoji for `[S]`) |
| Fonts multiplied by the DPI factor (double-scaled on high-DPI screens) | Fonts in points; only pixel sizes are scaled |
| Editing a custom property rebuilt and collapsed the whole tree | Only the Custom Properties branch is refreshed |
| Lookup didn't add the name to the list (only `CustomPropertyExplorer.py` did) | Both windows add looked-up and set names |

## Files

| File | What |
|---|---|
| `AlibreAddOn.cs` | Entry type `AlibreAddOnAssembly.AlibreAddOn` (the name Alibre looks for) |
| `CustomPropertyExplorerAddOn.cs` | `IAlibreAddOn`: menus, menu state, version gate, stateless commands, one explorer per session |
| `AlibreVersion.cs` | Runtime version gate (build ≥ 29000) |
| `Model/DesignContext.cs` | Session resolution (Part / Sheet Metal / Assembly) and unit formatting |
| `Model/CustomProperties.cs` | Read / write / track custom properties |
| `Model/PropertyBag.cs` | Dynamic PropertyGrid object (the scripts' `PropertyDisplayObject`) |
| `Model/PropertyReader.cs` | Per-type property sets (the scripts' `PropertyWrapper`) |
| `Ui/PropertyExplorerForm.cs`, `Ui/ModelTreeBuilder.cs` | Property Explorer window and its tree |
| `Ui/CustomPropertyExplorerForm.cs` | Custom Property Explorer window |
| `Ui/CustomPropertyDialogs.cs` | Lookup / Set dialogs |
| `Ui/Theme.cs`, `Ui/WindowWrapper.cs` | Dark theme, DPI scaling; Alibre window as dialog owner |
| `CustomPropertyExplorer.adc` | Add-on descriptor, `<DLL type="Managed">`, identifier `{8D6B7599-BBA9-4DC4-9A36-97D568C0A90E}` |
| `CustomPropertyExplorer.ico` | Icon — regenerate with `tools/New-Icon.ps1` |
| `addon.json` | Package metadata for the Add-On Manager / catalog |
| `deploy/install.ps1`, `deploy/uninstall.ps1` | Elevated install / removal (files + registry) |

## Build and install

```powershell
& "$env:LOCALAPPDATA\Programs\Rider\tools\MSBuild\Current\Bin\amd64\MSBuild.exe" src\CustomPropertyExplorer\CustomPropertyExplorer.csproj /p:Configuration=Release
powershell -ExecutionPolicy Bypass -File src\CustomPropertyExplorer\deploy\install.ps1   # UAC prompt; close Alibre first
```

The interop assemblies are referenced from
`C:\Program Files\Alibre Design 29.1.0.29127\Program`; override with
`/p:AlibreProgramDir=...`. Restart Alibre Design after installing, because it
reads add-ons only at startup.

To publish through a catalog instead, package the `bin\Release` folder with
`tools\New-AlibreAddonPackage.ps1` (see the repository README).
