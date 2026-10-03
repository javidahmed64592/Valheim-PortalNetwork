# PortalNetwork

Teleport to any portal through a map interface without tag linking.

Builders choose whether each portal is visible to everyone or only to them. Everything is checked by the server, so it works on dedicated servers and friend-hosted worlds.

![PortalNetwork Example](https://github.com/javidahmed64592/Valheim-PortalNetwork/raw/main/game_screenshot.png)

## How to use

1. **Build portals** as normal. Press **Use** on a portal to give it a name. Names are what you see on the map.
2. **Walk into a portal.** The large map opens with a pin on every portal you can use.
3. **Click a pin** to teleport there. Close the map (M or Esc) to stay where you are.

### Public and private portals

Every portal is **public** by default, so everyone can see and use it.

- **Shift + Use** on a portal you built toggles it between **public** and **private**.
- A private portal only appears on its builder's map.
- Hover over a portal to see its current visibility.

Admins, and the host of a self-hosted world, can see every portal and change any portal's visibility.

## Good to know

- **PortalNetwork must be installed on the server and on every player's game**, and versions must match. Players without it can't join a server that has it.
- Vanilla teleport rules still apply: items that can't be teleported, boss events and world modifiers that block portals work exactly as before.
- Portals that already exist in your world are public and work straight away.
- If a portal is removed or made private while your map is open, you'll get a message instead of teleporting.

## Credits

Inspired by [TargetPortal](https://thunderstore.io/c/valheim/p/Smoothbrain/TargetPortal/) by Smoothbrain.
