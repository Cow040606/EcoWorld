using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

// Cấu trúc để lưu tiến độ của từng nhiệm vụ
[System.Serializable]
public class NhiemVuDangLam
{
    public QuestSO duLieuQuest;
    public int soLuongHienTai;
    public bool daDatYeuCau;
}

public class Player_QuestManager : NetworkBehaviour
{
    public static Player_QuestManager localQuest; // Để NPC dễ gọi (Giống vụ Player_Controller)

    [Header("Danh sách nhiệm vụ đang làm")]
    public List<NhiemVuDangLam> danhSachNhiemVu = new List<NhiemVuDangLam>();
    
    [Header("Gắn UI bảng nhiệm vụ vào đây")]
    public TextMeshProUGUI txtBangNhiemVu; 

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            localQuest = this;
            KiemTraTienDo();
            GameObject uiObj = GameObject.Find("Nhiemvu"); 

            
            if (uiObj != null) 
            {
                txtBangNhiemVu = uiObj.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    // --- 1. NHẬN NHIỆM VỤ MỚI ---
    public void NhanNhiemVu(QuestSO questMoi)
    {
        // Kiểm tra xem đã nhận quest này chưa để khỏi nhận trùng
        if (danhSachNhiemVu.Exists(x => x.duLieuQuest.idNhiemVu == questMoi.idNhiemVu)) return;

        NhiemVuDangLam nvMoi = new NhiemVuDangLam();
        nvMoi.duLieuQuest = questMoi;
        danhSachNhiemVu.Add(nvMoi);
        Debug.Log("da nhan nhiem vu");
        KiemTraTienDo(); // Quét túi đồ xem có sẵn đồ chưa
    }

    // --- 2. CẬP NHẬT TIẾN ĐỘ & VẼ LÊN CANVAS ---
    public void KiemTraTienDo()
    {
        if (Player_Controller.localPlayer == null) return;

        string noiDungBang = "";

        // Quét từng nhiệm vụ đang nhận
        for (int j = danhSachNhiemVu.Count - 1; j >= 0; j--)
        {
            var nv = danhSachNhiemVu[j];
            int dem = 0;

            // Lục trong Balo (TuiDo) xem có bao nhiêu cục đồ
            for (int i = 0; i < Player_Controller.localPlayer.TuiDo.Length; i++)
            {
                if (Player_Controller.localPlayer.TuiDo[i].ItemID == nv.duLieuQuest.idVatPhamCanTim)
                {
                    dem += Player_Controller.localPlayer.TuiDo[i].SoLuong;
                }
            }

            nv.soLuongHienTai = dem;
            nv.daDatYeuCau = (nv.soLuongHienTai >= nv.duLieuQuest.soLuongCan);

            // Viết dòng chữ cho Nhiệm vụ này
            string dong = $"- {nv.duLieuQuest.tenNhiemVu}: {nv.soLuongHienTai}/{nv.duLieuQuest.soLuongCan}";
            
            if (nv.daDatYeuCau) 
            {
                // Thêm chữ màu vàng cực nổi bật
                dong += " <color=yellow>(Đã đạt yêu cầu)</color>";
            }

            noiDungBang += dong + "\n";
        }

        // Bắn dòng chữ lên Canvas
        if (txtBangNhiemVu != null) txtBangNhiemVu.text = noiDungBang;
    }

    // --- 3. TRẢ NHIỆM VỤ ---
    public void TraNhiemVu(QuestSO questCanTra)
    {
        NhiemVuDangLam nv = danhSachNhiemVu.Find(x => x.duLieuQuest == questCanTra);

        if (nv != null && nv.daDatYeuCau)
        {
            // (1) Gọi Server trừ đồ và cộng tiền (Bò cần viết thêm hàm RPC bên Player_Controller giống hàm Bán đồ nhé)
            Player_Controller.localPlayer.RPC_HoanThanhQuest(questCanTra.idVatPhamCanTim, questCanTra.soLuongCan, questCanTra.tienThuong);

            // (2) Xóa khỏi danh sách & UI biến mất
            danhSachNhiemVu.Remove(nv);
            KiemTraTienDo(); 
        }
    }
}