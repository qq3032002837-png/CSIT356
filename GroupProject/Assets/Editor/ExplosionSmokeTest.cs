using UnityEngine;
using UnityEditor;

/// <summary>
/// Headless check that Explosion.Detonate actually moves Rigidbodies.
/// Run with: Unity.exe -batchmode -quit -executeMethod ExplosionSmokeTest.Run
/// Temporary scaffolding, safe to delete.
/// </summary>
public static class ExplosionSmokeTest
{
    private const int CrateCount = 8;
    private const float BlastRadius = 6f;
    private const float BlastForce = 15f;
    private const float StepSeconds = 0.02f;
    private const int StepCount = 60;
    private const float MinPlausibleDisplacement = 0.5f;
    private const float MaxPlausibleDisplacement = 40f;

    public static void Run()
    {
        Debug.Log("[SMOKE] start");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(40f, 1f, 40f);

        var crates = new GameObject[CrateCount];
        var startPositions = new Vector3[CrateCount];

        for (int i = 0; i < CrateCount; i++)
        {
            float angle = (360f / CrateCount) * i * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * 2f, 0.5f, Mathf.Sin(angle) * 2f);

            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = $"Crate{i}";
            crate.transform.position = pos;
            crate.AddComponent<Rigidbody>().mass = 1f;

            crates[i] = crate;
            startPositions[i] = pos;
        }

        UnityEngine.Physics.simulationMode = SimulationMode.Script;

        Vector3 blastOrigin = new Vector3(0f, 0.5f, 0f);
        Explosion.Detonate(blastOrigin, BlastRadius, BlastForce, ~0);

        for (int step = 0; step < StepCount; step++)
        {
            UnityEngine.Physics.Simulate(StepSeconds);
        }

        int moved = 0;
        float totalDisplacement = 0f;

        for (int i = 0; i < CrateCount; i++)
        {
            float displacement = Vector3.Distance(startPositions[i], crates[i].transform.position);
            totalDisplacement += displacement;

            // A real grenade nudge, not a teleport. Catches both a dead blast
            // and a force value that launches crates into orbit.
            if (displacement > MinPlausibleDisplacement && displacement < MaxPlausibleDisplacement)
            {
                moved++;
            }

            Debug.Log($"[SMOKE] Crate{i} moved {displacement:F2}m");
        }

        Debug.Log($"[SMOKE] {moved}/{CrateCount} crates moved, average {totalDisplacement / CrateCount:F2}m");
        Debug.Log(moved == CrateCount
            ? "[SMOKE] RESULT PASS"
            : $"[SMOKE] RESULT FAIL - {moved}/{CrateCount} within {MinPlausibleDisplacement}m..{MaxPlausibleDisplacement}m");
    }
}
