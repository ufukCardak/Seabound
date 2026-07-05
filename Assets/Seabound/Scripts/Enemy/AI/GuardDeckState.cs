using UnityEngine;

public class GuardDeckState : GuardBaseState
{
    private BoatController boat;
    private Vector3 localOffset;
    private Quaternion localRotOffset;
    private float nextAttackTime;

    public override void Enter(EnemyGuardAI guard)
    {
        boat = guard.AssignedBoat;
        if (boat != null)
        {
            localOffset = boat.transform.InverseTransformPoint(guard.transform.position);
            localRotOffset = Quaternion.Inverse(boat.transform.rotation) * guard.transform.rotation;

            guard.Rb.isKinematic = true;

            if (guard.Animator != null)
            {
                guard.Animator.SetFloat("Speed", 0f);
                if (guard.IsPassenger)
                    guard.Animator.SetBool("IsSitting", true);
            }
            if (guard.AimIK != null)
            {
                guard.AimIK.SetAimInput(Vector3.zero, false);
            }
        }
    }

    public override void Tick(EnemyGuardAI guard)
    {
        if (boat == null)
        {
            guard.ChangeState(guard.PatrolState);
            return;
        }

        guard.transform.position = boat.transform.TransformPoint(localOffset);
        
        if (guard.CurrentTarget != null)
        {
            float dist = Vector3.Distance(guard.transform.position, guard.CurrentTarget.position);
            if (dist <= guard.AttackRange)
            {
                Vector3 dir = (guard.CurrentTarget.position - guard.transform.position);
                dir.y = 0;
                if (dir != Vector3.zero)
                    guard.transform.rotation = Quaternion.LookRotation(dir.normalized);

                if (guard.Animator != null && guard.IsPassenger)
                    guard.Animator.SetBool("IsSitting", false);

                if (guard.AimIK != null)
                    guard.AimIK.SetAimInput(guard.CurrentTarget.position + Vector3.up * 1f, true);

                if (Time.time >= nextAttackTime)
                {
                    Vector3 targetPos = guard.CurrentTarget.position + Vector3.up * 1f;
                    Vector3 aimDirection = (targetPos - guard.FirePoint.position).normalized;
                    guard.PerformHitscanAttack(guard.FirePoint, guard.AttackRange, guard.stats.attackDamage, aimDirection);
                    nextAttackTime = Time.time + (1f / guard.AttackRate);
                }
            }
            else
            {
                guard.transform.rotation = boat.transform.rotation * localRotOffset;
                
                if (guard.Animator != null && guard.IsPassenger)
                    guard.Animator.SetBool("IsSitting", true);

                if (guard.AimIK != null)
                    guard.AimIK.SetAimInput(Vector3.zero, false);
            }
        }
        else
        {
            guard.transform.rotation = boat.transform.rotation * localRotOffset;
            
            if (guard.Animator != null && guard.IsPassenger)
                guard.Animator.SetBool("IsSitting", true);

            if (guard.AimIK != null)
                guard.AimIK.SetAimInput(Vector3.zero, false);
        }
    }

    public override void Exit(EnemyGuardAI guard)
    {
        if (guard.Animator != null && guard.IsPassenger)
            guard.Animator.SetBool("IsSitting", false);

        if (guard.Rb != null)
            guard.Rb.isKinematic = false;
    }
}
