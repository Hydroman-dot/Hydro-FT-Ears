using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;
using nadena.dev.modular_avatar.core;
using Object = UnityEngine.Object;

namespace Hydro.Tools.FTEars
{
    public sealed class HydroFTEarsWizard : EditorWindow
    {
        public const string Root = "Assets/HydroTools/HydroFTEars";
        public const string Enable = "Hydro/FTEars/Enabled", Intensity = "Hydro/FTEars/Intensity";
        VRCAvatarDescriptor avatar;
        HydroFTEars module;
        Vector2 scroll;
        Dictionary<string, AnimatorControllerParameterType> parameters = new Dictionary<string, AnimatorControllerParameterType>();
        string report = "", error = "";
        bool preview;
        float px, py, pe, strength = 0.65f;
        double previewStart;
        Quaternion leftRest, rightRest;
        Transform previewLeft, previewRight;

        [MenuItem("Hydro Tools/Hydro FT Ears")]
        public static void Open()
        {
            var w = GetWindow<HydroFTEarsWizard>("Hydro FT Ears");
            w.minSize = new Vector2(560, 680);
            w.UseSelection();
        }

        void OnEnable() { EditorApplication.update += Tick; AssemblyReloadEvents.beforeAssemblyReload += StopPreview; EditorApplication.playModeStateChanged += PlayChanged; }
        void OnDisable() { StopPreview(); EditorApplication.update -= Tick; AssemblyReloadEvents.beforeAssemblyReload -= StopPreview; EditorApplication.playModeStateChanged -= PlayChanged; }
        void PlayChanged(PlayModeStateChange state) { StopPreview(); }
        void UseSelection()
        {
            StopPreview();
            var go = Selection.activeGameObject;
            avatar = go ? go.GetComponentInParent<VRCAvatarDescriptor>() : null;
            if (!avatar) avatar = Object.FindObjectsOfType<VRCAvatarDescriptor>().FirstOrDefault();
            module = avatar ? avatar.GetComponentInChildren<HydroFTEars>(true) : null;
            Scan();
        }

        public static List<AnimatorController> Controllers(VRCAvatarDescriptor a)
        {
            var found = new HashSet<RuntimeAnimatorController>();
            foreach (var layer in (a.baseAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>()).Concat(a.specialAnimationLayers ?? Array.Empty<VRCAvatarDescriptor.CustomAnimLayer>()))
                if (layer.animatorController) found.Add(layer.animatorController);
            foreach (var c in a.GetComponentsInChildren<Component>(true))
            {
                if (!c) continue;
                if (c is ModularAvatarMergeAnimator ma && ma.animator) found.Add(ma.animator);
                if (c is Animator an && an.runtimeAnimatorController) found.Add(an.runtimeAnimatorController);
                if (!c.GetType().FullName.Contains("VRCFury")) continue;
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                while (it.Next(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue is RuntimeAnimatorController ac) found.Add(ac);
                    if (it.propertyType == SerializedPropertyType.String)
                    {
                        var s = it.stringValue;
                        if (string.IsNullOrEmpty(s) || !s.EndsWith(".controller", StringComparison.OrdinalIgnoreCase)) continue;
                        int i = s.IndexOf("Assets/", StringComparison.Ordinal);
                        if (i >= 0)
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(s.Substring(i));
                            if (asset) found.Add(asset);
                        }
                    }
                }
            }
            return found.Select(c => c is AnimatorOverrideController o ? o.runtimeAnimatorController : c).OfType<AnimatorController>().Where(c=>c.name!="HydroFTEars").Distinct().ToList();
        }

        public static Dictionary<string, AnimatorControllerParameterType> Parameters(VRCAvatarDescriptor a)
        {
            var result = new Dictionary<string, AnimatorControllerParameterType>();
            foreach (var c in Controllers(a))
                foreach (var p in c.parameters) result[p.name] = p.type;
            return result;
        }

        void Scan()
        {
            parameters.Clear();
            if (!avatar) { report = "Avatar auswählen."; return; }
            var cs = Controllers(avatar);
            parameters = Parameters(avatar);
            report = cs.Count + " Controller über Avatar, Modular Avatar und VRCFury gefunden.\n" +
                string.Join("\n", cs.Where(c=>c.parameters.Any(p=>p.name.Contains("EchoFT"))).Select(c=>AssetDatabase.GetAssetPath(c)));
        }

