using UnityEngine;
using UnityEngine.AI;

/// <summary>Waits until the player is visible, then commits to permanent pursuit.</summary>
public class ZombieIdleState : IState
{
    private readonly ZombieEnemy zombie;
    public ZombieIdleState(ZombieEnemy zombie) { this.zombie = zombie; }
    public void Enter() { zombie.StopMoving(); }
    public void Tick(float deltaTime)
    {
        if (zombie.CanSeeTarget()) zombie.ChangeState(zombie.ChaseState);
    }
    public void Exit() { }
}

/// <summary>Follows the player's current position via NavMesh even after losing sight.</summary>
public class ZombieChaseState : IState
{
    private readonly ZombieEnemy zombie;
    public ZombieChaseState(ZombieEnemy zombie) { this.zombie = zombie; }
    public void Enter() { }
    public void Tick(float deltaTime)
    {
        if (!zombie.HasTarget) return;
        if (zombie.DistanceToTarget() <= zombie.AttackRange)
        {
            zombie.ChangeState(zombie.AttackState);
            return;
        }
        zombie.ChaseTarget();
    }
    public void Exit() { zombie.StopMoving(); }
}

/// <summary>Attacks at intervals, returning to pursuit if the player escapes.</summary>
public class ZombieAttackState : IState
{
    private readonly ZombieEnemy zombie;
    private float timeUntilNextHit;
    public ZombieAttackState(ZombieEnemy zombie) { this.zombie = zombie; }
    public void Enter()
    {
        zombie.StopMoving();
        timeUntilNextHit = 0f;
    }
    public void Tick(float deltaTime)
    {
        if (!zombie.HasTarget || zombie.DistanceToTarget() > zombie.AttackExitRange)
        {
            zombie.ChangeState(zombie.ChaseState);
            return;
        }
        zombie.FaceTarget(deltaTime);
        timeUntilNextHit -= deltaTime;
        if (timeUntilNextHit <= 0f)
        {
            zombie.HitTarget();
            timeUntilNextHit = zombie.AttackCooldown;
        }
    }
    public void Exit() { }
}

/// <summary>Stops the NavMeshAgent and removes the dead zombie after a delay.</summary>
public class ZombieDeadState : IState
{
    private readonly ZombieEnemy zombie;
    public ZombieDeadState(ZombieEnemy zombie) { this.zombie = zombie; }
    public void Enter()
    {
        zombie.StopMoving();
        NavMeshAgent agent = zombie.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        foreach (Collider collider in zombie.GetComponentsInChildren<Collider>())
            collider.enabled = false;
        if (Application.isPlaying) Object.Destroy(zombie.gameObject, zombie.DestroyDelay);
    }
    public void Tick(float deltaTime) { }
    public void Exit() { }
}
