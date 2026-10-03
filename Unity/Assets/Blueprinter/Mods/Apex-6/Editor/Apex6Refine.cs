using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Apex6Refine
{
    const string Root="Assets/Blueprinter/Mods/Apex-6/";
    const string Donor="Assets/Blueprinter/_donotship/";
    static Material railMaterial;
    static SerializedProperty P(SerializedObject s,string name)=>s.FindProperty(name)??throw new Exception("Missing field "+name);
    static void Float(UnityEngine.Object o,string name,float v){var s=new SerializedObject(o);P(s,name).floatValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Ref(UnityEngine.Object o,string name,UnityEngine.Object v){var s=new SerializedObject(o);P(s,name).objectReferenceValue=v;s.ApplyModifiedPropertiesWithoutUndo();}
    static void Box(Transform parent,string name,Vector3 pos,Vector3 size)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=railMaterial;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    static Transform Rail(Transform parent,int index,Vector3 pos)
    {
        var rail=new GameObject("LaunchRail_"+(index+1));rail.transform.SetParent(parent,false);rail.transform.localPosition=pos;
        Box(rail.transform,"Guide_L",new Vector3(-.105f,0,0),new Vector3(.045f,.065f,2.6f));
        Box(rail.transform,"Guide_R",new Vector3(.105f,0,0),new Vector3(.045f,.065f,2.6f));
        for(int j=0;j<3;j++) Box(rail.transform,"Crossmember_"+j,new Vector3(0,-.055f,(j-1)*1.05f),new Vector3(.38f,.045f,.08f));
        var point=new GameObject("LaunchPoint_"+(index+1));point.transform.SetParent(rail.transform,false);point.transform.localPosition=new Vector3(0,.14f,2.3f);
        return point.transform;
    }
    static void Materials()
    {
        // These are the authored Principled BSDF values of the supplied model.
        var names=new[]{"M_Engine_HeatShield","M_Nozzle_DarkMetal","M_Sensor_Optic","M_Stealth_Hull","M_Turbine_Blades_Radial"};
        var colors=new[]{new Color(.96f,.46f,.26f),new Color(.8f,.8f,.8f),new Color(.8f,.5f,.1f),new Color(.8f,.8f,.8f),new Color(.8f,.8f,.8f)};
        var metallic=new[]{1f,1f,.95f,0f,.95f};var roughness=new[]{.1f,.12f,.08f,.82f,.22f};
        for(int i=0;i<names.Length;i++)
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/M_Apex6_"+names[i]+".mat");
            if(m==null)throw new Exception("Missing model material "+names[i]);
            m.SetColor("_BaseColor",colors[i]);m.SetColor("_Color",colors[i]);m.SetFloat("_Metallic",metallic[i]);m.SetFloat("_Smoothness",1-roughness[i]);EditorUtility.SetDirty(m);
        }
    }
    static void Drone(string variant)
    {
        var path=Root+"Apex6_"+variant+".prefab";var g=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var visual=g.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Apex6_Visual");visual.localScale=Vector3.one*.72f;
            var c=g.GetComponent<CapsuleCollider>();c.height=1.55f;c.radius=.125f;c.center=Vector3.zero;
            foreach(var camera in g.GetComponentsInChildren<Camera>(true))camera.enabled=false;
            var m=g.GetComponent<Missile>();var so=new SerializedObject(m);var stages=P(so,"motors");
            var oldHaze=g.transform.Find("Apex6_DryJetHeatHaze");if(oldHaze!=null)UnityEngine.Object.DestroyImmediate(oldHaze.gameObject);
            var donor=AssetDatabase.LoadAssetAtPath<GameObject>(Donor+"GameObject/Multirole1_PLACEHOLDER.prefab");
            var haze=donor.GetComponentsInChildren<ParticleSystem>(true).First(p=>p.name.IndexOf("haze",StringComparison.OrdinalIgnoreCase)>=0);
            var effect=UnityEngine.Object.Instantiate(haze.gameObject,g.transform);effect.name="Apex6_DryJetHeatHaze";
            var nozzle=visual.GetComponentsInChildren<Renderer>(true).First(r=>r.name.Contains("Tempered_Nozzle"));
            effect.transform.position=nozzle.bounds.center;effect.transform.localRotation=Quaternion.Euler(0,180,0);effect.transform.localScale=Vector3.one*.2f;
            var ps=effect.GetComponent<ParticleSystem>();var main=ps.main;main.playOnAwake=false;main.loop=true;main.startLifetime=.45f;main.startSpeed=8;main.startSize=.16f;
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var oldJet=g.transform.Find("Apex6_TurbojetLoop");if(oldJet!=null)UnityEngine.Object.DestroyImmediate(oldJet.gameObject);
            var sound=new GameObject("Apex6_TurbojetLoop");sound.transform.SetParent(g.transform,false);sound.transform.position=nozzle.bounds.center;
            var audio=sound.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Donor+"AudioClip/CruiseMissileRunning_PLACEHOLDER.ogg");
            if(audio.clip==null)throw new Exception("Missing cruise turbojet sound");
            audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.65f;audio.pitch=1.15f;audio.minDistance=12;audio.maxDistance=900;
            audio.rolloffMode=AudioRolloffMode.Logarithmic;audio.dopplerLevel=1;
            for(int i=0;i<stages.arraySize;i++)
            {
                var stage=stages.GetArrayElementAtIndex(i);var particles=stage.FindPropertyRelative("particleSystems");particles.arraySize=1;particles.GetArrayElementAtIndex(0).objectReferenceValue=ps;
                var sounds=stage.FindPropertyRelative("audioSources");sounds.arraySize=1;sounds.GetArrayElementAtIndex(0).objectReferenceValue=audio;
                stage.FindPropertyRelative("startupSource").objectReferenceValue=null;
                stage.FindPropertyRelative("lights").arraySize=0;stage.FindPropertyRelative("trailEmitters").arraySize=0;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            // Detach unused donor engine sources so only the explicitly assigned turbojet is audible.
            foreach(var a in g.GetComponentsInChildren<AudioSource>(true))if(a!=audio){a.enabled=false;a.playOnAwake=false;}
            PrefabUtility.SaveAsPrefabAsset(g,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(g);}
        var def=AssetDatabase.LoadAssetAtPath<MissileDefinition>(Root+"Def_Apex6_"+variant+".asset");Float(def,"length",1.55f);Float(def,"width",1.55f);Float(def,"height",.26f);Float(def,"value",1);
        var info=AssetDatabase.LoadAssetAtPath<WeaponInfo>(Root+"WI_Apex6_"+variant+".asset");Float(info,"costPerRound",1);
    }
    static void TEL()
    {
        var path=Root+"Apex6_TEL.prefab";var g=PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach(var lod in g.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(lod);
            var launcher=g.GetComponentInChildren<MissileLauncher>(true);var turret=launcher.GetComponentInParent<Turret>();
            var elevation=(Transform)P(new SerializedObject(turret),"elevationTransform").objectReferenceValue;
            foreach(var old in g.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Apex6_SixTubeCassette"||t.name=="Apex6_SixRailRack").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var rack=new GameObject("Apex6_SixRailRack");rack.transform.SetParent(elevation,false);rack.transform.localPosition=new Vector3(0,.75f,-.3f);
            var so=new SerializedObject(launcher);var points=P(so,"launchTransforms");points.arraySize=6;
            for(int i=0;i<6;i++)points.GetArrayElementAtIndex(i).objectReferenceValue=Rail(rack.transform,i,new Vector3(i%2==0?-.84f:.84f,i/2*.58f,0));
            for(int side=-1;side<=1;side+=2)
            {
                Box(rack.transform,"RearUpright_"+side,new Vector3(side*1.0f,.5f,-1.08f),new Vector3(.065f,1.35f,.065f));
                Box(rack.transform,"FrontUpright_"+side,new Vector3(side*1.0f,.5f,.85f),new Vector3(.065f,1.35f,.065f));
            }
            Box(rack.transform,"RackBase",new Vector3(0,-.15f,-.1f),new Vector3(2.12f,.10f,2.5f));
            P(so,"launchSound").objectReferenceValue=null;P(so,"launchParticles").objectReferenceValue=null;
            P(so,"railLength").floatValue=2.6f;P(so,"railSpeed").floatValue=35;
            P(so,"ejectionVelocity").vector3Value=new Vector3(0,0,35);so.ApplyModifiedPropertiesWithoutUndo();
            foreach(var ps in elevation.GetComponentsInChildren<ParticleSystem>(true))ps.gameObject.SetActive(false);
            foreach(var collider in elevation.GetComponentsInChildren<BoxCollider>(true)){collider.center=new Vector3(0,1.32f,-.35f);collider.size=new Vector3(2.2f,1.55f,2.65f);}
            Float(turret,"minElevation",25);Float(turret,"maxElevation",45);elevation.localRotation=Quaternion.identity;
            PrefabUtility.SaveAsPrefabAsset(g,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(g);}
        var def=AssetDatabase.LoadAssetAtPath<VehicleDefinition>(Root+"Def_Apex6_TEL.asset");
        var ds=new SerializedObject(def);P(ds,"description").stringValue="Six-rail Apex-6 jet attack drone carrier on a vanilla MSV chassis. Sequential launch interval: 1.25 s. Drone cost: 1 million.";ds.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Pod()
    {
        var path=Root+"Apex6_AirRail.prefab";
        if(!File.Exists(path))
        {
            var error=AssetDatabase.MoveAsset(Root+"Apex6_TwinTube.prefab",path);if(!string.IsNullOrEmpty(error))throw new Exception(error);
            error=AssetDatabase.MoveAsset(Root+"WM_Apex6_TwinTube.asset",Root+"WM_Apex6_AirRail.asset");if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var g=PrefabUtility.LoadPrefabContents(path);
        try
        {
            g.name="Apex6_AirRail";
            foreach(var lod in g.GetComponentsInChildren<LODGroup>(true))UnityEngine.Object.DestroyImmediate(lod);
            var missiles=g.GetComponentsInChildren<MountedMissile>(true);
            foreach(var extra in missiles.Skip(1))UnityEngine.Object.DestroyImmediate(extra.gameObject);
            foreach(var old in g.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Tube_")||t.name=="Apex6_AirRailHardware").ToArray())UnityEngine.Object.DestroyImmediate(old.gameObject);
            var m=missiles[0];m.name="Apex6_ReadyDrone";m.transform.localPosition=new Vector3(0,-.28f,0);
            var visual=m.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Apex6_Visual");visual.localScale=Vector3.one*.72f;
            Float(m,"railLength",1.65f);Float(m,"railSpeed",20);
            var hardware=new GameObject("Apex6_AirRailHardware");hardware.transform.SetParent(g.transform,false);
            Rail(hardware.transform,0,new Vector3(0,-.38f,0));
            var adapter=g.transform.Find("PylonAdapter");adapter.localScale=new Vector3(.42f,.12f,.65f);adapter.localPosition=new Vector3(0,-.06f,0);
            PrefabUtility.SaveAsPrefabAsset(g,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(g);}
        var mount=AssetDatabase.LoadAssetAtPath<WeaponMount>(Root+"WM_Apex6_AirRail.asset");
        var so=new SerializedObject(mount);P(so,"jsonKey").stringValue="Apex6_AirRail";P(so,"mountName").stringValue="Apex-6 Single Rail";P(so,"ammo").intValue=1;
        P(so,"emptyMass").floatValue=20;P(so,"drag").floatValue=.04f;P(so,"emptyDrag").floatValue=.01f;P(so,"RCS").floatValue=.01f;P(so,"emptyRCS").floatValue=.005f;so.ApplyModifiedPropertiesWithoutUndo();
        var op=AssetDatabase.LoadAssetAtPath<Blueprinter.OpAddWeaponToHardpoint>(Root+"Op_Apex6_Aircraft.asset");op.weaponJsonKey="Apex6_AirRail";EditorUtility.SetDirty(op);
    }
    [MenuItem("Blueprinter/Apex-6/Apply rail and engine refinement")]
    public static void Run()
    {
        railMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/M_Apex6_Container.mat");
        Materials();Drone("Air");Drone("Ground");TEL();Pod();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Apex6FlightSetup.Run();Apex6Setup.Preview();Apex6Setup.Validate();Debug.Log("APEX6_REFINEMENT_OK");
    }
}
