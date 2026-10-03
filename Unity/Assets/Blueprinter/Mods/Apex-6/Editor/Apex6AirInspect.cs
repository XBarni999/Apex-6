using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class Apex6AirInspect
{
    public static void Run()
    {
        const string root="Assets/Blueprinter/Mods/Apex-6/";
        var s=new StringBuilder();
        foreach(var path in new[]{root+"Models/Apex6_Air.fbx",root+"Models/Apex6_AirPylonFrame.fbx",root+"Models/Apex-6.fbx",root+"Apex6_Air.prefab",root+"Apex6_AirRail.prefab"})
        {
            var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if(asset is AnimationClip clip)
                    s.AppendLine("CLIP "+clip.name+" duration="+clip.length+" legacy="+clip.legacy+
                        " bindings="+string.Join(",",Array.ConvertAll(AnimationUtility.GetCurveBindings(clip),
                            binding=>binding.path+"/"+binding.propertyName)));
            s.AppendLine(path+" root="+(go?go.name:"MISSING"));if(!go)continue;
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                s.AppendLine("RENDER "+r.transform.GetHierarchyPath()+" center="+r.bounds.center+" bounds="+r.bounds.size+" local="+r.transform.localPosition+" scale="+r.transform.localScale+" mats="+string.Join(",",Array.ConvertAll(r.sharedMaterials,m=>m?m.name:"null")));
            }
            foreach(var tr in go.GetComponentsInChildren<Transform>(true)) if(tr.name.Contains("Visual")||tr.name.Contains("Heat")||tr.name.Contains("launch")||tr.name=="Apex6_Air"||tr.name=="Apex6_AirPylonFrame")s.AppendLine("TR "+tr.GetHierarchyPath()+" pos="+tr.localPosition+" rot="+tr.localEulerAngles+" scale="+tr.localScale);
        }
        File.WriteAllText(root+"Validation~/air-model-inspect.txt",s.ToString());Debug.Log("APEX_AIR_MODEL_INSPECT_OK");
    }
    static string GetHierarchyPath(this Transform t){var a=t.name;while(t.parent!=null){t=t.parent;a=t.name+"/"+a;}return a;}
}
