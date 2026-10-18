using UnityEngine;

/// <summary>
/// Gives an enemy a voice: an idle groan every few seconds, a sound when it
/// is hit, and one when it dies. Works the way Minecraft's mobs do, with a
/// small random pitch change on every sound so repeats do not sound identical.
///
/// GameAudio adds this at runtime, so it needs no setup in the scene. It
/// listens to the enemy's Health events rather than being called by the enemy,
/// so the enemy scripts do not need to know it exists.
/// </summary>
[RequireComponent(typeof(Health))]
public class EnemyVoice : MonoBehaviour
{
    private const float MinIdleGap = 4f;
    private const float MaxIdleGap = 8f;
    private const float PitchVariation = 0.08f;
    private const float IdleVolume = 0.8f;

    private string idleSet;
    private string hurtSet;
    private string deathSet;
    private float basePitch = 1f;

    private Health health;
    private AudioSource voice;
    private float nextIdleTime;

    /// <summary>Chooses which sound sets this enemy uses and its base pitch.</summary>
    public void Configure(string idle, string hurt, string death, float pitch)
    {
        idleSet = idle;
        hurtSet = hurt;
        deathSet = death;
        basePitch = pitch;
    }

    private void Awake()
    {
        health = GetComponent<Health>();

        voice = gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;
        GameAudio.ConfigureSpatial(voice);

        // Stagger the first groan so a room full of enemies does not groan in unison.
        nextIdleTime = Time.time + Random.Range(0f, MaxIdleGap);
    }

    private void OnEnable()
    {
        health.onDamaged.AddListener(OnDamaged);
        health.onDied.AddListener(OnDied);
    }

    private void OnDisable()
    {
        health.onDamaged.RemoveListener(OnDamaged);
        health.onDied.RemoveListener(OnDied);
    }

    private void Update()
    {
        if (health.IsDead || Time.time < nextIdleTime)
        {
            return;
        }

        nextIdleTime = Time.time + Random.Range(MinIdleGap, MaxIdleGap);

        // Never talk over itself. A long moan just delays the next one.
        if (!voice.isPlaying)
        {
            Say(idleSet, IdleVolume);
        }
    }

    private void OnDamaged(float healthLeft)
    {
        // The killing hit plays the death sound instead.
        if (healthLeft > 0f)
        {
            Say(hurtSet, 1f);
        }
    }

    private void OnDied()
    {
        // Played from the world, not from this object, because the body is
        // destroyed a couple of seconds later and would cut the sound off.
        voice.Stop();
        if (!string.IsNullOrEmpty(deathSet))
        {
            GameAudio.PlayAt(deathSet, transform.position, 1f, RandomPitch());
        }
    }

    private float RandomPitch()
    {
        return basePitch + Random.Range(-PitchVariation, PitchVariation);
    }

    private void Say(string set, float volume)
    {
        if (string.IsNullOrEmpty(set))
        {
            return;
        }

        AudioClip clip = GameAudio.Pick(set);
        if (clip == null)
        {
            return;
        }

        voice.Stop();
        voice.clip = clip;
        voice.volume = volume;
        voice.pitch = RandomPitch();
        voice.Play();
    }
}
