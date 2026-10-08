using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless check that the ghost's state machine makes the right transitions.
/// Run with: Unity.exe -batchmode -quit -executeMethod GhostSmokeTest.Run
/// Temporary scaffolding, safe to delete.
///
/// Outside Play mode Unity does not call Awake or Start, so the test calls
/// them by hand, then steps the ghost with Tick instead of waiting for frames.
/// </summary>
public static class GhostSmokeTest
{
    private const float Step = 0.1f;

    private static int failures;

    public static void Run()
    {
        failures = 0;
        Debug.Log("[GHOST] start");

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        Health playerHealth = player.AddComponent<Health>();
        CallPrivate(playerHealth, "Awake");

        GameObject ghostObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ghostObject.name = "Ghost";
        Health ghostHealth = ghostObject.AddComponent<Health>();
        GhostEnemy ghost = ghostObject.AddComponent<GhostEnemy>();
        CallPrivate(ghostHealth, "Awake");
        CallPrivate(ghost, "Awake");

        // A wall directly between the ghost and the player.
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0f, 1f, 5f);
        wall.transform.localScale = new Vector3(10f, 3f, 1f);

        ghostObject.transform.position = new Vector3(0f, 1f, 0f);
        player.transform.position = new Vector3(0f, 1f, 30f);
        CallPrivate(ghost, "Start");

        Check(ghostObject.GetComponent<Collider>().isTrigger, "ghost collider is a trigger (passes through walls)");
        Check(ghost.CurrentState == ghost.IdleState, "starts Idle");

        ghost.Tick(Step);
        Check(ghost.CurrentState == ghost.IdleState, "stays Idle with the player 30 m away");

        player.transform.position = new Vector3(0f, 1f, 10f);
        ghost.Tick(Step);
        Check(ghost.CurrentState == ghost.ChaseState, "Idle -> Chase at 10 m");

        float before = ghost.DistanceToTarget();
        ghost.Tick(1f);
        float moved = before - ghost.DistanceToTarget();
        Check(Mathf.Abs(moved - 1.5f) < 0.01f, $"moves 1.5 m in 1 s at speed 1.5 (moved {moved:F2})");

        // Walk it through the wall and up to the player.
        for (int i = 0; i < 200 && ghost.CurrentState == ghost.ChaseState; i++)
        {
            ghost.Tick(Step);
        }
        Check(ghost.CurrentState == ghost.AttackState, "Chase -> Attack, having gone through the wall");
        Check(ghostObject.transform.position.z > 5.5f, "ghost is now past the wall");

        ghost.Tick(Step);
        Check(Mathf.Approximately(playerHealth.Current, 20f), $"first hit lands straight away (player health {playerHealth.Current})");

        ghost.Tick(0.5f);
        Check(Mathf.Approximately(playerHealth.Current, 20f), "no second hit before the cooldown");
        ghost.Tick(1.1f);
        Check(Mathf.Approximately(playerHealth.Current, 10f), $"second hit after the cooldown (player health {playerHealth.Current})");

        // 2.0 m: outside attackRange (1.5) but inside attackRange + buffer (2.25).
        Vector3 ghostPos = ghostObject.transform.position;
        player.transform.position = new Vector3(ghostPos.x, 1f, ghostPos.z + 2.0f);
        ghost.Tick(Step);
        Check(ghost.CurrentState == ghost.AttackState, "stays in Attack inside the buffer (no flicker)");

        player.transform.position = new Vector3(ghostPos.x, 1f, ghostPos.z + 4f);
        ghost.Tick(Step);
        Check(ghost.CurrentState == ghost.ChaseState, "Attack -> Chase once the player is clearly out of reach");

        ghostHealth.TakeDamage(30f);
        Check(ghost.CurrentState == ghost.DeadState, "any -> Dead when health hits zero");
        Check(!ghostObject.GetComponent<Collider>().enabled, "dead ghost's collider is off");

        Vector3 deadPos = ghostObject.transform.position;
        ghost.Tick(1f);
        Check(ghostObject.transform.position == deadPos, "dead ghost does not move");

        Debug.Log(failures == 0 ? "[GHOST] ALL PASSED" : $"[GHOST] {failures} FAILED");
        EditorApplication.Exit(failures == 0 ? 0 : 1);
    }

    private static void Check(bool condition, string label)
    {
        if (!condition)
        {
            failures++;
        }
        Debug.Log($"[GHOST] {(condition ? "PASS" : "FAIL")} {label}");
    }

    private static void CallPrivate(object target, string method)
    {
        target.GetType()
            .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(target, null);
    }
}
