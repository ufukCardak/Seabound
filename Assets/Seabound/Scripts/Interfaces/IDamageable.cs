using System;

public interface IDamageable
{
    int CurrentHealth { get; }

    void TakeDamage(int damage);

    event Action<int> OnHealthChanged;

    event Action OnDeath;
}
