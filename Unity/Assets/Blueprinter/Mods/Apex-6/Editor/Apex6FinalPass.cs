using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class Apex6FinalPass
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    static SerializedProperty P(SerializedObject s,string n)=>s.FindProperty(n)??throw new Exception(n);
    public static void Run()
    {
        var path=R+"Apex6_Ground.prefab";var g=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var old=g.transform.Find("Apex6_SolidBooster");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Blueprinter/_donotship/GameObject/AShM1_PLACEHOLDER.prefab");
            var source=donor.GetComponentInChildren<VLSBooster>(true);var booster=UnityEngine.Object.Instantiate(source.gameObject,g.transform);booster.name="Apex6_SolidBooster";
            booster.transform.localPosition=new Vector3(0,-.07f,-1.05f);booster.transform.localRotation=Quaternion.identity;booster.transform.localScale=Vector3.one*.2f;
            var b=booster.GetComponent<VLSBooster>();var s=new SerializedObject(b);P(s,"missile").objectReferenceValue=g.GetComponent<Missile>();P(s,"thrust").floatValue=9000;P(s,"burnTime").floatValue=1;P(s,"delayTimer").floatValue=0;P(s,"fuelMass").floatValue=3;P(s,"dryMass").floatValue=8;s.ApplyModifiedPropertiesWithoutUndo();
            var ms=new SerializedObject(g.GetComponent<Missile>());var motors=P(ms,"motors");if(motors.arraySize>1)motors.DeleteArrayElementAtIndex(0);motors.GetArrayElementAtIndex(0).FindPropertyRelative("delayTimer").floatValue=1.05f;ms.ApplyModifiedPropertiesWithoutUndo();
            var seeker=new SerializedObject(g.GetComponent<OpticalSeekerCruiseMissile>());P(seeker,"booster").objectReferenceValue=b;P(seeker,"guidanceDelay").floatValue=1.05f;seeker.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(g,path);
        }finally{PrefabUtility.UnloadPrefabContents(g);}
        foreach(var variant in new[]{"Air","Ground"})
        {
            var w=new SerializedObject(AssetDatabase.LoadAssetAtPath<WeaponInfo>(R+"WI_Apex6_"+variant+".asset"));
            // Native FireControl computes attacks from target.damageTolerance / pK.
            // The minimum target tolerance 0.1 now asks for five inexpensive drones.
            P(w,"pK").floatValue=.02f;w.ApplyModifiedPropertiesWithoutUndo();
        }
        var telPath=R+"Apex6_TEL.prefab";var tel=PrefabUtility.LoadPrefabContents(telPath);
        try{var s=new SerializedObject(tel.GetComponent<FireControl>());P(s,"planningTimePerFire").floatValue=.25f;P(s,"targetAssessmentInterval").floatValue=1;s.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(tel,telPath);}finally{PrefabUtility.UnloadPrefabContents(tel);}
        AssetDatabase.SaveAssets();Apex6Setup.Validate();Scan();Debug.Log("APEX6_FINAL_PASS_OK");
    }
    static void Describe(AircraftDefinition def,StringBuilder report,string origin)
    {
        report.AppendLine("AIRCRAFT | "+def.jsonKey+" | "+def.unitName+" | "+origin);
        if(def.unitPrefab==null){report.AppendLine("NO_PREFAB");return;}
        var manager=def.unitPrefab.GetComponentInChildren<WeaponManager>(true);
        if(manager==null){report.AppendLine("NO_WEAPON_MANAGER");return;}
        var sets=P(new SerializedObject(manager),"hardpointSets");
        for(int i=0;i<sets.arraySize;i++)
        {
            var set=sets.GetArrayElementAtIndex(i);var options=set.FindPropertyRelative("weaponOptions");
            var names=Enumerable.Range(0,options.arraySize).Select(j=>options.GetArrayElementAtIndex(j).objectReferenceValue as WeaponMount).Select(m=>m==null?"NULL":m.jsonKey);
            report.AppendLine("HP | "+i+" | "+set.FindPropertyRelative("name").stringValue+" | "+string.Join(",",names));
        }
    }
    public static void Scan()
    {
        var report=new StringBuilder();
        foreach(var guid in AssetDatabase.FindAssets("t:AircraftDefinition",new[]{"Assets/Blueprinter/_donotship"}))Describe(AssetDatabase.LoadAssetAtPath<AircraftDefinition>(AssetDatabase.GUIDToAssetPath(guid)),report,"vanilla");
        foreach(var file in Directory.GetFiles("F:/Games/Nuclear.Option.v0.34.1/BepInEx/plugins","*.nobp",SearchOption.AllDirectories))
        {
            if(Path.GetFileName(file).StartsWith("Apex-6"))continue;
            AssetBundle bundle=null;
            try{bundle=AssetBundle.LoadFromFile(file);if(bundle==null){report.AppendLine("FAILED_BUNDLE | "+file);continue;}foreach(var def in bundle.LoadAllAssets<AircraftDefinition>())Describe(def,report,Path.GetFileName(file));}
            catch(Exception e){report.AppendLine("SCAN_ERROR | "+file+" | "+e.Message);}
            finally{if(bundle!=null)bundle.Unload(true);}
        }
        // Older aircraft mods embed their bundle inside a DLL. Read resources only;
        // never instantiate their plugin classes or invoke their entry points.
        foreach(var file in Directory.GetFiles("F:/Games/Nuclear.Option.v0.34.1/BepInEx/plugins/com.nikkorap.blueprinter/addons","*.dll",SearchOption.AllDirectories))
        {
            try
            {
                var assembly=System.Reflection.Assembly.Load(File.ReadAllBytes(file));
                foreach(var name in assembly.GetManifestResourceNames())
                {
                    using(var stream=assembly.GetManifestResourceStream(name))
                    using(var memory=new MemoryStream())
                    {
                        stream.CopyTo(memory);var data=memory.ToArray();
                        if(data.Length<8||Encoding.ASCII.GetString(data,0,7)!="UnityFS")continue;
                        AssetBundle bundle=null;
                        try{bundle=AssetBundle.LoadFromMemory(data);if(bundle!=null)foreach(var def in bundle.LoadAllAssets<AircraftDefinition>())Describe(def,report,Path.GetFileName(file)+":"+name);}
                        finally{if(bundle!=null)bundle.Unload(true);}
                    }
                }
            }catch(Exception e){report.AppendLine("DLL_SCAN_ERROR | "+file+" | "+e.Message);}
        }
        File.WriteAllText(R+"Validation~/aircraft-hardpoints.txt",report.ToString());
        Debug.Log("APEX6_HARDPOINT_SCAN_OK");
    }
}
