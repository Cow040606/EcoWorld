using UnityEngine;

public class PhaoCauCa_Logic : MonoBehaviour
{
    public Player_Controller chuSohuu; // Lưu lại chủ nhân để gọi hàm khi chạm nước
    public bool isLocal; // Biến này để đảm bảo chỉ máy người ném mới xử lý logic
    private bool daChamCaiGiDo = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Khi phao đụng vào một vùng Trigger (như mặt nước)
    void OnTriggerEnter(Collider other)
    {
        if (!isLocal || daChamCaiGiDo) return;

        // Kiểm tra xem vật đụng trúng có thuộc Layer Nước không
        if (((1 << other.gameObject.layer) & chuSohuu.waterLayer) != 0)
        {
            daChamCaiGiDo = true;
            
            // Đóng băng phao lại để nó nổi trên mặt nước (không bị rớt xuyên qua)
            if (rb != null) rb.isKinematic = true; 
            
            // Báo về cho Nhân vật biết là phao đã tới nơi an toàn
            chuSohuu.PhaoDaChamNuoc();
        }
    }

    // Khi phao va chạm vật lý cứng (đất, đá, cây...)
    void OnCollisionEnter(Collision collision)
    {
        if (!isLocal || daChamCaiGiDo) return;

        // Nếu lỡ ném trúng chính mình thì bỏ qua
        if (collision.gameObject == chuSohuu.gameObject) return;

        daChamCaiGiDo = true;
        // Báo về cho Nhân vật biết là ném xịt lên bờ rồi!
        chuSohuu.PhaoRotTrenCan();
    }
}