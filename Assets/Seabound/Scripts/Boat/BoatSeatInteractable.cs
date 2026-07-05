using UnityEngine;

public class BoatSeatInteractable : MonoBehaviour, IInteractable
{
    private BoatController boat;

    private void Awake()
    {
        boat = GetComponentInParent<BoatController>();
    }

    public void Interact(PlayerController player)
    {
        if (boat == null) return;

        var spawner = boat.GetComponent<BoatCrewSpawner>();
        if (spawner != null && !spawner.IsCrewDefeated.Value) return;

        var boatInteraction = player.GetComponent<PlayerBoatInteraction>();
        if (boatInteraction == null) return;

        boatInteraction.RequestBoardAsPassenger(boat);
    }

    public string GetInteractText()
    {
        if (boat == null) return "[F] Interact";

        var spawner = boat.GetComponent<BoatCrewSpawner>();
        if (spawner != null && !spawner.IsCrewDefeated.Value)
            return "Seat Occupied";

        if (boat.GetPassengerCount() >= 1)
            return "Seat Occupied";

        return "[F] Board Passenger";
    }
}