        public static void AutoMap(HydroFTEars m, VRCAvatarDescriptor a)
        {
            var p = Parameters(a);
            Func<string[], string> pick = choices => choices.FirstOrDefault(n=>p.TryGetValue(n,out var t) && t==AnimatorControllerParameterType.Float) ?? "";
            var ts = a.GetComponentsInChildren<Transform>(true);
            Func<string[], Transform> ear = names => names.Select(n=>ts.Where(t=>string.Equals(t.name,n,StringComparison.OrdinalIgnoreCase)).ToArray()).Where(x=>x.Length==1).Select(x=>x[0]).FirstOrDefault();
            if (!m.leftEar) m.leftEar = ear(new[]{"Ear_L","Ear.L","LeftEar","ear_left","EarLeft"});
            if (!m.rightEar) m.rightEar = ear(new[]{"Ear_R","Ear.R","RightEar","ear_right","EarRight"});
            if(m.leftEar){m.leftLook=m.leftEar.InverseTransformDirection(a.transform.up)*7; m.leftUp=m.leftEar.InverseTransformDirection(a.transform.right)*-4;}
            if(m.rightEar){m.rightLook=m.rightEar.InverseTransformDirection(a.transform.up)*7; m.rightUp=m.rightEar.InverseTransformDirection(a.transform.right)*-4;}
            m.eyeXLeft = pick(new[]{"OSCm/Proxy/EchoFT/v2/EyeLeftX","OSCm/Proxy/EchoFT/v2/EyeX","EchoFT/v2/EyeLeftX","EchoFT/v2/EyeX","v2/EyeLeftX","v2/EyeX","EyeLeftX","EyeX"});
            m.eyeXRight = pick(new[]{"OSCm/Proxy/EchoFT/v2/EyeRightX","OSCm/Proxy/EchoFT/v2/EyeX","EchoFT/v2/EyeRightX","EchoFT/v2/EyeX","v2/EyeRightX","v2/EyeX","EyeRightX","EyeX"});
            m.eyeY = pick(new[]{"OSCm/Proxy/EchoFT/v2/EyeY","EchoFT/v2/EyeY","v2/EyeY","EyeY"});
            m.expressionLeft = pick(new[]{"OSCm/Proxy/EchoFT/v2/SmileFrownLeft","EchoFT/v2/SmileFrownLeft","EchoFT/v2/SmileFrown","v2/SmileFrownLeft","v2/SmileFrown"});
            m.expressionRight = pick(new[]{"OSCm/Proxy/EchoFT/v2/SmileFrownRight","EchoFT/v2/SmileFrownRight","EchoFT/v2/SmileFrown","v2/SmileFrownRight","v2/SmileFrown"});
            m.frownLeft = pick(new[]{"v2/MouthFrownLeft","MouthFrownLeft","FrownLeft"});
            m.frownRight = pick(new[]{"v2/MouthFrownRight","MouthFrownRight","FrownRight"});
            m.expressionSigned = m.expressionLeft.Contains("SmileFrown") || m.expressionRight.Contains("SmileFrown");
            if(string.IsNullOrEmpty(m.expressionLeft)) m.expressionLeft=pick(new[]{"v2/MouthSmileLeft","MouthSmileLeft","SmileLeft","Smile"});
            if(string.IsNullOrEmpty(m.expressionRight)) m.expressionRight=pick(new[]{"v2/MouthSmileRight","MouthSmileRight","SmileRight","Smile"});
            EditorUtility.SetDirty(m);
        }

        public static HydroFTEars CreateModule(VRCAvatarDescriptor a)
        {
            var existing = a.GetComponentInChildren<HydroFTEars>(true);
            if (existing) return existing;
            var go = new GameObject("Hydro FT Ears");
            Undo.RegisterCreatedObjectUndo(go, "Create Hydro FT Ears");
            go.transform.SetParent(a.transform,false);
            var m = Undo.AddComponent<HydroFTEars>(go);
            AutoMap(m,a);
            EditorSceneManager.MarkSceneDirty(a.gameObject.scene);
            return m;
        }

