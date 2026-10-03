using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Apex6FlightSetup
{
    const string Root = "Assets/Blueprinter/Mods/Apex-6/";
    static SerializedProperty P(SerializedObject s, string name) => s.FindProperty(name) ?? throw new Exception(name);

    [MenuItem("Blueprinter/Apex-6/Apply 135 km and moving launcher")]
    public static void Run()
    {
        foreach (var variant in new[] { "Air", "Ground" })
        {
            var info = new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponInfo>(Root + "WI_Apex6_" + variant + ".asset"));
            P(info, "targetRequirements.maxRange").floatValue = 137000;
            info.ApplyModifiedPropertiesWithoutUndo();
            var path = Root + "Apex6_" + variant + ".prefab";
            var drone = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (drone.GetComponent<OpticalSeekerCruiseMissile>() == null) throw new Exception("Optical seeker missing");
                var missile = new SerializedObject(drone.GetComponent<Missile>());
                var motors = P(missile, "motors");
                var cruise = motors.GetArrayElementAtIndex(motors.arraySize - 1);
                cruise.FindPropertyRelative("burnTime").floatValue = 900;
                cruise.FindPropertyRelative("fuelMass").floatValue = 52.5f;
                missile.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(drone, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(drone); }
        }
        var telPath = Root + "Apex6_TEL.prefab";
        var tel = PrefabUtility.LoadPrefabContents(telPath);
        try
        {
            var launcher = tel.GetComponentInChildren<MissileLauncher>(true);
            var turret = launcher.GetComponentInParent<Turret>();
            var s = new SerializedObject(turret);
            P(s, "firesWithoutAiming").boolValue = false;
            P(s, "minElevation").floatValue = 25;
            P(s, "maxElevation").floatValue = 45;
            P(s, "traverseRange").floatValue = 180;
            P(s, "lockTime").floatValue = 1.5f;
            var elevation = (Transform)P(s, "elevationTransform").objectReferenceValue;
            s.ApplyModifiedPropertiesWithoutUndo();
            elevation.localRotation = Quaternion.identity;
            var rack = tel.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Apex6_SixRailRack");
            if (rack.parent != elevation) throw new Exception("Rack must follow native elevation pivot");
            rack.localPosition = new Vector3(0, .75f, -.3f);
            foreach (var point in rack.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("LaunchPoint_")))
                point.localPosition = new Vector3(0, .14f, 2.3f);
            foreach (var collider in elevation.GetComponentsInChildren<BoxCollider>(true))
            {
                collider.center = new Vector3(0, 1.32f, -.35f);
                collider.size = new Vector3(2.2f, 1.55f, 2.65f);
            }
            PrefabUtility.SaveAsPrefabAsset(tel, telPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(tel); }
        AssetDatabase.SaveAssets();
        Apex6Setup.Validate();
        Debug.Log("APEX6_FLIGHT_SETUP_OK: optical, 135 km, 900 s cruise, native moving rack");
    }
}
