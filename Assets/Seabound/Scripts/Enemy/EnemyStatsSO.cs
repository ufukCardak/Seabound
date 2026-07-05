using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyStats", menuName = "Seabound/Enemy Stats")]
public class EnemyStatsSO : ScriptableObject
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float patrolWaitTime = 1f;
    public float waypointTolerance = 0.5f;

    [Header("Detection")]
    public float alertRange = 12f;
    public float attackRange = 8f;
    public float aggroAlertRange = 20f;
    public float aggroAttackRange = 15f;
    public float rotationSpeed = 5f;

    [Header("Combat")]
    public float projectileSpeed = 20f;
    public int attackDamage = 10;
    public float attackRate = 1.5f;
}
