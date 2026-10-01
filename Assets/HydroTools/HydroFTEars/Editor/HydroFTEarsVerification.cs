using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
namespace Hydro.Tools.FTEars
{
    public static class HydroFTEarsVerification
    {
        public static string Run(HydroFTEars module)
        {
            var avatar=module.GetComponentInParent<VRCAvatarDescriptor>();
            var root=new GameObject("Hydro FT Ears Verification (temporary)");
            root.hideFlags=HideFlags.HideAndDontSave;
            PlayableGraph graph=default;
            try
            {
                var bones=new List<Transform>();
                foreach(var original in new[]{module.leftEar,module.rightEar})
                {
                    var path=AnimationUtility.CalculateTransformPath(original,avatar.transform);
                    var current=root.transform;
                    foreach(var part in path.Split('/'))
                    {
                        var child=current.Find(part);
                        if(!child) {child=new GameObject(part).transform;child.SetParent(current,false);}
                        current=child;
                    }
                    current.localRotation=original.localRotation;bones.Add(current);
                }
                var animator=root.AddComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                var controller=module.GetComponent<ModularAvatarMergeAnimator>().animator;
                graph=PlayableGraph.Create("Hydro additive verification");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var playable=AnimatorControllerPlayable.Create(graph,controller);
                var output=AnimationPlayableOutput.Create(graph,"Ears",animator);output.SetSourcePlayable(playable);graph.Play();
                var rest=new[]{module.leftEar.localRotation,module.rightEar.localRotation};
                Action settle=()=>{for(int i=0;i<30;i++)graph.Evaluate(1f/60);};
                Action reset=()=>{
                    foreach(var p in ((AnimatorController)controller).parameters)if(p.type==AnimatorControllerParameterType.Float)playable.SetFloat(p.name,0);
                    playable.SetBool(HydroFTEarsWizard.Enable,true);
                    playable.SetFloat(HydroFTEarsWizard.Intensity,1);
                    for(int i=0;i<playable.GetLayerCount();i++)if(playable.GetLayerName(i).EndsWith("/ Idle"))playable.SetLayerWeight(i,0);
                };
                Func<Quaternion[]> pose=()=>bones.Select(t=>t.localRotation).ToArray();
                Func<Quaternion[],Quaternion[],float> distance=(x,y)=>Mathf.Max(Quaternion.Angle(x[0],y[0]),Quaternion.Angle(x[1],y[1]));
                var results=new List<string>();
                reset();settle();var neutral=pose();
                float neutralError=distance(rest,neutral);
                if(neutralError>.1f)throw new Exception("Neutral changes rest pose by "+neutralError);
                results.Add("Neutral preserves rest: "+neutralError.ToString("F4")+" deg");
                playable.SetFloat(module.eyeXLeft,1);playable.SetFloat(module.eyeXRight,1);settle();var eye=pose();
                if(distance(neutral,eye)<1)throw new Exception("Eye-follow has no effect.");
                results.Add("Eye-follow: "+distance(neutral,eye).ToString("F3")+" deg");
                reset();playable.SetFloat(module.expressionLeft,-1);playable.SetFloat(module.expressionRight,-1);settle();var expr=pose();
                playable.SetFloat(module.eyeXLeft,1);playable.SetFloat(module.eyeXRight,1);settle();var combined=pose();
                if(distance(expr,combined)<1 || distance(eye,combined)<1)throw new Exception("Expression / Eye combination failed.");
                results.Add("Expression + Eye combine: PASS");
                playable.SetFloat(HydroFTEarsWizard.Intensity,0);settle();
                if(distance(neutral,pose())>.1f)throw new Exception("Zero intensity is not neutral.");
                results.Add("0% restores neutral: PASS");
                playable.SetFloat(HydroFTEarsWizard.Intensity,1);playable.SetBool(HydroFTEarsWizard.Enable,false);settle();
                if(distance(neutral,pose())>.1f)throw new Exception("Disable is not neutral.");
                results.Add("Disabled restores neutral: PASS");
                reset();playable.SetFloat(module.eyeXLeft,1);playable.SetFloat(module.eyeXRight,1);playable.SetFloat(HydroFTEarsWizard.Intensity,.5f);settle();
                float half=distance(neutral,pose()),full=distance(neutral,eye);
                if(Mathf.Abs(half/full-.5f)>.12f)throw new Exception("Intensity scale incorrect.");
                results.Add("50% intensity: "+half.ToString("F3")+" deg");
                reset();playable.SetFloat(module.eyeY,1);settle();
                if(distance(neutral,pose())<.5f)throw new Exception("Vertical eye-follow has no effect.");
                results.Add("Vertical eye-follow: PASS");
                int idleIndex=-1;
                for(int i=0;i<playable.GetLayerCount();i++)if(playable.GetLayerName(i).EndsWith("/ Idle"))idleIndex=i;
                if(idleIndex>=0)
                {
                    reset();playable.SetFloat(module.eyeXLeft,.8f);playable.SetFloat(module.eyeXRight,.8f);
                    playable.SetFloat(module.expressionLeft,-.7f);playable.SetFloat(module.expressionRight,-.7f);settle();
                    var withoutIdle=pose();playable.SetLayerWeight(idleIndex,1);
                    float maxDelta=0;
                    for(int i=0;i<90;i++){graph.Evaluate(1f/60);maxDelta=Mathf.Max(maxDelta,distance(withoutIdle,pose()));}
                    if(maxDelta<.2f)throw new Exception("Idle does not combine with Eye + Expression.");
                    results.Add("Idle + Eye + Expression: PASS ("+maxDelta.ToString("F3")+" deg idle change)");
                    playable.SetFloat(HydroFTEarsWizard.Intensity,0);settle();
                    if(distance(neutral,pose())>.1f)throw new Exception("Idle remains at 0%.");
                    results.Add("0% also suppresses Idle: PASS");
                }
                return string.Join("\n",results);
            }
            finally {if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
