using UnityEngine;

public class BoatHelmInteractable : MonoBehaviour, IInteractable
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

        if (!boat.IsDriven.Value)
            boatInteraction.RequestDrive(boat);
    }

    public string GetInteractText()
    {
        if (boat == null) return "[F] Interact";
        
        var spawner = boat.GetComponent<BoatCrewSpawner>();
        if (spawner != null && !spawner.IsCrewDefeated.Value)
            return "Enemy Ship!";
        
        if (!boat.IsDriven.Value)
            return "[F] Drive Boat";
        else
            return "Helm in Use";
    }
}
