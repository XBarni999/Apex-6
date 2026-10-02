using System;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Apex6AmmoVisuals
{
    [BepInPlugin("ua.ncmod.apex6.ammo-visuals", "Apex-6", "1.0.0")]
    [BepInDependency("com.nikkorap.blueprinter", "2.0.1")]
    public sealed class Plugin : BaseUnityPlugin
    {
        private Harmony harmony;
        private void Awake()
        {
            harmony = new Harmony("ua.ncmod.apex6.ammo-visuals");
            harmony.Patch(AccessTools.Method(typeof(MissileLauncher), "OnEnable"),
                postfix: new HarmonyMethod(typeof(Plugin), nameof(Attach)));
            // Also cover a launcher already loaded when the plugin is enabled.
            foreach (var launcher in FindObjectsOfType<MissileLauncher>()) Attach(launcher);
            Logger.LogInfo("Apex-6 six-rail ammunition visuals enabled.");
        }
        private static void Attach(MissileLauncher __instance)
        {
            if (__instance == null || __instance.missile == null ||
                __instance.missile.jsonKey != "Apex6_Ground") return;
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
        private readonly GameObject[] models = new GameObject[6];
        public void Bind(MissileLauncher value)
        {
            launcher = value;
            var unit = launcher.GetComponentInParent<GroundVehicle>();
            if (unit == null) { enabled = false; return; }
            foreach (var t in unit.GetComponentsInChildren<Transform>(true))
            {
                const string prefix = "LoadedDrone_";
                int index;
                if (t.name.StartsWith(prefix, StringComparison.Ordinal) &&
                    int.TryParse(t.name.Substring(prefix.Length), out index) && index >= 1 && index <= 6)
                    models[index - 1] = t.gameObject;
            }
            enabled = true;
            LateUpdate();
        }
        private void LateUpdate()
        {
            if (launcher == null || Cell == null) return;
            int next = ((int)Cell.GetValue(launcher) % 6 + 6) % 6;
            int ammo = Mathf.Clamp(launcher.ammo, 0, 6);
            for (int i = 0; i < 6; i++)
            {
                bool loaded = (i - next + 6) % 6 < ammo;
                if (models[i] != null && models[i].activeSelf != loaded) models[i].SetActive(loaded);
            }
        }
    }
}
