using Unity.Netcode;
using UnityEngine;

public class EnemyAI : NetworkBehaviour
{
    public NetworkVariable<int> Health = new NetworkVariable<int>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Turret (Nişancı) Ayarları")]
    public float rotationSpeed = 5f;
    public float detectionRange = 15f;
    public float attackRate = 1f;

    [Header("Silah Ayarları")]
    public GameObject projectilePrefab; // Fırlatılacak mermi prefabı
    public Transform firePoint;         // Merminin çıkacağı namlu ucu
    public float projectileSpeed = 20f; // Merminin uçuş hızı

    private Transform targetPlayer;
    private Quaternion initialLocalRotation;
    private float nextAttackTime = 0f;

    private void Start()
    {
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!IsServer) return;

        FindNearestPlayer();

        // --- OYUNCU MENZİLDEYSE ---
        if (targetPlayer != null)
        {
            Vector3 directionToPlayer = (targetPlayer.position - transform.position).normalized;
            directionToPlayer.y = 0;

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
            }

            // Ateş etme süresi geldiyse ateş et
            if (Time.time >= nextAttackTime)
            {
                ShootTarget();
                nextAttackTime = Time.time + (1f / attackRate);
            }
        }
        // --- OYUNCU YOKSA ---
        else
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, initialLocalRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        targetPlayer = null;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance <= detectionRange)
            {
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    targetPlayer = player.transform;
                }
            }
        }
    }

    private void ShootTarget()
    {
        // 1. Güvenlik Kontrolü: Prefab veya namlu ucu atanmamışsa hata verip durdur
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("EnemyAI: Projectile Prefab veya FirePoint atanmamış!");
            return;
        }

        // 2. Mermiyi sunucuda tam namlunun ucunda yarat
        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // 3. Ağdaki (Network) diğer tüm oyuncuların ekranında da görünmesini sağla
        NetworkObject netObj = projectile.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Spawn();
        }

        // 4. Mermiye fiziksel bir itiş gücü (hız) ver
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.forward * projectileSpeed;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        Health.Value -= damage;

        if (Health.Value <= 0 && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}