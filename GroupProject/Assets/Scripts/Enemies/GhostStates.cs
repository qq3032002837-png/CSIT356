using UnityEngine;

/// <summary>Waits until the player comes close enough to notice.</summary>
public class GhostIdleState : IState
{
    private readonly GhostEnemy ghost;

    public GhostIdleState(GhostEnemy ghost)
    {
        this.ghost = ghost;
    }

    public void Enter() { }

    public void Tick(float deltaTime)
    {
        if (ghost.DistanceToTarget() <= ghost.DetectionRange)
        {
            ghost.ChangeState(ghost.ChaseState);
        }
    }

    public void Exit() { }
}

/// <summary>
/// Drifts straight at the player, through walls. Never gives up: the only way
/// out is reaching the player (Attack) or dying (Dead).
/// </summary>
public class GhostChaseState : IState
{
    private readonly GhostEnemy ghost;

    public GhostChaseState(GhostEnemy ghost)
    {
        this.ghost = ghost;
    }

    public void Enter() { }

    public void Tick(float deltaTime)
    {
        if (!ghost.HasTarget)
        {
            ghost.ChangeState(ghost.IdleState);
            return;
        }

        if (ghost.DistanceToTarget() <= ghost.AttackRange)
        {
            ghost.ChangeState(ghost.AttackState);
            return;
        }

        ghost.SeekTarget(deltaTime);
    }

    public void Exit() { }
}

/// <summary>
/// Stays put and hits the player every attackCooldown seconds. The first hit
/// lands straight away. Leaves only once the player has moved clearly out of
/// reach (attackRange + attackExitBuffer).
/// </summary>
public class GhostAttackState : IState
{
    private readonly GhostEnemy ghost;
    private float timeUntilNextHit;

    public GhostAttackState(GhostEnemy ghost)
    {
        this.ghost = ghost;
    }

    public void Enter()
    {
        timeUntilNextHit = 0f;
    }

    public void Tick(float deltaTime)
    {
        if (ghost.DistanceToTarget() > ghost.AttackExitRange)
        {
            ghost.ChangeState(ghost.ChaseState);
            return;
        }

        ghost.FaceTarget(deltaTime);

        timeUntilNextHit -= deltaTime;
        if (timeUntilNextHit <= 0f)
        {
            ghost.HitTarget();
            timeUntilNextHit = ghost.AttackCooldown;
        }
    }

    public void Exit() { }
}

/// <summary>Stops everything and removes the ghost after a short delay.</summary>
public class GhostDeadState : IState
{
    private readonly GhostEnemy ghost;

    public GhostDeadState(GhostEnemy ghost)
    {
        this.ghost = ghost;
    }

    public void Enter()
    {
        foreach (Collider part in ghost.GetComponentsInChildren<Collider>())
        {
            part.enabled = false;
        }

        // Destroy only works in Play mode. The editor test steps the ghost
        // outside Play mode, so it skips this.
        if (Application.isPlaying)
        {
            Object.Destroy(ghost.gameObject, ghost.DestroyDelay);
        }
    }

    // A dead ghost does nothing and has no way out of this state.
    public void Tick(float deltaTime) { }

    public void Exit() { }
}
