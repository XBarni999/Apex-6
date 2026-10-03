using System;
using UnityEditor;
using UnityEngine;

public static class Apex6WingSetup
{
    private const string Root = "Assets/Blueprinter/Mods/Apex-6/";
    private const string ClipPath = Root + "Apex6_AirWingDeploy.anim";

    public static void Run()
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(Root + "Models/Apex6_Air.fbx");
        var output = new AnimationClip { name = "Apex6_AirWingDeploy", legacy = true, frameRate = 24 };
        output.wrapMode = WrapMode.ClampForever;
        AnimationClip scene = null;
        foreach (var asset in clips)
            if (asset is AnimationClip candidate && candidate.name == "Scene_AirDrone") scene = candidate;
        if (scene == null) throw new Exception("Updated FBX Scene_AirDrone clip missing");
        foreach (var binding in AnimationUtility.GetCurveBindings(scene))
            if (binding.type == typeof(Transform))
                AnimationUtility.SetEditorCurve(output, binding, AnimationUtility.GetEditorCurve(scene, binding));
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(output, existing);
            UnityEngine.Object.DestroyImmediate(output);
            output = existing;
            EditorUtility.SetDirty(output);
        }
        else AssetDatabase.CreateAsset(output, ClipPath);
        ConfigurePrefab(Root + "Apex6_Air.prefab", "Apex6_Visual/Apex6_Air", output);
        ConfigurePrefab(Root + "Apex6_AirRail.prefab", "Apex6_ReadyDrone/Apex6_Visual/Apex6_Air", output);
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("APEX6_WINGS_READY duration=" + output.length);
    }

    public static void Validate()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null) throw new Exception("Combined wing clip is missing");
        foreach (var path in new[] { Root + "Apex6_Air.prefab", Root + "Apex6_AirRail.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var anim = root.GetComponentInChildren<Animation>(true);
                if (anim == null || anim.clip != clip || anim.playAutomatically)
                    throw new Exception("Wing animation is not ready on " + path);
                var left = anim.transform.Find("Apex6_AirDrone_Wing_L");
                var right = anim.transform.Find("Apex6_AirDrone_Wing_R");
                if (left == null || right == null) throw new Exception("Missing wing transforms in " + path);
                var sourceModel = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Models/Apex6_Air.fbx"));
                try
                {
                    AnimationClip sourceClip = null;
                    foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Root + "Models/Apex6_Air.fbx"))
                        if (asset is AnimationClip candidate && candidate.name == "Scene_AirDrone") sourceClip = candidate;
                    if (sourceClip == null) throw new Exception("Source clip missing");
                    foreach (var fraction in new[] { 0f, .25f, .5f, .75f, 1f })
                    {
                        clip.SampleAnimation(anim.gameObject, clip.length * fraction);
                        sourceClip.SampleAnimation(sourceModel, sourceClip.length * fraction);
                        foreach (var name in new[] { "Apex6_AirDrone_Wing_L", "Apex6_AirDrone_Wing_R" })
                        {
                            var actual = anim.transform.Find(name);
                            var expected = sourceModel.transform.Find(name);
                            if (Vector3.Distance(actual.localPosition, expected.localPosition) > .0001f ||
                                Vector3.Distance(actual.localScale, expected.localScale) > .0001f ||
                                Quaternion.Angle(actual.localRotation, expected.localRotation) > .05f)
                                throw new Exception("Wing pose differs from updated FBX at " + fraction + ": " + path + "/" + name);
                        }
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(sourceModel); }
                clip.SampleAnimation(anim.gameObject, 0f);
                var leftStart = left.localRotation;
                var rightStart = right.localRotation;
                clip.SampleAnimation(anim.gameObject, clip.length);
                var leftAngle = Quaternion.Angle(leftStart, left.localRotation);
                var rightAngle = Quaternion.Angle(rightStart, right.localRotation);
                if (leftAngle < 10f || rightAngle < 10f)
                    throw new Exception("Wing clip has no meaningful motion on " + path + ": " + leftAngle + ", " + rightAngle);
                Debug.Log("APEX6_WINGS_VALIDATE " + path + " angles=" + leftAngle + "," + rightAngle);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

    private static void CopyWing(UnityEngine.Object[] imported, AnimationClip output, string clipName, string wingName)
    {
        AnimationClip source = null;
        foreach (var asset in imported)
            if (asset is AnimationClip candidate && candidate.name == clipName) { source = candidate; break; }
        if (source == null) throw new Exception("Missing wing clip: " + clipName);
        var count = 0;
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            if (binding.path != wingName || !binding.propertyName.StartsWith("m_LocalRotation.")) continue;
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            AnimationUtility.SetEditorCurve(output, binding, curve);
            count++;
        }
        if (count != 4) throw new Exception("Expected four rotation curves on " + wingName + ", got " + count);
        Debug.Log("APEX6_WING_CLIP " + wingName + " duration=" + source.length);
    }

    private static void ConfigurePrefab(string path, string modelPath, AnimationClip clip)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var model = root.transform.Find(modelPath);
            if (model == null) throw new Exception("Model not found in " + path + ": " + modelPath);
            var anim = model.GetComponent<Animation>();
            if (anim == null) anim = model.gameObject.AddComponent<Animation>();
            anim.playAutomatically = false;
            anim.clip = clip;
            anim.cullingType = AnimationCullingType.AlwaysAnimate;
            clip.SampleAnimation(model.gameObject, 0f);
            PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
            if (!success) throw new Exception("Could not save wing animation on " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
