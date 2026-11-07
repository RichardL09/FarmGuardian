using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    // Select the actual runtime Animator rather than a controller asset or saved scenery.
    public static class FarmAnimatorInspector
    {
        [MenuItem("Tools/Farm Guardian/Inspect Animator/Farmer (Player)")]
        public static void Farmer() => Open("Farmer");
        [MenuItem("Tools/Farm Guardian/Inspect Animator/Burglar (Thief)")]
        public static void Burglar() => Open("Burglar");
        [MenuItem("Tools/Farm Guardian/Inspect Animator/Cow")]
        public static void Cow() => Open("Cow");

        static void Open(string character)
        {
            if (!EditorApplication.isPlaying)
            {
                string path = character == "Cow" ? "Assets/FarmGuardian/Animation/cow.controller" :
                    "Assets/FarmGuardian/Animation/Characters/"+character+".controller";
                var controller=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                if(controller==null){Debug.LogWarning("Animator Controller missing: "+path);return;}
                Selection.activeObject=controller;
                EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
                EditorGUIUtility.PingObject(controller);
                Debug.Log(character+": showing the controller graph in Edit Mode. For live parameters/state progress, press Play, start a level and use this menu again. Unlock the Animator window if it is pinned to another controller.",controller);
                return;
            }
            var session=Object.FindFirstObjectByType<FarmSession>();
            Animator animator=null;
            if(session!=null)
            {
                if(character=="Farmer"&&session.Player!=null)animator=session.Player.Visual.GetComponentInChildren<Animator>(true);
                if(character=="Burglar"&&session.Thief!=null)animator=session.Thief.Visual.GetComponentInChildren<Animator>(true);
                if(character=="Cow")animator=session.Animals.FirstOrDefault(a=>a.TypeIndex==0)?.Animator;
            }
            if(animator==null){Debug.LogWarning("Start a Farm Guardian level first, then choose Inspect Animator / "+character+" again.");return;}
            Selection.activeGameObject=animator.gameObject;
            EditorApplication.ExecuteMenuItem("Window/Animation/Animator");
            EditorGUIUtility.PingObject(animator.gameObject);
            Debug.Log(character+": selected the live Animator. Enable Auto Live Link and unlock the Animator window to follow this object. A paused game or inactive thief does not advance animation.",animator);
        }
    }
}
