using System;
using UnityEditor;
using UnityEngine;

public static class Apex6FlightFeedback
{
    const string R="Assets/Blueprinter/Mods/Apex-6/";
    public static void Run()
    {
        foreach(var variant in new[]{"Air","Ground"})
        {
            var path=R+"Apex6_"+variant+".prefab";var g=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var s=new SerializedObject(g.GetComponent<Missile>());var p=s.FindProperty("dragCurve");var curve=p.animationCurveValue;
                // Calibrate the previous curve against the observed steady low-altitude speed.
                // v_terminal scales with sqrt(1/Cd); do not restore the hard thrust cutoff.
                if(curve.keys[0].value>.1f)
                {
                    float factor=(454f/670f)*(454f/670f);var keys=curve.keys;
                    for(int i=0;i<keys.Length;i++){keys[i].value*=factor;keys[i].inTangent*=factor;keys[i].outTangent*=factor;}
                    curve.keys=keys;p.animationCurveValue=curve;s.ApplyModifiedPropertiesWithoutUndo();
                }
                var haze=g.transform.Find("Apex6_DryJetHeatHaze");if(haze==null)throw new Exception("Missing native heat haze");
                haze.localScale=Vector3.one*.9f;
                var ps=haze.GetComponent<ParticleSystem>();var main=ps.main;main.startLifetime=1.2f;main.startSize=.35f;main.startSpeed=5;main.maxParticles=180;
                var emission=ps.emission;emission.enabled=true;emission.rateOverTime=80;
                PrefabUtility.SaveAsPrefabAsset(g,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(g);}
        }
        AssetDatabase.SaveAssets();Apex6Setup.Validate();Debug.Log("APEX6_FLIGHT_FEEDBACK_OK");
    }
}
