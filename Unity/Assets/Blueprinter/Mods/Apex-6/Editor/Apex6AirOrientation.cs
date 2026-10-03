using System;
using UnityEditor;
using UnityEngine;

public static class Apex6AirOrientation
{
    private const string Root = "Assets/Blueprinter/Mods/Apex-6/";
    public static void Run()
    {
        Fix(Root + "Apex6_Air.prefab", "Apex6_Visual");
        Fix(Root + "Apex6_AirRail.prefab", "Apex6_ReadyDrone/Apex6_Visual");
        AssetDatabase.SaveAssets();
        Debug.Log("APEX6_AIR_ORIENTATION_OK");
    }
    private static void Fix(string path, string visualPath)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.transform.localRotation = Quaternion.identity;
            var visual = root.transform.Find(visualPath);
            if (visual == null) throw new Exception("Visual missing: " + path);
            visual.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var haze = root.transform.Find("Apex6_DryJetHeatHaze");
            if (haze != null)
            {
                haze.localPosition = new Vector3(0f, .2f, -.9f);
                haze.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
            var ready = root.transform.Find("Apex6_ReadyDrone");
            if (ready != null) ready.localRotation = Quaternion.identity;
            var frame = root.transform.Find("Apex6_AirPylonFrame");
            if (frame != null)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/Apex6_AirPylonFrame.fbx");
                frame.localRotation = Quaternion.Euler(0f, 180f, 0f) * source.transform.localRotation;
            }
            var nozzle = visual.Find("Apex6_Air/Apex6_AirDrone_Nozzle");
            var intake = visual.Find("Apex6_Air/Apex6_AirDrone_Intake");
            if (nozzle == null || intake == null) throw new Exception("Engine references missing");
            var nozzleCenter = root.transform.InverseTransformPoint(nozzle.GetComponent<Renderer>().bounds.center);
            var intakeCenter = root.transform.InverseTransformPoint(intake.GetComponent<Renderer>().bounds.center);
            if (nozzleCenter.z >= intakeCenter.z) throw new Exception("Nozzle must be aft of intake: " + path);
            PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
            if (!success) throw new Exception("Could not save " + path);
            Debug.Log("APEX6_ORIENTATION_CHECK " + path + " nozzleZ=" + nozzleCenter.z + " intakeZ=" + intakeCenter.z);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
