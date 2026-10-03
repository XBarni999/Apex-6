using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Apex6AmmoVisuals
{
    internal static class PalletHooks
    {
        internal static bool IsPallet(UnitDefinition definition) => definition != null &&
            (definition.jsonKey == "Apex6_Pallet8" || definition.jsonKey == "Apex8_Pallet4");
        private static readonly List<PalletManifest> Pending = new List<PalletManifest>();
        internal static void Install(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(MountedCargo), "Fire"),
                prefix: new HarmonyMethod(typeof(PalletHooks), nameof(Capture)));
            harmony.Patch(AccessTools.Method(typeof(Spawner), "SpawnUnit"),
                postfix: new HarmonyMethod(typeof(PalletHooks), nameof(Spawned)));
            harmony.Patch(AccessTools.Method(typeof(Container), "InitializeUnit"),
                postfix: new HarmonyMethod(typeof(PalletHooks), nameof(Initialized)));
            harmony.Patch(AccessTools.Method(typeof(CargoDeploymentSystem), "CargoDeploymentSystem_OnUnitLanded"),
                prefix: new HarmonyMethod(typeof(PalletHooks), nameof(Landed)));
            harmony.Patch(AccessTools.Method(typeof(Missile), "StartMissile"),
                postfix: new HarmonyMethod(typeof(PalletHooks), nameof(DroneVisible)));
        }
        private static void Capture(MountedCargo __instance, Unit owner, Unit target)
        {
            if (!IsPallet(__instance.cargo) ||
                owner == null || !owner.IsServer || __instance.GetAmmoLoaded() == 0) return;
            Pending.RemoveAll(p => p.Mount == null || p.Owner == null);
            if (Pending.Exists(p => p.Mount == __instance)) return;
            var targets = new List<Unit>();
            if (target != null) targets.Add(target);
            var aircraft = owner as Aircraft;
            if (aircraft != null && aircraft.weaponManager != null)
                foreach (var selected in aircraft.weaponManager.GetTargetList())
                    if (selected != null && !targets.Contains(selected)) targets.Add(selected);
            Pending.Add(new PalletManifest(__instance, owner, targets));
        }
        private static void Spawned(UnitDefinition unit, Vector3 spawnPosition, Unit owner, Unit __result)
        {
            if (!IsPallet(unit) || __result == null || owner == null) return;
            // Several cargo stations can be sliding out at once. Match the actual
            // mount at the spawn position instead of consuming an owner-wide FIFO.
            PalletManifest match = null;
            float distance = float.PositiveInfinity;
            foreach (var pending in Pending)
            {
                if (pending.Owner != owner || pending.Mount == null || pending.Mount.cargo != unit) continue;
                float candidate = (pending.Mount.transform.position - spawnPosition).sqrMagnitude;
                if (candidate < distance) { match = pending; distance = candidate; }
            }
            if (match == null || distance > 4f) return; // Includes cargo detached by damage.
            Pending.Remove(match);
            var view = __result.GetComponent<PalletSequence>() ?? __result.gameObject.AddComponent<PalletSequence>();
            view.Begin(__result, match);
        }
        private static void Initialized(Container __instance)
        {
            if (!IsPallet(__instance.definition)) return;
            var view = __instance.GetComponent<PalletSequence>() ?? __instance.gameObject.AddComponent<PalletSequence>();
            view.Bind(__instance);
            // Native initialization normally deploys the chute here. Cover low drops
            // too, where the native 10 m ground test suppresses deployment.
            if (__instance.GetComponentInChildren<CargoDeploymentSystem>() == null)
            {
                var prefab = AccessTools.Field(typeof(Container), "parachuteSystem").GetValue(__instance) as GameObject;
                if (prefab != null) UnityEngine.Object.Instantiate(prefab, __instance.transform)
                    .GetComponent<CargoDeploymentSystem>().Initialize(__instance);
            }
        }
        private static void Landed(CargoDeploymentSystem __instance)
        {
            var view = __instance.GetComponentInParent<PalletSequence>();
            if (view != null) view.TouchSurface();
        }
        private static void DroneVisible(Missile __instance)
        {
            var info = __instance.GetWeaponInfo();
            if (info == null || (info.name != "WI_Apex6_Air" && info.name != "WI_Apex6_PalletDrone")) return;
            // Clients observe the native network missile spawn, so they remove the
            // corresponding loaded visual without launching a second local drone.
            PalletSequence.ObserveRelease(__instance);
        }
    }

    internal sealed class PalletManifest
    {
        internal readonly MountedCargo Mount;
        internal readonly Unit Owner;
        internal readonly PersistentID OwnerID;
        internal readonly Unit[] Targets;
        internal readonly GlobalPosition[] Coordinates;
        internal PalletManifest(MountedCargo mount, Unit owner, List<Unit> targets)
        {
            Mount = mount; Owner = owner; OwnerID = owner.persistentID; Targets = targets.ToArray();
            Coordinates = new GlobalPosition[Targets.Length];
            for (int i = 0; i < Targets.Length; i++) Coordinates[i] = Targets[i].transform.position.ToGlobalPosition();
        }
    }

    public sealed class PalletSequence : MonoBehaviour
    {
        private static readonly List<PalletSequence> Active = new List<PalletSequence>();
        private int DroneCount => pallet != null && pallet.definition.jsonKey == "Apex6_Pallet8" ? 8 : 4;
        private string DroneKey => DroneCount == 8 ? "Apex6_PalletDrone" : "Apex6_Air";
        private const float LaunchElevation = 35f;
        private const float EjectionSpeed = 55f;
        private Vector3 ExitPosition(Transform cell, Vector3 heading)
        {
            // Exit beyond the frame's diagonal envelope, then climb outside the
            // lifting frame and parachute lines instead of launching underneath it.
            float clearance = new Vector2(pallet.definition.length, pallet.definition.width).magnitude / 2f + 2f;
            Vector3 position = (cell != null ? cell.position : transform.position) + heading * clearance + Vector3.up * .5f;
            position.y = Mathf.Max(position.y, Datum.LocalSeaY + 3f);
            if (landed) position.y = Mathf.Max(position.y, transform.position.y + pallet.definition.height / 2f + 1.5f);
            return position;
        }
        private static Vector3 HorizontalHeading(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) { direction = fallback; direction.y = 0; }
            if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
            return direction.normalized;
        }
        internal const float Delay = 7f, Interval = .8f, CleanupDelay = 1f;
        private Unit pallet;
        private PalletManifest manifest;
        private int released;
        private bool landed, started, cleanupStarted;
        public void Bind(Unit unit) { pallet = unit; if (!Active.Contains(this)) Active.Add(this); }
        private void OnDestroy() { Active.Remove(this); }
        internal static void ObserveRelease(Missile missile)
        {
            Transform nearest = null;
            float distance = 16f;
            foreach (var view in Active)
            {
                if (view == null || view.pallet == null || view.pallet.IsServer) continue;
                var container = view.pallet as Container;
                if (container == null || container.ownerID != missile.ownerID) continue;
                for (int i = 1; i <= view.DroneCount; i++)
                {
                    var cell = view.transform.Find("PalletFrame/DroneCell_" + i);
                    if (cell == null || !cell.gameObject.activeSelf) continue;
                    var heading = HorizontalHeading(missile.transform.forward, view.transform.forward);
                    float candidate = (view.ExitPosition(cell, heading) - missile.transform.position).sqrMagnitude;
                    if (candidate < distance) { distance = candidate; nearest = cell; }
                }
            }
            if (nearest != null) nearest.gameObject.SetActive(false);
        }
        internal void Begin(Unit unit, PalletManifest payload)
        {
            if (started || !unit.IsServer || unit.disabled) return;
            Bind(unit); manifest = payload; started = true;
            StartCoroutine(Release());
        }
        private IEnumerator Release()
        {
            yield return new WaitForSeconds(Delay);
            if (pallet == null || pallet.disabled) yield break;
            if (!Encyclopedia.Lookup.TryGetValue(DroneKey, out var definition) || !(definition is MissileDefinition drone))
            {
                Debug.LogError("Apex pallet: "+DroneKey+" definition unavailable; payload retained.");
                yield break;
            }
            for (int i = 0; i < DroneCount; i++)
            {
                if (pallet == null || pallet.disabled) yield break;
                var cell = transform.Find("PalletFrame/DroneCell_" + (i + 1));
                Unit target = manifest.Targets.Length == 0 ? null : manifest.Targets[i % manifest.Targets.Length];
                Vector3 heading = HorizontalHeading(manifest.Coordinates.Length > 0 ?
                    manifest.Coordinates[i % manifest.Coordinates.Length].ToLocalPosition() - transform.position : transform.forward, transform.forward);
                Vector3 position = ExitPosition(cell, heading);
                float elevation = LaunchElevation * Mathf.Deg2Rad;
                Vector3 direction = heading * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                Vector3 inheritedVelocity = pallet.rb.velocity;
                // The pallet is descending, but its downward velocity must not
                // cancel the drone's initial climb near the surface.
                inheritedVelocity.y = Mathf.Max(inheritedVelocity.y, 0f);
                Unit sourceUnit = manifest.Owner != null ? manifest.Owner : pallet;
                var missile = NetworkSceneSingleton<Spawner>.i.SpawnMissile(drone, position, rotation,
                    inheritedVelocity + direction * EjectionSpeed, target != null && !target.disabled ? target : null, sourceUnit);
                missile.NetworkownerID = manifest.OwnerID;
                // Preserve the launch snapshot for diagnostics; native optical guidance
                // keeps tracking the selected unit through its network target ID.
                var payload = missile.gameObject.AddComponent<PalletDronePayload>();
                payload.SourceID = manifest.OwnerID; payload.SelectedCoordinates = manifest.Coordinates;
                released++;
                if (cell != null) cell.gameObject.SetActive(false);
                if (i + 1 < DroneCount) yield return new WaitForSeconds(Interval);
            }
            TryCleanup();
        }
        private void FixedUpdate()
        {
            if (pallet == null || !pallet.IsServer || pallet.disabled || !started) return;
            // MountedCargo disables the Container while pushing it out of the ramp.
            // Leave that phase under native control; stabilize only after separation.
            if (!pallet.enabled) return;
            if (!landed)
            {
                var rb = pallet.rb;
                Vector3 horizontal = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(-horizontal * 1.1f, ForceMode.Acceleration);
                if (rb.velocity.y < -8f) rb.AddForce(Vector3.up * (-8f - rb.velocity.y) * 3f, ForceMode.Acceleration);
                Vector3 heading = transform.forward;
                if (manifest.Coordinates.Length > 0) heading = manifest.Coordinates[0].ToLocalPosition() - transform.position;
                heading.y = 0;
                if (heading.sqrMagnitude < .01f) heading = Vector3.forward;
                Quaternion desired = Quaternion.LookRotation(heading.normalized, Vector3.up);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, desired, Time.fixedDeltaTime * 2f));
                if (transform.position.y - pallet.definition.height/2 <= Datum.LocalSeaY) TouchSurface();
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (((1 << collision.gameObject.layer) & (int)PhysicsLayers.StaticsMask) != 0) TouchSurface();
        }
        public void TouchSurface() { landed = true; TryCleanup(); }
        private void TryCleanup()
        {
            if (!landed || released != DroneCount || cleanupStarted || pallet == null || !pallet.IsServer) return;
            cleanupStarted = true;
            StartCoroutine(Cleanup());
        }
        private IEnumerator Cleanup()
        {
            yield return new WaitForSeconds(CleanupDelay);
            if (pallet != null && pallet.IsServer)
                NetworkSceneSingleton<Spawner>.i.ServerObjectManager.Destroy(gameObject);
        }
    }

    public sealed class PalletDronePayload : MonoBehaviour
    {
        public PersistentID SourceID;
        public GlobalPosition[] SelectedCoordinates;
    }
}
