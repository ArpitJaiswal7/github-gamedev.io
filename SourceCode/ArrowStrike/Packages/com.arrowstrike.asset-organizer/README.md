# Asset Organizer Tool (`com.arrowstrike.asset-organizer`)

Editor-only Unity package for pinning Project Window assets into color-coded groups with Project Window icon badges, interactive top-right `C` (Copy Full Path) / `R` (Reveal in Explorer/Finder) tag-buttons, Build Report style asset inspection, drag-and-drop organization, and `Ctrl+Win+P` quick pinning.

## Features
- **Interactive Top-Right Tag-cum-Button (`C` / `R`) in Project Window**:
  - Works collaboratively alongside the **Pinned Asset** circular group badge in both **Tile (Grid) view** and **List view**.
  - Hovering over an asset and entering the **small top-right corner rect** enables an interactive circular tag-button displaying **`C`** (Copy Full Disk Path). Exiting that small top-right rect immediately disables the button (reverting to the passive group color dot if pinned).
  - **Left Click (`Mouse 1`) on `C`**: Copies the complete OS file system path starting from the parent drive (e.g., `D:\UnityProjects\...\Assets\MyAsset.png` on Windows or `/Users/.../Assets/MyAsset.png` on macOS) for that **single hovered asset**, even if multiple assets are currently selected.
  - **Press `R` while hovering the tag-button**: Switches the button letter from **`C`** to **`R`** (Reveal mode). Clicking `Mouse 1` on **`R`** reveals that asset in Windows Explorer or macOS Finder (press `C` or `R` again to switch back to `C`).
  - **`Ctrl + Mouse 1` (`Cmd + Mouse 1` on macOS) on the tag-button**: Reveals **all currently selected Project Window assets together** inside a **single Windows Explorer or macOS Finder window** with all selected items highlighted simultaneously (powered by Win32 `SHOpenFolderAndSelectItems` on Windows and AppleScript `Finder reveal` on macOS).
- **Multi-Select Pin / Unpin (`Ctrl+Win+P` & Right-Click Context Menu)**:
  - Select one or multiple assets in the Project Window and press **`Ctrl+Win+P`** or right-click **`Assets > Asset Organizer`**:
    - **Pin Asset (`Ctrl+Win+P`)**: Pins all selected assets together into `Ungrouped` (or unpins them if already pinned).
    - **Pin and Add to Group...**: Pins all selected assets together into a chosen group (or prompts to create a new group with name and color if none exist yet).
    - **Unpin Asset**: Unpins all selected assets.
    - **Copy Full Disk Path (`C`)** & **Reveal Selected in Explorer or Finder (`Ctrl+Click Tag`)**.
- **Pinned Assets Window** (`Tools > Asset Organizer > Pinned Assets Window` or `Window > Asset Organizer > Pinned Assets`):
  - Browse all pinned assets organized by **Ungrouped** (default yellow tag) and custom groups.
  - **Build Report style asset rows**: displays a `Ping` button, group tag dot, asset icon, folder path + bold filename, asset `Type`, file `Size`, and percentage of total pinned size.
  - **Click any row** (or the bottom selected-asset pill) to immediately ping & highlight the asset in the Project Window.
  - **Search Bar & Type Filter**: search pinned assets by name or path, filter by asset type, and sort columns by Path, Type, or Size.
  - **Full Drag-and-Drop**: drag assets from Project Window into any group, drag pinned assets between groups, and drag group headers to reorder groups.
