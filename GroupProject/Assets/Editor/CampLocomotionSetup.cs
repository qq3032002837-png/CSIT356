using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>Explicit, repeatable setup for the Camp scene. The request file is one-shot.</summary>
[InitializeOnLoad]
public static class CampLocomotionSetup
{
    private const string ScenePath = "Assets/Scenes/CampBaseScene.unity";
    private const string Folder = "Assets/Animations/CampPlayer";
    private const string ControllerPath = Folder + "/CampPlayerLocomotion.controller";
    private const string AvatarPath = Folder + "/CampPlayerAvatar.asset";
    private const string Male = "Assets/Kevin Iglesias/Human Animations/Animations/Male/";
    private const string PrefabPath = "Assets/Polytope Studio/Lowpoly_Characters/Prefabs/Modular_NPC/Peasants_Citizens/PT_Male_Modular_Free_Pack.prefab";
    private const string Pending = "Temp/CampLocomotionSetup.pending";
    private const string Report = "Logs/CampLocomotionSetup.txt";

    static CampLocomotionSetup() { EditorApplication.delayCall += RunPending; }
    private static void RunPending()
    {
        if (!File.Exists(Pending)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RunPending;
            return;
        }
        File.Delete(Pending);
        try { Configure(); }
        catch (Exception e) { File.WriteAllText(Report, "FAILED\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Camp Player/Configure Idle Walk Run")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before setup.");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        // Validate all clips before changing the scene.
        Clip("Idles/HumanM@Idle01.fbx");
        foreach (string direction in Directions)
        {
            Clip("Movement/Walk/HumanM@Walk01_" + direction + ".fbx");
            Clip("Movement/Run/HumanM@Run01_" + direction + ".fbx");
        }
        Avatar avatar = CreateAvatar();
        AnimatorController controller = CreateController();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        GameObject model = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == "PT_Male_Modular_Free_Pack").Select(t => t.gameObject).Single();
        string backup = "RepairBackups/CampLocomotion/" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);
        // Save a copy of the actual loaded scene, including any edits that were not on disk yet.
        if (!EditorSceneManager.SaveScene(scene, backup + "/CampBaseScene.before.unity", true))
            throw new IOException("Could not back up the loaded scene.");
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Configure Camp locomotion");
        int group = Undo.GetCurrentGroup();
        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = Undo.AddComponent<Animator>(model);
        Undo.RecordObject(animator, "Bind Camp animation");
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        CharacterController cc = model.GetComponent<CharacterController>();
        if (cc == null)
        {
            cc = Undo.AddComponent<CharacterController>(model);
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f; cc.skinWidth = 0.03f; cc.minMoveDistance = 0f;
        }
        PlayerMotor motor = model.GetComponent<PlayerMotor>();
        if (motor == null) motor = Undo.AddComponent<PlayerMotor>(model);
        var motorData = new SerializedObject(motor);
        motorData.FindProperty("walkSpeed").floatValue = 2f;
        motorData.FindProperty("allowRunning").boolValue = true;
        motorData.FindProperty("runSpeed").floatValue = 5f;
        motorData.ApplyModifiedProperties();
        PlayerLocomotionAnimator driver = model.GetComponent<PlayerLocomotionAnimator>();
        if (driver == null) driver = Undo.AddComponent<PlayerLocomotionAnimator>(model);
        var driverData = new SerializedObject(driver);
        driverData.FindProperty("characterAnimator").objectReferenceValue = animator;
        driverData.ApplyModifiedProperties();
        foreach (UnityEngine.Object obj in new UnityEngine.Object[] {animator, cc, motor, driver})
        {
            EditorUtility.SetDirty(obj);
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
        }
        // The template's free camera also consumes WASD; keep it from moving with the player.
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (var freeCamera in root.GetComponentsInChildren<UnityEngine.Rendering.FreeCamera>(true))
            {
                Undo.RecordObject(freeCamera, "Disable conflicting camera movement");
                freeCamera.enabled = false;
                EditorUtility.SetDirty(freeCamera);
            }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save Camp scene.");
        AssetDatabase.SaveAssets();
        string validation = ValidateAnimations(controller, avatar);
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Report, "SUCCESS\nScene: " + ScenePath + "\nBackup: " + backup
            + "\nAvatar valid: " + avatar.isValid + "; human: " + avatar.isHuman
            + "\nRoot motion: " + animator.applyRootMotion
            + "\nWalk: 2 m/s; Run: 5 m/s; Shift to run.\n" + validation);
        Undo.CollapseUndoOperations(group);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[CampLocomotion] SUCCESS. Idle, eight-way walk and eight-way run configured. " + validation);
    }

    private static readonly string[] Directions = {"Forward", "ForwardRight", "Right", "BackwardRight", "Backward", "BackwardLeft", "Left", "ForwardLeft"};
    private static readonly Vector2[] Positions = {new Vector2(0,1), new Vector2(.7071068f,.7071068f), new Vector2(1,0), new Vector2(.7071068f,-.7071068f), new Vector2(0,-1), new Vector2(-.7071068f,-.7071068f), new Vector2(-1,0), new Vector2(-.7071068f,.7071068f)};
    private static AnimationClip Clip(string relative)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(Male + relative).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        if (clip == null || !clip.isHumanMotion || !clip.isLooping)
            throw new InvalidOperationException("Missing, non-humanoid or non-looping clip: " + relative);
        return clip;
    }

    private static Avatar CreateAvatar()
    {
        GameObject instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        instance.name = "PT_Male_Modular_Free_Pack";
        instance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            var mapping = new Dictionary<string,string> {
                {"Hips","PT_Hips"}, {"Spine","PT_Spine"}, {"Chest","PT_Spine2"}, {"UpperChest","PT_Spine3"}, {"Neck","PT_Neck"}, {"Head","PT_Head"}
            };
            foreach (string side in new[] {"Left","Right"})
            {
                mapping[side+"UpperLeg"]="PT_"+side+"UpLeg";
                mapping[side+"LowerLeg"]="PT_"+side+"Leg";
                mapping[side+"Foot"]="PT_"+side+"Foot";
                mapping[side+"Toes"]="PT_"+side+"Toe";
                mapping[side+"Shoulder"]="PT_"+side+"Shoulder";
                mapping[side+"UpperArm"]="PT_"+side+"Arm";
                mapping[side+"LowerArm"]="PT_"+side+"ForeArm";
                mapping[side+"Hand"]="PT_"+side+"Hand";
                string[] fingers={"Thumb","Index","Middle","Ring","Little"};
                string[] segments={"Proximal","Intermediate","Distal"};
                foreach (string finger in fingers)
                    for (int i=0;i<3;i++) mapping[side+" "+finger+" "+segments[i]]="PT_"+side+"Hand"+(finger=="Little"?"Pinky":finger)+(i+1);
            }
            var transforms=instance.GetComponentsInChildren<Transform>(true);
            foreach (string bone in mapping.Values)
                if (!transforms.Any(t=>t.name==bone)) throw new InvalidOperationException("Missing model bone: "+bone);
            var description=new HumanDescription {
                human=mapping.Select(kv=>new HumanBone {humanName=kv.Key,boneName=kv.Value,limit=new HumanLimit {useDefaultValues=true}}).ToArray(),
                skeleton=transforms.Select(t=>new SkeletonBone {name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
                upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0f,hasTranslationDoF=false
            };
            Avatar built=AvatarBuilder.BuildHumanAvatar(instance,description);
            if (!built.isValid || !built.isHuman) {UnityEngine.Object.DestroyImmediate(built);throw new InvalidOperationException("Camp humanoid avatar is invalid.");}
            built.name="CampPlayerAvatar";
            Avatar existing=AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
            if(existing==null) {AssetDatabase.CreateAsset(built,AvatarPath);return built;}
            EditorUtility.CopySerialized(built,existing);UnityEngine.Object.DestroyImmediate(built);EditorUtility.SetDirty(existing);return existing;
        }
        finally {UnityEngine.Object.DestroyImmediate(instance);}
    }

    private static AnimatorController CreateController()
    {
        AnimatorController controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if(controller!=null) return controller;
        controller=AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveX",AnimatorControllerParameterType.Float);
        controller.AddParameter("MoveY",AnimatorControllerParameterType.Float);
        controller.AddParameter("Running",AnimatorControllerParameterType.Bool);
        AnimatorStateMachine sm=controller.layers[0].stateMachine;
        AnimatorState idle=sm.AddState("Idle",new Vector3(250,50));idle.motion=Clip("Idles/HumanM@Idle01.fbx");sm.defaultState=idle;
        AnimatorState walk=sm.AddState("Walk",new Vector3(250,170));walk.motion=Tree(controller,"Walk");
        AnimatorState run=sm.AddState("Run",new Vector3(510,170));run.motion=Tree(controller,"Run");
        Transition(idle,walk,AnimatorConditionMode.Greater,"Speed",.1f,AnimatorConditionMode.IfNot,"Running");
        Transition(idle,run,AnimatorConditionMode.Greater,"Speed",.1f,AnimatorConditionMode.If,"Running");
        Transition(walk,idle,AnimatorConditionMode.Less,"Speed",.1f);
        Transition(run,idle,AnimatorConditionMode.Less,"Speed",.1f);
        Transition(walk,run,AnimatorConditionMode.If,"Running",0);
        Transition(run,walk,AnimatorConditionMode.IfNot,"Running",0,AnimatorConditionMode.Greater,"Speed",.1f);
        EditorUtility.SetDirty(controller);return controller;
    }
    private static BlendTree Tree(AnimatorController controller,string gait)
    {
        var tree=new BlendTree {name=gait+" Directions",blendType=BlendTreeType.FreeformDirectional2D,blendParameter="MoveX",blendParameterY="MoveY",useAutomaticThresholds=false};
        AssetDatabase.AddObjectToAsset(tree,controller);
        for(int i=0;i<Directions.Length;i++) tree.AddChild(Clip("Movement/"+gait+"/HumanM@"+gait+"01_"+Directions[i]+".fbx"),Positions[i]);
        return tree;
    }
    private static void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode mode,string parameter,float threshold,AnimatorConditionMode? second=null,string secondParameter=null,float secondThreshold=0)
    {
        var t=from.AddTransition(to);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=.15f;t.canTransitionToSelf=false;
        t.AddCondition(mode,threshold,parameter);if(second.HasValue)t.AddCondition(second.Value,secondThreshold,secondParameter);
    }
    private static string ValidateAnimations(AnimatorController controller,Avatar avatar)
    {
        Scene preview=EditorSceneManager.NewPreviewScene();
        GameObject test=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        SceneManager.MoveGameObjectToScene(test,preview);
        try
        {
            Animator animator=test.GetComponent<Animator>();animator.avatar=avatar;animator.runtimeAnimatorController=controller;
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();
            int checkedPoses=0;
            foreach(string state in new[]{"Idle","Walk","Run"})
            {
                int count=state=="Idle"?1:Directions.Length;
                for(int i=0;i<count;i++)
                {
                    animator.SetFloat("MoveX",Positions[i].x);animator.SetFloat("MoveY",Positions[i].y);
                    animator.SetFloat("Speed",state=="Idle"?0:state=="Walk"?2:5);animator.SetBool("Running",state=="Run");
                    animator.Play(state,0,0);animator.Update(0);
                    Transform leg=animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
                    Transform hips=animator.GetBoneTransform(HumanBodyBones.Hips);
                    Transform head=animator.GetBoneTransform(HumanBodyBones.Head);
                    var first=leg.localRotation;var firstHead=head.localRotation;var firstHips=hips.localPosition;
                    Vector3 root=test.transform.position;
                    animator.Update(.23f);
                    if(!animator.GetCurrentAnimatorStateInfo(0).IsName(state))throw new Exception("Wrong state: "+state);
                    var clips=animator.GetCurrentAnimatorClipInfo(0);
                    string expected=state=="Idle"?"HumanM@Idle01":"HumanM@"+state+"01_"+Directions[i];
                    if(!clips.Any(c=>c.clip.name==expected && c.weight>.9f))throw new Exception("Wrong clip: "+expected);
                    if(Quaternion.Angle(first,leg.localRotation)<.001f && Quaternion.Angle(firstHead,head.localRotation)<.001f && Vector3.Distance(firstHips,hips.localPosition)<.00001f)
                        throw new Exception("Bones did not animate: "+expected);
                    if(Vector3.Distance(root,test.transform.position)>.001f)throw new Exception("Unexpected root motion");
                    checkedPoses++;
                }
            }
            return "PASS: "+checkedPoses+" animation/direction samples; expected clips, animated bones and stationary root verified.";
        }
        finally {UnityEngine.Object.DestroyImmediate(test);EditorSceneManager.ClosePreviewScene(preview);}
    }
}
