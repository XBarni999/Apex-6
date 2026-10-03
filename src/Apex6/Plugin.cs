using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Apex6AmmoVisuals
{
    [BepInPlugin("ua.ncmod.apex6.ammo-visuals", "Apex-6", "1.2.1")]
    [BepInDependency("com.nikkorap.blueprinter", "2.0.1")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private float nextRangeRefresh;
        internal const float EngagementRange = 137000f;
        private void Awake()
        {
            harmony = new Harmony("ua.ncmod.apex6.ammo-visuals");
            harmony.Patch(AccessTools.Method(typeof(MissileLauncher), "OnEnable"),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(Attach)));
            harmony.Patch(AccessTools.Method(typeof(Missile), "CalcRange"),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(LimitRange)));
            harmony.Patch(AccessTools.Method(typeof(Missile), "StartMissile"),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(DeployAirWings)));
            PalletHooks.Install(harmony);
            RefreshWeaponRanges();
            // Also cover a launcher already loaded when the plugin is enabled.
            foreach (var launcher in FindObjectsOfType<MissileLauncher>()) Attach(launcher);
            Logger.LogInfo("Apex-6 six-rail ammunition visuals enabled.");
        }
        private void Update()
        {
            if (Time.unscaledTime < nextRangeRefresh) return;
            nextRangeRefresh = Time.unscaledTime + 2f;
            RefreshWeaponRanges();
        }
        private static void RefreshWeaponRanges()
        {
            // Includes definitions loaded by Blueprinter after this plugin's Awake.
            foreach (var info in Resources.FindObjectsOfTypeAll<WeaponInfo>())
                if (IsDroneInfo(info))
                {
                    info.targetRequirements.maxRange = EngagementRange;
                    if (info.name == "WI_Apex6_Air") { info.weaponName = "Apex-8"; info.shortName = "APX-8"; }
                }
            foreach (var mount in Resources.FindObjectsOfTypeAll<WeaponMount>())
                if (mount.jsonKey == "Apex6_AirRail") mount.mountName = "Apex-8 Single Rail";
        }
        private static bool IsDroneInfo(WeaponInfo info) => info != null &&
            (info.name == "WI_Apex6_Air" || info.name == "WI_Apex6_Ground" ||
             info.name == "WI_Apex8_Ground" || info.name == "WI_Apex6_PalletDrone");
        private static void LimitRange(Missile __instance, ref float __result, ref float noEscapeDistance)
        {
            var info = __instance == null ? null : __instance.GetWeaponInfo();
            if (!IsDroneInfo(info)) return;
            // Preserve shorter native estimates, but never advertise beyond the mission envelope.
            __result = Mathf.Min(__result, EngagementRange);
            noEscapeDistance = Mathf.Min(noEscapeDistance, __result);
        }
        private static void DeployAirWings(Missile __instance)
        {
            var info = __instance == null ? null : __instance.GetWeaponInfo();
            if (info == null || (info.name != "WI_Apex6_Air" && info.name != "WI_Apex8_Ground")) return;
            var model = __instance.transform.Find("Apex6_Visual");
            var animation = model == null ? null : model.GetComponentInChildren<Animation>();
            if (animation == null || animation.clip == null) return;
            __instance.StartCoroutine(PlayAfterSeparation(animation));
        }
        private static IEnumerator PlayAfterSeparation(Animation animation)
        {
            yield return new WaitForSeconds(0.2f);
            if (animation != null) animation.Play();
        }
        private static void Attach(MissileLauncher __instance)
        {
            if (__instance == null || __instance.missile == null ||
                (__instance.missile.jsonKey != "Apex6_Ground" && __instance.missile.jsonKey != "Apex8_Ground")) return;
            var view = __instance.GetComponent<RailAmmoView>();
            if (view == null) view = __instance.gameObject.AddComponent<RailAmmoView>();
            view.Bind(__instance);
        }
        private void OnDestroy() { harmony?.UnpatchSelf(); }
    }

    // Read-only rendering adapter: native ammo and next launch cell are authoritative,
    // including clients receiving RpcSyncAmmoCount and partial rearming.
    public sealed class RailAmmoView : MonoBehaviour
    {
        private static readonly FieldInfo Cell = AccessTools.Field(typeof(MissileLauncher), "currentCell");
        private MissileLauncher launcher;
        private GameObject[] models;
        public void Bind(MissileLauncher value)
        {
            launcher = value;
            models = new GameObject[value.missile.jsonKey == "Apex8_Ground" ? 4 : 6];
            var unit = launcher.GetComponentInParent<GroundVehicle>();
            if (unit == null) { enabled = false; return; }
            foreach (var t in unit.GetComponentsInChildren<Transform>(true))
            {
                const string prefix = "LoadedDrone_";
                int index;
                if (t.name.StartsWith(prefix, StringComparison.Ordinal) &&
                    int.TryParse(t.name.Substring(prefix.Length), out index) && index >= 1 && index <= models.Length)
                    models[index - 1] = t.gameObject;
            }
            enabled = true;
            LateUpdate();
        }
        private void LateUpdate()
        {
            if (launcher == null || Cell == null) return;
            int count = models.Length;
            int next = ((int)Cell.GetValue(launcher) % count + count) % count;
            int ammo = Mathf.Clamp(launcher.ammo, 0, count);
            for (int i = 0; i < count; i++)
            {
                bool loaded = (i - next + count) % count < ammo;
                if (models[i] != null && models[i].activeSelf != loaded) models[i].SetActive(loaded);
            }
        }
    }
}