        void MapField(SerializedObject so, string property, string label)
        {
            var p = so.FindProperty(property);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(p,new GUIContent(label));
            if (GUILayout.Button("…",GUILayout.Width(28)))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("(aus)"),string.IsNullOrEmpty(p.stringValue),()=> { StopPreview(); Undo.RecordObject(module,"FT parameter"); var s=new SerializedObject(module); s.FindProperty(property).stringValue=""; s.ApplyModifiedProperties(); });
                foreach(var name in parameters.Where(k=>k.Value==AnimatorControllerParameterType.Float).Select(k=>k.Key).OrderBy(k=>k))
                {
                    var key=name;
                    menu.AddItem(new GUIContent(key.Replace("/"," ∕ ")),p.stringValue==key,()=> { StopPreview(); Undo.RecordObject(module,"FT parameter"); var s=new SerializedObject(module); s.FindProperty(property).stringValue=key; s.ApplyModifiedProperties(); });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
        }

        void OnGUI()
        {
            EditorGUIUtility.labelWidth=225;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Hydro FT Ears",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Ergänzt Ohrbewegungen über additive FX-Layer. FT-Parameter werden gelesen, nicht ersetzt. Winkel sind lokale Grad pro Ohr.",MessageType.Info);
            EditorGUI.BeginChangeCheck();
            var next=(VRCAvatarDescriptor)EditorGUILayout.ObjectField("Avatar",avatar,typeof(VRCAvatarDescriptor),true);
            if(EditorGUI.EndChangeCheck()){StopPreview(); avatar=next; module=avatar?avatar.GetComponentInChildren<HydroFTEars>(true):null; Scan();}
            if(GUILayout.Button("Aktuelle Auswahl / erneut prüfen")) UseSelection();
            EditorGUILayout.HelpBox(report,MessageType.None);
            if(!avatar){EditorGUILayout.EndScrollView();return;}
            if(!module)
            {
                if(GUILayout.Button("Modulares GameObject anlegen")) {module=CreateModule(avatar); Scan();}
                EditorGUILayout.EndScrollView();return;
            }
            using(new EditorGUI.DisabledScope(preview))
            {
                if(GUILayout.Button("Ohr-Bones und FT automatisch zuordnen")) {Undo.RecordObject(module,"Detect FT"); AutoMap(module,avatar);Scan();}
                var so=new SerializedObject(module); so.Update();
                EditorGUILayout.PropertyField(so.FindProperty("leftEar"),new GUIContent("Linkes Ohr"));
                EditorGUILayout.PropertyField(so.FindProperty("rightEar"),new GUIContent("Rechtes Ohr"));
                MapField(so,"eyeXLeft","Blick X links (−1…1)");
                MapField(so,"eyeXRight","Blick X rechts (−1…1)");
                MapField(so,"eyeY","Blick Y (−1…1)");
                MapField(so,"expressionLeft","Smile/Frown links");
                MapField(so,"expressionRight","Smile/Frown rechts");
                MapField(so,"frownLeft","Separates Frown links (optional)");
                MapField(so,"frownRight","Separates Frown rechts (optional)");
                EditorGUILayout.PropertyField(so.FindProperty("expressionSigned"),new GUIContent("Negativ = Frown, positiv = Smile"));
                EditorGUILayout.HelpBox("Unsigniert: 0 neutral bis 1 Smile. Leer deaktiviert den jeweiligen Treiber. Fremde Systeme benötigen passende Float-Parameter und gegebenenfalls angepasste Achsen.",MessageType.None);
                EditorGUILayout.PropertyField(so.FindProperty("defaultIntensity"),new GUIContent("Standardintensität (0–5)"));
                foreach(var field in new[]{"leftLook","rightLook","leftUp","rightUp","leftSmile","rightSmile","leftFrown","rightFrown","leftIdle","rightIdle"}) EditorGUILayout.PropertyField(so.FindProperty(field));
                EditorGUILayout.PropertyField(so.FindProperty("idleEnabled"),new GUIContent("Sanfte Idle-Bewegung"));
                EditorGUILayout.PropertyField(so.FindProperty("idlePeriod"),new GUIContent("Idle-Periode (Sekunden)"));
                MapField(so,"idleParameter","Idle-Stärke (optional 0…1)");
                so.ApplyModifiedProperties();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live-Vorschau (simulierte FT-Eingaben)",EditorStyles.boldLabel);
            px=EditorGUILayout.Slider("Blick X",px,-1,1); py=EditorGUILayout.Slider("Blick Y",py,-1,1);
            pe=EditorGUILayout.Slider("Frown ← Ausdruck → Smile",pe,-1,1);
            strength=EditorGUILayout.Slider("Gesamtintensität (0–5)",strength,0,5);
            if(GUILayout.Button(preview?"Vorschau stoppen / Pose wiederherstellen":"Live-Vorschau starten"))
            {
                if(preview) StopPreview();
                else if(module.leftEar && module.rightEar && !EditorApplication.isPlaying && !AnimationMode.InAnimationMode())
                {
                    previewLeft=module.leftEar; previewRight=module.rightEar;
                    leftRest=previewLeft.localRotation; rightRest=previewRight.localRotation;
                    previewStart=EditorApplication.timeSinceStartup;
                    AnimationMode.StartAnimationMode(); preview=true;
                }
                else error="Für die Vorschau beide Ohren zuweisen; Play Mode und andere Animationsvorschauen beenden.";
            }
            using(new EditorGUI.DisabledScope(preview || EditorApplication.isPlaying))
            {
                if(GUILayout.Button("Controller und VRChat-Menü erstellen / aktualisieren"))
                {
                    try { Build(module,avatar);error=""; } catch(Exception ex) {error=ex.Message; Debug.LogException(ex);}
                }
                if(GUILayout.Button("Eigenes .unitypackage exportieren"))
                {
                    var path=EditorUtility.SaveFilePanel("Hydro FT Ears exportieren","","HydroFTEars.unitypackage","unitypackage");
                    if(!string.IsNullOrEmpty(path)) {try{Export(path);error="";}catch(Exception ex){error=ex.Message;}}
                }
            }
            if(!string.IsNullOrEmpty(module.generatedFolder)) EditorGUILayout.LabelField("Generiert: "+module.generatedFolder,EditorStyles.wordWrappedLabel);
            if(!string.IsNullOrEmpty(error)) EditorGUILayout.HelpBox(error,MessageType.Error);
            EditorGUILayout.EndScrollView();
        }

