using UnityEngine;

public enum WeaponType { Pistol, Shotgun }

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Seabound/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    public string weaponName = "Pistol";
    public WeaponType weaponType = WeaponType.Pistol;
    
    [Header("Hitscan & Projectile")]
    public int damage = 25;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public float fireRate = 0.5f;

    [Header("Shotgun Settings")]
    public int pelletCount = 5;
    public float spreadAngle = 15f;
}
