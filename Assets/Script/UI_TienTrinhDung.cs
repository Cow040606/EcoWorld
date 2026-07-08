using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_TienTrinhDung : MonoBehaviour
{
    public static UI_TienTrinhDung instance;

    [Header("UI Elements")]
    public GameObject khungUI; // Kéo cha của vòng tròn vào đây để bật/tắt
    public Image vongTron; // Kéo ảnh vòng tròn vào đây
    public TextMeshProUGUI chuThoiGian; // Kéo text đếm số vào đây

    void Awake()
    {
        instance = this;
        if (khungUI != null) khungUI.SetActive(false); // Vừa vào game thì ẩn đi
    }

    // Hàm này sẽ được Player gọi liên tục mỗi khung hình khi đang dùng đồ
    public void CapNhatUI(float thoiGianConLai, float tongThoiGian)
    {
        if (!khungUI.activeSelf) khungUI.SetActive(true);

        // Vòng tròn sẽ mòn dần
        if (vongTron != null) 
        {
            vongTron.fillAmount = thoiGianConLai / tongThoiGian;
        }

        // Chữ sẽ đếm ngược: 9.5s, 9.4s...
        if (chuThoiGian != null)
        {
            chuThoiGian.text = thoiGianConLai.ToString("F1") + "s";
        }
    }

    public void AnUI()
    {
        if (khungUI != null) khungUI.SetActive(false);
    }
}