        static Vector3 Expression(HydroFTEars m,bool left,float value)
        {
            return value>=0 ? (left?m.leftSmile:m.rightSmile)*value :
                (m.expressionSigned?(left?m.leftFrown:m.rightFrown)*-value:Vector3.zero);
        }
        void Tick()
        {
            if(!preview)return;
            if(!module || !previewLeft || !previewRight || EditorApplication.isPlayingOrWillChangePlaymode){StopPreview();return;}
            float wave=module.idleEnabled?(float)Math.Sin((EditorApplication.timeSinceStartup-previewStart)*Math.PI*2/Math.Max(.5,module.idlePeriod)):0;
            PreviewRotation(previewLeft,leftRest,leftRest*Quaternion.Euler(strength*(module.leftLook*px+module.leftUp*py+Expression(module,true,pe)+module.leftIdle*wave)));
            PreviewRotation(previewRight,rightRest,rightRest*Quaternion.Euler(strength*(module.rightLook*px+module.rightUp*py+Expression(module,false,pe)+module.rightIdle*wave)));
            SceneView.RepaintAll();Repaint();
        }
        static void PreviewRotation(Transform t,Quaternion rest,Quaternion value)
        {
            string[] axes={"x","y","z","w"};
            for(int i=0;i<4;i++)
            {
                var binding=EditorCurveBinding.FloatCurve("",typeof(Transform),"m_LocalRotation."+axes[i]);
                AnimationMode.AddPropertyModification(binding,new PropertyModification {target=t,propertyPath="m_LocalRotation."+axes[i],value=rest[i].ToString(System.Globalization.CultureInfo.InvariantCulture)},true);
            }
            t.localRotation=value;
        }
        void StopPreview()
        {
            if(!preview)return;
            preview=false;
            AnimationMode.StopAnimationMode();
            if(previewLeft)previewLeft.localRotation=leftRest;
            if(previewRight)previewRight.localRotation=rightRest;
            previewLeft=null;previewRight=null;
            SceneView.RepaintAll();
        }

