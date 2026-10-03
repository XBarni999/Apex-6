using Blueprinter;
using UnityEditor;
using UnityEngine;

public static class Apex6PackageBuild
{
    public static void Run()
    {
        const string output = "C:/Users/123/AppData/Local/Temp/Apex6-PackageOutput";
        ModBuilder.Build("Apex-6", "Apex-6", "1.0.2", output);
        var path = output + "/Apex-6_1.0.2.nobp";
        if (!System.IO.File.Exists(path)) throw new System.Exception("Blueprinter did not produce " + path);
        Debug.Log("APEX6_PACKAGE_READY " + path + " bytes=" + new System.IO.FileInfo(path).Length);
    }
}
