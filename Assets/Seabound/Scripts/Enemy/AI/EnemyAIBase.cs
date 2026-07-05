using Unity.Netcode;
using UnityEngine;

public abstract class EnemyAIBase : NetworkBehaviour
{

    protected abstract float DetectionRange { get; }

    protected abstract void Tick();

protected Transform Target;
    public bool IsAggroed = false;

private void FixedUpdate()
    {
        if (!IsServer) return;
        RefreshTarget();
        Tick();
    }

    public void OnAttackedBy(Transform attacker)
    {
        if (attacker != null && !IsAggroed)
        {
            IsAggroed = true;
            Target = attacker;

            if (this is EnemyGuardAI guard && guard.AssignedBoat != null)
            {
                var allGuards = FindObjectsByType<EnemyGuardAI>(FindObjectsSortMode.None);
                foreach (var g in allGuards)
                {
                    if (g.AssignedBoat == guard.AssignedBoat && !g.IsAggroed)
                    {
                        g.OnAttackedBy(attacker);
                    }
                }
            }
        }
    }

    private void RefreshTarget()
    {
        if (!IsAggroed)
        {
            Target = null;
            return;
        }

        var players = EnemyTargetRegistry.Players;

        Transform nearest = null;
        float minDist = DetectionRange;

        foreach (var p in players)
        {
            if (p == null) continue;
            
            var hc = p.GetComponent<HealthComponent>();
            if (hc != null && hc.CurrentHealth <= 0) continue;

            float d = Vector3.Distance(transform.position, p.position);
            if (d < minDist)
            {
                minDist = d;
                nearest = p;
            }
        }

        if (nearest == null && IsAggroed)
        {
            IsAggroed = false;
        }

        Target = nearest;
    }

protected void FaceTarget(Vector3 targetPos, float speed, Rigidbody rb = null)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0f;
        if (dir == Vector3.zero) return;

        Quaternion look = Quaternion.LookRotation(dir.normalized);

        if (rb != null)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, look, speed * Time.fixedDeltaTime));
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, look, speed * Time.deltaTime);
    }

    protected void PerformHitscanAttack(Transform firePoint, float range, int damage, Vector3 direction)
    {
        if (firePoint == null) return;

        Vector3 startPos = firePoint.position;
        Vector3 endPos = startPos + direction * range;

        if (IsServer)
        {
            if (Physics.Raycast(startPos, direction, out RaycastHit hit, range))
            {
                endPos = hit.point;

                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
            }
            SpawnTracerRpc(startPos, endPos);
        }
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnTracerRpc(Vector3 start, Vector3 end)
    {
        GameObject tracer = new GameObject("Tracer");
        LineRenderer lr = tracer.AddComponent<LineRenderer>();
        
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.yellow;
        lr.material = mat;

        Destroy(tracer, 0.1f);
    }
}
