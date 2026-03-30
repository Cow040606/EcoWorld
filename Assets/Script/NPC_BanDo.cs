using UnityEngine;
using Fusion;

public class NPC_BanDo : MonoBehaviour
{
    [Header("C?u hình UI")]
    public GameObject uiShop; // Ô ?? kéo cái Shop_Panel vào

    private bool isPlayerNearby = false;
    private Player_Controller localPlayer;

    private void OnTriggerEnter(Collider other)
    {
        // Ki?m tra ?úng Tag Player c?a nhân v?t
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            localPlayer = other.GetComponent<Player_Controller>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            if (uiShop != null) uiShop.SetActive(false); // ?i xa t? ?óng shop
        }
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.J))
        {
            if (uiShop != null)
            {
                // ??o ng??c tr?ng thái ?óng/m? c?a Shop
                bool dangMo = !uiShop.activeSelf;
                uiShop.SetActive(dangMo);

                if (dangMo)
                {
                    // HI?N CHU?T KHI M? SHOP
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    if (InventoryManager.instance != null)
                        InventoryManager.instance.BatTatBalo(localPlayer.TuiDo, localPlayer);
                }
                else
                {
                    // KHÓA CHU?T KHI ?ÓNG SHOP
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}