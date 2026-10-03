using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// One-shot requests let the already-open editor generate assets without changing
// the active scene. All generated assets are confined to this mod directory.
[InitializeOnLoad]
public static class Apex6PalletSetup
{
    const string R = "Assets/Blueprinter/Mods/Apex-6/";
    const string D = "Assets/Blueprinter/_donotship/";
    static float Mass;
    static int Count;
    static string PalletName;
    static GameObject PayloadPrefab;
    static float Width=>2.5f;
    static float Length=>Count==8?4.1f:3.2f;
    static float Height=>Count==8?1.8f:2.5f;
    static Apex6PalletSetup() { EditorApplication.update += CheckRequest; }
    static void CheckRequest()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(R+"Tools~/visual-repair.request"))
        {
            File.Delete(R+"Tools~/visual-repair.request");
            try { ApexVisualAudit.Repair(); File.WriteAllText(R+"Tools~/visual-repair.result","APEX_VISUAL_REPAIR_OK"); }
            catch(Exception ex){File.WriteAllText(R+"Tools~/visual-repair.result",ex.ToString());Debug.LogException(ex);}
            return;
        }
        if (File.Exists(R+"Tools~/visual-inspect.request"))
        {
            File.Delete(R+"Tools~/visual-inspect.request");
            try { ApexVisualAudit.Run(); }
            catch(Exception ex){File.WriteAllText(R+"Validation~/visual-audit.txt",ex.ToString());Debug.LogException(ex);}
            return;
        }
        string packageRequest=R+"Tools~/pallet-package.request";
        if(File.Exists(packageRequest) && !File.Exists(R+"Tools~/pallet-setup.request"))
        {
            File.Delete(packageRequest);
            try
            {
                const string output=R+"Delivery~/Swarm";
                Blueprinter.ModBuilder.Build("Apex-6","Apex-6","1.2.0",output);
                string bundle=output+"/Apex-6_1.2.0.nobp";
                if(!File.Exists(bundle))throw new Exception("Pallet bundle not produced");
                File.Copy(bundle,R+"Tools~/Runtime/Apex6AmmoVisuals/Bundle/Apex-6.nobp",true);
                File.WriteAllText(R+"Tools~/pallet-package.result","APEX_SWARM_BUNDLE_OK "+new FileInfo(bundle).Length);
            }
            catch(Exception ex){Debug.LogException(ex);File.WriteAllText(R+"Tools~/pallet-package.result",ex.ToString());}
            return;
        }
        string request = R + "Tools~/pallet-setup.request";
        if (!File.Exists(request)) return;
        bool package=File.ReadAllText(request).Trim()=="setup-and-package";
        File.Delete(request);
        try { Run(); File.WriteAllText(R + "Tools~/pallet-setup.result", "APEX6_PALLET_SETUP_OK"); if(package)File.WriteAllText(packageRequest,"build"); }
        catch (Exception ex) { Debug.LogException(ex); File.WriteAllText(R + "Tools~/pallet-setup.result", ex.ToString()); }
    }
    static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new Exception(path);
    static T Copy<T>(string source, string name) where T : UnityEngine.Object
    {
        var path = R + name + ".asset";
        if (!File.Exists(path) && !AssetDatabase.CopyAsset(source, path)) throw new Exception("Cannot copy " + source);
        var value = Load<T>(path); value.name = name; return value;
    }
    static void Edit(UnityEngine.Object value, Action<SerializedObject> edit)
    { var s = new SerializedObject(value); edit(s); s.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(value); }
    static SerializedProperty P(SerializedObject s, string name) => s.FindProperty(name) ?? throw new Exception(s.targetObject.name + ": " + name);
    static GameObject Clone(string path)
    {
        var g = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction); return g;
    }
    public static void Run()
    {
        Apex6SwarmVariants.CreateDronesAndTEL();
        BuildVariant("Apex6_Pallet8",8,1640,Load<GameObject>(R+"Apex6_PalletDrone.prefab"));
        BuildVariant("Apex8_Pallet4",4,1240,Load<WeaponInfo>(R+"WI_Apex6_Air.asset").weaponPrefab);
        Debug.Log("APEX6_SWARM_VARIANTS_OK: 8 x Apex-6 pallet, 4 x Apex-8 pallet, 4 x Apex-8 TEL");
    }
    static void BuildVariant(string name,int count,float mass,GameObject payload)
    {
        PalletName=name;Count=count;Mass=mass;PayloadPrefab=payload;
        var label=count+" x "+(name.StartsWith("Apex8")?"Apex-8":"Apex-6");
        var def = Copy<UnitDefinition>(D + "MonoBehaviour/MunitionsPallet2_PLACEHOLDER.asset", "Def_"+PalletName);
        Edit(def, s => {
            P(s,"jsonKey").stringValue=PalletName; P(s,"unitName").stringValue=label+" Drone Pallet";
            P(s,"description").stringValue=label+". Parachute deployment; 7 s delay; 0.8 s sequential release.";
            P(s,"mass").floatValue=Mass; P(s,"length").floatValue=Length; P(s,"width").floatValue=Width; P(s,"height").floatValue=Height;
            P(s,"spawnOffset").vector3Value=new Vector3(0,Height/2,0); P(s,"value").floatValue=count;
        });
        var pallet = Clone(D + "GameObject/MunitionsPallet2_PLACEHOLDER.prefab");
        try
        {
            pallet.name=PalletName;
            foreach(var lod in pallet.GetComponentsInChildren<LODGroup>()) UnityEngine.Object.DestroyImmediate(lod);
            foreach(var renderer in pallet.GetComponentsInChildren<Renderer>()) renderer.enabled=false;
            // Native Container expects a root Renderer on disable; keep it present.
            var trigger = pallet.transform.Find("triggerZone");
            foreach(var behaviour in pallet.GetComponentsInChildren<MonoBehaviour>())
            {
                if(behaviour==null)continue;
                var s=new SerializedObject(behaviour);
                if(s.FindProperty("Capacity")!=null && s.FindProperty("Range")!=null) UnityEngine.Object.DestroyImmediate(behaviour);
            }
            if(trigger!=null) UnityEngine.Object.DestroyImmediate(trigger.gameObject);
            var unit=pallet.GetComponent<Container>();
            Edit(unit,s=>{P(s,"definition").objectReferenceValue=def; P(s,"collisionTriggerZone").objectReferenceValue=null;});
            var rb=pallet.GetComponent<Rigidbody>(); rb.mass=Mass;
            var box=pallet.GetComponent<BoxCollider>(); box.size=new Vector3(Width,Height,Length); box.center=Vector3.zero;
            foreach(var part in pallet.GetComponentsInChildren<UnitPart>()) Edit(part,s=>{
                P(s,"mass").floatValue=Mass;
                P(s,"damageEffects").arraySize=0;
            });
            var frame = MakeFrame(pallet.transform);
            var prefab=PrefabUtility.SaveAsPrefabAsset(pallet,R+PalletName+".prefab");
            Edit(def,s=>P(s,"unitPrefab").objectReferenceValue=prefab);
        }
        finally { UnityEngine.Object.DestroyImmediate(pallet); }
        var info=Copy<WeaponInfo>(D+"MonoBehaviour/MunitionsSmallPallet_info_PLACEHOLDER.asset","WI_"+PalletName);
        Edit(info,s=>{
            P(s,"weaponName").stringValue=label+" Drone Pallet";P(s,"shortName").stringValue=label;
            P(s,"description").stringValue="Ramp-dropped "+label+"; 7 s separation delay, 0.8 s launch interval.";
            P(s,"massPerRound").floatValue=Mass; P(s,"costPerRound").floatValue=count*(name.StartsWith("Apex8")?1f:.55f);
            P(s,"rearmGround").boolValue=false;
            // Cargo must be droppable at aircraft speed and normal airdrop altitude.
            var req=P(s,"targetRequirements");req.FindPropertyRelative("maxAltitude").floatValue=10000;
            req.FindPropertyRelative("maxSpeed").floatValue=1000;
        });
        var mount=Copy<WeaponMount>(D+"MonoBehaviour/MunitionsSmallPallet2x1_PLACEHOLDER.asset","WM_"+PalletName);
        var mounted=Clone(D+"GameObject/MunitionsSmallPallet2x1_PLACEHOLDER.prefab");
        try
        {
            mounted.name=PalletName+"Mount";
            foreach(var renderer in mounted.GetComponentsInChildren<Renderer>())renderer.enabled=false;
            var cargo=mounted.GetComponentInChildren<MountedCargo>();
            Edit(cargo,s=>{
                P(s,"cargo").objectReferenceValue=def;P(s,"info").objectReferenceValue=info;
                P(s,"railDelay").floatValue=0;P(s,"railSpeed").floatValue=4;
                P(s,"damageEffects").arraySize=0;
            });
            cargo.GetComponent<BoxCollider>().size=new Vector3(Width,Height,Length);
            cargo.transform.localPosition=new Vector3(0,Height/2,0);
            MakeFrame(cargo.transform);
            var prefab=PrefabUtility.SaveAsPrefabAsset(mounted,R+PalletName+"Mount.prefab");
            Edit(mount,s=>{P(s,"prefab").objectReferenceValue=prefab;P(s,"info").objectReferenceValue=info;
                P(s,"jsonKey").stringValue=PalletName+"Mount";P(s,"mountName").stringValue=label+" Drone Pallet";
                P(s,"mass").floatValue=Mass;
            });
        }
        finally { UnityEngine.Object.DestroyImmediate(mounted); }
        CreateCargoOperation();
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log(PalletName+"_SETUP_OK");
    }
    static Transform MakeFrame(Transform parent)
    {
        var frame=new GameObject("PalletFrame").transform;frame.SetParent(parent,false);
        var material=Load<Material>(R+"Materials/M_Apex6_M_Stealth_Hull.mat");
        Action<string,Vector3,Vector3> beam=(name,pos,size)=>{
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(frame,false);
            box.transform.localPosition=pos;box.transform.localScale=size;
            box.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        };
        for(int x=-1;x<=1;x+=2)
        {
            beam("SideRail",new Vector3(x*(Width/2-.06f),0,0),new Vector3(.12f,.12f,Length));
            beam("TopRail",new Vector3(x*(Width/2-.06f),Height/2-.06f,0),new Vector3(.12f,.12f,Length));
            for(int z=-1;z<=1;z+=2)beam("Upright",new Vector3(x*(Width/2-.06f),0,z*(Length/2-.06f)),new Vector3(.12f,Height,.12f));
        }
        for(int z=-1;z<=1;z++)beam("Crossbar",new Vector3(0,Height/2-.06f,z*(Length/2-.12f)),new Vector3(Width,.12f,.12f));
        var dronePrefab=PayloadPrefab;
        if(dronePrefab==null)throw new Exception("Aircraft WeaponInfo prefab missing");
        for(int i=0;i<Count;i++)
        {
            var cell=new GameObject("DroneCell_"+(i+1)).transform;cell.SetParent(frame,false);
            cell.localPosition=Count==8?new Vector3(0,-.65f+(i/2)*.42f,i%2==0?-.98f:.98f):new Vector3(0,-.9f+i*.6f,0);
            var model=Apex6SwarmVariants.LoadedVisual(dronePrefab,cell,"LoadedDrone");
            var renderers=model.GetComponentsInChildren<Renderer>();
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            if(i==0)Debug.Log(PalletName+" LOADED_MODEL_BOUNDS "+bounds.size);
        }
        return frame;
    }
    static void CreateCargoOperation()
    {
        var entries=new List<Tuple<string,int[]>>();
        foreach(var guid in AssetDatabase.FindAssets("t:AircraftDefinition",new[]{D+"MonoBehaviour"}))
        {
            var def=Load<AircraftDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if(def.unitPrefab==null)continue;
            if(def.unitPrefab.GetComponentInChildren<CargoRamp>(true)==null)continue;
            var manager=def.unitPrefab.GetComponentInChildren<WeaponManager>(true);
            if(manager==null || manager.hardpointSets==null)continue;
            var indices=Enumerable.Range(0,manager.hardpointSets.Length).Where(i=>
                manager.hardpointSets[i]!=null && manager.hardpointSets[i].weaponOptions!=null &&
                manager.hardpointSets[i].weaponOptions.Any(m=> m!=null && m.prefab!=null &&
                    P(new SerializedObject(m),"Cargo").boolValue && m.mass>=Mass &&
                    m.prefab.GetComponentInChildren<MountedCargo>(true)!=null)).ToArray();
            if(indices.Length>0)entries.Add(Tuple.Create(def.jsonKey,indices));
        }
        if(entries.Count==0)throw new Exception("No aircraft cargo stations found for native pallet donor");
        var op=Copy<ScriptableObject>(R+"Op_Apex6_Aircraft.asset","Op_"+PalletName);
        Edit(op,s=>{
            P(s,"weaponJsonKey").stringValue=PalletName+"Mount";
            var array=P(s,"aircraft");array.arraySize=entries.Count;
            for(int i=0;i<entries.Count;i++)
            {
                var item=array.GetArrayElementAtIndex(i);item.FindPropertyRelative("aircraftJsonKey").stringValue=entries[i].Item1;
                var indices=item.FindPropertyRelative("hardpointIndices");indices.arraySize=entries[i].Item2.Length;
                for(int j=0;j<indices.arraySize;j++)indices.GetArrayElementAtIndex(j).intValue=entries[i].Item2[j];
            }
        });
        File.WriteAllLines(R+"Tools~/"+PalletName+"-aircraft.txt",entries.Select(e=>e.Item1+": "+string.Join(",",e.Item2)));
    }
    public static void Validate()
    {
        var def=Load<UnitDefinition>(R+"Def_"+PalletName+".asset");
        var pallet=def.unitPrefab;
        if(pallet.GetComponent<Container>()==null || pallet.GetComponent<Rigidbody>().mass!=Mass)throw new Exception("Pallet unit invalid");
        for(int i=1;i<=Count;i++)if(pallet.transform.Find("PalletFrame/DroneCell_"+i)==null)throw new Exception("Cell missing "+i);
        if(pallet.transform.Find("PalletFrame/DroneCell_"+(Count+1))!=null)throw new Exception("Extra cell");
        var bounds=new List<Bounds>();
        for(int i=1;i<=Count;i++)
        {
            var renderers=pallet.transform.Find("PalletFrame/DroneCell_"+i).GetComponentsInChildren<Renderer>();
            var body=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))body.Encapsulate(renderer.bounds);
            if(bounds.Any(other=>other.Intersects(body)))throw new Exception("Loaded drones overlap in "+PalletName+" cell "+i);
            bounds.Add(body);
            var center=pallet.transform.InverseTransformPoint(body.center);
            if(Mathf.Abs(center.x)+body.extents.x>Width/2 || Mathf.Abs(center.y)+body.extents.y>Height/2 ||
                Mathf.Abs(center.z)+body.extents.z>Length/2)throw new Exception("Drone exceeds pallet envelope: "+PalletName+" cell "+i);
        }
        var mount=Load<WeaponMount>(R+"WM_"+PalletName+".asset");
        var cargo=mount.prefab.GetComponentInChildren<MountedCargo>();
        if(cargo.cargo!=def)throw new Exception("Cargo definition mismatch");
        if(new SerializedObject(pallet.GetComponent<Container>()).FindProperty("parachuteSystem").objectReferenceValue==null)throw new Exception("Parachute missing");
        Debug.Log("APEX6_PALLET_VALIDATED: "+PalletName+", "+Count+" cells, native parachute, cargo mount, "+Mass+" kg");
    }
}

