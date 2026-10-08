using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class CampLocomotionPlayTest
{
    private static Keyboard keyboard, previous;
    private static PlayerMotor motor;
    private static Animator animator;
    private static float started;
    private static int lastChecked=-1;
    private static bool active;
    private static readonly List<string> results=new List<string>();
    private static readonly string[] States={"Idle","Walk","Run","Walk","Idle"};
    static CampLocomotionPlayTest()
    {
        EditorApplication.playModeStateChanged+=Changed;
    }
    [MenuItem("Tools/Camp Player/Test Movement In Play Mode")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode first.");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/CampBaseScene.unity")throw new InvalidOperationException("Open CampBaseScene first.");
        SessionState.SetBool("CampMovementTest",true);
        EditorApplication.isPlaying=true;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CampMovementTest",false))
        {
            SessionState.SetBool("CampMovementTest",false);
            try
            {
                motor=GameObject.Find("PT_Male_Modular_Free_Pack").GetComponent<PlayerMotor>();animator=motor.GetComponent<Animator>();
                previous=Keyboard.current;keyboard=InputSystem.AddDevice<Keyboard>("CampTestKeyboard");keyboard.MakeCurrent();
                started=Time.time;lastChecked=-1;results.Clear();active=true;
                InputSystem.onBeforeUpdate+=QueueInput;EditorApplication.update+=Check;
            }
            catch(Exception e){Finish("FAILED: "+e);}
        }
        if(state==PlayModeStateChange.ExitingPlayMode && active)Finish("INTERRUPTED",false);
    }
    private static void QueueInput()
    {
        if(!active || InputState.currentUpdateType!=InputUpdateType.Dynamic)return;
        int phase=Mathf.FloorToInt((Time.time-started)/1f);
        KeyboardState input=phase==1?new KeyboardState(Key.W):phase==2?new KeyboardState(Key.W,Key.LeftShift):phase==3?new KeyboardState(Key.S):new KeyboardState();
        InputSystem.QueueStateEvent(keyboard,input);
    }
    private static void Check()
    {
        if(!active)return;
        try
        {
            float elapsed=Time.time-started;int phase=Mathf.FloorToInt(elapsed);
            if(phase>=States.Length){Finish("PASS\n"+string.Join("\n",results));return;}
            if(phase==lastChecked || elapsed-phase<.75f)return;
            float speed=motor.PlanarVelocity.magnitude;
            float expected=phase==2?5:phase==1||phase==3?2:0;
            if(Mathf.Abs(speed-expected)>.3f)throw new Exception("Speed phase "+phase+": "+speed+" expected "+expected);
            if(!animator.GetCurrentAnimatorStateInfo(0).IsName(States[phase]))throw new Exception("Wrong animation phase "+phase);
            if(!motor.IsGrounded)throw new Exception("Character not grounded phase "+phase);
            if(phase==3 && Vector3.Dot(motor.PlanarVelocity,motor.transform.forward)>=0)throw new Exception("Backward input did not move backward");
            results.Add(States[phase]+": speed="+speed.ToString("F2")+", grounded="+motor.IsGrounded+", rootY="+motor.transform.position.y.ToString("F3"));
            lastChecked=phase;
        }
        catch(Exception e){Finish("FAILED\n"+e+"\n"+string.Join("\n",results));}
    }
    private static void Finish(string result,bool stop=true)
    {
        active=false;InputSystem.onBeforeUpdate-=QueueInput;EditorApplication.update-=Check;
        if(keyboard!=null && keyboard.added)InputSystem.RemoveDevice(keyboard);
        if(previous!=null && previous.added)previous.MakeCurrent();
        keyboard=null;
        File.WriteAllText("Logs/CampLocomotionPlayTest.txt",result);
        Debug.Log("[CampLocomotionPlayTest] "+result);
        if(stop)EditorApplication.isPlaying=false;
    }
}
