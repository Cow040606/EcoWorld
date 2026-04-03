using UnityEngine;
using Fusion; 
using DialogueEditor; 
using UnityEngine.InputSystem; // Nhớ có thư viện này để đọc phím F

public class NPC_DialogueTrigger : MonoBehaviour
{
    [Header("Kéo thả cuộc hội thoại vào đây")]
    public NPCConversation cuocHoiThoaiCuaNPC; 

    // CÔNG TẮC: Để biết người chơi có đang đứng gần NPC không
    private bool isPlayerNearby = false;

    // 1. KHI BƯỚC VÀO VÙNG
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority) 
            {
                isPlayerNearby = true; 
            }
        }
    }

    // 2. KHI ĐI RA KHỎI VÙNG
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority) 
            {
                // Tắt công tắc đi
                isPlayerNearby = false; 
            }
        }
    }

    // 3. KIỂM TRA PHÍM BẤM LIÊN TỤC
    private void Update()
    {
        // Nếu đứng gần NPC VÀ có bấm phím F
        if (isPlayerNearby && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (cuocHoiThoaiCuaNPC != null && ConversationManager.Instance != null)
            {
                if (!ConversationManager.Instance.IsConversationActive)
                {
                    ConversationManager.Instance.StartConversation(cuocHoiThoaiCuaNPC);
                }
            }
        }
    }
}