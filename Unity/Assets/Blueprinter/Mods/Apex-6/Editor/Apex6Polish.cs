using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Apex6Polish
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    static SerializedProperty P(SerializedObject s,string n)=>s.FindProperty(n)??throw new Exception(n);
    public static void Run()
    {
        foreach(var variant in new[]{"Air","Ground"})
        {
            var path=R+"Apex6_"+variant+".prefab";var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var s=new SerializedObject(g.GetComponent<Missile>());var motors=P(s,"motors");
                for(int i=0;i<motors.arraySize;i++)motors.GetArrayElementAtIndex(i).FindPropertyRelative("topSpeed").floatValue=299792450;
                // The original curve predicted 1802 km/h; drag-limited speed scales as sqrt(1/Cd).
                var curve=P(s,"dragCurve").animationCurveValue;
                if(curve.keys[0].value<.05f){var keys=curve.keys;for(int i=0;i<keys.Length;i++){keys[i].value*=7.233f;keys[i].inTangent*=7.233f;keys[i].outTangent*=7.233f;}curve.keys=keys;P(s,"dragCurve").animationCurveValue=curve;}
                P(s,"gLimit").floatValue=4;P(s,"maxTurnRate").floatValue=20;s.ApplyModifiedPropertiesWithoutUndo();
                foreach(var t in g.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Apex6_Visual"))t.localScale=Vector3.one*.8f;
                var haze=g.transform.Find("Apex6_DryJetHeatHaze");haze.localScale=Vector3.one*.45f;
                var ps=haze.GetComponent<ParticleSystem>();var main=ps.main;main.startLifetime=.65f;main.startSpeed=5;main.startSize=.22f;
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }finally{PrefabUtility.UnloadPrefabContents(g);}
            var info=new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponInfo>(R+"WI_Apex6_"+variant+".asset"));P(info,"costPerRound").floatValue=.55f;P(info,"targetRequirements.maxSpeed").floatValue=100;P(info,"targetRequirements.minRange").floatValue=500;info.ApplyModifiedPropertiesWithoutUndo();
            var def=new SerializedObject(AssetDatabase.LoadAssetAtPath<MissileDefinition>(R+"Def_Apex6_"+variant+".asset"));P(def,"value").floatValue=.55f;P(def,"length").floatValue=1.72f;P(def,"width").floatValue=1.72f;P(def,"height").floatValue=.29f;def.ApplyModifiedPropertiesWithoutUndo();
        }
        var airPath=R+"Apex6_AirRail.prefab";var air=PrefabUtility.LoadPrefabContents(airPath);
        try{var s=new SerializedObject(air.GetComponentInChildren<MountedMissile>(true));P(s,"railSpeed").floatValue=4;P(s,"railDelay").floatValue=.15f;s.ApplyModifiedPropertiesWithoutUndo();foreach(var t in air.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Apex6_Visual"))t.localScale=Vector3.one*.8f;PrefabUtility.SaveAsPrefabAsset(air,airPath);}finally{PrefabUtility.UnloadPrefabContents(air);}
        var telPath=R+"Apex6_TEL.prefab";var tel=PrefabUtility.LoadPrefabContents(telPath);
        try
        {
            var turret=tel.GetComponentInChildren<Turret>(true);var ts=new SerializedObject(turret);var elevation=(Transform)P(ts,"elevationTransform").objectReferenceValue;
            var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blueprinter/Mods/R460Poseidon/R460_Poseidon_TEL.prefab");
            // The validation project may not import the other mod; load its native controller from the original asset path when available.
            if(donor==null)throw new Exception("Poseidon donor must be available for native FireControl copy");
            var control=tel.GetComponent<FireControl>()??tel.AddComponent<FireControl>();EditorUtility.CopySerialized(donor.GetComponent<FireControl>(),control);
            var cs=new SerializedObject(control);P(cs,"attachedUnit").objectReferenceValue=tel.GetComponent<GroundVehicle>();P(cs,"radar").objectReferenceValue=null;
            var turrets=P(cs,"turrets");turrets.arraySize=1;turrets.GetArrayElementAtIndex(0).objectReferenceValue=turret;P(cs,"availableTurrets").arraySize=0;
            var deploy=P(cs,"deployables");deploy.arraySize=1;var d=deploy.GetArrayElementAtIndex(0);d.FindPropertyRelative("transform").objectReferenceValue=elevation;d.FindPropertyRelative("stowedAngle").vector3Value=Vector3.zero;d.FindPropertyRelative("deployedAngle").vector3Value=new Vector3(-25,0,0);d.FindPropertyRelative("deployRate").floatValue=.2f;
            P(cs,"salvoInterval").floatValue=1.25f;cs.ApplyModifiedPropertiesWithoutUndo();
            P(ts,"targetAcquisitionMode").enumValueIndex=4;P(ts,"fireControl").objectReferenceValue=control;P(ts,"firesWithoutAiming").boolValue=true;ts.ApplyModifiedPropertiesWithoutUndo();
            foreach(var t in tel.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("LoadedDrone_")))t.localScale=Vector3.one*.8f;
            PrefabUtility.SaveAsPrefabAsset(tel,telPath);
        }finally{PrefabUtility.UnloadPrefabContents(tel);}
        var rail=AssetDatabase.LoadAssetAtPath<Material>(R+"Materials/M_Apex6_Container.mat");rail.SetColor("_BaseColor",new Color(.48f,.47f,.44f));rail.SetColor("_Color",new Color(.48f,.47f,.44f));EditorUtility.SetDirty(rail);
        AssetDatabase.SaveAssets();Apex6Setup.Validate();Debug.Log("APEX6_POLISH_OK");
    }
}
