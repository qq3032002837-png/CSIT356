using UnityEngine;

/// <summary>
/// Headless check of the shotgun bash: in reach gets hit, out of reach does not,
/// a wall in between blocks it, and a ghost's trigger collider still counts.
/// Run with: Unity.exe -batchmode -quit -executeMethod MeleeSmokeTest.Run
/// </summary>
public static class MeleeSmokeTest
{
    public static void Run()
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);

        int failures = 0;
        failures += Check("enemy at 1.5 m is hit", MakeEnemy(1.5f, false), null, true);
        failures += Check("enemy at 3 m is out of reach", MakeEnemy(3f, false), null, false);
        failures += Check("ghost trigger at 1.5 m is hit", MakeEnemy(1.5f, true), null, true);
        failures += Check("wall at 0.8 m blocks enemy at 1.5 m", MakeEnemy(1.5f, false), MakeWall(0.8f), false);

        Debug.Log(failures == 0
            ? "[MELEE] RESULT PASS"
            : $"[MELEE] RESULT FAIL - {failures} checks failed");
    }

    private static int Check(string label, Health enemy, GameObject wall, bool expectHit)
    {
        GameObject player = new GameObject("Player");
        ShotgunBash bash = player.AddComponent<ShotgunBash>();
        Physics.SyncTransforms();

        float before = enemy.Current;
        bash.Bash();
        bool wasHit = enemy.Current < before;

        Debug.Log($"[MELEE] {label}: {(wasHit == expectHit ? "ok" : "WRONG")} (health {before} -> {enemy.Current})");

        Object.DestroyImmediate(player);
        Object.DestroyImmediate(enemy.gameObject);
        if (wall != null)
        {
            Object.DestroyImmediate(wall);
        }

        return wasHit == expectHit ? 0 : 1;
    }

    private static Health MakeEnemy(float distance, bool isTrigger)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.transform.position = new Vector3(0f, 0f, distance);
        enemy.GetComponent<Collider>().isTrigger = isTrigger;
        Health health = enemy.AddComponent<Health>();

        // Awake does not run outside Play mode, so set full health by hand.
        typeof(Health).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(health, null);
        return health;
    }

    private static GameObject MakeWall(float distance)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.position = new Vector3(0f, 0f, distance);
        wall.transform.localScale = new Vector3(3f, 3f, 0.1f);
        return wall;
    }
}
