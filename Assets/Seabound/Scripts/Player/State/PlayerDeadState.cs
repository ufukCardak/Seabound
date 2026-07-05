using System.Collections;
using UnityEngine;

public class PlayerDeadState : PlayerBaseState
{
    private const float RespawnDelay = 5f;

    public override void Enter(PlayerController player)
    {
        if (player.IsOwner)
            player.SyncState.Value = PlayerState.Dead;

        player.StopPlayer();
        player.Rb.isKinematic = true;
        player.Capsule.enabled = false;
        
        if (player.Animator != null)
        {
            player.Animator.SetTrigger("DieTrigger");
        }
        
        if (player.CurrentChest != null && player.ChestInteraction != null)
        {
            player.ChestInteraction.RequestDrop();
        }

if (player.IsServer)
            player.StartCoroutine(RespawnCoroutine(player));
    }

public override void Exit(PlayerController player)
    {
        player.Rb.isKinematic = false;
        player.Capsule.enabled = true;
        
        if (player.Animator != null)
        {
            player.Animator.ResetTrigger("DieTrigger");
            player.Animator.Play("Idle"); 
        }
    }

    public override void Update(PlayerController player)
    {
    }

    private IEnumerator RespawnCoroutine(PlayerController player)
    {
        yield return new UnityEngine.WaitForSeconds(RespawnDelay);
        player.RespawnServerRpc();
    }
}
