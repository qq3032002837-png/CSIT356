/// <summary>
/// One state of an enemy's behaviour.
///
/// Enter runs once when the state starts, Tick runs every frame while it is
/// active, and Exit runs once when it ends. Keeping setup in Enter means Tick
/// never needs "have I started yet?" flags.
/// </summary>
public interface IState
{
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}

/// <summary>
/// Holds the one active state and swaps it.
///
/// It knows nothing about ghosts or players. Each state decides for itself
/// when to hand over to another, so adding a new state never means editing
/// this class or one big switch statement.
/// </summary>
public class StateMachine
{
    public IState Current { get; private set; }

    public void ChangeState(IState next)
    {
        if (next == null || next == Current)
        {
            return;
        }

        Current?.Exit();
        Current = next;
        Current.Enter();
    }

    public void Tick(float deltaTime)
    {
        Current?.Tick(deltaTime);
    }
}
