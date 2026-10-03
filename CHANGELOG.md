# Changelog

## 1.2.0 — 2026-10-03

- Named the smaller drone Apex-6 and the larger drone Apex-8. Existing aircraft identifiers remain compatible with earlier loadouts.
- Added an eight-drone Apex-6 cargo pallet and a four-drone Apex-8 cargo pallet for compatible native cargo stations on QuadVTOL1 and UtilityHelo1.
- Added a separate four-drone Apex-8 ground launcher with a detachable booster and ammunition visuals. The original six-drone Apex-6 launcher remains available.
- Added parachute stabilization, a seven-second separation delay, and sequential drone releases at 0.8-second intervals. Assigned targets and damage credit follow the releasing aircraft.
- Changed pallet launches to a 35-degree upward attitude with a 55 m/s ejection velocity. Drones clear the frame and do not inherit its downward velocity.
- Added cleanup of empty landed pallet frames. Pallets retain the game's shared cargo selection and release behavior.
- Corrected inverted wing and turbine faces. Restored Lit materials, consistent wing material assignments, and dark inner surfaces for the intake and nozzle without two-sided rendering.
- Updated the single-DLL package with the rebuilt Blueprinter bundle.

Validation: the user confirmed pallet operation and upward launches in-game. Unity checks verified material assignments for the aircraft drone, both Apex-8 pallet prefabs, the aircraft rail, and the four-drone ground launcher. The turbine is visible in the front preview. Release compilation and embedded-bundle SHA-256 checks passed. The final visual package and multiplayer behavior still require in-game verification.

## 1.1.0 — 2026-10-03

- Added a separate, larger aircraft drone with its own FBX materials: 260 kg mass, 68 kg warhead, $1,000,000 per round, and a target maximum speed of approximately 1050 km/h.
- Added the custom upper aircraft pylon frame and corrected its orientation and position.
- Added wing deployment after launch: a 0.2-second separation delay followed by the authored 1.2-second animation. Wings remain folded on the aircraft rail.
- Updated the animation from the revised FBX, including position, rotation, and scale curves, to fix incorrect wing poses after model changes.
- Corrected the aircraft drone's orientation: the missile and mount use +Z forward, while the visual model receives the required rotation. Moved heat haze behind the nozzle.
- Kept the 137 km engagement limit and the single-DLL package. The ground drone retains its existing model and settings.

Validation: Release DLL build passed without warnings or errors. The embedded bundle matches the latest user-built `.nobp` by SHA-256. Unity sampled both aircraft prefabs at five points in the animation and confirmed that wing poses match the updated FBX. The latest wing deployment and flight speed still require in-game confirmation.

## 1.0.1 — 2026-10-02

- Capped the aircraft and ground range estimates at 137 km while preserving shorter native estimates.
- Set both variants' engagement range to 137 km.

## 1.0.0 — 2026-10-02

- Initial single-DLL release with an embedded Blueprinter bundle, aircraft launch, a six-rail ground TEL, detachable booster, and ammunition visibility.
