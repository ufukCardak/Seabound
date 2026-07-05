using System;
using Unity.Netcode;
using UnityEngine;

public class HealthComponent : NetworkBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private int maxHealth = 100;

    private NetworkVariable<int> health;

    public int CurrentHealth => health?.Value ?? maxHealth;

    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

private void Awake()
    {
        health = new NetworkVariable<int>(
            maxHealth,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            health.Value = maxHealth;

        health.OnValueChanged += HandleHealthChanged;

        OnHealthChanged?.Invoke(health.Value);
    }

    public override void OnNetworkDespawn()
    {
        health.OnValueChanged -= HandleHealthChanged;
    }

public void TakeDamage(int damage)
    {
        if (!IsServer) return;

        int next = Mathf.Clamp(health.Value - damage, 0, maxHealth);
        health.Value = next;

        if (next <= 0)
            NotifyDeathClientRpc();
    }

    public void Heal(int amount)
    {
        if (!IsServer) return;
        health.Value = Mathf.Clamp(health.Value + amount, 0, maxHealth);
    }

    public void FullHeal()
    {
        if (!IsServer) return;
        health.Value = maxHealth;
    }

private void HandleHealthChanged(int _, int newValue)
    {
        OnHealthChanged?.Invoke(newValue);
    }

    [ClientRpc]
    private void NotifyDeathClientRpc()
    {
        OnDeath?.Invoke();
    }
}
