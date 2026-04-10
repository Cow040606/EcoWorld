using Fusion;
using UnityEngine;

public class LocalPortal : NetworkBehaviour
{
    public Transform targetDestination;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkCharacterController ncc = other.GetComponent<NetworkCharacterController>();
            
            if (ncc != null && ncc.HasStateAuthority)
            {
                ncc.Teleport(targetDestination.position);
            }
        }
    }
}