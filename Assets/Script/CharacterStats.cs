using UnityEngine;
using TMPro; // Bắt buộc phải có để dùng TextMeshPro

public class UI_StatsManager : MonoBehaviour
{
    public static UI_StatsManager instance;

    [Header("Chữ hiển thị Điểm Cộng")]
    public TextMeshProUGUI txtDiemMau;
    public TextMeshProUGUI txtDiemTiemNang;
    public TextMeshProUGUI txtSucManh;
    public TextMeshProUGUI txtTheLuc;
    public TextMeshProUGUI txtNhanhNhen;

    [Header("Chữ hiển thị Chỉ Số Tổng")]
    public TextMeshProUGUI txtMauToiDa;
    public TextMeshProUGUI txtStaminaToiDa;
    public TextMeshProUGUI txtSatThuong;
    public TextMeshProUGUI txtTocDo;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Update()
    {
        // Liên tục cập nhật hình ảnh UI dựa trên dữ liệu mạng của Player cục bộ
        if (Player_Controller.localPlayer != null)
        {
            CapNhatThongTinTrenBang();
        }
    }

    public void CapNhatThongTinTrenBang()
    {
        var player = Player_Controller.localPlayer;

        // 1. In số điểm tiềm năng hiện có
        if (txtDiemTiemNang != null) txtDiemTiemNang.text = "Điểm chưa cộng: " + player.AvailablePoints;
        if (txtDiemMau != null) txtDiemMau.text = "Chỉ số Máu: " + player.DiemMau;
        if (txtSucManh != null) txtSucManh.text = "Sức mạnh: " + player.DiemSucManh;
        if (txtTheLuc != null) txtTheLuc.text = "Thể lực: " + player.DiemTheLuc;
        if (txtNhanhNhen != null) txtNhanhNhen.text = "Tốc độ: " + player.DiemNhanhNhen;

        // 2. In chỉ số tổng (Đã bao gồm gốc + điểm cộng + trang bị)
        if (txtMauToiDa != null) txtMauToiDa.text = "Máu tối đa: " + player.MaxHealth;
        if (txtStaminaToiDa != null) txtStaminaToiDa.text = "Năng lượng: " + player.MaxStamina;
        if (txtSatThuong != null) txtSatThuong.text = "Sát thương: " + player.attackDamageToAnimal;
        if (txtTocDo != null) txtTocDo.text = "Tốc độ chạy: " + player.speed;
    }

    // ==========================================
    // CÁC HÀM NÀY DÙNG ĐỂ GẮN VÀO NÚT BẤM [+]
    // ==========================================
    
    public void NutBam_HP()
    {
        if (Player_Controller.localPlayer != null)
        {
            Player_Controller.localPlayer.RPC_CongDiemTiemNang(4); // 1 là Sức mạnh
            YeuCauCapNhatTongChiSo();
        }
    }

    public void NutBam_CongTheLuc()
    {
        if (Player_Controller.localPlayer != null)
        {
            Player_Controller.localPlayer.RPC_CongDiemTiemNang(2); // 2 là Thể lực
            YeuCauCapNhatTongChiSo();
        }
    }

    public void NutBam_CongNhanhNhen()
    {
        if (Player_Controller.localPlayer != null)
        {
            Player_Controller.localPlayer.RPC_CongDiemTiemNang(3); // 3 là Nhanh nhẹn
            YeuCauCapNhatTongChiSo();
        }
    }

    private void YeuCauCapNhatTongChiSo()
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.CapNhatLaiToanBoChiSo();
        }
    }
}