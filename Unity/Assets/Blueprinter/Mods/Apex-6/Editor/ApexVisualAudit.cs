using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ApexVisualAudit
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    public static void Repair()
    {
        string liningPath=R+"Materials/M_Apex8_InnerWall.mat";
        var lining=AssetDatabase.LoadAssetAtPath<Material>(liningPath);
        if(!lining){lining=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(lining,liningPath);}
        lining.SetColor("_BaseColor",new Color(.055f,.065f,.075f,1));
        lining.SetFloat("_Metallic",0f);lining.SetFloat("_Smoothness",.15f);lining.SetFloat("_Cull",2f);
        EditorUtility.SetDirty(lining);AssetDatabase.SaveAssets();
        var importer=(ModelImporter)AssetImporter.GetAtPath(R+"Models/Apex6_Air.fbx");
        foreach(var name in new[]{"StealthHull","CopperEngine","TemperedNozzle","TurbineFace"})
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(R+"Materials/M_Apex6_M_AirDrone_"+name+".mat");
            if(!material)throw new Exception("Missing material "+name);
            material.shader=Shader.Find("Universal Render Pipeline/Lit");
            material.SetFloat("_Cull",2f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            EditorUtility.SetDirty(material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),"M_AirDrone_"+name),material);
        }
        importer.importNormals=ModelImporterNormals.Calculate;
        importer.normalCalculationMode=ModelImporterNormalCalculationMode.AreaAndAngleWeighted;
        importer.normalSmoothingAngle=60f;
        importer.importTangents=ModelImporterTangents.CalculateMikk;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        // Clones in the cargo and TEL have serialized material assignments.
        var validation=new StringBuilder();
        foreach(var name in new[]{"Apex8_Air","Apex8_Ground","Apex6_AirRail","Apex8_Pallet4","Apex8_Pallet4Mount","Apex8_TEL"})
        {
            var path=R+name+".prefab";
            if(!File.Exists(path))continue;
            var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var renderer in g.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(!renderer.name.StartsWith("Apex6_AirDrone_"))continue;
                    string kind=renderer.name.Contains("Engine")?"CopperEngine":renderer.name.Contains("Nozzle")?"TemperedNozzle":renderer.name.Contains("Turbine")?"TurbineFace":"StealthHull";
                    var exterior=AssetDatabase.LoadAssetAtPath<Material>(R+"Materials/M_Apex6_M_AirDrone_"+kind+".mat");
                    renderer.sharedMaterials=renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount==2?new[]{exterior,lining}:new[]{exterior};
                }
                int count=0;
                foreach(var renderer in g.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(!renderer.name.StartsWith("Apex6_AirDrone_"))continue;
                    count++;
                    if(renderer.sharedMaterials.Length!=renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount)throw new Exception("Material slot mismatch "+path+"/"+renderer.name);
                    foreach(var material in renderer.sharedMaterials)
                        if(!material || material.shader.name!="Universal Render Pipeline/Lit" || material.GetFloat("_Cull")!=2f)throw new Exception("Invalid Lit material "+path+"/"+renderer.name);
                }
                validation.AppendLine(name+": "+count+" renderers; Lit/front faces; material slots verified.");
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(g);}
        }
        Render("after");
        Render("front");
        Render("pallet");
        File.WriteAllText(R+"Validation~/visual-validation.txt",validation.ToString());
        var report=new StringBuilder();
        foreach(var filter in AssetDatabase.LoadAssetAtPath<GameObject>(R+"Models/Apex6_Air.fbx").GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;
            var v=mesh.vertices;var indices=mesh.triangles;
            double volume=0;float radial=0;
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=v[indices[i]];var b=v[indices[i+1]];var c=v[indices[i+2]];
                volume+=Vector3.Dot(a,Vector3.Cross(b,c))/6d;
                var normal=Vector3.Cross(b-a,c-a);
                radial+=Vector3.Dot(normal,(a+b+c)/3-mesh.bounds.center);
            }
            report.AppendLine(mesh.name+" volume="+volume+" outward="+radial+" scale="+filter.transform.lossyScale);
        }
        File.WriteAllText(R+"Validation~/mesh-winding.txt",report.ToString());
    }
    public static void Run()
    {
        Render("before");
        var s=new StringBuilder();
        foreach(var p in new[]{"Models/Apex6_Air.fbx","Apex8_Air.prefab","Apex8_Ground.prefab","Apex6_AirRail.prefab"})
            Inspect(AssetDatabase.LoadAssetAtPath<GameObject>(R+p),p,s);
        var b=AssetBundle.LoadFromFile("Releases/Apex-6/Apex-6_1.1.0.nobp");
        if(b!=null)
        {
            try{foreach(var p in b.GetAllAssetNames())if(p.EndsWith("apex6_air.prefab"))Inspect(b.LoadAsset<GameObject>(p),"OLD BUNDLE "+p,s);}
            finally{b.Unload(true);}
        }
        File.WriteAllText(R+"Validation~/visual-audit.txt",s.ToString());
    }
    static void Inspect(GameObject g,string p,StringBuilder s)
    {
        s.AppendLine(p);if(!g){s.AppendLine("MISSING");return;}
        foreach(var r in g.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(!r.name.Contains("AirDrone"))continue;
            var m=r.GetComponent<MeshFilter>().sharedMesh;
            s.AppendLine(r.name+" mesh="+m.name+" vertices="+m.vertexCount+" readable="+m.isReadable+" rot="+r.transform.localEulerAngles);
            foreach(var mat in r.sharedMaterials)
            {
                if(!mat){s.AppendLine("NULL material");continue;}
                s.AppendLine("MAT "+mat.name+" shader="+mat.shader.name+" path="+AssetDatabase.GetAssetPath(mat)+" keywords="+string.Join(",",mat.shaderKeywords));
                foreach(var property in new[]{"_MainTex","_BaseMap","_BumpMap","_MetallicGlossMap"})
                    if(mat.HasProperty(property)){var t=mat.GetTexture(property);s.AppendLine(property+"="+(t?t.name:"NULL"));}
            }
        }
    }
    public static void Render(string suffix)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(R+(suffix=="pallet"?"Apex8_Pallet4Mount.prefab":"Apex8_Air.prefab"));
        var visual=prefab.transform.Find("Apex6_Visual");
        var g=suffix=="pallet"?StaticModel(prefab.transform):UnityEngine.Object.Instantiate(visual.gameObject);
        var preview=new PreviewRenderUtility();
        try
        {
            g.transform.position=Vector3.zero;
            preview.AddSingleGO(g);
            preview.camera.transform.position=suffix=="front"?new Vector3(0,.28f,1.35f):new Vector3(1.4f,.9f,2.6f);
            preview.camera.transform.LookAt(new Vector3(0,suffix=="front"?.22f:.15f,0));
            if(suffix=="pallet"){preview.camera.transform.position=new Vector3(3f,1.5f,4f);preview.camera.transform.LookAt(Vector3.zero);}
            preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=30;
            preview.camera.fieldOfView=52;
            preview.lights[0].intensity=1.3f;
            preview.lights[0].transform.rotation=Quaternion.Euler(35,35,0);
            preview.lights[1].intensity=.6f;
            preview.ambientColor=new Color(.35f,.35f,.35f);
            preview.BeginStaticPreview(new Rect(0,0,1000,750));
            preview.Render(true);
            var t=preview.EndStaticPreview();
            File.WriteAllBytes(R+"Validation~/visual-"+suffix+".png",t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }
        finally{preview.Cleanup();}
    }
    static GameObject StaticModel(Transform source)
    {
        var g=new GameObject(source.name);
        g.transform.localPosition=source.localPosition;g.transform.localRotation=source.localRotation;g.transform.localScale=source.localScale;
        var filter=source.GetComponent<MeshFilter>();var renderer=source.GetComponent<MeshRenderer>();
        if(filter && renderer){g.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;}
        foreach(Transform child in source)StaticModel(child).transform.SetParent(g.transform,false);
        return g;
    }
}
