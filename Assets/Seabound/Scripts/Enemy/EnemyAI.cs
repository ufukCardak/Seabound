using Unity.Netcode;
using UnityEngine;

public class EnemyAI : EnemyAIBase
{
    [Header("Stats")]
    [SerializeField] private EnemyStatsSO stats;

    [Header("Combat")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    protected override float DetectionRange => stats.alertRange;
    private float rotationSpeed => stats.rotationSpeed;
    private float projectileSpeed => stats.projectileSpeed;
    private float attackRate => stats.attackRate;

    private float nextAttackTime;
    private Quaternion initialLocalRotation;
    private HealthComponent health;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        initialLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        if (health != null) health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDeath -= HandleDeath;
    }

    protected override void Tick()
    {
        if (Target != null)
        {
            FaceTarget(Target.position, rotationSpeed);

            if (Time.time >= nextAttackTime)
            {
                Vector3 targetPos = Target.position + Vector3.up * 1f;
                Vector3 direction = (targetPos - firePoint.position).normalized;
                PerformHitscanAttack(firePoint, stats.attackRange, stats.attackDamage, direction);
                nextAttackTime = Time.time + (1f / attackRate);
            }
        }
        else
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                initialLocalRotation,
                rotationSpeed * Time.fixedDeltaTime);
        }
    }

    public void TakeDamage(int damage) => health?.TakeDamage(damage);

    private void HandleDeath()
    {
        if (IsServer && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}
