using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;

public class EnemyAnimationSetup : EditorWindow
{
    [MenuItem("Tools/Mad Doctor/Setup Enemy Animations")]
    public static void SetupAnimations()
    {
        string spriteBaseFolder = "Assets/Mad Doctor Assets/Sprites/Enemy";
        string animBaseFolder = "Assets/Mad Doctor Assets/Animations/Enemy";
        
        if (!AssetDatabase.IsValidFolder("Assets/Mad Doctor Assets/Animations"))
            AssetDatabase.CreateFolder("Assets/Mad Doctor Assets", "Animations");
        if (!AssetDatabase.IsValidFolder("Assets/Mad Doctor Assets/Animations/Enemy"))
            AssetDatabase.CreateFolder("Assets/Mad Doctor Assets/Animations", "Enemy");

        string[] enemyFolders = { 
            "Enemy Character01", "Enemy Character02", "Enemy Character03", 
            "Enemy Character04", "Enemy Character05", "Enemy Character06" 
        };
        string[] prefabNames = { "Enemy_1", "Enemy02", "Enemy03", "Enemy04", "Enemy05", "Enemy06" };

        for (int i = 0; i < enemyFolders.Length; i++)
        {
            string eFolder = enemyFolders[i];
            string pName = prefabNames[i];
            string eAnimFolder = animBaseFolder + "/" + eFolder;

            if (!AssetDatabase.IsValidFolder(eAnimFolder))
                AssetDatabase.CreateFolder(animBaseFolder, eFolder);

            // Create clips
            AnimationClip idleClip = CreateClip(spriteBaseFolder + "/" + eFolder + "/Idle", eAnimFolder + "/Idle.anim", true);
            AnimationClip walkClip = CreateClip(spriteBaseFolder + "/" + eFolder + "/Walk", eAnimFolder + "/Walk.anim", true);
            AnimationClip hitClip = CreateClip(spriteBaseFolder + "/" + eFolder + "/Hit", eAnimFolder + "/Hit.anim", false);
            AnimationClip getHitClip = CreateClip(spriteBaseFolder + "/" + eFolder + "/Get Hit", eAnimFolder + "/GetHit.anim", false);
            AnimationClip deathClip = CreateClip(spriteBaseFolder + "/" + eFolder + "/Death", eAnimFolder + "/Death.anim", false);

            // Create Controller
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(eAnimFolder + "/" + pName + "_Controller.controller");

            // Add Parameters
            controller.AddParameter("isWalking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("GetHit", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            // Setup States
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            AnimatorState idleState = rootStateMachine.AddState("Idle");
            idleState.motion = idleClip;

            AnimatorState walkState = rootStateMachine.AddState("Walk");
            walkState.motion = walkClip;

            AnimatorState hitState = rootStateMachine.AddState("Hit");
            hitState.motion = hitClip;

            AnimatorState getHitState = rootStateMachine.AddState("GetHit");
            getHitState.motion = getHitClip;

            AnimatorState deathState = rootStateMachine.AddState("Death");
            deathState.motion = deathClip;

            // Setup Transitions
            // Idle <-> Walk
            var idleToWalk = idleState.AddTransition(walkState);
            idleToWalk.AddCondition(AnimatorConditionMode.If, 0, "isWalking");
            idleToWalk.hasExitTime = false;

            var walkToIdle = walkState.AddTransition(idleState);
            walkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isWalking");
            walkToIdle.hasExitTime = false;

            // Any -> Attack (Hit)
            var anyToAttack = rootStateMachine.AddAnyStateTransition(hitState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            anyToAttack.hasExitTime = false;
            
            var attackExit = hitState.AddExitTransition();
            attackExit.hasExitTime = true;
            attackExit.exitTime = 1f;

            // Any -> GetHit
            var anyToGetHit = rootStateMachine.AddAnyStateTransition(getHitState);
            anyToGetHit.AddCondition(AnimatorConditionMode.If, 0, "GetHit");
            anyToGetHit.hasExitTime = false;

            var getHitExit = getHitState.AddExitTransition();
            getHitExit.hasExitTime = true;
            getHitExit.exitTime = 1f;

            // Any -> Die
            var anyToDie = rootStateMachine.AddAnyStateTransition(deathState);
            anyToDie.AddCondition(AnimatorConditionMode.If, 0, "Die");
            anyToDie.hasExitTime = false;

            // Apply to Prefab
            string prefabPath = "Assets/Mad Doctor Assets/Prefabs/" + pName + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                Animator anim = prefab.GetComponent<Animator>();
                if (anim == null) anim = prefab.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                EditorUtility.SetDirty(prefab);
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Enemy Animations and Controllers Setup Complete!");
    }

    static AnimationClip CreateClip(string spriteFolder, string savePath, bool isLooping)
    {
        if (!Directory.Exists(spriteFolder)) return null;

        string[] files = Directory.GetFiles(spriteFolder, "*.png");
        List<Sprite> sprites = new List<Sprite>();
        foreach (string file in files)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(file.Replace("\\", "/"));
            if (s != null) sprites.Add(s);
        }

        if (sprites.Count == 0) return null;

        sprites.Sort((a, b) => a.name.CompareTo(b.name));

        AnimationClip clip = new AnimationClip();
        clip.frameRate = 12f;
        if (isLooping)
        {
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Count];
        for (int i = 0; i < sprites.Count; i++)
        {
            keyFrames[i] = new ObjectReferenceKeyframe();
            keyFrames[i].time = i / clip.frameRate;
            keyFrames[i].value = sprites[i];
        }

        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);
        AssetDatabase.CreateAsset(clip, savePath);
        return clip;
    }
}
