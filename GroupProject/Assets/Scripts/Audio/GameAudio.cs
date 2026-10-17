using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One place that loads and plays every sound in the game.
///
/// Clips live in Resources/Audio/&lt;SetName&gt;/, so nothing has to be dragged
/// into a scene or a prefab. A set with several clips plays a random one each
/// time, which stops repeated sounds (footsteps, groans) from sounding robotic.
///
/// The manager creates itself when the first scene loads. It then plays the
/// background loop and gives the player and every enemy their sound
/// components, so the scene file never has to change.
/// </summary>
public class GameAudio : MonoBehaviour
{
    // Set names. Each one is a folder under Resources/Audio.
    public const string ShotgunFire = "ShotgunFire";
    public const string ShotgunPump = "ShotgunPump";
    public const string ShotgunEmpty = "ShotgunEmpty";
    public const string Explosion = "Explosion";
    public const string WallBreak = "WallBreak";
    public const string Footsteps = "Footsteps";
    public const string Hurt = "Hurt";
    public const string Land = "Land";
    public const string ZombieIdle = "ZombieIdle";
    public const string ZombieHurt = "ZombieHurt";
    public const string ZombieDeath = "ZombieDeath";
    public const string Ghost = "Ghost";
    public const string Ambience = "Ambience";
    public const string MeleeSwing = "MeleeSwing";
    public const string MeleeHit = "MeleeHit";

    /// <summary>Every set, so a test can check none of them is empty.</summary>
    public static readonly string[] AllSets =
    {
        ShotgunFire, ShotgunPump, ShotgunEmpty, Explosion, WallBreak, Footsteps,
        Hurt, Land, ZombieIdle, ZombieHurt, ZombieDeath, Ghost, Ambience,
        MeleeSwing, MeleeHit,
    };

    private const string ResourcesRoot = "Audio/";
    private const float AmbienceVolume = 0.3f;

    // How often the manager looks for enemies that spawned after the scene loaded.
    private const float ScanInterval = 1f;

    // 3D sounds are full volume within MinHearing metres and silent past MaxHearing.
    private const float MinHearing = 2f;
    private const float MaxHearing = 30f;

    private static readonly Dictionary<string, AudioClip[]> Cache = new Dictionary<string, AudioClip[]>();
    private static GameAudio instance;

    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject host = new GameObject("GameAudio");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<GameAudio>();
    }

    private void Start()
    {
        StartAmbience();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
        {
            return;
        }

        nextScanTime = Time.unscaledTime + ScanInterval;
        AttachSoundComponents();
    }

    /// <summary>A random clip from a set, or null if the set is missing or empty.</summary>
    public static AudioClip Pick(string set)
    {
        if (!Cache.TryGetValue(set, out AudioClip[] clips))
        {
            clips = Resources.LoadAll<AudioClip>(ResourcesRoot + set);
            Cache[set] = clips;

            if (clips.Length == 0)
            {
                Debug.LogWarning($"GameAudio: no clips found in Resources/{ResourcesRoot}{set}.");
            }
        }

        return clips.Length == 0 ? null : clips[Random.Range(0, clips.Length)];
    }

    /// <summary>A sound heard the same everywhere, like the player's own gun or footsteps.</summary>
    public static void Play2D(string set, float volume = 1f, float pitch = 1f, float delay = 0f)
    {
        SpawnOneShot(Pick(set), Vector3.zero, false, volume, pitch, delay);
    }

    /// <summary>A sound that comes from a place in the world and fades with distance.</summary>
    public static void PlayAt(string set, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        SpawnOneShot(Pick(set), position, true, volume, pitch, 0f);
    }

    /// <summary>Applies the shared 3D falloff, so every positional sound fades the same way.</summary>
    public static void ConfigureSpatial(AudioSource source)
    {
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = MinHearing;
        source.maxDistance = MaxHearing;
    }

    // A short-lived object per sound, so overlapping sounds never cut each other off.
    private static void SpawnOneShot(AudioClip clip, Vector3 position, bool spatial, float volume, float pitch, float delay)
    {
        // Edit-mode tests call code that plays sounds. Destroy is not allowed outside play mode.
        if (clip == null || !Application.isPlaying)
        {
            return;
        }

        GameObject temp = new GameObject($"Sound ({clip.name})");
        temp.transform.position = position;

        AudioSource source = temp.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        if (spatial)
        {
            ConfigureSpatial(source);
        }

        source.PlayDelayed(delay);
        Destroy(temp, delay + clip.length / Mathf.Max(pitch, 0.01f) + 0.1f);
    }

    private void StartAmbience()
    {
        AudioClip loop = Pick(Ambience);
        if (loop == null)
        {
            return;
        }

        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = loop;
        source.loop = true;
        source.volume = AmbienceVolume;
        source.Play();
    }

    // Adds sound components at runtime instead of editing the scene, so this
    // cannot conflict with anyone else's scene or prefab changes.
    private static void AttachSoundComponents()
    {
        // Found by its PlayerMotor rather than the "Player" tag, because the
        // player prefab is not tagged.
        PlayerMotor player = FindFirstObjectByType<PlayerMotor>();
        if (player != null && player.GetComponent<PlayerAudio>() == null)
        {
            player.gameObject.AddComponent<PlayerAudio>();
        }

        foreach (GhostEnemy ghost in FindObjectsByType<GhostEnemy>(FindObjectsSortMode.None))
        {
            if (ghost.GetComponent<EnemyVoice>() == null)
            {
                ghost.gameObject.AddComponent<EnemyVoice>().Configure(Ghost, Ghost, Ghost, 1f);
            }
        }

        foreach (ZombieEnemy zombie in FindObjectsByType<ZombieEnemy>(FindObjectsSortMode.None))
        {
            if (zombie.GetComponent<EnemyVoice>() == null)
            {
                // Pitched down like Minecraft's zombie, which makes short groans sound heavier.
                zombie.gameObject.AddComponent<EnemyVoice>().Configure(ZombieIdle, ZombieHurt, ZombieDeath, 0.8f);
            }
        }
    }
}
