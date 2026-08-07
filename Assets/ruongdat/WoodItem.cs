using Fusion;
using UnityEngine;

public class WoodItem : NetworkBehaviour
{
    // Bạn có thể thêm các Networked variable vào đây nếu gỗ có số lượng, độ bền,...
    // Ví dụ:
    [Networked] public int woodAmount { get; set; } = 1;

    private Rigidbody rb;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Đảm bảo ban đầu không có lực cản để rớt nhanh
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            
            // Bắt đầu quá trình đóng băng tạm thời để tránh va chạm mạnh lúc mới đẻ ra
            StartCoroutine(DongBangTamThoi());
        }
    }

    private System.Collections.IEnumerator DongBangTamThoi()
    {
        // 1. Tạm thời vô hiệu hóa vật lý (treo lơ lửng trên không)
        rb.isKinematic = true;

        // 2. Chờ 0.5 giây để đảm bảo cái cây cũ đã biến mất hoàn toàn khỏi Terrain
        yield return new WaitForSeconds(0.5f);

        // 3. Bật lại vật lý để rớt tự do
        if (rb != null)
        {
            rb.isKinematic = false;
            
            // Tùy chọn: Thêm một lực nảy nhẹ tưng tưng cho đẹp mắt
            rb.AddForce(new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)), ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // KHI GỖ CHẠM VÀO MẶT ĐẤT HOẶC BẤT CỨ THỨ GÌ
        if (rb != null)
        {
            // Lập tức tung "phanh" (Damping) cực mạnh để nó ngừng lăn/trượt ngay lập tức
            rb.linearDamping = 5f;
            rb.angularDamping = 5f;
        }
    }
}