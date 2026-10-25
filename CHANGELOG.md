# Changelog

## Unreleased

- Added an `.editorconfig`, `Directory.Build.props` and a CI workflow that runs `dotnet format`, a Release build and a NuGet vulnerability check.
- Reformatted the code to match the new code style rules and fixed the analyzer warnings (no behaviour change).
- Tidied up packaging scripts and workflow files.
- Added a `.gitattributes` to keep line endings consistent.
- Moved the local Valheim install paths into `Directory.Build.props`; set `VALHEIM_DIR` and `BEPINEX_PROFILE_DIR` once to build every mod without editing the `.csproj`.
- Updated README and icon.

## 0.2.1

- The portal icons now stay on the minimap and remain red after closing the map interface and walking.

## 0.2.0

- The large map's key hints now include a "Toggle portals" entry for the portal icon key, drawn like the vanilla hints and updated if you rebind the key. It is hidden while the portal picker is open.

## 0.1.2

- Press **P** while the map is open to show or hide portal icons on it, so you can see where portals are relative to other landmarks.
- The key is configurable (`Map` > `TogglePortalIcons` in the config file).

## 0.1.1

- The map UI now only closes when moving more than 2 metres from the portal.
- The portal icons are now red and bigger on the map, making them easier to see.

## 0.1.0

- **Portal map.** Walking into any portal opens the large map with a pin on every portal you can use. Click a pin to teleport there. No tag matching or pairing needed.
- **Portal names on the map.** Pins use the name set on the portal (press Use on a portal to name it). Unnamed portals show as "Portal".
- **Public and private portals.** Portals are public by default. The builder can press Shift + Use to switch a portal between public and private, and hovering over a portal shows its current visibility.
- **Admins and hosts.** Admins, and the host of a self-hosted world, can see every portal and change any portal's visibility.
- **Walk away to close.** The map closes on its own when you start walking away, move too far from the portal, or die. M and Esc still close it too.
- **Multiplayer with server-side checks.** The server works out who each player is from their connection, confirms every teleport against the portal's current state, and validates every visibility change. A portal that is removed or made private while your map is open gives a message instead of teleporting.
- **Name filtering.** Portal names go through the game's platform text filter and have rich text stripped before they are shown.
- Vanilla teleport restrictions still apply: items that can't be teleported, boss events and world modifiers that block portals.
