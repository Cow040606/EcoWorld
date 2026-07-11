using UnityEngine;
using UnityEngine.UI;
using TMPro; // Dùng để hiển thị chữ

public class UI_Giap : MonoBehaviour
{
    [Header("--- THÀNH PHẦN UI ---")]
    [Tooltip("Kéo cục cha chứa toàn bộ thanh giáp vào đây (để ẩn đi khi không mặc giáp)")]
    public GameObject khungGiapTong; 

    [Tooltip("Kéo cái hình ảnh thanh giáp (Image type: Filled) vào đây")]
    public Image thanhGiapFill; 
    public GameObject Giapbroke; 
    private Image anhgiapbroke;

    [Tooltip("Kéo text hiển thị số (ví dụ: 50/100) vào đây (Nếu không có thì để trống)")]
    public TextMeshProUGUI chuGiap; 
    void Awake()
    {
        if (Giapbroke != null) anhgiapbroke = Giapbroke.GetComponent<Image>();
    }

    void Update()
    {
        // Kiểm tra xem nhân vật của mình đã xuất hiện trong game chưa
        if (Player_Controller.localPlayer != null)
        {
            float giapHienTai = Player_Controller.localPlayer.CurrentArmor;
            float giapToiDa = Player_Controller.localPlayer.MaxArmor;

            // 1. TRƯỜNG HỢP CỞI TRUỒNG (Không mặc giáp -> Tắt sạch)
            if (giapToiDa <= 0)
            {
                if (khungGiapTong != null) khungGiapTong.SetActive(false);
                if (Giapbroke != null) Giapbroke.SetActive(false); // Cởi truồng thì tắt luôn cái icon bể
                return; // Ngừng chạy code bên dưới để tránh lỗi chia cho 0
            }
            
            // 2. TRƯỜNG HỢP CÓ MẶC GIÁP
            if (khungGiapTong != null && !khungGiapTong.activeSelf)
            {
                khungGiapTong.SetActive(true);
            }

            // --- XỬ LÝ BẬT/TẮT ICON GIÁP HƯ NẰM Ở ĐÂY ---
            if (Giapbroke != null)
            {
                if (giapHienTai <= 0) 
                {
                    // Giáp hiện tại về 0 (bị đập bể) -> Bật icon vỡ
                    Giapbroke.SetActive(true);
                }
                else 
                {
                    // Vẫn còn giáp -> Tắt icon vỡ
                    Giapbroke.SetActive(false);
                }
            }

            // 3. CẬP NHẬT THANH CHẠY (IMAGE FILL)
            if (thanhGiapFill != null)
            {
                // Chia lấy tỷ lệ từ 0.0 đến 1.0 cho thanh Fill Amount
                thanhGiapFill.fillAmount = giapHienTai / giapToiDa;
            }

            // 4. CẬP NHẬT CHỮ SỐ (TEXT)
            if (chuGiap != null)
            {
                // Dùng Mathf.RoundToInt để làm tròn số, lỡ giáp bị lẻ 49.5 thì nó hiện 50 cho đẹp
                chuGiap.text = $"{Mathf.RoundToInt(giapHienTai)} / {Mathf.RoundToInt(giapToiDa)}";
            }
        }
    }
}