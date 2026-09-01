using UnityEngine;
// Nếu game Bò là Multiplayer, có thể cần thêm using Fusion; để check chính chủ

public class ZoneMusic : MonoBehaviour
{
    [Header("=== CÀI ĐẶT NHẠC ===")]
    public AudioClip nhacSukuna;       // Kéo nhạc Boss/Khu vực đặc biệt vào đây
    public AudioClip nhacBinhThuong;   // Kéo nhạc dạo quanh Map vào đây
    
    [Header("=== LOA PHÁT ===")]
    public AudioSource nguonPhatChinh; // Kéo AudioSource chính của Map vào đây

    // 1. Khi nhân vật bước VÀO vùng này
    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem có đúng là Player bước vào không
        if (other.CompareTag("Player"))
        {
            // (Bảo mật Multiplayer): Nếu Bò muốn chỉ chủ acc bước vào mới đổi nhạc, 
            // bỏ comment 2 dòng dưới và comment dòng CompareTag ở trên nhé.
            // Player_Data data = other.GetComponent<Player_Data>();
            // if (data != null && data.Object.HasInputAuthority)

            if (nguonPhatChinh.clip != nhacSukuna)
            {
                nguonPhatChinh.clip = nhacSukuna;
                nguonPhatChinh.Play();
                // Debug.Log("<color=red>Cảnh Báo:</color> Đã vào lãnh địa Sukuna!");
            }
        }
    }

    // 2. Khi nhân vật bước RA KHỎI vùng này
    private void OnTriggerExit(Collider other)
    {
        // Kiểm tra xem thằng vừa bước ra có phải là Player không
        if (other.CompareTag("Player"))
        {
            // Chuyển lại nhạc bình thường
            if (nguonPhatChinh.clip != nhacBinhThuong)
            {
                nguonPhatChinh.clip = nhacBinhThuong;
                nguonPhatChinh.Play();
                // Debug.Log("<color=green>An Toàn:</color> Đã thoát khỏi lãnh địa, bật nhạc Chill~");
            }
        }
    }
}