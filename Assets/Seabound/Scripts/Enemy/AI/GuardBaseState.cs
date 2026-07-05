using UnityEngine;

public abstract class GuardBaseState
{
    public abstract void Enter(EnemyGuardAI guard);
    public abstract void Tick(EnemyGuardAI guard);
    public abstract void Exit(EnemyGuardAI guard);
}
