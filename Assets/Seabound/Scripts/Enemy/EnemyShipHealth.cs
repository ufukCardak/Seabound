using Unity.Netcode;
using UnityEngine;

public class EnemyShipHealth : NetworkBehaviour
{
    [Header("Sinking")]
    [SerializeField] private float sinkDuration = 4f;
    [SerializeField] private float sinkDepth = 8f;

    [Header("Crew & Loot")]
    [Tooltip("Child NetworkObjects (guards, chests) to despawn when ship sinks.")]
    [SerializeField] private NetworkObject[] crewNetworkObjects;

    private bool isSinking;
    private float sinkTimer;
    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 startScale;

    private HealthComponent health;

private void Awake() => health = GetComponent<HealthComponent>();

    private void OnEnable()
    {
        if (health != null) health.OnDeath += StartSinking;
    }

    private void OnDisable()
    {
        if (health != null) health.OnDeath -= StartSinking;
    }

    private void Update()
    {
        if (!isSinking) return;

        sinkTimer += Time.deltaTime;
        float t = Mathf.Clamp01(sinkTimer / sinkDuration);

        transform.position = Vector3.Lerp(startPos, targetPos, t);
        transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

        if (IsServer && t >= 1f)
            DespawnAll();
    }

public void TakeDamage(int damage) => health?.TakeDamage(damage);

private void StartSinking()
    {
        if (isSinking) return;

        isSinking = true;
        sinkTimer = 0f;
        startPos = transform.position;
        targetPos = startPos + Vector3.down * sinkDepth;
        startScale = transform.localScale;

        if (TryGetComponent<Collider>(out var col))
            col.enabled = false;
    }

    private void DespawnAll()
    {
        if (crewNetworkObjects != null)
        {
            foreach (var no in crewNetworkObjects)
                if (no != null && no.IsSpawned) no.Despawn();
        }

        if (NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}