public static class Apex6SwarmVariants
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    static SerializedProperty P(SerializedObject s,string name)=>s.FindProperty(name)??throw new Exception(name);
    static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path)??throw new Exception(path);
    static T Copy<T>(string source,string name) where T:UnityEngine.Object
    {
        string path=R+name+".asset";
        if(!System.IO.File.Exists(path)&&!AssetDatabase.CopyAsset(source,path))throw new Exception(source);
        var a=Load<T>(path);a.name=name;return a;
    }
    static GameObject Clone(GameObject source)
    {
        var g=(GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);return g;
    }
    static void Edit(UnityEngine.Object o,Action<SerializedObject> action)
    {var s=new SerializedObject(o);action(s);s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(o);}
    public static void CreateDronesAndTEL()
    {
        var airInfo=Load<WeaponInfo>(R+"WI_Apex6_Air.asset");
        Edit(airInfo,s=>{P(s,"weaponName").stringValue="Apex-8";P(s,"shortName").stringValue="APX-8";});
        Edit(Load<WeaponMount>(R+"WM_Apex6_AirRail.asset"),s=>P(s,"mountName").stringValue="Apex-8 Single Rail");
        CreateSmallAirDrone();
        CreateLargeGroundDrone();
        CreateLargeTEL();
        AssetDatabase.SaveAssets();
    }
    static void CreateSmallAirDrone()
    {
        var def=Copy<MissileDefinition>(R+"Def_Apex6_Ground.asset","Def_Apex6_PalletDrone");
        var info=Copy<WeaponInfo>(R+"WI_Apex6_Ground.asset","WI_Apex6_PalletDrone");
        var g=Clone(Load<GameObject>(R+"Apex6_Ground.prefab"));
        try
        {
            g.name="Apex6_PalletDrone";
            var missile=g.GetComponent<Missile>();
            // A pallet drone has no solid booster; the smaller TEL variant retains its own.
            foreach(var booster in g.GetComponentsInChildren<VLSBooster>(true))UnityEngine.Object.DestroyImmediate(booster.gameObject);
            foreach(var behaviour in g.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(behaviour==null)continue;
                var s=new SerializedObject(behaviour);var booster=s.FindProperty("booster");
                if(booster!=null){booster.objectReferenceValue=null;s.ApplyModifiedPropertiesWithoutUndo();}
            }
            Edit(missile,s=>{
                P(s,"definition").objectReferenceValue=def;P(s,"info").objectReferenceValue=info;
                P(s,"motors").GetArrayElementAtIndex(0).FindPropertyRelative("delayTimer").floatValue=0;
            });
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,R+"Apex6_PalletDrone.prefab");
            Edit(def,s=>{P(s,"jsonKey").stringValue="Apex6_PalletDrone";P(s,"unitName").stringValue="Apex-6";
                P(s,"description").stringValue="Small Apex-6 drone deployed from an eight-drone pallet.";P(s,"unitPrefab").objectReferenceValue=prefab;
            });
            Edit(info,s=>{P(s,"weaponPrefab").objectReferenceValue=prefab;P(s,"weaponName").stringValue="Apex-6";P(s,"shortName").stringValue="APX-6";});
        }
        finally{UnityEngine.Object.DestroyImmediate(g);}
    }
    static void CreateLargeGroundDrone()
    {
        var air=Load<WeaponInfo>(R+"WI_Apex6_Air.asset").weaponPrefab;
        var airDef=air.GetComponent<Missile>().definition;
        var def=Copy<MissileDefinition>(AssetDatabase.GetAssetPath(airDef),"Def_Apex8_Ground");
        var info=Copy<WeaponInfo>(R+"WI_Apex6_Air.asset","WI_Apex8_Ground");
        var g=Clone(air);
        try
        {
            g.name="Apex8_Ground";
            var missile=g.GetComponent<Missile>();
            Edit(missile,s=>{P(s,"definition").objectReferenceValue=def;P(s,"info").objectReferenceValue=info;
                P(s,"motors").GetArrayElementAtIndex(0).FindPropertyRelative("delayTimer").floatValue=1.05f;
            });
            var source=Load<GameObject>(R+"Apex6_Ground.prefab").GetComponentInChildren<VLSBooster>(true);
            var booster=UnityEngine.Object.Instantiate(source.gameObject,g.transform).GetComponent<VLSBooster>();
            booster.name="Apex8_SolidBooster";
            Edit(booster,s=>{P(s,"missile").objectReferenceValue=missile;P(s,"thrust").floatValue=13000;});
            var seeker=g.GetComponent<OpticalSeekerCruiseMissile>();
            Edit(seeker,s=>P(s,"booster").objectReferenceValue=booster);
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,R+"Apex8_Ground.prefab");
            Edit(def,s=>{P(s,"jsonKey").stringValue="Apex8_Ground";P(s,"unitName").stringValue="Apex-8";
                P(s,"description").stringValue="Larger Apex-8 drone with a one-second detachable ground-launch booster.";P(s,"unitPrefab").objectReferenceValue=prefab;
            });
            Edit(info,s=>{P(s,"weaponPrefab").objectReferenceValue=prefab;P(s,"weaponName").stringValue="Apex-8";P(s,"shortName").stringValue="APX-8";});
        }
        finally{UnityEngine.Object.DestroyImmediate(g);}
    }
    static void Beam(Transform parent,string name,Vector3 position,Vector3 size)
    {
        var box=GameObject.CreatePrimitive(PrimitiveType.Cube);box.name=name;box.transform.SetParent(parent,false);
        box.transform.localPosition=position;box.transform.localScale=size;
        box.GetComponent<Renderer>().sharedMaterial=Load<Material>(R+"Materials/M_Apex6_M_Stealth_Hull.mat");
        UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
    }
    internal static GameObject LoadedVisual(GameObject drone,Transform parent,string name)
    {
        var visual=drone.transform.Find("Apex6_Visual");
        var g=UnityEngine.Object.Instantiate(visual.gameObject,parent);g.name=name;g.transform.localPosition=Vector3.zero;
        foreach(var animation in g.GetComponentsInChildren<Animation>(true))
        {
            if(animation.clip!=null)animation.clip.SampleAnimation(animation.gameObject,0);
            UnityEngine.Object.DestroyImmediate(animation);
        }
        return g;
    }
    static void CreateLargeTEL()
    {
        var def=Copy<VehicleDefinition>(R+"Def_Apex6_TEL.asset","Def_Apex8_TEL");
        var drone=Load<MissileDefinition>(R+"Def_Apex8_Ground.asset");
        var info=Load<WeaponInfo>(R+"WI_Apex8_Ground.asset");
        var g=Clone(Load<GameObject>(R+"Apex6_TEL.prefab"));
        try
        {
            g.name="Apex8_TEL";
            Edit(g.GetComponent<GroundVehicle>(),s=>P(s,"definition").objectReferenceValue=def);
            var launcher=g.GetComponentInChildren<MissileLauncher>(true);
            var turret=launcher.GetComponentInParent<Turret>();
            var elevation=(Transform)P(new SerializedObject(turret),"elevationTransform").objectReferenceValue;
            var old=g.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Apex6_SixRailRack");
            var position=old.localPosition;UnityEngine.Object.DestroyImmediate(old.gameObject);
            var rack=new GameObject("Apex8_FourRailRack").transform;rack.SetParent(elevation,false);rack.localPosition=position;
            var points=new Transform[4];
            for(int i=0;i<4;i++)
            {
                var rail=new GameObject("LaunchRail_"+(i+1)).transform;rail.SetParent(rack,false);
                rail.localPosition=new Vector3(0,i*.63f,0);
                Beam(rail,"Guide_L",new Vector3(-.2f,0,0),new Vector3(.06f,.08f,3.1f));
                Beam(rail,"Guide_R",new Vector3(.2f,0,0),new Vector3(.06f,.08f,3.1f));
                var loaded=new GameObject("LoadedDrone_"+(i+1)).transform;loaded.SetParent(rail,false);loaded.localPosition=new Vector3(0,.24f,0);
                LoadedVisual(drone.unitPrefab,loaded,"Apex6_Visual");
                var point=new GameObject("LaunchPoint_"+(i+1)).transform;point.SetParent(rail,false);point.localPosition=new Vector3(0,.24f,2.2f);points[i]=point;
            }
            Beam(rack,"RackBase",new Vector3(0,-.16f,0),new Vector3(2.5f,.12f,3.4f));
            for(int side=-1;side<=1;side+=2)
                for(int end=-1;end<=1;end+=2)Beam(rack,"Upright",new Vector3(side*1.2f,1.05f,end*1.5f),new Vector3(.09f,2.6f,.09f));
            Edit(launcher,s=>{
                P(s,"missile").objectReferenceValue=drone;P(s,"info").objectReferenceValue=info;P(s,"ammo").intValue=4;
                var transforms=P(s,"launchTransforms");transforms.arraySize=4;
                for(int i=0;i<4;i++)transforms.GetArrayElementAtIndex(i).objectReferenceValue=points[i];
                P(s,"railLength").floatValue=3.1f;
            });
            foreach(var collider in elevation.GetComponentsInChildren<BoxCollider>(true))
            {collider.center=new Vector3(0,1.9f,-.3f);collider.size=new Vector3(2.6f,2.8f,3.5f);}
            var prefab=PrefabUtility.SaveAsPrefabAsset(g,R+"Apex8_TEL.prefab");
            Edit(def,s=>{
                P(s,"jsonKey").stringValue="Apex8_TEL";P(s,"unitName").stringValue="Apex-8 TEL";P(s,"code").stringValue="APX-8 TEL";
                P(s,"description").stringValue="Four-rail launcher for larger Apex-8 drones. Sequential launch interval: 1.25 s.";
                P(s,"unitPrefab").objectReferenceValue=prefab;
            });
            if(launcher.ammo!=4||P(new SerializedObject(launcher),"launchTransforms").arraySize!=4)throw new Exception("Four-rail TEL invalid");
        }
        finally{UnityEngine.Object.DestroyImmediate(g);}
        Debug.Log("APEX8_TEL_VALIDATED: four launch transforms, four loaded models, native booster");
    }
}
