using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Hydro.Tools.FTEars
{
    // Editor-only SDK callback. Runs after MA, VRCFury and avatar optimization.
    public sealed class HydroFTEarsBuildOrder : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => 10000;
        public bool OnPreprocessAvatar(GameObject avatar)
        {
            var descriptor = avatar.GetComponent<VRCAvatarDescriptor>();
            if (!descriptor) return true;
            var playableLayers = (descriptor.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>())
                .Concat(descriptor.specialAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>()).ToArray();
            var fx = playableLayers.FirstOrDefault(l=>l.type==VRCAvatarDescriptor.AnimLayerType.FX).animatorController as AnimatorController;
            if (!fx) return true;
            var original = fx.layers;
            var order = Enumerable.Range(0, original.Length)
                .Where(i=>!original[i].name.StartsWith("Hydro FT Ears / ",StringComparison.Ordinal))
                .Concat(Enumerable.Range(0, original.Length).Where(i=>original[i].name.StartsWith("Hydro FT Ears / ",StringComparison.Ordinal))).ToArray();
            if (order.SequenceEqual(Enumerable.Range(0,original.Length))) return true;
            var remap = new int[original.Length];
            for(int i=0;i<order.Length;i++) remap[order[i]]=i;
            var visited = new HashSet<StateMachineBehaviour>();
            foreach(var controller in playableLayers.Select(l=>l.animatorController).OfType<AnimatorController>().Distinct())
                foreach(var layer in controller.layers)
                    foreach(var behaviour in Behaviours(layer.stateMachine))
                        if(visited.Add(behaviour) && behaviour is VRCAnimatorLayerControl control &&
                           control.playable.ToString()=="FX" && control.layer>=0 && control.layer<remap.Length)
                            control.layer=remap[control.layer];
            foreach(var layer in original)
                if(layer.syncedLayerIndex>=0) layer.syncedLayerIndex=remap[layer.syncedLayerIndex];
            fx.layers=order.Select(i=>original[i]).ToArray();
            return true;
        }
        static IEnumerable<StateMachineBehaviour> Behaviours(AnimatorStateMachine sm)
        {
            return sm.behaviours.Concat(sm.states.SelectMany(s=>s.state.behaviours))
                .Concat(sm.stateMachines.SelectMany(s=>Behaviours(s.stateMachine)));
        }
    }
}