# GTA V Offline Mod Launcher

WPF desktop app for installing GTA V Offline / Story Mode add-on vehicle mods into the `mods` folder only.

## Tech Stack

- C#
- .NET 8
- WPF
- JSON storage
- No database

## What the MVP does

- Browse and validate GTA V folder by checking `GTA5.exe`
- Select only a file named `dlc.rpf`
- Validate mod folder name
- Copy the add-on vehicle mod to:
  - `{GtaPath}\mods\update\x64\dlcpacks\{ModName}\dlc.rpf`
- Prepare `mods\update\update.rpf`
  - If missing, copy from `{GtaPath}\update\update.rpf`
- Always back up before future archive edits to:
  - `{GtaPath}\mods_launcher_backups`
- Generate the `dlclist.xml` line:
  - `<Item>dlcpacks:/{ModName}/</Item>`
- Save installed mod records to:
  - `%AppData%\GTAVOfflineModLauncher\installed_mods.json`
- Launch `GTA5.exe`
- Uninstall selected mod from the `mods` folder

## RPF editing support

`IRpfService` is now backed by `CodeWalkerRpfService`, which uses `CodeWalker.Core` to:

- extract `common/data/dlclist.xml` from `mods\update\update.rpf`
- add or remove `<Item>dlcpacks:/{ModName}/</Item>`
- write the updated XML back into the same `mods` archive

Safety rules still apply:

- the launcher never edits `{GtaPath}\update\update.rpf` directly
- if `mods\update\update.rpf` is missing, it is copied from the original first
- `mods\update\update.rpf` is backed up before edit operations

`StubRpfService` is kept in the project as a fallback/reference implementation.

## Project structure

- `Models/`
- `Services/`
- `ViewModels/`
- `Views/`
- `Helpers/`

## Build

### Option 1: Visual Studio

1. Open the project folder in Visual Studio 2022.
2. Make sure the **.NET 8 Desktop Runtime / SDK** is installed.
3. Build the project.
4. Run the WPF app.

### Option 2: CLI

```powershell
cd GtavOfflineModLauncher
dotnet build
```

Run:

```powershell
cd GtavOfflineModLauncher
dotnet run
```

## Publish a self-contained Windows build

The project includes a publish script that creates a timestamped self-contained `win-x64` release folder and zip package.

```powershell
cd GtavOfflineModLauncher
.\scripts\Publish-WinX64.ps1
```

Example output:

- `release\win-x64\20260615_120000\publish\GTAVOfflineModLauncher.exe`
- `release\win-x64\20260615_120000\GTAVOfflineModLauncher-win-x64.zip`

This publish mode uses:

- self-contained deployment
- single-file executable
- native library self-extract

## Config files

### App settings

Saved beside the application as:

- `appsettings.json`

Current fields:

```json
{
  "gtaPath": "D:\\GTA_OFFLINE",
  "autoEditDlclist": false
}
```

### Installed mods registry

Saved to:

- `%AppData%\GTAVOfflineModLauncher\installed_mods.json`

Example:

```json
{
  "mods": [
    {
      "name": "murcevo",
      "spawnName": "murcevo",
      "installedAt": "2026-06-15T09:00:00",
      "dlcRpfPath": "D:\\GTA_OFFLINE\\mods\\update\\x64\\dlcpacks\\murcevo\\dlc.rpf",
      "dlclistEntry": "<Item>dlcpacks:/murcevo/</Item>",
      "enabled": true
    }
  ]
}
```

## Manual test with `murcevo`

### Test setup

Example GTA folder:

- `D:\GTA_OFFLINE`

Example source mod file:

- any folder that contains `dlc.rpf`

### Test steps

1. Launch the app.
2. Click **Browse GTA Folder**.
3. Select `D:\GTA_OFFLINE`.
4. Confirm the folder contains `GTA5.exe`.
5. Click **Select dlc.rpf**.
6. Pick the vehicle mod file named `dlc.rpf`.
7. Enter:
   - `Mod folder name`: `murcevo`
   - `Spawn name`: `murcevo`
8. Enable **Auto edit dlclist.xml inside update.rpf** if you want the launcher to update `dlclist.xml` automatically.
9. Click **Install Mod**.

### Expected result

The app should create:

- `D:\GTA_OFFLINE\mods\update\x64\dlcpacks\murcevo\dlc.rpf`

If missing, it should also create:

- `D:\GTA_OFFLINE\mods\update\update.rpf`

Backups should appear in:

- `D:\GTA_OFFLINE\mods_launcher_backups`

If auto edit is enabled, the launcher should also update:

- archive: `D:\GTA_OFFLINE\mods\update\update.rpf`
- internal path: `common/data/dlclist.xml`

The inserted line is:

```xml
<Item>dlcpacks:/murcevo/</Item>
```

## Notes

- This launcher is for **GTA V Offline / Story Mode only**.
- It should never edit:
  - `{GtaPath}\update\update.rpf`
- All mod work stays inside:
  - `{GtaPath}\mods`
- The release build is branded with custom launcher artwork in:
  - `Assets\GtavLauncher.png`
  - `Assets\GtavLauncher.ico`