        static string PathOf(Transform t,VRCAvatarDescriptor a) {return AnimationUtility.CalculateTransformPath(t,a.transform);}
        static void Validate(HydroFTEars m,VRCAvatarDescriptor a)
        {
            if(!m||!a||!m.transform.IsChildOf(a.transform))throw new InvalidOperationException("Modul muss unter dem Avatar liegen.");
            if(!m.leftEar||!m.rightEar||m.leftEar==m.rightEar)throw new InvalidOperationException("Zwei unterschiedliche Ohr-Bones zuweisen.");
            if(!m.leftEar.IsChildOf(a.transform)||!m.rightEar.IsChildOf(a.transform))throw new InvalidOperationException("Beide Ohren müssen zum Avatar gehören.");
            if(m.leftEar.IsChildOf(m.rightEar)||m.rightEar.IsChildOf(m.leftEar))throw new InvalidOperationException("Ohr-Bones dürfen nicht ineinander verschachtelt sein.");
            var p=Parameters(a);
            foreach(var n in new[]{m.eyeXLeft,m.eyeXRight,m.eyeY,m.expressionLeft,m.expressionRight,m.frownLeft,m.frownRight,m.idleParameter}.Where(n=>!string.IsNullOrWhiteSpace(n)))
                if(!p.TryGetValue(n,out var type)||type!=AnimatorControllerParameterType.Float)throw new InvalidOperationException("Kein Float-Parameter im Avatar gefunden: "+n);
            foreach(var t in new[]{m.leftEar,m.rightEar})
            {
                var path=PathOf(t,a);
                if(a.GetComponentsInChildren<Transform>(true).Count(x=>PathOf(x,a)==path)!=1)throw new InvalidOperationException("Nicht eindeutiger Bone-Pfad: "+path);
            }
        }

