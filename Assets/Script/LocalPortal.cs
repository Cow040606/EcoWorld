using Fusion;
using UnityEngine;

public class LocalPortal : NetworkBehaviour
{
    public Transform targetDestination;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Có vật thể chạm vào cổng: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Đã xác nhận đây là Player!");
            
            NetworkCharacterController ncc = other.GetComponent<NetworkCharacterController>();
            
            if (ncc != null)
            {
                Debug.Log("Đã tìm thấy NetworkCharacterController!");
                
                if (ncc.HasStateAuthority)
                {
                    Debug.Log("Có quyền Authority, tiến hành Teleport!");
                    ncc.Teleport(targetDestination.position);
                }
                else
                {
                    Debug.Log("Không có State Authority, bỏ qua Teleport.");
                }
            }
            else
            {
                Debug.Log("Không tìm thấy Component NetworkCharacterController trên Player!");
            }
        }
    }
}