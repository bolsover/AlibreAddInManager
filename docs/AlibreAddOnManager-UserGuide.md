# Alibre Add-On Manager — User Guide

*Version 1.1 · David Bolsover · <https://github.com/bolsover/AlibreAddOnManager>*

The **Alibre Add-On Manager** is an add-on for Alibre Design that installs, updates,
disables and removes other Alibre Design add-ons. It can install add-ons from an
**online catalog** or from a **`.alibreaddon` package file** you have been sent.

> **Online catalog:** a public catalog of add-ons is available at
> **<http://www.bolsover.com/catalog/index.json>**. Enter this address on the
> **Available** tab to browse and install the add-ons it lists.

---

## Contents

1. [Requirements](#1-requirements)
2. [Installing the manager](#2-installing-the-manager)
3. [Opening the manager](#3-opening-the-manager)
4. [The manager window](#4-the-manager-window)
5. [Installed tab](#5-installed-tab)
6. [Available tab — installing from a catalog](#6-available-tab--installing-from-a-catalog)
7. [Installing from a package file](#7-installing-from-a-package-file)
8. [Pending changes and Apply](#8-pending-changes-and-apply)
9. [Updating the manager itself](#9-updating-the-manager-itself)
10. [Uninstalling the manager](#10-uninstalling-the-manager)
11. [Multiple Alibre versions (side-by-side installs)](#11-multiple-alibre-versions-side-by-side-installs)
12. [Security notes](#12-security-notes)
13. [Troubleshooting](#13-troubleshooting)
14. [Where things are stored](#14-where-things-are-stored)

---

## 1. Requirements

- 64-bit Windows.
- **Alibre Design V29 or later** (build 29000 or higher).
- Administrator permission when installing, updating or removing add-ons. Windows
  shows a UAC prompt each time you apply a batch of changes.
- Internet access only if you install from an online catalog.

## 2. Installing the manager

The manager is installed once with its setup program,
**`AlibreAddOnManager_1.1.exe`**. After that it can update itself (see
[§9](#9-updating-the-manager-itself)).

1. **Save your work and close Alibre Design.** Setup cannot replace the files of a
   running add-on. If Alibre Design is open, Setup asks you to close it and click
   **Retry**.
2. Run `AlibreAddOnManager_1.1.exe` and accept the UAC prompt.
3. Follow the wizard. The install folder is fixed:
   `C:\ProgramData\Alibre AddOns\AlibreAddOnManager`.
4. Start Alibre Design.

Setup copies the manager's files and registers it with Alibre Design under the
registry key `HKLM\SOFTWARE\Alibre Design Add-Ons`.

## 3. Opening the manager

In Alibre Design, open the **Add-Ons** tab of the ribbon and choose
**Add-On Manager → Manage Add-Ons**.

The manager window opens over Alibre Design. Close it to return to your work.

## 4. The manager window

The window has three areas:

| Area | Purpose |
|---|---|
| **Installed** tab | Every add-on registered with Alibre Design on this computer, and actions on them. |
| **Available** tab | Add-ons listed in a catalog, ready to install or update. |
| **Pending changes** | The changes you have queued. Nothing is changed until you click **Apply changes…**. |

The status bar at the bottom shows which Alibre Design build is running and, if
there is more than one, which other Alibre versions are installed.

## 5. Installed tab

The list shows every add-on registered with Alibre Design, wherever its files are.

| Column | Meaning |
|---|---|
| **Name** | The add-on's friendly name. |
| **Version** | The installed version, if known. |
| **Status** | See the table below. |
| **DLL** | Managed (.NET) or native (C++). |
| **Folder** | The folder Alibre Design loads the add-on from. |

Select an add-on to see its details (author, DLL, workspace, description and any
problem) below the list.

### Status values

| Status | Meaning | Colour |
|---|---|---|
| **Tracked** | Installed by the manager. The manager can update, disable or uninstall it. | Normal |
| **External** | Installed by something else — a vendor's own installer, by hand, or a developer's build. The manager can disable it but **never deletes its files**. Use its own uninstaller (Windows *Settings → Apps → Installed apps*) to remove it. | Normal |
| **Disabled** / **Disabled (tracked)** | Turned off by the manager. Its files are still there; **Enable** turns it back on exactly as it was. | Grey |
| **Broken** | The registration points to a folder that no longer exists, or the folder has no matching `.adc` file. | Red |
| **Warning** | Loads, but its `.adc` file's identifier does not match its registration. | Orange |
| **This add-on** | The Add-On Manager itself. | Normal |

### Buttons

| Button | What it does |
|---|---|
| **Refresh** | Re-reads the registered add-ons. |
| **Install from file…** | Installs or updates from a `.alibreaddon` package — see [§7](#7-installing-from-a-package-file). |
| **Disable** | Removes the add-on's registration so Alibre Design no longer loads it. The files are kept. Available for Tracked, External and Warning add-ons. |
| **Enable** | Restores the registration of a disabled add-on. |
| **Uninstall** | Deletes a **Tracked** add-on's folder and registration. Not available for External add-ons or for the manager itself. You are asked to confirm. |
| **Remove broken registration** | Removes the registration of a **Broken** add-on. |
| **Open folder** | Opens the add-on's folder in File Explorer. |

All of these except **Refresh** and **Open folder** add an entry to
**Pending changes**; nothing happens until you apply them.

> **Tip:** *Disable* is a safe way to find out whether an add-on is causing a
> problem. Disable it, restart Alibre Design, and see if the problem goes away.
> Enable it again afterwards.

## 6. Available tab — installing from a catalog

A **catalog** is a list of add-ons published on a web server or in a folder.

### Loading a catalog

1. Open the **Available** tab.
2. In **Catalog**, enter the catalog's address. To use the public catalog, enter:

   ```
   http://www.bolsover.com/catalog/index.json
   ```

   You can also enter:
   - any other `https://…/index.json` or `http://…/index.json` address;
   - a local or network folder that contains a catalog, such as `D:\catalog` or
     `\\server\share\catalog` (the manager reads `index.json` in that folder).
3. Click **Load**.

The manager remembers the last catalog you loaded and loads it automatically the
next time you open the window.

### The list

| Column | Meaning |
|---|---|
| **Name** | The add-on's name. |
| **Publisher** | Who publishes it. |
| **Latest** | The newest version in the catalog. |
| **Installed** | The version installed on this computer (or its status), blank if not installed. |
| **Summary** | A one-line description. |

Select an add-on to see its full description, publisher, licence, homepage and
every version in the catalog. For each version you see its release date, DLL
type, the Alibre builds it supports, and its release notes. The arrow (**→**)
marks the version that will be installed. Versions that do not support the
running Alibre build are marked **NOT compatible with the running build**.

### Installing or updating

1. Select the add-on.
2. Click **Install / Update**.

The manager picks the **newest version compatible with the Alibre build you are
running**. If no version is compatible, it picks the newest version and warns
you. It then downloads the package, checks its size and SHA-256 checksum against
the catalog, and validates its contents. A confirmation shows:

- the version and where it will be installed;
- whether it replaces an installed version, re-enables a disabled add-on, or takes
  over an External installation (see below);
- any warnings — for example an incompatible Alibre build, or prerequisites that
  are not installed automatically.

Click **OK** to add it to **Pending changes**, then **Apply changes…**.

#### Taking over an External add-on

If the add-on is already installed by its vendor's installer (status
**External**), installing it from the catalog *takes it over*: Alibre Design is
pointed at the new copy under `C:\ProgramData\Alibre AddOns`. The old files are
left where they are. Note that the vendor's uninstaller may later remove the
registration; if that happens, reinstall from the catalog.

## 7. Installing from a package file

An add-on publisher may send you a package file ending in **`.alibreaddon`**.

1. On the **Installed** tab, click **Install from file…**.
2. Choose the `.alibreaddon` file.
3. Review the confirmation and click **OK**.
4. Click **Apply changes…**.

The package is checked before anything is installed. The manager refuses a
package that, for example:

- is not a valid package, or has no `addon.json` manifest;
- has no `.adc` file matching its identifier, or is missing the DLL that the `.adc`
  names;
- contains a 32-bit DLL (Alibre Design is 64-bit), or a C++ DLL without the
  required entry points;
- would install into a folder that belongs to another add-on, or contains unsafe
  file paths.

## 8. Pending changes and Apply

Actions are **queued** so you can make several changes with one administrator
prompt.

- Each add-on has at most one pending change. Choosing a new action for the same
  add-on replaces the earlier one.
- **Remove selected** removes one pending change; **Clear all** removes them all.
- **Apply changes…** shows a summary, then asks Windows for administrator
  permission and applies the batch.

### Changes that wait for Alibre Design to close

Alibre Design locks the files of every loaded add-on. Changes that replace or
delete those files — updates, uninstalls and updates of the manager itself — are
marked **(applied after Alibre Design closes)**.

When the batch contains such a change:

1. Click **Apply changes…** and accept the UAC prompt.
2. A small window, **Waiting for Alibre Design to close**, lists the queued
   changes.
3. Save your work and close Alibre Design. The changes are then applied, and a
   summary is shown.

Click **Cancel** in the waiting window to abandon the batch before anything is
changed. The manager never closes Alibre Design for you.

### Restart Alibre Design

**Alibre Design reads its add-ons only when it starts.** After any change —
install, update, enable, disable or uninstall — restart Alibre Design, then check
the **Add-Ons** tab to confirm the add-on behaves as expected.

## 9. Updating the manager itself

The Add-On Manager is listed in the public catalog. To update it:

1. Load the catalog on the **Available** tab.
2. Select **Alibre Add-On Manager** and click **Install / Update**.
3. Click **Apply changes…**, then close Alibre Design when asked.

You can also update it with **Install from file…** using a newer
`AlibreAddOnManager-<version>.alibreaddon` package, or by running a newer setup
program with Alibre Design closed.

## 10. Uninstalling the manager

The manager cannot uninstall itself from its own window. Close Alibre Design and
then either:

- open Windows **Settings → Apps → Installed apps**, find **AlibreAddOnManager**
  and choose **Uninstall**; or
- run `C:\ProgramData\Alibre AddOns\AlibreAddOnManager\unins000.exe`.

Uninstalling the manager **does not remove the add-ons it installed**. They stay
installed and keep working. Its record of those add-ons in
`C:\ProgramData\Alibre AddOns\.manager` is kept so that, if you reinstall the
manager, it recognises them again.

## 11. Multiple Alibre versions (side-by-side installs)

Alibre Design versions can be installed side by side. The add-on registration is
**shared by all of them**: every installed Alibre version loads every registered
add-on.

The manager therefore checks each package against the Alibre build you are
running *and* every other installed version, and warns when one of them is
outside the add-on's supported range. The status bar lists the installed
versions.

## 12. Security notes

Add-ons run inside Alibre Design with your full user rights. Install add-ons only
from publishers and catalogs you trust.

- Every downloaded package is checked against the **SHA-256 checksum** in the
  catalog, and refused if it does not match.
- Over a plain `http://` address, that checksum protects against corrupted
  downloads but not against deliberate tampering, because the catalog itself is
  not encrypted. Use an `https://` address or a local folder where one is
  available.
- Catalog signatures and publisher (Authenticode) signatures are not yet verified
  in this version. The status bar says so when a catalog is loaded.

## 13. Troubleshooting

| Problem | What to do |
|---|---|
| The change made no difference. | Restart Alibre Design. It reads add-ons only at startup. |
| *Could not load the catalog…* | Check the address and your internet connection. Open the address in a web browser — it should show JSON text. For a folder, check that it contains `index.json`. |
| *Downloaded package does not match the catalog sha256.* | The package on the server differs from the catalog entry. Try again later and tell the catalog's publisher. |
| *This package cannot be installed* | The package failed validation; the message lists the reasons. Contact the add-on's publisher. |
| *The folder … already exists and was not created by the add-on manager.* | Another program put files there. Remove or rename that folder (or uninstall the program that created it), then try again. |
| *Administrator permission was declined; nothing was changed.* | Click **Apply changes…** again and accept the UAC prompt. |
| An add-on shows **Broken**. | Its folder was deleted or moved. Use **Remove broken registration**, then reinstall it if you still need it. |
| Alibre Design misbehaves after installing an add-on. | **Disable** the add-on, restart Alibre Design, and contact its publisher. |
| The **Uninstall** button is greyed out. | The add-on is External (use its own uninstaller or **Disable**), or it is the manager itself (see [§10](#10-uninstalling-the-manager)). |

## 14. Where things are stored

| What | Location |
|---|---|
| The manager | `C:\ProgramData\Alibre AddOns\AlibreAddOnManager\` |
| Add-ons installed by the manager | `C:\ProgramData\Alibre AddOns\<FolderName>\` |
| Add-on registrations | Registry: `HKEY_LOCAL_MACHINE\SOFTWARE\Alibre Design Add-Ons` |
| The manager's record of installed and disabled add-ons | `C:\ProgramData\Alibre AddOns\.manager\state.json` |
| Your settings (last catalog address) | `%APPDATA%\AlibreAddOnManager\settings.json` |
| Downloaded packages | `%LOCALAPPDATA%\AlibreAddOnManager\cache\` |
