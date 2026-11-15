# Changelog

## Unreleased

- Hovering over a portal in the portal list makes its icon on the map pulse, brighter and in front of the other icons, so you can see where you are about to teleport.
- The portal list can be split into sections with the new **Biome** and **Distance** buttons above it. **Biome** gives a section per biome, in the order the game unlocks them. **Distance** gives a section for each set of portals near each other, such as the portals around one island, headed with how far away it is and in which direction, nearest first. With both on, each biome is split by distance, so, say, the Mistlands in the east and in the west are listed apart. Inside each section the order is the same as the plain list: the portal you are at, then favourites, then by name. The home portal stays at the very top, and the search box still filters the list. The buttons are remembered (`PortalList.GroupByBiome` and `GroupByDistance`), and `PortalList.GroupDistance` (default 600 metres) sets how close portals must be to share a section.
- The portal list has a search box: type part of a portal's name or biome to show only the portals that match. Typing in it doesn't trigger the map's keys or the portal keys.
- Portals can be renamed from the portal list with the pencil next to each one, using the same naming box as at the portal, even when the portal is far away. By default anyone who can see a portal can rename it, as anyone with access can rename it while standing at it.
- The builder of a portal, and admins, can lock its name with the padlock next to it in the portal list. A locked name can only be changed by them, from the list or at the portal, and the portal's hover text says it is locked. Click the padlock again to unlock it.
- The portal list is a little wider to fit the new buttons.
- The server and clients must both be updated: the portal list now carries the name lock (protocol version 4).
- Press **B** while looking at a portal, or with the portal map open, to go back to the portal of your last trip, like the portal list's **Go back to** button. At the end of a trip it takes you back where you came from, so you can hop between two portals. The portal's hover text and the portal map's key hints show it when there is somewhere to go back to. The key can be changed with the new `Travel.GoBackKey` setting.
- The ping key now pings the spot you are looking at, or where you stand if you look at the sky, so you can show other players a place without opening the map.
- The mod's keys now work while you hold other keys, such as moving or running. Before, every other key had to be let go first, so pinging while running didn't work.
- The default key for setting the home portal is now **Alt + H** (it was **Shift + H**). Now that the keys work while running, admins holding Shift to run back to a portal would set or remove the home portal when pressing **H** to travel home. Config files that already exist keep their key; change `Home.SetHomeKey` to switch.
- When one of the portal keys (**B**, **H**, **Alt + H**) does something at the portal you are looking at, or in the portal map, it no longer also does what other mods bound to the same key, such as BetterHammer's build menu on **B**. This works for keys the game and Jotunn mods read through the game's own controls; mods that read the keyboard directly can't be stopped.
- Typing in chat or the console while the portal map is open no longer triggers the portal keys, so typing a **B** or an **H** doesn't teleport you.

## 0.6.0

- When another player teleports, their map icon now takes the same time to reach their destination however far they went. Before, it moved at a fixed speed, so it could take a long time to cross the map after a long trip. The time can be changed with the new `PlayerIconTeleportSeconds` setting (default 2 seconds, 0 = instant).
- Press **P** while walking around, with no map or menu open, to ping your location on everyone's map. Other players can find you without you opening the map. The key can be changed with the new `PingLocation` setting.
- **Home portal.** Admins, and the host of a self-hosted world, can press **Shift + H** while looking at a portal, or with the portal map open, to make it the world's home portal, shared by everyone. Pressing it again at the home portal stops it being home. Press **H** while looking at any other portal, or with the portal map open, to travel straight there. The portal map's key hints show both keys whenever they would do something. The home portal is always public, green on the map and first in the portal list, where it has a house instead of the favourite star. The keys can be changed with the new `TravelHomeKey` and `SetHomeKey` settings.
- Fixed portal names, public/private settings and the home portal sometimes being lost when the world was reloaded. The game only saved changes to portals in a session where a portal was also built, destroyed or linked, which rarely happens with this mod. Portals are now saved every time the world is.
- Fixed admins on a crossplay dedicated server not seeing the admin-only key hints on portals, such as toggling another player's portal's visibility. The keys themselves already worked.
- Favourite portals have a gold ring around their icon on the map and minimap, so they stand out from the other portals.
- The portal icon colours and the favourite ring can be changed in the new `Icons` config section: `PortalIconColour`, `HomePortalColour`, `FavouriteColour`, `FavouriteRingSize` and `FavouriteRingThickness`.
- Moved the source files into folders (`Core`, `Network`, `Portals`, `Home`, `Map`, `UI`, `Patches`), leaving only `Plugin.cs` at the root. No behaviour change.

## 0.5.0

- With a gamepad, move the map so a portal icon is under the crosshair and press the confirm button to travel there. Previously the portal map only responded to the mouse. While the portal map is open, the confirm button no longer opens the box for naming a new pin.
- The mouse wheel now scrolls the portal list as fast as the game's own lists, such as the crafting list. It was much slower before.
- The README now explains that a private portal is only hidden from the map: anyone who walks into it can still use it.
- Added a CI check that the version matches in every file.
- Moved `package.sh` into `.github/scripts` and made it the same for every mod: the name, version and DLL now come from `manifest.json` and the `.csproj`.

## 0.4.0

- **Portal list.** The portal map now has a list of every portal you can use on its left-hand side, with each portal's biome (e.g. "Home (Plains)") and its distance. Click a name to teleport there. The portal you are standing at is always listed first, marked "You are here".
- **Favourites.** Click the star next to a portal to make it a favourite. Favourites move to the top of the list straight away. Favourites are saved with your character, separately for each world.
- **Go back.** A button at the top of the list takes you to your last destination. If you are standing at it, it takes you back to where that trip started, so you can go back and forth between two portals with one click.
- The mouse wheel scrolls the list instead of zooming the map while the pointer is over it.

## 0.3.2

- Portal icons are now always drawn in front of other map icons, on both the minimap and the portal map.
- While choosing a destination portal, other map icons are faded so the portal icons stand out on crowded maps. The opacity can be changed with the new `PickerOtherIconOpacity` setting (default 0.4).

## 0.3.1

- Updated the BepInExPack_Valheim dependency to 5.4.2360.

## 0.3.0

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
