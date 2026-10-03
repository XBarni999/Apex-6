using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Apex6MissionFix
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    static SerializedProperty P(SerializedObject s,string n)=>s.FindProperty(n)??throw new Exception(n);
    static void Cube(Transform p,string n,Vector3 pos,Vector3 size,Material m)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=m;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());}
    public static void Run()
    {
        var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blueprinter/_donotship/GameObject/CruiseMissile1_PLACEHOLDER.prefab");
        var ds=new SerializedObject(donor.GetComponent<Missile>());var dm=P(ds,"motors").GetArrayElementAtIndex(0);
        foreach(var v in new[]{"Air","Ground"})
        {
            var path=R+"Apex6_"+v+".prefab";var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var a in g.GetComponentsInChildren<AudioSource>(true)) {a.enabled=false;a.playOnAwake=false;}
                var old=g.transform.Find("Apex6_TurbojetLoop");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                var loop=g.AddComponent<AudioSource>();EditorUtility.CopySerialized(dm.FindPropertyRelative("audioSources").GetArrayElementAtIndex(0).objectReferenceValue,loop);loop.enabled=true;
                var start=g.AddComponent<AudioSource>();EditorUtility.CopySerialized(dm.FindPropertyRelative("startupSource").objectReferenceValue,start);start.enabled=true;
                var ms=new SerializedObject(g.GetComponent<Missile>());var motors=P(ms,"motors");
                for(int i=0;i<motors.arraySize;i++){var m=motors.GetArrayElementAtIndex(i);var a=m.FindPropertyRelative("audioSources");a.arraySize=i==motors.arraySize-1?1:0;if(a.arraySize>0)a.GetArrayElementAtIndex(0).objectReferenceValue=loop;m.FindPropertyRelative("startupSource").objectReferenceValue=i==0?start:null;}
                ms.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(g,path);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
        }
        var railMat=AssetDatabase.LoadAssetAtPath<Material>(R+"Materials/M_Apex6_Container.mat");
        var airPath=R+"Apex6_AirRail.prefab";var air=PrefabUtility.LoadPrefabContents(airPath);
        try{var parent=air.transform.Find("Apex6_AirRailHardware");foreach(var t in parent.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("PylonRailBridge")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);foreach(float z in new[]{-.25f,.25f})Cube(parent,"PylonRailBridge",new Vector3(0,-.22f,z),new Vector3(.32f,.32f,.12f),railMat);PrefabUtility.SaveAsPrefabAsset(air,airPath);}finally{PrefabUtility.UnloadPrefabContents(air);}
        var telPath=R+"Apex6_TEL.prefab";var tel=PrefabUtility.LoadPrefabContents(telPath);
        try
        {
            var launcher=tel.GetComponentInChildren<MissileLauncher>(true);var turret=launcher.GetComponentInParent<Turret>();var s=new SerializedObject(turret);var elevation=(Transform)P(s,"elevationTransform").objectReferenceValue;
            foreach(var t in elevation.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("launchTransform",StringComparison.Ordinal)).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            // Native firing clearance ray starts at this pivot. Place it above the deck and
            // remove the donor launcher box that enclosed that ray's origin.
            elevation.localPosition=new Vector3(elevation.localPosition.x,1.6f,elevation.localPosition.z);
            foreach(var c in elevation.GetComponentsInChildren<BoxCollider>(true))UnityEngine.Object.DestroyImmediate(c);
            var rack=elevation.Find("Apex6_SixRailRack");rack.localPosition=new Vector3(0,.18f,-.3f);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(R+"Apex6_AirRail.prefab").GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Apex6_Visual");
            foreach(var t in rack.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("LoadedDrone_")).ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
            int index=0;foreach(var rail in rack.Cast<Transform>().Where(t=>t.name.StartsWith("LaunchRail_"))) {var visual=UnityEngine.Object.Instantiate(model.gameObject,rail);visual.name="LoadedDrone_"+(++index);visual.transform.localPosition=new Vector3(0,.14f,0);visual.transform.localRotation=Quaternion.identity;}
            PrefabUtility.SaveAsPrefabAsset(tel,telPath);
        }finally{PrefabUtility.UnloadPrefabContents(tel);}
        AssetDatabase.SaveAssets();Apex6Setup.Validate();Debug.Log("APEX6_MISSION_FIX_OK");
    }
}
