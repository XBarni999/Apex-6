using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Blueprinter;

// All generated game assets stay inside this mod. Donor assets are read-only.
public static class Apex6Setup
{
    const string Root = "Assets/Blueprinter/Mods/Apex-6/";
    const string Donor = "Assets/Blueprinter/_donotship/";
    static Material casing;
    static GameObject model;
    static SerializedProperty Prop(SerializedObject s, string p) => s.FindProperty(p) ?? throw new Exception(s.targetObject.name + ": missing " + p);
    static void F(UnityEngine.Object o, string p, float v) { var s = new SerializedObject(o); Prop(s,p).floatValue=v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void I(UnityEngine.Object o, string p, int v) { var s = new SerializedObject(o); Prop(s,p).intValue=v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void B(UnityEngine.Object o, string p, bool v) { var s = new SerializedObject(o); Prop(s,p).boolValue=v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void S(UnityEngine.Object o, string p, string v) { var s = new SerializedObject(o); Prop(s,p).stringValue=v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Ref(UnityEngine.Object o, string p, UnityEngine.Object v) { var s = new SerializedObject(o); Prop(s,p).objectReferenceValue=v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static void Vec(UnityEngine.Object o, string p, Vector3 v) { var s = new SerializedObject(o); Prop(s,p).vector3Value=v; s.ApplyModifiedPropertiesWithoutUndo(); }
    static T Load<T>(string p) where T:UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(p) ?? throw new Exception("Missing asset " + p);
    static T Copy<T>(string source, string dest) where T:UnityEngine.Object
    {
        if (!File.Exists(dest) && !AssetDatabase.CopyAsset(source,dest)) throw new Exception("Cannot copy " + source);
        var a=Load<T>(dest); a.name=Path.GetFileNameWithoutExtension(dest); return a;
    }
    public static void Inspect()
    {
        foreach(var path in new[]{Donor+"MonoBehaviour/CruiseMissile1_PLACEHOLDER.asset",Root+"Def_Apex6_Air.asset"})
            foreach(var a in AssetDatabase.LoadAllAssetsAtPath(path)) Debug.Log("APEX_ASSET_TYPE "+path+" "+(a==null?"NULL":a.name+" "+a.GetType().AssemblyQualifiedName));
        foreach(var a in AssetDatabase.LoadAllAssetsAtPath("Packages/nuclearoption/Assembly-CSharp.dll"))
        {
            var script=a as MonoScript; if(script==null) continue;
            if(!new[]{"MissileDefinition","Missile","WeaponInfo","VehicleDefinition"}.Contains(script.name)) continue;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(script,out string guid,out long id);
            Debug.Log("APEX_SCRIPT "+script.name+" "+guid+" "+id+" "+script.GetClass());
        }
    }
    static GameObject Instance(string p)
    {
        var g=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(p));
        PrefabUtility.UnpackPrefabInstance(g,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction); return g;
    }
    static void Save(GameObject g,string name) { PrefabUtility.SaveAsPrefabAsset(g,Root+name+".prefab"); UnityEngine.Object.DestroyImmediate(g); }
    static void ArrayClear(UnityEngine.Object o,string name) { var s=new SerializedObject(o); Prop(s,name).arraySize=0; s.ApplyModifiedPropertiesWithoutUndo(); }
    static Bounds BoundsOf(GameObject g)
    {
        var rs=g.GetComponentsInChildren<Renderer>(true); var b=rs[0].bounds; foreach(var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    static GameObject Visual(Transform parent, bool folded=false)
    {
        var pivot=new GameObject("Apex6_Visual"); pivot.transform.SetParent(parent,false);
        var g=(GameObject)PrefabUtility.InstantiatePrefab(model); g.transform.SetParent(pivot.transform,false);
        // Resolve FBX handedness from the actual nose-mounted sensor mesh.
        g.transform.localRotation=Quaternion.identity;
        var sensor=g.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r=>r.name.Contains("FLIR_Aperture"));
        if(sensor==null) throw new Exception("Cannot determine Apex model nose direction");
        if(sensor.bounds.center.z<BoundsOf(g).center.z) g.transform.localRotation=Quaternion.Euler(0,180,0);
        var b=BoundsOf(g); g.transform.position-=b.center-pivot.transform.position;
        foreach(var renderer in g.GetComponentsInChildren<MeshRenderer>(true))
        {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(src=>
            {
                if(src==null) return casing;
                var path=Root+"Materials/M_Apex6_"+string.Concat(src.name.Select(c=>char.IsLetterOrDigit(c)?c:'_'))+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material!=null) return material;
                material=new Material(Shader.Find("Universal Render Pipeline/Lit")); material.name=Path.GetFileNameWithoutExtension(path);
                material.SetColor("_BaseColor",src.HasProperty("_Color")?src.GetColor("_Color"):new Color(.25f,.27f,.24f));
                if(src.HasProperty("_MainTex")) material.SetTexture("_BaseMap",src.GetTexture("_MainTex"));
                material.SetFloat("_Metallic",src.HasProperty("_Metallic")?src.GetFloat("_Metallic"):.3f);
                material.SetFloat("_Smoothness",.4f); AssetDatabase.CreateAsset(material,path); return material;
            }).ToArray();
        }
        if (folded) pivot.transform.localScale=new Vector3(.25f,1,1);
        foreach(var c in g.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(c);
        return pivot;
    }
    static void HideDonorMeshes(GameObject g)
    {
        foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true)) r.enabled=false;
        foreach(var l in g.GetComponentsInChildren<LODGroup>(true)) UnityEngine.Object.DestroyImmediate(l);
    }
    static void Box(Transform parent,string name,Vector3 position,Vector3 size)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent,false);
        g.transform.localPosition=position; g.transform.localScale=size;
        g.GetComponent<Renderer>().sharedMaterial=casing; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
    }
    static Transform Tube(Transform parent,int n,Vector3 center)
    {
        var g=new GameObject("Tube_"+(n+1)); g.transform.SetParent(parent,false); g.transform.localPosition=center;
        // Open rectangular launch container. Stored wing package fits a 0.62 m cell.
        Box(g.transform,"Left",new Vector3(-.33f,0,0),new Vector3(.045f,.46f,2.65f));
        Box(g.transform,"Right",new Vector3(.33f,0,0),new Vector3(.045f,.46f,2.65f));
        Box(g.transform,"Roof",new Vector3(0,.23f,0),new Vector3(.70f,.045f,2.65f));
        Box(g.transform,"Floor",new Vector3(0,-.23f,0),new Vector3(.70f,.045f,2.65f));
        var mouth=new GameObject("LaunchPoint_"+(n+1)); mouth.transform.SetParent(g.transform,false); mouth.transform.localPosition=new Vector3(0,0,1.65f);
        return mouth.transform;
    }
    [MenuItem("Blueprinter/Apex-6/Create initial assets")]
    public static void Create()
    {
        if(File.Exists(Root+"Validation~/assets.txt")) throw new Exception("Apex-6 already created. Edit the generated assets directly.");
        AssetDatabase.Refresh();
        model=Load<GameObject>(Root+"Models/Apex-6.fbx");
        var importer=(ModelImporter)AssetImporter.GetAtPath(Root+"Models/Apex-6.fbx");
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        model=Load<GameObject>(Root+"Models/Apex-6.fbx");
        var imported=(GameObject)PrefabUtility.InstantiatePrefab(model);
        Debug.Log("APEX_MODEL_BOUNDS "+BoundsOf(imported)); UnityEngine.Object.DestroyImmediate(imported);
        casing=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/M_Apex6_Container.mat");
        bool newCasing=casing==null; if(newCasing) casing=new Material(Shader.Find("Universal Render Pipeline/Lit")); casing.name="M_Apex6_Container";
        casing.SetColor("_BaseColor",new Color(.16f,.19f,.15f)); casing.SetFloat("_Metallic",.45f); casing.SetFloat("_Smoothness",.32f);
        if(newCasing) AssetDatabase.CreateAsset(casing,Root+"Materials/M_Apex6_Container.mat");
        var def=Copy<MissileDefinition>(Donor+"MonoBehaviour/CruiseMissile1_PLACEHOLDER.asset",Root+"Def_Apex6_Air.asset");
        var groundDef=Copy<MissileDefinition>(Donor+"MonoBehaviour/CruiseMissile1_PLACEHOLDER.asset",Root+"Def_Apex6_Ground.asset");
        var wi=Copy<WeaponInfo>(Donor+"MonoBehaviour/info_CruiseMissile1_PLACEHOLDER.asset",Root+"WI_Apex6_Air.asset");
        var groundWi=Copy<WeaponInfo>(Donor+"MonoBehaviour/info_CruiseMissile1_PLACEHOLDER.asset",Root+"WI_Apex6_Ground.asset");
        ConfigureInfo(wi,false); ConfigureInfo(groundWi,true);
        ConfigureDefinition(def,"Apex6_Air"); ConfigureDefinition(groundDef,"Apex6_Ground");
        CreateDrone(def,wi,false); CreateDrone(groundDef,groundWi,true);
        var air=Load<GameObject>(Root+"Apex6_Air.prefab"); var ground=Load<GameObject>(Root+"Apex6_Ground.prefab");
        Ref(def,"unitPrefab",air); Ref(wi,"weaponPrefab",air); Ref(groundDef,"unitPrefab",ground); Ref(groundWi,"weaponPrefab",ground);
        CreatePod(wi); CreateTEL(groundDef,groundWi);
        var op=ScriptableObject.CreateInstance<OpAddWeaponToHardpoint>(); op.weaponJsonKey="Apex6_TwinTube";
        // Select actual current hardpoint sets which already accept medium/heavy missiles.
        foreach(var guid in AssetDatabase.FindAssets("t:AircraftDefinition",new[]{Donor.TrimEnd('/')}))
        {
            var definition=Load<AircraftDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            if(!new[]{"CAS1","Multirole1","EW1","FastBomber1","Darkreach"}.Contains(definition.jsonKey)) continue;
            var aircraft=definition.unitPrefab; if(aircraft==null) continue;
            var manager=aircraft.GetComponentsInChildren<MonoBehaviour>(true).FirstOrDefault(c=>c!=null&&c.GetType().Name=="WeaponManager");
            if(manager==null) throw new Exception("Missing weapon manager "+definition.jsonKey);
            var hp=Prop(new SerializedObject(manager),"hardpointSets");
            var target=new OpAddWeaponToHardpoint.AircraftTarget{aircraftJsonKey=definition.jsonKey};
            for(int i=0;i<hp.arraySize;i++)
            {
                var set=hp.GetArrayElementAtIndex(i); var options=set.FindPropertyRelative("weaponOptions");
                for(int j=0;j<options.arraySize;j++)
                {
                    var option=options.GetArrayElementAtIndex(j).objectReferenceValue as WeaponMount;
                    if(option==null||!(option.jsonKey.StartsWith("CruiseMissile1")||option.jsonKey.StartsWith("AGM_heavy"))) continue;
                    target.hardpointIndices.Add(i); Debug.Log("APEX_HARDPOINT "+definition.jsonKey+" "+i+" "+set.FindPropertyRelative("name").stringValue); break;
                }
            }
            if(target.hardpointIndices.Count>0) op.aircraft.Add(target);
        }
        if(op.aircraft.Count==0) throw new Exception("No compatible aircraft hardpoints found");
        AssetDatabase.CreateAsset(op,Root+"Op_Apex6_Aircraft.asset");
        File.WriteAllText(Root+"modinfo.json","{\n  \"displayName\": \"Apex-6\",\n  \"version\": \"0.1.0\"\n}\n");
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        Preview(); Validate(); Debug.Log("APEX6_SETUP_OK");
    }
    static void ConfigureDefinition(MissileDefinition d,string key)
    {
        S(d,"jsonKey",key); S(d,"unitName","Apex-6"); S(d,"code","APX-6"); S(d,"bogeyName","DRONE");
        S(d,"description","Jet-powered low-observable one-way attack drone. 670 km/h cruise; 20 m terrain-following; 38 kg warhead.");
        F(d,"radarSize",.001f); F(d,"visibleRange",1200); F(d,"mass",180); F(d,"value",1);
        F(d,"length",2.1f); F(d,"width",2.3f); F(d,"height",.4f); F(d,"armorTier",.1f); F(d,"damageTolerance",0);
        F(d,"roleIdentity.antiRadar",1); Vec(d,"spawnOffset",Vector3.zero);
    }
    static void ConfigureInfo(WeaponInfo w,bool ground)
    {
        S(w,"weaponName",ground?"Apex-6 Ground":"Apex-6"); S(w,"shortName","APX-6");
        S(w,"description","Low-altitude jet attack drone; 38 kg shaped-charge/HE warhead. Selected surface target required.");
        F(w,"fireInterval",1.25f); F(w,"maxSpeed",670f/3.6f); F(w,"massPerRound",180); F(w,"costPerRound",1);
        F(w,"pierceDamage",250); F(w,"blastDamage",38); F(w,"targetRequirements.minRange",1500);
        F(w,"targetRequirements.maxRange",80000); F(w,"targetRequirements.maxSpeed",40);
        F(w,"targetRequirements.minOwnerSpeed",0); F(w,"targetRequirements.minValue",0);
        F(w,"targetRequirements.maxAltitude",5000); F(w,"effectiveness.antiRadar",1);
    }
    static void CreateDrone(MissileDefinition d,WeaponInfo wi,bool ground)
    {
        var g=Instance(Donor+"GameObject/CruiseMissile1_PLACEHOLDER.prefab"); g.name=ground?"Apex6_Ground":"Apex6_Air";
        HideDonorMeshes(g); var visual=Visual(g.transform);
        var m=g.GetComponent<Missile>(); Ref(m,"definition",d); Ref(m,"info",wi);
        F(m,"mass",180); F(m,"finArea",1.2f); F(m,"torque",1.5f); F(m,"blastYield",38); F(m,"pierceDamage",250);
        ArrayClear(m,"foldingFins"); m.GetComponent<Rigidbody>().mass=180;
        var s=new SerializedObject(m); var motors=Prop(s,"motors"); if(ground) motors.arraySize=2;
        for(int n=0;n<motors.arraySize;n++)
        {
            var stage=motors.GetArrayElementAtIndex(n); bool boost=ground&&n==0;
            stage.FindPropertyRelative("delayTimer").floatValue=0;
            stage.FindPropertyRelative("thrust").floatValue=boost?9000:1800;
            stage.FindPropertyRelative("burnTime").floatValue=boost?2:600;
            stage.FindPropertyRelative("fuelMass").floatValue=boost?3:35;
            stage.FindPropertyRelative("topSpeed").floatValue=670f/3.6f;
            stage.FindPropertyRelative("IR_intensity").floatValue=.035f;
            if(boost) { stage.FindPropertyRelative("audioSources").arraySize=0; stage.FindPropertyRelative("startupSource").objectReferenceValue=null; }
        }
        s.ApplyModifiedPropertiesWithoutUndo();
        var seeker=g.GetComponent<OpticalSeekerCruiseMissile>(); F(seeker,"altitudeTarget",20); F(seeker,"terminalRange",3000);
        F(seeker,"formationSpacing",60); F(seeker,"maxTargetSpeed",40); F(seeker,"finDelay",0);
        F(seeker,"guidanceDelay",ground?.5f:1); F(seeker,"tangibleDelay",1.5f);
        F(seeker,"jinkEvasion.amount",.005f); F(seeker,"jinkEvasion.period",4);
        F(seeker,"jinkEvasion.minRange",700); F(seeker,"jinkEvasion.maxRange",3000); F(seeker,"jinkEvasion.minSpeed",140); B(seeker,"jinkEvasion.flat",true);
        var c=g.GetComponent<CapsuleCollider>(); c.radius=.17f; c.height=2.07f; c.center=Vector3.zero;
        Save(g,g.name);
    }
    static void CreatePod(WeaponInfo wi)
    {
        var g=Instance(Donor+"GameObject/CruiseMissile1_internalx2_PLACEHOLDER.prefab"); g.name="Apex6_TwinTube";
        HideDonorMeshes(g); var missiles=g.GetComponentsInChildren<MountedMissile>(true);
        if(missiles.Length!=2) throw new Exception("Expected two mounted missiles");
        for(int i=0;i<2;i++)
        {
            var m=missiles[i]; m.transform.localPosition=new Vector3(i==0?-.38f:.38f,-.35f,0);
            m.transform.localRotation=Quaternion.identity; Ref(m,"info",wi); I(m,"ammo",1);
            I(m,"railDirection",0); F(m,"railLength",1.65f); F(m,"railSpeed",20); Visual(m.transform,true);
            Tube(g.transform,i,m.transform.localPosition);
        }
        Box(g.transform,"PylonAdapter",new Vector3(0,-.06f,0),new Vector3(1.5f,.15f,.65f));
        Save(g,"Apex6_TwinTube");
        var mount=Copy<WeaponMount>(Donor+"MonoBehaviour/CruiseMissile1_internalx2_PLACEHOLDER.asset",Root+"WM_Apex6_TwinTube.asset");
        S(mount,"jsonKey","Apex6_TwinTube"); S(mount,"mountName","Apex-6 Twin-Tube Pod");
        Ref(mount,"prefab",Load<GameObject>(Root+"Apex6_TwinTube.prefab")); Ref(mount,"info",wi);
        I(mount,"ammo",2); F(mount,"emptyMass",70); F(mount,"drag",.16f); F(mount,"emptyDrag",.08f); F(mount,"RCS",.04f); F(mount,"emptyRCS",.03f);
    }
    static void CreateTEL(MissileDefinition missile,WeaponInfo wi)
    {
        var def=Copy<VehicleDefinition>(Donor+"MonoBehaviour/Truck2-MLRS_PLACEHOLDER.asset",Root+"Def_Apex6_TEL.asset");
        S(def,"jsonKey","Apex6_TEL"); S(def,"unitName","Apex-6 TEL"); S(def,"code","APX-6 TEL");
        S(def,"description","Six-cell jet attack drone carrier on a vanilla MSV chassis. Sequential launches every 1.25 seconds.");
        var g=Instance(Donor+"GameObject/Truck2-MLRS_PLACEHOLDER.prefab"); g.name="Apex6_TEL";
        var vehicle=g.GetComponent<GroundVehicle>(); Ref(vehicle,"definition",def);
        var launcher=g.GetComponentInChildren<MissileLauncher>(true); Ref(launcher,"missile",missile); Ref(launcher,"info",wi); I(launcher,"ammo",6); F(launcher,"fireInterval",1.25f); F(launcher,"reloadTime",60);
        Vec(launcher,"ejectionVelocity",new Vector3(0,0,35));
        var turret=launcher.GetComponentInParent<Turret>();
        B(turret,"aimSolver.artillery",false); B(turret,"firesWithoutAiming",true); F(turret,"minElevation",10); F(turret,"maxElevation",25);
        F(turret,"targetAssessmentInterval",1); B(turret,"newTargetSearchAfterFire",false);
        var elevation=(Transform)Prop(new SerializedObject(turret),"elevationTransform").objectReferenceValue;
        elevation.localRotation=Quaternion.Euler(-12,0,0);
        foreach(var r in elevation.GetComponentsInChildren<MeshRenderer>(true)) r.enabled=false;
        var cassette=new GameObject("Apex6_SixTubeCassette"); cassette.transform.SetParent(elevation,false);
        var so=new SerializedObject(launcher); var points=Prop(so,"launchTransforms"); points.arraySize=6;
        for(int i=0;i<6;i++) points.GetArrayElementAtIndex(i).objectReferenceValue=Tube(cassette.transform,i,new Vector3((i%3-1)*.75f,(i/3)*.55f,0));
        so.ApplyModifiedPropertiesWithoutUndo(); Save(g,"Apex6_TEL"); Ref(def,"unitPrefab",Load<GameObject>(Root+"Apex6_TEL.prefab"));
    }
    [MenuItem("Blueprinter/Apex-6/Validate assets")]
    public static void Validate()
    {
        var airMount=File.Exists(Root+"Apex6_AirRail.prefab")?"Apex6_AirRail":"Apex6_TwinTube";
        foreach(var p in new[]{"Apex6_Air","Apex6_Ground","Apex6_TEL",airMount})
        {
            var g=Load<GameObject>(Root+p+".prefab");
            foreach(var t in g.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new Exception("Missing script in "+p);
            foreach(var o in g.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var it=new SerializedObject(o).GetIterator();
                while(it.Next(true)) if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue==null&&it.objectReferenceInstanceIDValue!=0) throw new Exception("Broken reference "+p+":"+it.propertyPath);
            }
        }
        var tel=Load<GameObject>(Root+"Apex6_TEL.prefab").GetComponentInChildren<MissileLauncher>(true);
        if(tel.ammo!=6 || Prop(new SerializedObject(tel),"launchTransforms").arraySize!=6) throw new Exception("TEL cell count");
        var pod=Load<GameObject>(Root+airMount+".prefab").GetComponentsInChildren<MountedMissile>(true);
        if(pod.Length!=(airMount=="Apex6_AirRail"?1:2) || pod.Any(m=>m.ammo!=1)) throw new Exception("Aircraft station count");
        File.WriteAllText(Root+"Validation~/assets.txt","Unity reference validation passed. TEL: six rails, 1.25 s interval. Aircraft rail: one mounted missile. Flight and mission behavior not tested.\n");
        Debug.Log("APEX6_VALIDATION_OK");
    }
    [MenuItem("Blueprinter/Apex-6/Create preview scene")]
    public static void Preview()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var tel=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+"Apex6_TEL.prefab")); tel.transform.position=new Vector3(-4,0,0);
        var drone=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+"Apex6_Air.prefab")); drone.transform.position=new Vector3(4,2,0);
        var pod=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Root+(File.Exists(Root+"Apex6_AirRail.prefab")?"Apex6_AirRail":"Apex6_TwinTube")+".prefab")); pod.transform.position=new Vector3(4,0,0);
        var light=new GameObject("Sun").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=2; light.transform.rotation=Quaternion.Euler(40,-35,0);
        var camera=new GameObject("PreviewCamera").AddComponent<Camera>(); camera.transform.position=new Vector3(12,9,15); camera.transform.LookAt(new Vector3(0,1,0)); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.15f,.19f);
        camera.orthographic=true;camera.orthographicSize=5.8f;
        Directory.CreateDirectory("Assets/Editor/Apex6Preview"); EditorSceneManager.SaveScene(scene,"Assets/Editor/Apex6Preview/Apex6_Preview.unity");
        var rt=new RenderTexture(1400,900,24); camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
        var texture=new Texture2D(1400,900,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1400,900),0,0); texture.Apply();
        File.WriteAllBytes(Root+"Validation~/preview.png",texture.EncodeToPNG()); camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(rt);
    }
}
