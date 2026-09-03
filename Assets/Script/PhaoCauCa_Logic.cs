using UnityEngine;

public class PhaoCauCa_Logic : MonoBehaviour
{
    public Player_Controller chuSohuu; 
    public bool isLocal; 
    private bool daChamCaiGiDo = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (chuSohuu != null)
        {
            Collider[] playerColliders = chuSohuu.GetComponentsInChildren<Collider>();
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            
            foreach (var myCol in myColliders)
            {
                foreach (var pCol in playerColliders)
                {
                    Physics.IgnoreCollision(myCol, pCol);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isLocal || daChamCaiGiDo || chuSohuu == null) return;
        
        // Gá»i hÃ m kiá»ƒm tra xem cÃ³ pháº£i Ä‘á»¥ng nháº§m nhÃ¢n váº­t khÃ´ng
        if (KiemTraVatTheVoHinh(other.gameObject)) return;

        if (((1 << other.gameObject.layer) & chuSohuu.waterLayer) != 0)
        {
            daChamCaiGiDo = true;
            chuSohuu.PhaoDaChamNuoc(); 
            if (rb != null) 
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLocal || daChamCaiGiDo || chuSohuu == null) return;

        // Gá»i hÃ m kiá»ƒm tra xem cÃ³ pháº£i Ä‘á»¥ng nháº§m nhÃ¢n váº­t khÃ´ng
        if (KiemTraVatTheVoHinh(collision.gameObject)) return;

        if (((1 << collision.gameObject.layer) & chuSohuu.waterLayer) != 0)
        {
            daChamCaiGiDo = true;
            chuSohuu.PhaoDaChamNuoc();
            if (rb != null) 
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            return;
        }

        daChamCaiGiDo = true;
        chuSohuu.PhaoRotTrenCan();
    }

    // --- Bá»˜ Lá»ŒC Cá»°C Máº NH: Bá» qua Player, Camera, vÃ  cÃ¡c váº­t thá»ƒ linh tinh ---
    private bool KiemTraVatTheVoHinh(GameObject vatCham)
    {
        // 1. Náº¿u Ä‘á»¥ng trÃºng cÆ¡ thá»ƒ, tay chÃ¢n, phá»¥ kiá»‡n cá»§a chÃ­nh nhÃ¢n váº­t (cÃ¹ng chung 1 gá»‘c)
        if (vatCham.transform.root == chuSohuu.transform.root) return true;

        // 2. Náº¿u Ä‘á»¥ng trÃºng báº¥t ká»³ ai cÃ³ gÃ¡n Tag "Player"
        if (vatCham.CompareTag("Player")) return true;

        // 3. Náº¿u Ä‘á»¥ng trÃºng váº­t thá»ƒ thuá»™c Layer "Player" (Äá» phÃ²ng vÅ© khÃ­, Ã¡o choÃ ng bá»‹ lá»t lÆ°á»›i)
        if (vatCham.layer == LayerMask.NameToLayer("Player")) return true;
        
        // 4. Bá» qua Layer "Ignore Raycast" (ThÆ°á»ng lÃ  Camera, lÆ°á»›i cá»§a UI, v.v...)
        if (vatCham.layer == LayerMask.NameToLayer("Ignore Raycast")) return true;

        return false; // KhÃ´ng káº¹t cÃ¡i nÃ o á»Ÿ trÃªn thÃ¬ má»›i tÃ­nh lÃ  cháº¡m Ä‘áº¥t/nÆ°á»›c thá»±c sá»±
    }
}
