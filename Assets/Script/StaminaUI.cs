using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [Header("Giao diện")]
    public Image thanhTheLucVang; 
    public CanvasGroup khungGoc;  

    [Header("Cài đặt tàng hình")]
    public float thoiGianDoi = 1f;
    private float dongHoDemNguoc = 0f;

    void Update()
    {
        // Kiểm tra xem nhân vật đã xuất hiện chưa
        if (Player_Controller.localPlayer != null && Player_Controller.localPlayer.Object != null && Player_Controller.localPlayer.Object.IsValid)
        {
            float hienTai = Player_Controller.localPlayer.CurrentStamina;
            float toiDa = Player_Controller.localPlayer.MaxStamina;

            // 1. Chạy thanh thể lực mượt mà
            thanhTheLucVang.fillAmount = hienTai / toiDa;

            // 2. Logic Ẩn/Hiện thông minh
            if (hienTai < toiDa)
            {
                // Khi KHÔNG ĐẦY (đang chạy hoặc đang hồi) -> Hiện rõ lên
                khungGoc.alpha = Mathf.Lerp(khungGoc.alpha, 1f, Time.deltaTime * 10f);
                
                // Liên tục nạp lại đồng hồ đếm ngược về 2 giây
                dongHoDemNguoc = thoiGianDoi;
            }
            else // Khi THỂ LỰC ĐÃ ĐẦY (hienTai >= toiDa)
            {
                if (dongHoDemNguoc > 0)
                {
                    // Vẫn đang trong 2 giây chờ đợi -> Trừ dần thời gian, giữ nguyên độ nét
                    dongHoDemNguoc -= Time.deltaTime;
                    khungGoc.alpha = Mathf.Lerp(khungGoc.alpha, 1f, Time.deltaTime * 10f);
                }
                else
                {
                    // Đã hết 2 giây và không có sự thay đổi -> Bắt đầu mờ dần đi
                    khungGoc.alpha = Mathf.Lerp(khungGoc.alpha, 0f, Time.deltaTime * 5f);
                }
            }
        }
    }
}