using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class BoatCrewSpawner : NetworkBehaviour
{
    [Header("Spawning")]
    public GameObject GuardPrefab;
    public GameObject ChestPrefab;
    public int CrewSize = 2;

    [Header("Spawn Settings")]
    public bool SpawnEnemies = true;

    public NetworkVariable<bool> IsCrewDefeated = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private List<EnemyGuardAI> spawnedCrew = new List<EnemyGuardAI>();
    private bool hasSpawned = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer && SpawnEnemies && GuardPrefab != null)
        {
            StartCoroutine(SpawnCrewDeferred());
        }
    }

    private IEnumerator SpawnCrewDeferred()
    {
        yield return new WaitForSeconds(0.5f);
        if (!IsServer) yield break;
        SpawnCrew();
    }

    private void SpawnCrew()
    {
        BoatController boat = GetComponent<BoatController>();
        
        Vector3[] crewOffsets = CalculateCrewPositions(CrewSize);

        for (int i = 0; i < CrewSize; i++)
        {
            Vector3 spawnPos = transform.TransformPoint(crewOffsets[i]);

            GameObject guardObj = Instantiate(GuardPrefab, spawnPos, transform.rotation);

            var ai = guardObj.GetComponent<EnemyGuardAI>();
            if (ai != null && boat != null)
            {
                ai.AssignedBoat = boat;
                ai.IsCaptain = (i == 0);
                
                if (i == 1 && boat.PassengerSeatPosition != null)
                    ai.IsPassenger = true;
            }

            var no = guardObj.GetComponent<NetworkObject>();
            if (no != null)
            {
                no.Spawn(true);
                no.TrySetParent(transform);
            }
            if (ai != null) spawnedCrew.Add(ai);
        }

        hasSpawned = true;

        if (ChestPrefab != null)
        {
            Vector3 chestOffset = new Vector3(0f, 0.3f, -1.5f);
            Vector3 chestPos = transform.TransformPoint(chestOffset);
            
            GameObject chestObj = Instantiate(ChestPrefab, chestPos, transform.rotation);
            var chestNet = chestObj.GetComponent<NetworkObject>();
            if (chestNet != null) 
            {
                chestNet.Spawn(true);
            }

            var chest = chestObj.GetComponent<ChestController>();
            if (chest != null)
            {
                chest.AttachToBoat(transform);
            }
        }
    }

    private Vector3[] CalculateCrewPositions(int count)
    {
        var positions = new Vector3[count];
        
        BoatController bc = GetComponent<BoatController>();
        
        if (bc != null && bc.HelmPosition != null)
        {
            positions[0] = transform.InverseTransformPoint(bc.HelmPosition.position);
        }
        else
        {
            positions[0] = new Vector3(0f, 0.4f, 0.2f);
        }

        if (count > 1)
        {
            if (bc != null && bc.PassengerSeatPosition != null)
            {
                positions[1] = transform.InverseTransformPoint(bc.PassengerSeatPosition.position);
            }
            else
            {
                positions[1] = new Vector3(0.3f, 0.4f, 2.0f);
            }
        }

        float deckStartZ = 2.0f;
        float deckEndZ = -1.0f;
        float deckY = 0.4f;

        for (int i = 2; i < count; i++)
        {
            float t = (count > 2) ? (float)(i - 2) / (count - 2) : 0f;
            float z = Mathf.Lerp(deckStartZ, deckEndZ, t);
            float x = (i % 2 == 0) ? -0.3f : 0.3f;
            positions[i] = new Vector3(x, deckY, z);
        }

        return positions;
    }

    private void Update()
    {
        if (!IsServer || !hasSpawned || IsCrewDefeated.Value) return;

        spawnedCrew.RemoveAll(c => c == null || c.gameObject == null);

        if (spawnedCrew.Count == 0)
        {
            IsCrewDefeated.Value = true;
        }
    }
}
