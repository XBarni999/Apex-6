# Apex-6

A low-observable jet-powered attack drone for **Nuclear Option 0.34.2**, featuring optical guidance, terrain-following flight, and aircraft or six-rail ground launch.

- Launch range: **137 km** for both variants. Ground target cruise speed: **670 km/h**; aircraft variant target maximum: **about 1050 km/h**.
- Ground drone: **38 kg** warhead, **$550,000**. Aircraft drone: **68 kg** warhead, **$1,000,000**.
- Ground launcher: a one-second detachable solid-fuel booster and sequential launches. Loaded drones disappear after launch and return when rearmed.
- Aircraft: a separate larger drone on its own upper pylon frame; its wings unfold after launch. One drone per rail, supporting **10 vanilla and 9 modded aircraft**. [Aircraft and hardpoints](AIRCRAFT_COMPATIBILITY.md).

## Installation

Requires **BepInEx 5** and **Blueprinter 2.0.1+**.

Download [Apex-6.dll](https://github.com/XBarni999/Apex-6/releases/latest), place it in `BepInEx/plugins`, and restart the game. All mod assets are embedded in the DLL. When upgrading, remove old `Apex-6_*.nobp` files and `Apex6AmmoVisuals.dll` to prevent duplicate loading.

## Mod lore

As the war between PALA and BDF became a grinding hunt for radars and supply depots, PALA commissioned Apex-6: an expendable jet drone that could be lost instead of a crewed aircraft. Six-rail trucks brought it to the front, while attack aircraft launched from behind the lines, sending ground and airborne waves toward the same target.

BDF initially dismissed them as "disposable wings." After capturing several launchers, its engineers rebuilt the design for their own units. Both sides now field Apex-6, and air-defense crews share a warning: if you spot one, look for five more.

This is original mod lore, not official canon. Faction background: [PALA](https://nuclearoption.wiki.gg/wiki/PALA), [BDF](https://nuclearoption.wiki.gg/wiki/BDF).

## Source

`src/Apex6` contains the BepInEx plugin and embedded bundle. `Unity/Assets/Blueprinter/Mods/Apex-6` contains Blueprinter Editor assets; vanilla dependencies are not included.

Build the DLL with `dotnet build src/Apex6/Apex-6.csproj -c Release -p:GameDir="path to Nuclear Option"`. After editing Unity assets, build a new `.nobp` and replace `src/Apex6/Bundle/Apex-6.nobp` before rebuilding the DLL.

Ground and aircraft launches were tested by the user. The combined package passed build and embedded-resource hash checks; the single-file DLL has not yet been tested in-game.
