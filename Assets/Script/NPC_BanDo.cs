using UnityEngine;
using Fusion;

public class NPC_BanDo : MonoBehaviour
{
    [Header("cấu hình UI")]
    public GameObject uiShop; // � ?? k�o c�i Shop_Panel v�o

    private bool isPlayerNearby = false;
    private Player_Controller localPlayer;

    private void OnTriggerEnter(Collider other)
    {
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
            if (uiShop != null) uiShop.SetActive(false); // ?i xa t? ?�ng shop
        }
    }

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.J))
        {
            if (uiShop != null)
            {
                // ??o ng??c tr?ng th�i ?�ng/m? c?a Shop
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
                    // KH�A CHU?T KHI ?�NG SHOP
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}