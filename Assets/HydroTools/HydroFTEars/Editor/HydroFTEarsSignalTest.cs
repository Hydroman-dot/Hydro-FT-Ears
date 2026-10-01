using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.Animations;
using UnityEngine.Playables;
using VRC.SDK3.Avatars.Components;
namespace Hydro.Tools.FTEars
{
    public static class HydroFTEarsSignalTest
    {
        public static string Run(AnimatorController controller, HydroFTEars module)
        {
            var root=new GameObject("Hydro Signal Probe");
            root.hideFlags=HideFlags.HideAndDontSave;
            PlayableGraph graph=default;
            try
            {
                var avatar=module.GetComponentInParent<VRCAvatarDescriptor>();
                var ears=new Transform[2];int e=0;
                foreach(var source in new[]{module.leftEar,module.rightEar})
                {
                    var t=root.transform;
                    foreach(var part in AnimationUtility.CalculateTransformPath(source,avatar.transform).Split('/'))
                    {
                        var child=t.Find(part);if(!child){child=new GameObject(part).transform;child.SetParent(t,false);}t=child;
                    }
                    t.localRotation=source.localRotation;ears[e++]=t;
                }
                var animator=root.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                graph=PlayableGraph.Create("Hydro built signal test");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var p=AnimatorControllerPlayable.Create(graph,controller);
                var output=AnimationPlayableOutput.Create(graph,"Avatar FX",animator);output.SetSourcePlayable(p);graph.Play();
                foreach(var param in controller.parameters)
                {
                    if(param.name=="IsLocal"){if(param.type==AnimatorControllerParameterType.Float)p.SetFloat(param.name,1);else p.SetBool(param.name,true);}
                    if(param.name=="EyeTrackingActive"||param.name=="LipTrackingActive")p.SetBool(param.name,true);
                }
                p.SetBool(HydroFTEarsWizard.Enable,true);
                for(int i=0;i<p.GetLayerCount();i++)if(p.GetLayerName(i).EndsWith("/ Idle"))p.SetLayerWeight(i,0);
                Action settle=()=>{for(int i=0;i<240;i++)graph.Evaluate(1f/60);};
                p.SetFloat("EchoFT/v2/EyeLeftX",1);p.SetFloat("EchoFT/v2/EyeRightX",1);
                p.SetFloat(HydroFTEarsWizard.Intensity,0);settle();
                var before=ears.Select(t=>t.localRotation).ToArray();
                p.SetFloat(HydroFTEarsWizard.Intensity,1);settle();
                var angle=Mathf.Max(Quaternion.Angle(before[0],ears[0].localRotation),Quaternion.Angle(before[1],ears[1].localRotation));
                var proxy=p.GetFloat(module.eyeXLeft);
                return "Echo raw=1; smoothed proxy="+proxy.ToString("F4")+"; ear response="+angle.ToString("F3")+" degrees";
            }
            finally{if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}