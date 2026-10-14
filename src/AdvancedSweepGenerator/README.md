# Advanced Sweep Generator

A managed (C#, .NET Framework 4.8, x64) Alibre Design add-on, ported from the
AlibreScript `Reference/Addon Samples/AdvancedSweepGenerator/AdvancedSweepGenerator.py`.

It sweeps a **circle** or **square** profile — solid or **hollow** — along a
2D or 3D path sketch. The profile plane and sketch are created for you at the
start of the path, normal to it, so only the path has to be drawn.

## Use

Open a part (or sheet-metal part), then **Add-Ons** ribbon →
**Advanced Sweep → Advanced Sweep Generator**.

| Input | Meaning |
|---|---|
| Use 3D path | Sweep along a 3D sketch instead of a 2D sketch |
| 2D / 3D path sketch | The path. Only the list matching *Use 3D path* is enabled |
| Profile type | Circle or Square |
| Size (mm) | Outer diameter (circle) or side length (square) |
| Hollow / Thickness (mm) | Adds an inner profile, making a tube; thickness must be under half the size |

**Create Sweep** adds, per sweep:

- three hidden design points and a design plane `SweepProfilePlane` through them,
- a sketch `SweepProfile` on that plane holding the profile,
- a sweep boss `AdvancedSweep` (entire path, not rigid, no draft).

Repeated runs get `… 2`, `… 3` names. The dialog stays open so several sweeps
can be made in a row. If anything fails before the sweep is created, the
construction geometry is removed again.

The profile is placed at the start of the **first non-reference figure** in the
path sketch (as in the original script). The path does not have to begin
there — a sweep only needs its path to pass through the profile plane. Lines,
circular arcs, circles and splines give an exact start direction; elliptic arcs
use their chord. A 3D path that starts with an exact 180° arc is rejected,
because its direction is ambiguous.

## Differences from the script

- `AddPlane(name, normal, point)` is an AlibreScript helper; AlibreX has no
  point-and-normal plane, so the add-on builds the plane from three hidden
  design points (`IADDesignPlanes.CreateBy3Points`).
- `GetPointAt(0.001)` is replaced by exact tangents from each figure type.
- Inputs are validated (size, thickness < size / 2) before any geometry is made.

## Files

| File | What |
|---|---|
| `AlibreAddOn.cs` | Entry type `AlibreAddOnAssembly.AlibreAddOn` (the name Alibre looks for) |
| `AdvancedSweepAddOn.cs` | `IAlibreAddOn`: menu, menu state, session resolution, stateless command |
| `AlibreVersion.cs` | Runtime version gate (build ≥ 29000) |
| `SweepBuilder.cs`, `SweepRequest.cs`, `Vec3.cs` | Geometry: path start/tangent, plane, profile sketch, sweep |
| `Ui/SweepForm.cs` | The dialog |
| `AdvancedSweepGenerator.adc` | Add-on descriptor, `<DLL type="Managed">`, identifier `{D23F2FF2-09A0-46B9-9D84-532C8561CFD5}` |
| `AdvancedSweepGenerator.ico` | Icon — regenerate with `tools/New-Icon.ps1` |
| `addon.json` | Package metadata for the Add-On Manager / catalog |
| `deploy/install.ps1`, `deploy/uninstall.ps1` | Elevated install / removal (files + registry) |

## Build and install

```powershell
& "$env:LOCALAPPDATA\Programs\Rider\tools\MSBuild\Current\Bin\amd64\MSBuild.exe" src\AdvancedSweepGenerator\AdvancedSweepGenerator.csproj /p:Configuration=Release
powershell -ExecutionPolicy Bypass -File src\AdvancedSweepGenerator\deploy\install.ps1   # UAC prompt; close Alibre first
```

The interop assemblies are referenced from
`C:\Program Files\Alibre Design 29.1.0.29127\Program`; override with
`/p:AlibreProgramDir=...`. Restart Alibre Design after installing — it reads
add-ons only at startup.

To publish through a catalog instead, package the `bin\Release` folder with
`tools\New-AlibreAddonPackage.ps1` (see the repository README).
