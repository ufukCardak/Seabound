using UnityEngine;

public class GuardCombatState : GuardBaseState
{
    private float nextAttackTime;

    public override void Enter(EnemyGuardAI guard)
    {
        nextAttackTime = Time.time;
    }

    public override void Tick(EnemyGuardAI guard)
    {
        if (guard.CurrentTarget == null) 
        { 
            if (guard.AssignedBoat != null && !guard.IsCaptain)
                guard.ChangeState(guard.DeckState);
            else if (guard.AssignedBoat != null)
                guard.ChangeState(guard.DriveState);
            else
                guard.ChangeState(guard.PatrolState); 
            return; 
        }

        float dist = Vector3.Distance(guard.transform.position, guard.CurrentTarget.position);

        if (dist > guard.AlertRange) 
        { 
            if (guard.AssignedBoat != null && guard.IsCaptain)
                guard.ChangeState(guard.DriveState);
            else if (guard.AssignedBoat != null)
                guard.ChangeState(guard.DeckState);
            else
                guard.ChangeState(guard.PatrolState); 
            return; 
        }
        if (dist > guard.AttackRange)
        {
            if (guard.AssignedBoat != null && guard.IsCaptain)
                guard.ChangeState(guard.DriveState);
            else if (guard.AssignedBoat != null)
                guard.ChangeState(guard.DeckState);
            else
                guard.ChangeState(guard.AlertState);
            return;
        }

        guard.LookAt(guard.CurrentTarget.position);

        if (guard.AimIK != null)
        {
            guard.AimIK.SetAimInput(guard.CurrentTarget.position + Vector3.up * 1f, true);
        }

        if (Time.time >= nextAttackTime)
        {
            Vector3 targetPos = guard.CurrentTarget.position + Vector3.up * 1f;
            Vector3 direction = (targetPos - guard.FirePoint.position).normalized;
            guard.PerformHitscanAttack(guard.FirePoint, guard.AttackRange, guard.stats.attackDamage, direction);
            nextAttackTime = Time.time + (1f / guard.AttackRate);
        }
    }

    public override void Exit(EnemyGuardAI guard) 
    { 
        if (guard.AimIK != null)
        {
            guard.AimIK.SetAimInput(Vector3.zero, false);
        }
    }
}
