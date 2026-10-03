using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Keep the source FBX and its animation intact. Correct the imported render mesh.
public sealed class ApexAirMeshImport : AssetPostprocessor
{
    void OnPostprocessModel(GameObject model)
    {
        if(assetPath!="Assets/Blueprinter/Mods/Apex-6/Models/Apex6_Air.fbx")return;
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh=filter.sharedMesh;
            if(filter.name=="Apex6_AirDrone_Wing_L" || filter.name=="Apex6_AirDrone_Wing_R" || filter.name=="Apex6_AirDrone_Turbine")
            {
                for(int sub=0;sub<mesh.subMeshCount;sub++)
                {
                    var triangles=mesh.GetTriangles(sub);
                    for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
                    mesh.SetTriangles(triangles,sub);
                }
                mesh.RecalculateNormals();mesh.RecalculateTangents();
            }
            else if(filter.name=="Apex6_AirDrone_Intake" || filter.name=="Apex6_AirDrone_Nozzle")
            {
                AddInnerWall(mesh);
                var renderer=filter.GetComponent<MeshRenderer>();
                var materials=new List<Material>(renderer.sharedMaterials);
                materials.Add(AssetDatabase.LoadAssetAtPath<Material>("Assets/Blueprinter/Mods/Apex-6/Materials/M_Apex8_InnerWall.mat"));
                renderer.sharedMaterials=materials.ToArray();
            }
        }
    }
    static void AddInnerWall(Mesh mesh)
    {
        var vertices=mesh.vertices;var normals=mesh.normals;int count=vertices.Length;
        var positions=new List<Vector3>(vertices);
        var directions=new List<Vector3>(normals);
        for(int i=0;i<count;i++){positions.Add(vertices[i]-normals[i]*.001f);directions.Add(-normals[i]);}
        var submeshes=new List<int[]>();
        for(int sub=0;sub<mesh.subMeshCount;sub++)submeshes.Add(mesh.GetTriangles(sub));
        var uv=mesh.uv;var uv2=mesh.uv2;
        mesh.SetVertices(positions);mesh.SetNormals(directions);
        if(uv.Length==count){var expanded=new List<Vector2>(uv);expanded.AddRange(uv);mesh.SetUVs(0,expanded);}
        if(uv2.Length==count){var expanded=new List<Vector2>(uv2);expanded.AddRange(uv2);mesh.SetUVs(1,expanded);}
        var inside=new List<int>();
        mesh.subMeshCount=submeshes.Count+1;
        for(int sub=0;sub<submeshes.Count;sub++)
        {
            var original=submeshes[sub];
            for(int i=0;i<original.Length;i+=3){inside.Add(original[i]+count);inside.Add(original[i+2]+count);inside.Add(original[i+1]+count);}
            mesh.SetTriangles(original,sub);
        }
        mesh.SetTriangles(inside,submeshes.Count);
        mesh.RecalculateTangents();mesh.RecalculateBounds();
    }
    public override uint GetVersion(){return 3;}
}
