using UnityEngine;

public class KhoiNuoc_VatLy : MonoBehaviour
{
    [Header("Cấu hình Nước")]
    public float lucDayNoi = 15f; // Lực đẩy lên (Số càng to nảy càng mạnh)
    public float lucCanNuoc = 2f; // Độ cản của nước (Làm vật trôi chậm lại, bồng bềnh hơn)

    private float matNuocY; // Lưu tọa độ bề mặt nước

    void Start()
    {
        // Tự động tính xem cái mặt hồ nằm ở độ cao bao nhiêu
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            matNuocY = transform.position.y + box.bounds.extents.y;
        }
    }

    // Khi có một vật rớt vào bên trong khối nước (Trigger)
    void OnTriggerStay(Collider other)
    {
        // Kiểm tra xem vật đó có chịu tác động vật lý không
        Rigidbody rb = other.GetComponent<Rigidbody>();
        
        if (rb != null)
        {
            // Tính toán xem vật đang chìm sâu bao nhiêu so với mặt nước
            float doChim = matNuocY - other.transform.position.y;

            if (doChim > 0)
            {
                // Chìm càng sâu, lực đẩy lên càng mạnh (nhưng khóa tối đa là 1 để không văng lên trời)
                float lucDay = Mathf.Clamp(doChim, 0f, 1f) * lucDayNoi;
                
                // Đẩy vật ngược lên trên
                rb.AddForce(Vector3.up * lucDay, ForceMode.Acceleration);
                
                // Bơm thêm lực cản để vật không bị nảy tưng tưng như quả bóng cao su
                rb.linearDamping = lucCanNuoc;
                rb.angularDamping = lucCanNuoc;
            }
        }
    }

    // Khi người chơi vớt vật đó ra khỏi nước
    void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Trả lại trạng thái rơi tự do bình thường
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
        }
    }
}