        sealed class Builder
        {
            public AnimatorController controller;
            public string folder;
            public Transform[] ears;
            public string[] paths;
            public Quaternion[] rest;
            public AnimationClip neutral;
            int index;
            public float duration = 5;
            public void Param(string p,AnimatorControllerParameterType type,float value=0)
            {
                if(string.IsNullOrEmpty(p)||controller.parameters.Any(x=>x.name==p))return;
                controller.AddParameter(new AnimatorControllerParameter{name=p,type=type,defaultFloat=value,defaultBool=value>.5f});
            }
            public AnimationClip Clip(string name,Vector3 left,Vector3 right,bool loop=false,float period=5)
            {
                var clip=new AnimationClip{name=name,frameRate=60};
                for(int e=0;e<2;e++)
                {
                    var offset=e==0?left:right;
                    var keys=new List<Keyframe>[4];for(int j=0;j<4;j++)keys[j]=new List<Keyframe>();
                    int samples=loop?80:1;
                    for(int s=0;s<=samples;s++)
                    {
                        float time=loop?period*s/samples:s*duration;
                        float amount=loop?Mathf.Sin(2*Mathf.PI*s/samples):1;
                        Quaternion q=rest[e]*Quaternion.Euler(offset*amount*5f);
                        for(int j=0;j<4;j++)keys[j].Add(new Keyframe(time,q[j]));
                    }
                    string[] axes={"x","y","z","w"};
                    for(int j=0;j<4;j++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(paths[e],typeof(Transform),"m_LocalRotation."+axes[j]),new AnimationCurve(keys[j].ToArray()));
                }
                clip.EnsureQuaternionContinuity();
                var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
                AssetDatabase.CreateAsset(clip,folder+"/"+(index++).ToString("D2")+"_"+name+".anim");
                if(neutral)AnimationUtility.SetAdditiveReferencePose(clip,neutral,0);
                return clip;
            }
            public BlendTree Tree(string name,string parameter,params ChildMotion[] children)
            {
                Param(parameter,AnimatorControllerParameterType.Float);
                var tree=new BlendTree{name=name,blendType=BlendTreeType.Simple1D,blendParameter=parameter,useAutomaticThresholds=false,children=children};
                AssetDatabase.AddObjectToAsset(tree,controller);
                return tree;
            }
            public ChildMotion Child(Motion motion,float threshold) {return new ChildMotion{motion=motion,threshold=threshold,timeScale=1};}
            public void Layer(string name,Motion motion)
            {
                var sm=new AnimatorStateMachine{name=name};AssetDatabase.AddObjectToAsset(sm,controller);
                var layer=new AnimatorControllerLayer{name="Hydro FT Ears / "+name,stateMachine=sm,defaultWeight=1,blendingMode=AnimatorLayerBlendingMode.Additive};
                controller.AddLayer(layer);
                var off=sm.AddState("Disabled");off.motion=neutral;off.writeDefaultValues=false;
                var on=sm.AddState("Enabled");on.motion=Tree(name+" Intensity",Intensity,Child(neutral,0),Child(motion,1));on.writeDefaultValues=false;
                sm.defaultState=on;
                var enter=off.AddTransition(on);enter.hasExitTime=false;enter.duration=.08f;enter.AddCondition(AnimatorConditionMode.If,0,Enable);
                var leave=on.AddTransition(off);leave.hasExitTime=false;leave.duration=.08f;leave.AddCondition(AnimatorConditionMode.IfNot,0,Enable);
            }
            public void Signed(string name,string parameter,Vector3 minusL,Vector3 minusR,Vector3 plusL,Vector3 plusR,bool signed=true)
            {
                if(string.IsNullOrEmpty(parameter))return;
                var positive=Clip(name+"_Positive",plusL,plusR);
                Motion tree=signed?Tree(name,parameter,Child(Clip(name+"_Negative",minusL,minusR),-1),Child(neutral,0),Child(positive,1)):
                    Tree(name,parameter,Child(neutral,0),Child(positive,1));
                Layer(name,tree);
            }
        }

