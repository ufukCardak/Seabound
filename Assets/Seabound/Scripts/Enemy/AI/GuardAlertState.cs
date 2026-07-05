using UnityEngine;

public class GuardAlertState : GuardBaseState
{
    public override void Enter(EnemyGuardAI guard) { }

    public override void Tick(EnemyGuardAI guard)
    {
        if (guard.CurrentTarget == null)
        {
            if (guard.AssignedBoat != null)
                guard.ChangeState(guard.DriveState);
            else
                guard.ChangeState(guard.PatrolState);
            return;
        }

        float dist = Vector3.Distance(guard.transform.position, guard.CurrentTarget.position);

        if (dist > guard.AlertRange) 
        { 
            if (guard.AssignedBoat != null && !guard.IsCaptain)
                guard.ChangeState(guard.DeckState);
            else if (guard.AssignedBoat != null)
                guard.ChangeState(guard.DriveState);
            else
                guard.ChangeState(guard.PatrolState); 
            return; 
        }
        if (dist <= guard.AttackRange){ guard.ChangeState(guard.CombatState); return; }

        guard.LookAt(guard.CurrentTarget.position);
    }

    public override void Exit(EnemyGuardAI guard) { }
}
