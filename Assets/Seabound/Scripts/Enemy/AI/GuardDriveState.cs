using UnityEngine;

public class GuardDriveState : GuardBaseState
{
    private BoatController boat;
    private Transform helmPos;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private int currentWaypointIndex = 0;
    private float nextAttackTime;

    public override void Enter(EnemyGuardAI guard)
    {
        boat = guard.AssignedBoat;
        if (boat != null && boat.HelmPosition != null)
        {
            helmPos = boat.HelmPosition;
            
            guard.Rb.isKinematic = true;
            guard.transform.position = helmPos.position;
            guard.transform.rotation = helmPos.rotation;
            
            if (guard.Animator != null)
            {
                guard.Animator.SetFloat("Speed", 0f);
                guard.Animator.SetBool("IsSitting", true);
            }
            if (guard.AimIK != null)
            {
                guard.AimIK.SetAimInput(Vector3.zero, false);
            }
            
            wanderTarget = boat.transform.position + boat.transform.forward * 30f;
            wanderTimer = 15f;
        }
    }

    public override void Tick(EnemyGuardAI guard)
    {
        if (boat == null || boat.IsDriven.Value)
        {
            guard.ChangeState(guard.CombatState);
            return;
        }

        if (helmPos != null)
        {
            guard.transform.position = helmPos.position;
            guard.transform.rotation = helmPos.rotation;
        }

        if (guard.CurrentTarget != null)
        {
            float dist = Vector3.Distance(guard.transform.position, guard.CurrentTarget.position);

            if (dist <= guard.AttackRange)
            {
                boat.SetInput(0, 0);
                
                if (guard.Animator != null)
                    guard.Animator.SetBool("IsSitting", false);

                if (guard.AimIK != null)
                    guard.AimIK.SetAimInput(guard.CurrentTarget.position + Vector3.up * 1f, true);

                if (Time.time >= nextAttackTime)
                {
                    Vector3 targetPos = guard.CurrentTarget.position + Vector3.up * 1f;
                    Vector3 direction = (targetPos - guard.FirePoint.position).normalized;
                    guard.PerformHitscanAttack(guard.FirePoint, guard.AttackRange, guard.stats.attackDamage, direction);
                    nextAttackTime = Time.time + (1f / guard.AttackRate);
                }
            }
            else
            {
                if (guard.Animator != null)
                    guard.Animator.SetBool("IsSitting", true);
                    
                if (guard.AimIK != null)
                    guard.AimIK.SetAimInput(Vector3.zero, false);

                Vector3 dirToTarget = (guard.CurrentTarget.position - boat.transform.position).normalized;
                float targetThrottle = Vector3.Dot(boat.transform.forward, dirToTarget) < 0.5f ? 0.5f : 1f;
                CalculateAvoidanceAndDrive(dirToTarget, targetThrottle);
            }
        }
        else
        {
            if (guard.Animator != null)
                guard.Animator.SetBool("IsSitting", true);

            if (guard.PatrolWaypoints != null && guard.PatrolWaypoints.Length > 0)
            {
                Transform wp = guard.PatrolWaypoints[currentWaypointIndex];
                float wpDist = Vector3.Distance(boat.transform.position, wp.position);
                if (wpDist < 15f)
                {
                    currentWaypointIndex = (currentWaypointIndex + 1) % guard.PatrolWaypoints.Length;
                    wp = guard.PatrolWaypoints[currentWaypointIndex];
                }

                Vector3 dirToTarget = (wp.position - boat.transform.position).normalized;
                float targetThrottle = Vector3.Dot(boat.transform.forward, dirToTarget) < 0.5f ? 0.4f : 0.8f;
                CalculateAvoidanceAndDrive(dirToTarget, targetThrottle);
            }
            else
            {
                wanderTimer -= Time.deltaTime;
                float wanderDist = Vector3.Distance(boat.transform.position, wanderTarget);
                
                if (wanderDist < 10f || wanderTimer <= 0f)
                {
                    Vector2 rnd = Random.insideUnitCircle * 80f;
                    wanderTarget = boat.transform.position + new Vector3(rnd.x, 0, rnd.y);
                    wanderTimer = 20f;
                }

                Vector3 dirToTarget = (wanderTarget - boat.transform.position).normalized;
                float targetThrottle = Vector3.Dot(boat.transform.forward, dirToTarget) < 0.5f ? 0.3f : 0.6f;
                CalculateAvoidanceAndDrive(dirToTarget, targetThrottle);
            }
        }
    }

    private void CalculateAvoidanceAndDrive(Vector3 dirToTarget, float targetThrottle)
    {
        float rightDot = Vector3.Dot(boat.transform.right, dirToTarget);
        float forwardDot = Vector3.Dot(boat.transform.forward, dirToTarget);
        
        float turn = rightDot;
        if (forwardDot < -0.8f && Mathf.Abs(turn) < 0.1f) turn = 1f;

        float analogTurn = Mathf.Clamp(turn * 2f, -1f, 1f);
        float throttle = targetThrottle;

        Vector3 origin = boat.transform.position + Vector3.up * 0.5f; 
        Vector3 fwd = boat.transform.forward;
        Vector3 right = boat.transform.right;
        
        float lookAhead = 50f;
        int mask = 1 << 0;
        
        float sphereRadius = 3f;
        
        bool hitC = Physics.SphereCast(origin, sphereRadius, fwd, out RaycastHit hitInfoC, lookAhead, mask);
        bool hitR = Physics.SphereCast(origin, sphereRadius, (fwd + right).normalized, out RaycastHit hitInfoR, lookAhead * 0.8f, mask);
        bool hitL = Physics.SphereCast(origin, sphereRadius, (fwd - right).normalized, out RaycastHit hitInfoL, lookAhead * 0.8f, mask);

        if (DebugManager.Instance != null && DebugManager.Instance.showDebugVisuals)
        {
            Debug.DrawLine(origin, origin + fwd * lookAhead, hitC ? Color.red : Color.green);
            Debug.DrawLine(origin, origin + (fwd + right).normalized * (lookAhead * 0.8f), hitR ? Color.red : Color.green);
            Debug.DrawLine(origin, origin + (fwd - right).normalized * (lookAhead * 0.8f), hitL ? Color.red : Color.green);
        }

        if (hitC)
        {
            analogTurn = hitR ? -1f : 1f;
            throttle = 0.1f;
        }
        else if (hitR)
        {
            analogTurn = -1f;
            throttle = 0.3f;
        }
        else if (hitL)
        {
            analogTurn = 1f;
            throttle = 0.3f;
        }

        boat.SetInput(throttle, analogTurn);
    }

    public override void Exit(EnemyGuardAI guard)
    {
        if (boat != null)
        {
            boat.SetInput(0, 0);
        }
        guard.Rb.isKinematic = false;
        
        if (guard.Animator != null)
            guard.Animator.SetBool("IsSitting", false);
            
        if (guard.AimIK != null)
            guard.AimIK.SetAimInput(Vector3.zero, false);
    }
}