        public static void Build(HydroFTEars m,VRCAvatarDescriptor a)
        {
            if(AnimationMode.InAnimationMode())throw new InvalidOperationException("Vorschau vor dem Erstellen stoppen.");
            Validate(m,a);
            Directory.CreateDirectory(Root+"/Generated");
            string folder=Root+"/Generated/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N").Substring(0,6);
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"/HydroFTEars.controller");
            var baseState=controller.layers[0].stateMachine.AddState("No override");
            baseState.writeDefaultValues=false;
            var b=new Builder{controller=controller,folder=folder,ears=new[]{m.leftEar,m.rightEar},paths=new[]{PathOf(m.leftEar,a),PathOf(m.rightEar,a)},rest=new[]{m.leftEar.localRotation,m.rightEar.localRotation},duration=Mathf.Max(.5f,m.idlePeriod)};
            b.Param(Enable,AnimatorControllerParameterType.Bool,1);
            b.Param(Intensity,AnimatorControllerParameterType.Float,Mathf.Clamp(m.defaultIntensity,0,5)/5f);
            b.neutral=b.Clip("NeutralReference",Vector3.zero,Vector3.zero);
            AnimationUtility.SetAdditiveReferencePose(b.neutral,b.neutral,0);
            b.Signed("Eye X Left",m.eyeXLeft,-m.leftLook,Vector3.zero,m.leftLook,Vector3.zero);
            b.Signed("Eye X Right",m.eyeXRight,Vector3.zero,-m.rightLook,Vector3.zero,m.rightLook);
            b.Signed("Eye Y",m.eyeY,-m.leftUp,-m.rightUp,m.leftUp,m.rightUp);
            b.Signed("Expression Left",m.expressionLeft,m.leftFrown,Vector3.zero,m.leftSmile,Vector3.zero,m.expressionSigned);
            b.Signed("Expression Right",m.expressionRight,Vector3.zero,m.rightFrown,Vector3.zero,m.rightSmile,m.expressionSigned);
            b.Signed("Frown Left",m.frownLeft,Vector3.zero,Vector3.zero,m.leftFrown,Vector3.zero,false);
            b.Signed("Frown Right",m.frownRight,Vector3.zero,Vector3.zero,Vector3.zero,m.rightFrown,false);
            if(m.idleEnabled)
            {
                Motion idle=b.Clip("Idle",m.leftIdle,m.rightIdle,true,Mathf.Max(.5f,m.idlePeriod));
                if(!string.IsNullOrEmpty(m.idleParameter))idle=b.Tree("Idle driver",m.idleParameter,b.Child(b.neutral,0),b.Child(idle,1));
                b.Layer("Idle",idle);
            }
            var menu=ScriptableObject.CreateInstance<VRCExpressionsMenu>();menu.name="Hydro FT Ears";
            menu.controls.Add(new VRCExpressionsMenu.Control{name="Enabled",type=VRCExpressionsMenu.Control.ControlType.Toggle,parameter=new VRCExpressionsMenu.Control.Parameter{name=Enable},value=1});
            menu.controls.Add(new VRCExpressionsMenu.Control{name="Intensity (x5)",type=VRCExpressionsMenu.Control.ControlType.RadialPuppet,subParameters=new[]{new VRCExpressionsMenu.Control.Parameter{name=Intensity}}});
            AssetDatabase.CreateAsset(menu,folder+"/Controls.asset");
            var parentMenu=ScriptableObject.CreateInstance<VRCExpressionsMenu>();parentMenu.name="Hydro FT Ears Menu";
            parentMenu.controls.Add(new VRCExpressionsMenu.Control{name="Hydro FT Ears",type=VRCExpressionsMenu.Control.ControlType.SubMenu,subMenu=menu});
            AssetDatabase.CreateAsset(parentMenu,folder+"/Menu.asset");
            Undo.RecordObject(m,"Build Hydro FT Ears");
            var merge=m.GetComponent<ModularAvatarMergeAnimator>()??Undo.AddComponent<ModularAvatarMergeAnimator>(m.gameObject);
            Undo.RecordObject(merge,"Configure ears animator");
            merge.animator=controller;merge.layerType=VRCAvatarDescriptor.AnimLayerType.FX;merge.pathMode=MergeAnimatorPathMode.Absolute;merge.layerPriority=100;merge.matchAvatarWriteDefaults=false;
            var installer=m.GetComponent<ModularAvatarMenuInstaller>()??Undo.AddComponent<ModularAvatarMenuInstaller>(m.gameObject);
            Undo.RecordObject(installer,"Configure ears menu");installer.menuToAppend=parentMenu;
            var pars=m.GetComponent<ModularAvatarParameters>()??Undo.AddComponent<ModularAvatarParameters>(m.gameObject);
            Undo.RecordObject(pars,"Configure ears parameters");
            pars.parameters=new List<ParameterConfig>{
                new ParameterConfig{nameOrPrefix=Enable,syncType=ParameterSyncType.Bool,defaultValue=1,hasExplicitDefaultValue=true,saved=true},
                new ParameterConfig{nameOrPrefix=Intensity,syncType=ParameterSyncType.Float,defaultValue=Mathf.Clamp(m.defaultIntensity,0,5)/5f,hasExplicitDefaultValue=true,saved=true}
            };
            m.generatedFolder=folder;
            foreach(var obj in new Object[]{m,merge,installer,pars,controller})EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(a.gameObject.scene);
        }

        public static void Export(string destination)
        {
            EnsureTemplate();
            var paths=AssetDatabase.FindAssets("",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p=>!AssetDatabase.IsValidFolder(p) && !p.StartsWith(Root+"/Generated/",StringComparison.Ordinal)).Distinct().ToArray();
            AssetDatabase.ExportPackage(paths,destination,ExportPackageOptions.Default);
        }
        public static void EnsureTemplate()
        {
            Directory.CreateDirectory(Root+"/Prefabs");AssetDatabase.Refresh();
            string path=Root+"/Prefabs/Hydro FT Ears.prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path))return;
            var go=new GameObject("Hydro FT Ears");
            try {go.AddComponent<HydroFTEars>();PrefabUtility.SaveAsPrefabAsset(go,path);} finally {Object.DestroyImmediate(go);}
        }
    }
}
