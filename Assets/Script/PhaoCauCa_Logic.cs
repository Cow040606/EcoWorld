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

    void OnTriggerEnter(Collider other)
    {
        if (!isLocal || daChamCaiGiDo || chuSohuu == null) return;
        
        // Gọi hàm kiểm tra xem có phải đụng nhầm nhân vật không
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

        // Gọi hàm kiểm tra xem có phải đụng nhầm nhân vật không
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

    // --- BỘ LỌC CỰC MẠNH: Bỏ qua Player, Camera, và các vật thể linh tinh ---
    private bool KiemTraVatTheVoHinh(GameObject vatCham)
    {
        // 1. Nếu đụng trúng cơ thể, tay chân, phụ kiện của chính nhân vật (cùng chung 1 gốc)
        if (vatCham.transform.root == chuSohuu.transform.root) return true;

        // 2. Nếu đụng trúng bất kỳ ai có gán Tag "Player"
        if (vatCham.CompareTag("Player")) return true;

        // 3. Nếu đụng trúng vật thể thuộc Layer "Player" (Đề phòng vũ khí, áo choàng bị lọt lưới)
        if (vatCham.layer == LayerMask.NameToLayer("Player")) return true;
        
        // 4. Bỏ qua Layer "Ignore Raycast" (Thường là Camera, lưới của UI, v.v...)
        if (vatCham.layer == LayerMask.NameToLayer("Ignore Raycast")) return true;

        return false; // Không kẹt cái nào ở trên thì mới tính là chạm đất/nước thực sự
    }
}