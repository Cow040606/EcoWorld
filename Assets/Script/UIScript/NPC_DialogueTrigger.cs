using UnityEngine;
using DialogueEditor; 
using UnityEngine.InputSystem; 

public class NPC_DialogueTrigger : MonoBehaviour
{
    [Header("Định danh NPC")]
    public int npcID; // ID NPC (khớp với targetID trong QuestSO)

    [Header("Kéo thả cuộc hội thoại vào đây")]
    public NPCConversation cuocHoiThoaiCuaNPC; 

    [Header("Khoảng cách được phép chat (Mét)")]
    public float tamHoatDong = 5f; 

    [Header("Icon Dấu Chấm Cảm (!) Nhiệm Vụ")]
    public GameObject iconDauChamCam; // GameObject icon ! trên đầu NPC hoặc trên Map

    private bool dangNóiChuyenVoiNPCNay = false;

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += KhiKetThucHoiThoai;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= KhiKetThucHoiThoai;
    }

    private void Start()
    {
        // Ẩn icon ban đầu
        CapNhatIconNhiemVu(false);
    }

    private void Update()
    {
        if (Player_Controller.localPlayer == null) return;
        if (ShopUIController.instance != null && ShopUIController.instance.isShopOpen) return; 
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo) return;
        if (QuestManager.instance != null && QuestManager.instance.isQuest_Open) return;

        float khoangCach = Vector3.Distance(transform.position, Player_Controller.localPlayer.transform.position);

        if (khoangCach <= tamHoatDong)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (cuocHoiThoaiCuaNPC != null && ConversationManager.Instance != null)
                {
                    if (!ConversationManager.Instance.IsConversationActive)
                    {
                        dangNóiChuyenVoiNPCNay = true;
                        ConversationManager.Instance.StartConversation(cuocHoiThoaiCuaNPC);
                    }
                }
            }
        }
    }

    private void KhiKetThucHoiThoai()
    {
        if (dangNóiChuyenVoiNPCNay)
        {
            dangNóiChuyenVoiNPCNay = false;

            // Báo cho QuestManager hoàn thành nhiệm vụ và tự nhận thưởng!
            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.HoanThanhNhiemVuNPC(npcID);
            }
        }
    }

    // Hàm bật / ẩn Icon dấu chấm cảm !
    public void CapNhatIconNhiemVu(bool hienIcon)
    {
        if (iconDauChamCam != null)
        {
            iconDauChamCam.SetActive(hienIcon);
        }
    }
}