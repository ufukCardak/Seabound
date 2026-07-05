using Unity.Netcode;
using UnityEngine;

public class EnemyGuardAI : EnemyAIBase
{

    [Header("Stats")]
    public EnemyStatsSO stats;

    [Header("Patrol")]
    [Tooltip("World-space waypoints. Guard loops through them.")]
    public Transform[] PatrolWaypoints;

    [Header("Combat")]
    public GameObject ProjectilePrefab;
    public Transform FirePoint;

    [Header("Animation & IK")]
    public Animator Animator;
    public PlayerAimIK AimIK;

    [Header("Ship Settings")]
    public BoatController AssignedBoat;
    public bool IsCaptain;
    public bool IsPassenger;

    public float MoveSpeed => stats.moveSpeed;
    public float WaypointTolerance => stats.waypointTolerance;
    public float PatrolWaitTime => stats.patrolWaitTime;
    public float AlertRange => IsAggroed ? stats.aggroAlertRange : stats.alertRange;
    public float AttackRange => IsAggroed ? stats.aggroAttackRange : stats.attackRange;
    public float RotationSpeed => stats.rotationSpeed;
    public float ProjectileSpeed => stats.projectileSpeed;
    public float AttackRate => stats.attackRate;

protected override float DetectionRange => AlertRange;

    protected override void Tick() => currentState?.Tick(this);

public readonly GuardPatrolState PatrolState = new GuardPatrolState();
    public readonly GuardAlertState AlertState = new GuardAlertState();
    public readonly GuardCombatState CombatState = new GuardCombatState();
    public readonly GuardDriveState DriveState = new GuardDriveState();
    public readonly GuardDeckState DeckState = new GuardDeckState();

    private GuardBaseState currentState;

public Transform CurrentTarget => Target;
    public void LookAt(Vector3 pos) => FaceTarget(pos, RotationSpeed, Rb);
    public new void PerformHitscanAttack(Transform firePoint, float range, int damage, Vector3 direction)
        => base.PerformHitscanAttack(firePoint, range, damage, direction);

public Rigidbody Rb;
    private HealthComponent health;

private void Awake()
    {
        Rb = GetComponent<Rigidbody>();
        health = GetComponent<HealthComponent>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer) 
        {
            if (AssignedBoat != null)
            {
                IgnoreBoatCollisions(AssignedBoat, true);

                if (IsCaptain)
                    ChangeState(DriveState);
                else
                    ChangeState(DeckState);
            }
            else
            {
                ChangeState(PatrolState);
            }
        }
    }

    private void IgnoreBoatCollisions(BoatController boat, bool ignore)
    {
        if (boat == null) return;
        var myColliders = GetComponentsInChildren<Collider>();
        var boatColliders = boat.GetComponentsInChildren<Collider>();
        foreach (var mc in myColliders)
        {
            foreach (var bc in boatColliders)
            {
                Physics.IgnoreCollision(mc, bc, ignore);
            }
        }
    }

public void ChangeState(GuardBaseState newState)
    {
        currentState?.Exit(this);
        currentState = newState;
        currentState.Enter(this);
    }

    private Vector3 lastLocalPos;

    private void Update()
    {
        if (!IsServer) return;
        
        if (Animator != null)
        {
            float localSpeed = Vector3.Distance(transform.localPosition, lastLocalPos) / Time.deltaTime;
            Animator.SetFloat("Speed", localSpeed);
            Animator.SetBool("IsGrounded", true);
            lastLocalPos = transform.localPosition;
        }
    }

public void TakeDamage(int damage) => health?.TakeDamage(damage);

    private void OnEnable()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
            
        if (IsServer && IsCaptain && AssignedBoat != null)
        {
            AssignedBoat.SetInput(0f, 0f);
        }
    }

    private void HandleDeath()
    {
        if (IsServer && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}
