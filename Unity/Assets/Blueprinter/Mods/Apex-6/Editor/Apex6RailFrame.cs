using System;
using UnityEditor;
using UnityEngine;

public static class Apex6RailFrame
{
    private const string Root = "Assets/Blueprinter/Mods/Apex-6/";

    public static void Run()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/Apex6_AirPylonFrame.fbx");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "Materials/M_Apex6_M_AirDrone_PylonFrame.mat");
        if (model == null || material == null) throw new Exception("Aircraft pylon frame or material is missing");
        var path = Root + "Apex6_AirRail.prefab";
        var rail = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var previous = rail.transform.Find("Apex6_AirPylonFrame");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var frame = (GameObject)PrefabUtility.InstantiatePrefab(model, rail.transform);
            frame.name = "Apex6_AirPylonFrame";
            frame.transform.localPosition = new Vector3(0f, -0.40f, 0f);
            // The FBX importer supplies a 270-degree X correction. Preserve it so
            // the frame's long axis runs along the drone rather than standing up.
            frame.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
            frame.transform.localScale = Vector3.one;
            foreach (var renderer in frame.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterial = material;

            // Retain the mount's launch transforms and behavior, but hide its old cube geometry.
            foreach (var name in new[] { "Apex6_AirRailHardware", "PylonAdapter" })
            {
                var old = rail.transform.Find(name);
                if (old == null) throw new Exception("Existing launch hardware missing: " + name);
                foreach (var renderer in old.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            }
            PrefabUtility.SaveAsPrefabAsset(rail, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(rail); }
        Debug.Log("APEX6_TOP_RAIL_READY");
    }
}
