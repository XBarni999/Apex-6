# Changelog

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
