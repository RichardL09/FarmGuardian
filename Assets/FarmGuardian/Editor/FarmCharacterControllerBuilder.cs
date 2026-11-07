using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static class FarmCharacterControllerBuilder
    {
        const string Folder = "Assets/FarmGuardian/Animation/Characters";
        static readonly string[] Bones = {"Body", "Body/Left thigh", "Body/Right thigh", "Body/Left thigh/Left shin", "Body/Right thigh/Right shin", "Body/Left shoulder", "Body/Right shoulder"};

        [MenuItem("Tools/Farm Guardian/Update Character Animator Controllers")]
        public static void Build()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            foreach (var name in new[] {"Farmer", "Burglar"})
            {
                string path = "Assets/FarmGuardian/Prefabs/Characters/" + name + ".prefab";
                var prefab = PrefabUtility.LoadPrefabContents(path);
                try { Attach(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, path); }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("FARM_CHARACTER_CONTROLLERS_OK: Farmer and Burglar use editable Animator Controllers and .anim clips.");
        }

        public static void Attach(GameObject root)
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = Controller(root.name.StartsWith("Burglar") ? "Burglar" : "Farmer");
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        static AnimationClip Clip(string name, bool walking, bool carrying, bool stunned)
        {
            string path = Folder + "/" + name + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            // Preserve artist edits on subsequent rig rebuilds.
            if (existing != null) return existing;
            var clip = new AnimationClip {name = name, frameRate = 30};
            for (int bone = 0; bone < Bones.Length; bone++)
                for (int axis = 0; axis < 3; axis++)
                {
                    var curve = new AnimationCurve();
                    for (int sample = 0; sample <= 60; sample++)
                    {
                        float time = sample / 60f, swing = walking ? Mathf.Sin(time * Mathf.PI * 2) : 0;
                        Vector3 rotation = Vector3.zero;
                        switch (bone)
                        {
                            case 0: rotation = stunned ? new Vector3(0,0,Mathf.Sin(time*Mathf.PI*2)*10) :
                                new Vector3((walking?3:0)+(carrying?5:0),swing*3,walking?Mathf.Cos(time*Mathf.PI*2)*(carrying?.6f:2):0); break;
                            case 1: rotation.x = swing*32; break;
                            case 2: rotation.x = -swing*32; break;
                            case 3: rotation.x = Mathf.Max(0,-swing)*26; break;
                            case 4: rotation.x = Mathf.Max(0,swing)*26; break;
                            case 5: rotation = new Vector3((carrying?-62:0)-swing*(carrying?3:27),carrying?12:0,carrying?-15:0); break;
                            case 6: rotation = new Vector3((carrying?-62:0)+swing*(carrying?3:27),carrying?-12:0,carrying?15:0); break;
                        }
                        curve.AddKey(time, rotation[axis]);
                    }
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(Bones[bone],typeof(Transform),"localEulerAnglesRaw."+"xyz"[axis]),curve);
                }
            var bounce = new AnimationCurve();
            for(int sample=0;sample<=60;sample++)bounce.AddKey(sample/60f,.72f+(walking?Mathf.Abs(Mathf.Sin(sample/60f*Mathf.PI*2))*.045f:0));
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("Body",typeof(Transform),"m_LocalPosition.y"),bounce);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; settings.loopBlend = true;
            AnimationUtility.SetAnimationClipSettings(clip,settings); AssetDatabase.CreateAsset(clip,path); return clip;
        }

        static AnimatorController Controller(string name)
        {
            string path = Folder + "/" + name + ".controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            var idle=Clip("Idle",false,false,false);var walk=Clip("Walk",true,false,false);
            var carryIdle=Clip("CarryIdle",false,true,false);var carryWalk=Clip("CarryWalk",true,true,false);var stun=Clip("Stunned",false,false,true);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            controller.AddParameter("WalkRate",AnimatorControllerParameterType.Float);
            controller.AddParameter("Carrying",AnimatorControllerParameterType.Bool);
            controller.AddParameter("Stunned",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;
            var normal=Locomotion(controller,machine,"Locomotion",idle,walk,new Vector3(240,40));
            var carry=Locomotion(controller,machine,"Carrying",carryIdle,carryWalk,new Vector3(240,160));
            var stunned=machine.AddState("Stunned",new Vector3(500,100));stunned.motion=stun;
            machine.defaultState=normal;
            Transition(normal,carry,false,true);Transition(carry,normal,false,false);
            var enter=machine.AddAnyStateTransition(stunned);enter.hasExitTime=false;enter.duration=.12f;enter.canTransitionToSelf=false;
            enter.AddCondition(AnimatorConditionMode.If,0,"Stunned");
            Transition(stunned,normal,false,false);Transition(stunned,carry,false,true);
            EditorUtility.SetDirty(controller);return controller;
        }
        static AnimatorState Locomotion(AnimatorController controller,AnimatorStateMachine machine,string name,AnimationClip idle,AnimationClip walk,Vector3 position)
        {
            var tree=new BlendTree{name=name+" speed blend",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(idle,0);tree.AddChild(walk,1.5f);
            var state=machine.AddState(name,position);state.motion=tree;state.speedParameter="WalkRate";state.speedParameterActive=true;return state;
        }
        static void Transition(AnimatorState from,AnimatorState to,bool stunned,bool carrying)
        {
            var transition=from.AddTransition(to);transition.hasExitTime=false;transition.duration=.12f;
            transition.AddCondition(stunned?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Stunned");
            transition.AddCondition(carrying?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Carrying");
        }
    }
}
