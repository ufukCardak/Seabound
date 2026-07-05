using UnityEngine;

public class GuardPatrolState : GuardBaseState
{
    private int waypointIndex;
    private float waitTimer;

    public override void Enter(EnemyGuardAI guard) { }

    public override void Tick(EnemyGuardAI guard)
    {
        if (guard.CurrentTarget != null)
        {
            float dist = Vector3.Distance(guard.transform.position, guard.CurrentTarget.position);
            guard.ChangeState(dist <= guard.AttackRange
                ? (GuardBaseState)guard.CombatState
                : guard.AlertState);
            return;
        }

        if (guard.PatrolWaypoints == null || guard.PatrolWaypoints.Length == 0) return;

        Transform wp = guard.PatrolWaypoints[waypointIndex];
        Vector3 dir = wp.position - guard.transform.position;
        dir.y = 0f;

        if (dir.magnitude < guard.WaypointTolerance)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= guard.PatrolWaitTime)
            {
                waitTimer = 0f;
                waypointIndex = (waypointIndex + 1) % guard.PatrolWaypoints.Length;
            }
        }
        else
        {
            guard.Rb.MovePosition(guard.Rb.position + dir.normalized * guard.MoveSpeed * Time.fixedDeltaTime);
            Quaternion look = Quaternion.LookRotation(dir.normalized);
            guard.Rb.MoveRotation(Quaternion.Slerp(guard.Rb.rotation, look, guard.RotationSpeed * Time.fixedDeltaTime));
        }
    }

    public override void Exit(EnemyGuardAI guard) { }
}
