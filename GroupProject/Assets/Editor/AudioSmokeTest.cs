using UnityEngine;

/// <summary>
/// Headless check that every GameAudio sound set loads at least one clip.
/// Catches a renamed folder or a file Unity failed to import.
/// Run with: Unity.exe -batchmode -quit -executeMethod AudioSmokeTest.Run
/// </summary>
public static class AudioSmokeTest
{
    // Sets played from a position in the world, so they must import as mono.
    private static readonly string[] SpatialSets =
    {
        GameAudio.Ghost, GameAudio.ZombieIdle, GameAudio.ZombieHurt,
        GameAudio.ZombieDeath, GameAudio.Explosion, GameAudio.WallBreak, GameAudio.BombFuse,
    };

    public static void Run()
    {
        int missing = 0;

        foreach (string set in GameAudio.AllSets)
        {
            AudioClip[] clips = Resources.LoadAll<AudioClip>("Audio/" + set);
            if (clips.Length == 0)
            {
                missing++;
                Debug.Log($"[AUDIO] {set}: NO CLIPS");
                continue;
            }

            foreach (AudioClip clip in clips)
            {
                // Unity only positions a sound in 3D properly when the clip is mono.
                bool needsMono = System.Array.IndexOf(SpatialSets, set) >= 0;
                if (needsMono && clip.channels != 1)
                {
                    missing++;
                }

                Debug.Log($"[AUDIO] {set}: {clip.name} {clip.length:F2}s {clip.channels}ch" +
                          (needsMono && clip.channels != 1 ? " NOT MONO" : ""));
            }
        }

        // GameAudio finds the player by its PlayerMotor. If MainScene has none,
        // footsteps, landing and hurt sounds silently never play.
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        PlayerMotor player = Object.FindFirstObjectByType<PlayerMotor>();
        Debug.Log(player != null
            ? $"[AUDIO] player found in MainScene: {player.name}"
            : "[AUDIO] NO PlayerMotor in MainScene");
        if (player == null)
        {
            missing++;
        }

        Debug.Log(missing == 0
            ? $"[AUDIO] RESULT PASS - all {GameAudio.AllSets.Length} sets load"
            : $"[AUDIO] RESULT FAIL - {missing} problems (empty sets or 3D clips not mono)");
    }
}
