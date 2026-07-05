using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public static class EnemyTargetRegistry
{
    private static readonly List<Transform> players = new List<Transform>();

    public static IReadOnlyList<Transform> Players => players;

    public static void Register(Transform player)
    {
        if (!players.Contains(player))
            players.Add(player);
    }

    public static void Unregister(Transform player)
    {
        players.Remove(player);
    }
}
