# Apex-6 (Unity source)

Apex-6 is a Blueprinter mod for Nuclear Option 0.34.2. Apex-6 is the smaller drone; Apex-8 is the larger aircraft drone. Both use optical guidance and low radar and infrared signatures.

| Variant | Model | Target speed | Warhead | Cost | Mass |
| --- | --- | ---: | ---: | ---: | ---: |
| Apex-6 | `Models/Apex-6.fbx` | 670 km/h | 38 kg | $550,000 | 180 kg |
| Apex-8 | `Models/Apex6_Air.fbx` | about 1050 km/h | 68 kg | $1,000,000 | 260 kg |

The available carriers are the original six-drone Apex-6 TEL, a separate four-drone Apex-8 TEL, an eight-drone Apex-6 cargo pallet, a four-drone Apex-8 cargo pallet, and the Apex-8 aircraft single rail. Internal identifiers for the existing aircraft drone and single rail remain unchanged for compatibility.

Both pallets use the native cargo ramp and parachute. The release sequence begins seven seconds after the pallet exits the ramp, then ejects one drone every 0.8 seconds. The eight-drone pallet completes its salvo at 12.6 seconds, and the four-drone pallet at 9.4 seconds. Targets and their global coordinates are captured when the player releases the pallet and distributed in order, cycling through the selection when needed. Native optical guidance follows each assigned unit, and the missile owner ID remains the releasing aircraft's ID for damage credit. The empty frame disappears one second after touching terrain or water; if it lands before the salvo is complete, cleanup waits for the final release. Only the server spawns drones and removes the pallet.

Pallet drones launch toward their assigned target at 35 degrees above the horizon with a 55 m/s ejection speed. They exit beyond the frame's diagonal envelope, and their starting position is at least 3 m above sea level. The pallet's downward velocity is excluded from the launch velocity so it cannot cancel the initial climb. The parachute frame stays upright while the drone receives the upward launch attitude.

Cargo pallets are added to compatible native cargo stations with ramps on VL-49 Tarantula (`QuadVTOL1`) and the MC-260 Chimera mod (`Aryx_CargoPlane1`, rear/front cargo groups 1 and 2). Payload masses including the frame are 1,640 kg for eight Apex-6 drones and 1,240 kg for four Apex-8 drones. The Apex-8 TEL uses a one-second detachable booster and four native launch stations with ammunition visuals. Unity asset checks and a Release DLL build verify structure and compilation. The user confirmed the original pallet behavior in-game; the user also confirmed upward launches. The final visual update and multiplayer behavior still require a mission test.

Both variants have a configured engagement limit of 137 km. The actual aircraft cruise speed and full-distance flight still need an in-game test. The aircraft model is 2.74 m wide at its current prefab scale, uses its five corresponding FBX materials, and is also displayed on the ready aircraft rail. The aircraft motor uses 4400 N thrust and a 560 s burn, with no hard speed clamp. The aircraft heat haze is placed behind its nozzle.

The TEL retains its six-rail load, detachable one-second booster, and ammunition display logic. The published DLL includes an embedded Blueprinter bundle; rebuild that bundle after changing these Unity assets, then rebuild the DLL. See the public repository for installation instructions and aircraft hardpoints.
