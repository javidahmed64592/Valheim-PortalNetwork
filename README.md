[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-portalnetwork/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-portalnetwork/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-portalnetwork/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-portalnetwork/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# PortalNetwork

Teleport to any portal through a map interface without tag linking.
To open the portal interface, walk into any portal.
To show/hide the portal icons on the map and minimap, open the map as normal and press `P` (configurable).

![PortalNetwork Example](https://github.com/javidahmed64592/Valheim-PortalNetwork/raw/main/images/game_screenshot_1.png)

## Home Portal

Each world can have one **home portal**, shared by everyone.

- Admins, and the host of a self-hosted world, press `Shift + H` while looking at a portal, or with the portal map open, to make that portal the home portal. It becomes public, and can't be made private while it is home. Press `Shift + H` at the home portal to stop it being home.
- Press `H` while looking at any other portal, or with the portal map open, to travel straight home. Nothing happens if you're already at the home portal.
- The portal map's key hints show these keys whenever pressing them would do something.
- The home portal is green on the map and comes first in the portal list, with a house instead of the favourite star.
- If the home portal is destroyed, the previous home portal becomes home again, unless it was removed as home or made private.

## Pinging Your Location

Press `P` (configurable) while walking around, with no map or menu open, to ping your location on everyone's map. It's the same ping as clicking on the map, so other players can find you without you opening the map.

## Public and Private Portals

Every portal is **public** by default, so everyone can see and use it.

- `Shift + Use` on a portal you built toggles it between **public** and **private**.
- A private portal only appears on its builder's map. Anyone who walks into it can still use it to travel to the portals they can see.
- Hover over a portal to see its current visibility.

Admins, and the host of a self-hosted world, can see every portal and change any portal's visibility.

## Configuration

Settings are in the BepInEx config file `javidahmed64592.portalnetwork.cfg`.

## Compatibility

- Other mods that change how portals link or teleport (such as TargetPortal) replace the same behaviour, so only use one of them.
- This mod has not been tested with a gamepad, only with keyboard and mouse. With a gamepad, move the map so a portal is under the crosshair and press the confirm button to travel there; the portal list needs a mouse.

## Uninstalling

Portals go back to vanilla linking, where two portals with the same name connect, so rename them in pairs to keep using them.
Whether a portal is public or private, and your favourites, stay saved but do nothing without the mod.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-PortalNetwork/blob/main/LICENSE) file for details.
