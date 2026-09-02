using UnityEngine;
using TMPro;
using Fusion; // Nhớ thêm dòng này để xài tính năng mạng

public class NameManager : MonoBehaviour
{
    [Header("=== UI CÀI ĐẶT TÊN ===")]
    public TMP_InputField inputTenMoi; // Kéo ô nhập tên ngoài Map vào đây

    // Bò gắn hàm này vào sự kiện OnClick() của cái Nút "Lưu Tên"
    public void BamNut_LuuTenVaoPlayer()
    {
        // 1. Lấy tên Bò vừa gõ trong ô Input
        string tenCanDoi = inputTenMoi.text.Trim();

        // Nếu Bò bấm lưu mà lỡ để trống thì dẹp, không làm gì cả
        if (string.IsNullOrEmpty(tenCanDoi))
        {
            // Debug.LogWarning("<color=yellow>Hệ Thống:</color> Bò chưa nhập tên kìa!");
            return;
        }

        // 2. Lưu tên vào bộ nhớ máy để ván sau chơi nó vẫn nhớ
        PlayerPrefs.SetString("TenNhanVat", tenCanDoi);
        PlayerPrefs.Save();

        // 3. QUÉT RADAR TÌM NHÂN VẬT CỦA BÒ TRONG MAP
        // Tìm tất cả các thẻ căn cước Player_Data đang có trong cảnh
        Player_Data[] danhSachNguoiChoi = FindObjectsOfType<Player_Data>();

        bool daTimThayChinhChu = false;

        foreach (Player_Data nguoiChoi in danhSachNguoiChoi)
        {
            // BỨC TƯỜNG LỬA: Chỉ cho phép nhét tên nếu đây là nhân vật do Bò điều khiển
            if (nguoiChoi.Object.HasInputAuthority)
            {
                // Gọi điện báo Server đổi tên ngay lập tức
                nguoiChoi.RPC_SetPlayerName(tenCanDoi); 
                
                // Debug.Log($"<color=cyan>Hệ Thống:</color> Đã gắn tên [{tenCanDoi}] vào nhân vật thành công!");
                daTimThayChinhChu = true;
                
                // Tìm thấy rồi thì nghỉ, không quét nữa cho nhẹ máy
                break; 
            }
        }

        if (!daTimThayChinhChu)
        {
            // Debug.LogError("<color=red>LỖI:</color> Không tìm thấy nhân vật của Bò để đổi tên. Chắc chưa Spawn ra rồi!");
        }
    }
    private void OnEnable()
    {
        // Kéo cái tên cũ trong máy tính ra và nhét sẵn vào ô nhập
        if (inputTenMoi != null)
        {
            inputTenMoi.text = PlayerPrefs.GetString("TenNhanVat", "");
        }
    }
}