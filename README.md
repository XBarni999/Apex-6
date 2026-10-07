# Apex-6

A family of low-observable jet-powered attack drones for **Nuclear Option 0.34.2**, featuring optical guidance, terrain-following flight, aircraft launch, ground launchers, and parachute-deployed cargo pallets.

- Launch range: **137 km** for both variants. Ground target cruise speed: **670 km/h**; aircraft variant target maximum: **about 1050 km/h**.
- **Apex-6** is the smaller drone: **38 kg** warhead, **$550,000**. **Apex-8** is the larger drone: **68 kg** warhead, **$1,000,000**.
- Cargo options: **8 × Apex-6** or **4 × Apex-8**, supported on **VL-49 Tarantula** and the **MC-260 Chimera** mod. Chimera uses its rear and front cargo bays (groups 1 and 2). UH-90 Ibis does not receive these pallets. Pallets use the native shared cargo selection; mixed payloads may display the first pallet type's name.
- Ground options: the original **6 × Apex-6 TEL** and a separate **4 × Apex-8 TEL**.
- Ground launcher: a one-second detachable solid-fuel booster and sequential launches. Loaded drones disappear after launch and return when rearmed.
- Aircraft: a separate larger drone on its own upper pylon frame; its wings unfold after launch. One drone per rail, supporting **10 vanilla and 9 modded aircraft**. [Aircraft and hardpoints](AIRCRAFT_COMPATIBILITY.md).

## Installation

Pallets descend under a parachute, wait seven seconds after ramp exit, then release one drone every 0.8 seconds. Drones eject upward at 35 degrees and 55 m/s, receive the selected targets, and retain the aircraft's damage credit. Empty frames disappear one second after landing; cleanup waits for any remaining drone releases.

Requires **BepInEx 5** and **Blueprinter 2.0.1+**.

Download [Apex-6.dll](https://github.com/XBarni999/Apex-6/releases/latest), place it in `BepInEx/plugins`, and restart the game. All mod assets are embedded in the DLL. When upgrading, remove old `Apex-6_*.nobp` files and `Apex6AmmoVisuals.dll` to prevent duplicate loading.

## Mod lore

As the war between PALA and BDF became a grinding hunt for radars and supply depots, PALA commissioned Apex-6: an expendable jet drone that could be lost instead of a crewed aircraft. Six-rail trucks brought it to the front, while attack aircraft launched from behind the lines, sending ground and airborne waves toward the same target.

BDF initially dismissed them as "disposable wings." After capturing several launchers, its engineers rebuilt the design for their own units. Both sides now field Apex-6, and air-defense crews share a warning: if you spot one, look for five more.

This is original mod lore, not official canon. Faction background: [PALA](https://nuclearoption.wiki.gg/wiki/PALA), [BDF](https://nuclearoption.wiki.gg/wiki/BDF).

## Source

`src/Apex6` contains the BepInEx plugin and embedded bundle. `Unity/Assets/Blueprinter/Mods/Apex-6` contains Blueprinter Editor assets; vanilla dependencies are not included.

Build the DLL with `dotnet build src/Apex6/Apex-6.csproj -c Release -p:GameDir="path to Nuclear Option"`. After editing Unity assets, build a new `.nobp` and replace `src/Apex6/Bundle/Apex-6.nobp` before rebuilding the
DLL.



This project is an unofficial community modification and is not affiliated with, sponsored by, or endorsed by Shockfront Studios Pty Ltd. Original Nuclear Option assets, vehicle designs, audio, and code are Copyright (c) 2026 Shockfront Studios Pty Ltd. All rights reserved. Nuclear Option and Shockfront Studios are trademarks or registered trademarks of Shockfront Studios Pty Ltd. Original mod content and all other trademarks belong to their respective owners.

The user tested ground and aircraft launches, pallet operation, and the revised upward launch behavior. The package passed Release compilation, Unity material checks, and embedded-resource hash verification. The final visual update and multiplayer behavior have not yet been verified in-game. See [CHANGELOG.md](CHANGELOG.md) for release details.
