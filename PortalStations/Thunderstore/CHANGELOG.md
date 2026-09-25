### 1.4.6
- deep north update
- updated server sync
- updated piece manager
- updated UI position
- updated item manager

### 1.4.5
- fixed teleport cost only being 1
- added code to make sure panel is always within screen bounds on first game load

### 1.4.4
- added config to disable station tab
- changed player list to use ZNet PlayerInfo instead of Peer

### 1.4.3
- Added teleport keys config for each station, to customize individual portals
- If list is empty, will use default teleport keys defined in config category `Teleport Keys`
- optimized how station check keys

### 1.4.2
- Added config to toggle only owner can edit
- Settings tab disabled if only owner can edit and user is not owner
- Settings tab disabled if using portable portal

### 1.4.1
- Added config to toggle only owner can destroy

### 1.4.0
- Force sending ZDO to fix portals not showing up when server is not creating portal station

### 1.3.24
- Fixed position slipping off screen on logout/login by changing Transform to RectTransform on initializing
- Fixed portal platform emission

### 1.3.23
- Added search field
- Ordering portals by name alphabetically

### 1.3.22
- Forgot what I changed

### 1.3.21
- Added missing localized text
- Fixed registering cloned portals twice

### 1.3.2
- UI overhaul
- Fixed platform portal emission

### 1.3.1
- Removed option to make portals free (admin-only option)

### 1.3.0
- Fixed destinations not appearing for personal teleporters
- Fixed draggable UI elements
- Tweaked portal privacy conditionals

### 1.2.9
- Overhauled UI
- Updated Server Sync to version 1.19

### 1.2.8
- Ensured "Stranger" does not appear as an option

### 1.2.7
- Fixed admin-only build toggle
- Saved guild name to portal for guild check function

### 1.2.6
- Added toggles for groups and guilds
- Saved creator name to portal for guild membership checks

### 1.2.5
- Fixed duplicate portals in build menu
- Added fountain portal

### 1.2.4
- Updated piece manager

### 1.2.3
- Bog Witch update

### 1.2.2
- Fixed guilds bug where creator info wasn't available if not online

### 1.2.1
- Made all vanilla portals also portal stations (clones)
- Improved effects on original portals

### 1.2.0
- Ensured "Stranger" doesn't appear as a teleport option

### 1.1.9
- Fixed conditional logic for guild membership (used "and" instead of "or")

### 1.1.8
- Added Guilds API
- Added sorting for portal destinations
- Ensured player destinations are valid

### 1.1.7
- Improved teleport-to-player functionality
- Cloned stone portal

### 1.1.6
- Added dynamic config creation for non-teleportable items

### 1.1.5
- Updated piece manager

### 1.1.4
- Re-added ability to teleport to players

### 1.1.3
- Fixed shader issues
- Added cost to use portals (off by default)

### 1.1.2
- Material and shader fixes

### 1.1.1
- Added configurable toggle to only allow admins to see portal pieces

### 1.1.0
- Fixed saving favorite portal on logout patch

### 1.0.9
- Improved effect fade
- Added volume control
- Enhanced favorites system

### 1.0.8
- Added config to control durability drain of portable portal item

### 1.0.7
- Added favorite functionality

### 1.0.6
- Added private/public functionality

### 1.0.5
- Added localization for "not enough fuel" text
- Portable portal can use keys for ore teleportation
- Added admin-only rename setting

### 1.0.4
- Added localization configs
- Tweaked hover behavior
- Added config to use private keys

### 1.0.3
- Removed condition to rename
- Fixed portal placement conditions

### 1.0.2
- Added Gate Portal
- Added toggle to allow anyone to rename

### 1.0.1
- Added teleport keys
- Added new portal platform

### 1.0.0
- Initial Release