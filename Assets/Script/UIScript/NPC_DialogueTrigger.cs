using UnityEngine;
using DialogueEditor; 
using UnityEngine.InputSystem; 

public class NPC_DialogueTrigger : MonoBehaviour
{
    [Header("Kéo thả cuộc hội thoại vào đây")]
    public NPCConversation cuocHoiThoaiCuaNPC; 

    [Header("Khoảng cách được phép chat (Mét)")]
    public float tamHoatDong = 5f; // Bò có thể chỉnh xa gần tùy ý ngoài Inspector

    private void Update()
    {
        if (Player_Controller.localPlayer == null) return;
        if (ShopUIController.instance != null && ShopUIController.instance.isShopOpen) return; 
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo) return;
        if (QuestManager.instance != null && QuestManager.instance.isQuest_Open) return;
        // ---------------------------------

        float khoangCach = Vector3.Distance(transform.position, Player_Controller.localPlayer.transform.position);

        if (khoangCach <= tamHoatDong)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
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
}