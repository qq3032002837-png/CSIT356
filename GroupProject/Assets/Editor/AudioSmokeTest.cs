using UnityEngine;

/// <summary>
/// Headless check that every GameAudio sound set loads at least one clip.
/// Catches a renamed folder or a file Unity failed to import.
/// Run with: Unity.exe -batchmode -quit -executeMethod AudioSmokeTest.Run
/// </summary>
public static class AudioSmokeTest
{
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
                Debug.Log($"[AUDIO] {set}: {clip.name} {clip.length:F2}s");
            }
        }

        Debug.Log(missing == 0
            ? $"[AUDIO] RESULT PASS - all {GameAudio.AllSets.Length} sets load"
            : $"[AUDIO] RESULT FAIL - {missing} empty sets");
    }
}
