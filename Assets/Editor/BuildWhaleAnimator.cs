// 职责：从 Tripo 导出的 FBX 生成动画控制器（Speed 混合 + Slash/Jump 触发）
// 依赖：UnityEditor.Animations、Resources/PlayerModel.fbx
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class BuildWhaleAnimator
{
    const string ModelPath = "Assets/Resources/PlayerModel.fbx";
    const string ControllerPath = "Assets/Resources/WhaleGirl.controller";

    [MenuItem("Wilderness/46. Build Character Animator")]
    public static void Build()
    {
        // --- find the clips inside the FBX
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            var clip = obj as AnimationClip;
            if (clip == null) continue;
            clips[clip.name.ToLowerInvariant()] = clip;
            Debug.Log("[Anim] clip: " + clip.name + " len=" + clip.length.ToString("F2") + "s");
        }
        if (clips.Count == 0) { Debug.LogError("[Anim] no clips in " + ModelPath); return; }

        AnimationClip Find(string key)
        {
            foreach (var kv in clips)
                if (kv.Key.Contains(key)) return kv.Value;
            return null;
        }

        var idle = Find("idle");
        var walk = Find("walk");
        var run = Find("run");
        var slash = Find("slash");
        var jump = Find("jump");

        // --- controller
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Slash", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;

        // locomotion blend tree
        var blend = new BlendTree();
        AssetDatabase.AddObjectToAsset(blend, ctrl);
        blend.blendType = BlendTreeType.Simple1D;
        blend.blendParameter = "Speed";
        blend.name = "Locomotion";
        blend.hideFlags = HideFlags.HideInHierarchy;

        if (idle != null) blend.AddChild(idle, 0f);
        if (walk != null) blend.AddChild(walk, 0.5f);
        if (run != null) blend.AddChild(run, 1f);

        var locoState = sm.AddState("Locomotion");
        locoState.motion = blend;
        locoState.speed = 1f;
        sm.defaultState = locoState;

        // action states
        void AddAction(string name, AnimationClip clip, string trigger, bool loop)
        {
            if (clip == null) return;
            var st = sm.AddState(name);
            st.motion = clip;
            if (loop)
            {
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }

            var enter = locoState.AddTransition(st);
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            enter.hasExitTime = false;
            enter.duration = 0.05f;

            var back = st.AddTransition(locoState);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.1f;
        }

        AddAction("Slash", slash, "Slash", false);
        AddAction("Jump", jump, "Jump", false);

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        Debug.Log("[Anim] controller saved -> " + ControllerPath
                  + "  idle=" + (idle != null) + " walk=" + (walk != null) + " run=" + (run != null)
                  + " slash=" + (slash != null) + " jump=" + (jump != null));
    }
}
