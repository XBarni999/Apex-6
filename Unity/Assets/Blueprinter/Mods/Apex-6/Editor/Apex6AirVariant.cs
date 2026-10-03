using System;
using UnityEditor;
using UnityEngine;
public static class Apex6AirVariant
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    static SerializedProperty P(SerializedObject s,string name)=>s.FindProperty(name)??throw new Exception(name);
    public static void Run()
    {
        const string modelPath=R+"Models/Apex6_Air.fbx";
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if(source==null)throw new Exception("New aircraft FBX missing: "+modelPath);
        var path=R+"Apex6_Air.prefab";
        var drone=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual=drone.transform.Find("Apex6_Visual");
            if(visual==null)throw new Exception("Aircraft visual anchor missing");
            for(int i=visual.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(visual.GetChild(i).gameObject);
            visual.localScale=Vector3.one*.9f; visual.localRotation=Quaternion.Euler(0,180,0);
            var shape=(GameObject)PrefabUtility.InstantiatePrefab(source,visual);
            shape.transform.localPosition=Vector3.zero;shape.transform.localRotation=Quaternion.identity;shape.transform.localScale=Vector3.one;
            foreach(var renderer in shape.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    if(materials[i]==null)throw new Exception("Missing FBX material on "+renderer.name);
                    var material=AssetDatabase.LoadAssetAtPath<Material>(R+"Materials/M_Apex6_"+materials[i].name+".mat");
                    if(material==null)throw new Exception("Material mapping missing: "+materials[i].name);
                    materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
            var haze=drone.transform.Find("Apex6_DryJetHeatHaze");
            if(haze!=null){haze.localPosition=new Vector3(0,.2f,-.9f);haze.localRotation=Quaternion.Euler(0,180,0);}
            var rigidbody=drone.GetComponent<Rigidbody>();rigidbody.mass=260;
            var collider=drone.GetComponent<CapsuleCollider>();collider.radius=.24f;collider.height=2.7f;
            var missile=new SerializedObject(drone.GetComponent<Missile>());
            P(missile,"mass").floatValue=260;
            var motor=P(missile,"motors").GetArrayElementAtIndex(0);
            motor.FindPropertyRelative("thrust").floatValue=4400;
            motor.FindPropertyRelative("burnTime").floatValue=560;
            motor.FindPropertyRelative("fuelMass").floatValue=75;
            P(missile,"blastYield").floatValue=68;
            P(missile,"pierceDamage").floatValue=350;
            missile.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(drone,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(drone);}
        var def=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnitDefinition>(R+"Def_Apex6_Air.asset"));
        P(def,"description").stringValue="Air-launched low-observable jet attack drone. Up to 1050 km/h; 20 m terrain-following; 68 kg warhead.";
        P(def,"length").floatValue=2.66f;P(def,"width").floatValue=2.74f;P(def,"height").floatValue=.48f;
        P(def,"value").floatValue=1;P(def,"mass").floatValue=260;def.ApplyModifiedPropertiesWithoutUndo();
        var weapon=new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponInfo>(R+"WI_Apex6_Air.asset"));
        P(weapon,"description").stringValue="Air-launched low-observable jet attack drone; 68 kg shaped-charge/HE warhead. Selected surface target required.";
        P(weapon,"maxSpeed").floatValue=1050f/3.6f;
        P(weapon,"blastDamage").floatValue=68;P(weapon,"pierceDamage").floatValue=350;
        P(weapon,"costPerRound").floatValue=1;P(weapon,"massPerRound").floatValue=260;
        weapon.ApplyModifiedPropertiesWithoutUndo();
        // The displayed round on the aircraft mount is a separate nested prefab instance.
        var railPath=R+"Apex6_AirRail.prefab";
        var rail=PrefabUtility.LoadPrefabContents(railPath);
        try
        {
            var ready=rail.transform.Find("Apex6_ReadyDrone");
            var railVisual=ready==null?null:ready.Find("Apex6_Visual");
            if(railVisual==null)throw new Exception("Ready aircraft drone visual missing");
            for(int i=railVisual.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(railVisual.GetChild(i).gameObject);
            railVisual.localScale=Vector3.one*.9f; railVisual.localRotation=Quaternion.Euler(0,180,0);
            var loadedShape=(GameObject)PrefabUtility.InstantiatePrefab(source,railVisual);
            loadedShape.transform.localPosition=Vector3.zero;loadedShape.transform.localRotation=Quaternion.identity;loadedShape.transform.localScale=Vector3.one;
            foreach(var renderer in loadedShape.GetComponentsInChildren<Renderer>(true))
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var material=AssetDatabase.LoadAssetAtPath<Material>(R+"Materials/M_Apex6_"+materials[i].name+".mat");
                    if(material==null)throw new Exception("Ready drone material missing: "+materials[i].name);
                    materials[i]=material;
                }
                renderer.sharedMaterials=materials;
            }
            PrefabUtility.SaveAsPrefabAsset(rail,railPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(rail);}
        var mount=new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponMount>(R+"WM_Apex6_AirRail.asset"));
        P(mount,"mass").floatValue=280;mount.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Apex6Setup.Validate();
        Debug.Log("APEX6_AIR_VARIANT_OK");
    }
}
