using Unity.Netcode;
using UnityEngine;

public class WeaponComponent : NetworkBehaviour
{
    [Header("Available Weapons")]
    [SerializeField] private WeaponDataSO[] availableWeapons;

    private IWeapon currentWeapon;
    private WeaponDataSO currentWeaponData;

    private float nextFireTime = 0f;

public override void OnNetworkSpawn()
    {
        EquipWeaponByIndex(0);
    }

    public void EquipWeaponByIndex(int index)
    {
        if (availableWeapons == null || availableWeapons.Length == 0) return;
        
        index = Mathf.Clamp(index, 0, availableWeapons.Length - 1);
        var data = availableWeapons[index];

        if (data != null)
        {
            currentWeaponData = data;
            if (data.weaponType == WeaponType.Shotgun)
                currentWeapon = new ShotgunWeapon(data, this);
            else
                currentWeapon = new HitscanWeapon(data, this);
        }
    }

public void RequestFire()
    {
        if (!IsOwner) return;

        if (Time.time < nextFireTime) return;

        var inventory = GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            if (currentWeaponData != null && currentWeaponData.weaponType == WeaponType.Shotgun)
            {
                if (inventory.BlunderbussAmmo.Value <= 0)
                {
                    Debug.Log("No Blunderbuss ammo!");
                    return;
                }
            }
            else
            {
                if (inventory.Ammo.Value <= 0)
                {
                    Debug.Log("No Pistol ammo!");
                    return;
                }
            }
        }

        if (currentWeaponData != null)
        {
            nextFireTime = Time.time + currentWeaponData.fireRate;
            if (CameraController.Instance != null) 
            {
                CameraController.Instance.AddRecoil(currentWeaponData.weaponType == WeaponType.Shotgun ? 4f : 2f);
            }
        }

        if (inventory != null)
        {
            if (currentWeaponData != null && currentWeaponData.weaponType == WeaponType.Shotgun)
                inventory.AddBlunderbussAmmoServerRpc(-1);
            else
                inventory.AddAmmoServerRpc(-1);
        }

        Transform activeFP = GetActiveFirePoint();
        if (activeFP == null) 
        {
            Debug.LogError("No Active FirePoint found!");
            return;
        }

        Debug.Log("Active FirePoint found: " + activeFP.name);

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Vector3 targetPoint = ray.GetPoint(100f);
        
        if (DebugManager.Instance != null && DebugManager.Instance.showDebugVisuals)
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.magenta, 2f);

        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        bool foundValidHit = false;
        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == transform.root) continue;
            
            targetPoint = hit.point;
            foundValidHit = true;
            break;
        }

        Vector3 direction = (targetPoint - activeFP.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        Debug.Log($"Firing towards targetPoint: {targetPoint}, Direction: {direction}");
        FireServerRpc(activeFP.position, rotation);
    }

    private Transform GetActiveFirePoint()
    {
        foreach (var fp in GetComponentsInChildren<Transform>())
        {
            if (fp.name == "FirePoint" && fp.gameObject.activeInHierarchy)
            {
                return fp;
            }
        }
        return transform;
    }

    public string CurrentWeaponName => currentWeapon?.WeaponName ?? "None";

[Rpc(SendTo.Server)]
    private void FireServerRpc(Vector3 position, Quaternion rotation)
    {
        currentWeapon?.Fire(position, rotation, OwnerClientId);
    }

    public void PerformHitscan(Vector3 origin, Vector3 direction, int damage, ulong ownerClientId, Color tracerColor)
    {
        float range = 100f;
        Vector3 endPos = origin + direction * range;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, range);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == transform.root) continue;

            endPos = hit.point;
            
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                
                ShowHitmarkerRpc(RpcTarget.Single(ownerClientId, RpcTargetUse.Temp));
                
                Transform attacker = GetPlayerByClientId(ownerClientId);
                if (attacker != null)
                {
                    var ai = hit.collider.GetComponentInParent<EnemyAIBase>();
                    if (ai != null) ai.OnAttackedBy(attacker);
                    
                    var boat = hit.collider.GetComponentInParent<BoatController>();
                    if (boat != null)
                    {
                        var allGuards = FindObjectsByType<EnemyGuardAI>(FindObjectsSortMode.None);
                        foreach (var g in allGuards)
                            if (g.AssignedBoat == boat) g.OnAttackedBy(attacker);
                    }
                }
            }
            break;
        }
        
        SpawnTracerRpc(origin, endPos, tracerColor);
    }

    [Rpc(SendTo.Everyone)]
    private void SpawnTracerRpc(Vector3 start, Vector3 end, Color tracerColor)
    {
        GameObject tracer = new GameObject("PlayerTracer");
        LineRenderer lr = tracer.AddComponent<LineRenderer>();
        
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        
        Material tracerMat = new Material(Shader.Find("Sprites/Default"));
        tracerMat.color = tracerColor;
        lr.material = tracerMat;
        
        Destroy(tracer, 0.1f);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowHitmarkerRpc(RpcParams rpcParams)
    {
        if (HUDManager.Instance != null)
            HUDManager.Instance.ShowHitmarker();
    }

    private Transform GetPlayerByClientId(ulong clientId)
    {
        foreach (var p in EnemyTargetRegistry.Players)
        {
            if (p != null && p.TryGetComponent<NetworkObject>(out var no))
            {
                if (no.OwnerClientId == clientId) return p;
            }
        }
        return null;
    }

private class HitscanWeapon : IWeapon
    {
        private readonly WeaponDataSO data;
        private readonly WeaponComponent comp;

        public string WeaponName => data.weaponName;

        public HitscanWeapon(WeaponDataSO data, WeaponComponent comp)
        {
            this.data = data;
            this.comp = comp;
        }

        public void Fire(Vector3 position, Quaternion rotation, ulong ownerClientId)
        {
            if (data == null) return;
            comp.PerformHitscan(position, rotation * Vector3.forward, data.damage, ownerClientId, new Color(1f, 0.8f, 0f, 0.8f));
        }
    }

    private class ShotgunWeapon : IWeapon
    {
        private readonly WeaponDataSO data;
        private readonly WeaponComponent comp;

        public string WeaponName => data.weaponName;

        public ShotgunWeapon(WeaponDataSO data, WeaponComponent comp)
        {
            this.data = data;
            this.comp = comp;
        }

        public void Fire(Vector3 position, Quaternion rotation, ulong ownerClientId)
        {
            if (data == null) return;

            int count = data.pelletCount;
            float angleStep = count > 1 ? data.spreadAngle / (count - 1) : 0f;
            float startAngle = -data.spreadAngle / 2f;

            for (int i = 0; i < count; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Quaternion spreadRotation = rotation * Quaternion.Euler(0, currentAngle, 0);
                
                comp.PerformHitscan(position, spreadRotation * Vector3.forward, data.damage, ownerClientId, new Color(1f, 0.3f, 0f, 0.8f));
            }
        }
    }
}
