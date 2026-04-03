using UnityEngine;
using Fusion; 
using DialogueEditor; // Bắt buộc phải có dòng này để gọi công cụ của họ

public class NPC_DialogueTrigger : MonoBehaviour
{
    [Header("Kéo thả cuộc hội thoại vào đây")]
    // Cái biến này sẽ nhận cái hộp thoại Bò đã thiết kế bằng Dialogue Editor
    public NPCConversation cuocHoiThoaiCuaNPC; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // BƯỚC 1: Kiểm tra xem có đúng là nhân vật của người đang chơi không?
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.HasInputAuthority) 
            {
                if (cuocHoiThoaiCuaNPC != null)
                {
                    ConversationManager.Instance.StartConversation(cuocHoiThoaiCuaNPC);
                }
            }
        }
    }
}