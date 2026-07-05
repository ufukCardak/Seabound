using UnityEngine;

public interface IWeapon
{
    string WeaponName { get; }

    void Fire(Vector3 position, Quaternion rotation, ulong ownerClientId);
}
