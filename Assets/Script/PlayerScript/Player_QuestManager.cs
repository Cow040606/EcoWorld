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
    public static Player_QuestManager localQuest; // Để NPC / Script khác dễ gọi

    [Header("Danh sách nhiệm vụ đang làm")]
    public List<NhiemVuDangLam> danhSachNhiemVu = new List<NhiemVuDangLam>();

    [Header("Gắn UI bảng nhiệm vụ vào đây")]
    public TextMeshProUGUI txtBangNhiemVu;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            localQuest = this;
            GameObject uiObj = QuestManager.instance != null ? QuestManager.instance.txtBangNhiemVu : null;

            if (uiObj != null)
            {
                txtBangNhiemVu = uiObj.GetComponent<TextMeshProUGUI>();
            }
            KiemTraTienDo();
        }
    }

    // --- 1. NHẬN NHIỆM VỤ MỚI ---
    public void NhanNhiemVu(QuestSO questMoi)
    {
        if (questMoi == null) return;
        // Kiểm tra xem đã nhận quest này chưa để khỏi nhận trùng
        if (danhSachNhiemVu.Exists(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == questMoi.idNhiemVu)) return;

        NhiemVuDangLam nvMoi = new NhiemVuDangLam();
        nvMoi.duLieuQuest = questMoi;
        nvMoi.soLuongHienTai = 0;
        nvMoi.daDatYeuCau = false;
        danhSachNhiemVu.Add(nvMoi);

        KiemTraTienDo(); // Quét ngay tiến độ ban đầu
    }

    // --- 2. CẬP NHẬT TIẾN ĐỘ, ICON BẢN ĐỒ & VẼ LÊN CANVAS ---
    public void KiemTraTienDo()
    {
        if (txtBangNhiemVu == null && QuestManager.instance != null && QuestManager.instance.txtBangNhiemVu != null)
        {
            txtBangNhiemVu = QuestManager.instance.txtBangNhiemVu.GetComponent<TextMeshProUGUI>();
        }

        if (Player_Controller.localPlayer == null) return;

        string noiDungBang = "";

        // Quét từng nhiệm vụ đang nhận
        for (int j = danhSachNhiemVu.Count - 1; j >= 0; j--)
        {
            var nv = danhSachNhiemVu[j];
            if (nv.duLieuQuest == null) continue;

            // Xử lý đếm theo loại nhiệm vụ
            switch (nv.duLieuQuest.loaiNhiemVu)
            {
                case LoaiNhiemVu.GiaoVatPham:
                    int demVatPham = 0;
                    for (int i = 0; i < Player_Controller.localPlayer.TuiDo.Length; i++)
                    {
                        if (Player_Controller.localPlayer.TuiDo[i].ItemID == nv.duLieuQuest.targetID)
                        {
                            demVatPham += Player_Controller.localPlayer.TuiDo[i].SoLuong;
                        }
                    }
                    nv.soLuongHienTai = demVatPham;
                    break;

                case LoaiNhiemVu.TichLuyTien:
                    nv.soLuongHienTai = Player_Controller.localPlayer.Gold;
                    break;

                case LoaiNhiemVu.DatCapDo:
                    nv.soLuongHienTai = Player_Controller.localPlayer.level;
                    break;

                case LoaiNhiemVu.TieuDietQuai:
                case LoaiNhiemVu.CauCa:
                case LoaiNhiemVu.ThuHoach:
                case LoaiNhiemVu.TroChuyenNPC:
                case LoaiNhiemVu.CheTao:
                    break;
            }

            nv.daDatYeuCau = (nv.soLuongHienTai >= nv.duLieuQuest.soLuongCan);

            // Viết dòng chữ cho Nhiệm vụ này
            string dong = $"- {nv.duLieuQuest.tenNhiemVu}: {nv.soLuongHienTai}/{nv.duLieuQuest.soLuongCan}";

            if (nv.daDatYeuCau)
            {
                dong += " <color=yellow>(Đã đạt yêu cầu)</color>";
            }

            noiDungBang += dong + "\n";
        }

        // Bắn dòng chữ lên Canvas (Giữ lại dự phòng nếu vẫn xài txtBangNhiemVu)
        if (txtBangNhiemVu != null) txtBangNhiemVu.text = noiDungBang;

        // Cập nhật giao diện Prefab mới
        if (QuestManager.instance != null)
        {
            QuestManager.instance.CapNhatUI_NhiemVu(danhSachNhiemVu);
        }

        // Cập nhật các icon ! trên đầu NPC / Map
        CapNhatTatCaIconNPC();
    }

    // --- 3. HÀM CỘNG TIẾN ĐỘ NHIỆM VỤ THÔNG THƯỜNG ---
    public void TangTienDoNhiemVu(LoaiNhiemVu loai, int targetID, int soLuong = 1)
    {
        bool coThayDoi = false;

        foreach (var nv in danhSachNhiemVu)
        {
            if (nv.duLieuQuest == null) continue;

            if (nv.duLieuQuest.loaiNhiemVu == loai)
            {
                if (nv.duLieuQuest.targetID == 0 || nv.duLieuQuest.targetID == targetID)
                {
                    if (!nv.daDatYeuCau)
                    {
                        nv.soLuongHienTai = Mathf.Min(nv.duLieuQuest.soLuongCan, nv.soLuongHienTai + soLuong);
                        coThayDoi = true;
                    }
                }
            }
        }

        if (coThayDoi)
        {
            KiemTraTienDo();
        }
    }

    // --- 4. TỰ ĐỘNG HOÀN THÀNH & NHẬN THƯỞNG NHIỆM VỤ NÓI CHUYỆN NPC ---
    public void HoanThanhNhiemVuNPC(int npcID)
    {
        NhiemVuDangLam nvNPC = danhSachNhiemVu.Find(x =>
            x.duLieuQuest != null &&
            x.duLieuQuest.loaiNhiemVu == LoaiNhiemVu.TroChuyenNPC &&
            (x.duLieuQuest.targetID == 0 || x.duLieuQuest.targetID == npcID)
        );

        if (nvNPC != null)
        {
            nvNPC.soLuongHienTai = nvNPC.duLieuQuest.soLuongCan;
            nvNPC.daDatYeuCau = true;

            // Tự động trao thưởng ngay lập tức
            TraNhiemVu(nvNPC.duLieuQuest);
        }
    }

    // --- 6. CẬP NHẬT ICON (!) CHO TẤT CẢ NPC TRONG SCENE ---
    public void CapNhatTatCaIconNPC()
    {
        NPC_DialogueTrigger[] danhSachNPC = FindObjectsOfType<NPC_DialogueTrigger>();

        foreach (var npc in danhSachNPC)
        {
            if (npc.npcID <= 0)
            {
                npc.CapNhatIconNhiemVu(false);
                continue;
            }

            bool coNhiemVuLienQuan = danhSachNhiemVu.Exists(x =>
                x.duLieuQuest != null &&
                (
                    x.duLieuQuest.npcID == npc.npcID ||
                    (x.duLieuQuest.loaiNhiemVu == LoaiNhiemVu.TroChuyenNPC && (x.duLieuQuest.targetID == 0 || x.duLieuQuest.targetID == npc.npcID))
                )
            );

            npc.CapNhatIconNhiemVu(coNhiemVuLienQuan);
        }
    }

    // --- 7. TRẢ NHIỆM VỤ (ĐÃ SỬA CHỐNG BUG SKIP) ---
    public void TraNhiemVu(QuestSO questCanTra)
    {
        if (questCanTra == null) return;

        // [BẢO MẬT]: Ép cập nhật lại tiến độ lần cuối để đảm bảo số liệu chính xác nhất
        KiemTraTienDo();

        NhiemVuDangLam nv = danhSachNhiemVu.Find(x => x.duLieuQuest == questCanTra);

        // Kiểm tra chặt chẽ: Phải có trong danh sách ĐANG LÀM và thực sự ĐÃ ĐẠT YÊU CẦU
        if (nv != null && nv.daDatYeuCau)
        {
            // Chỉ trừ đồ nếu nhiệm vụ thuộc loại GiaoVatPham
            int idVatPhamCanTru = (questCanTra.loaiNhiemVu == LoaiNhiemVu.GiaoVatPham) ? questCanTra.targetID : 0;
            int soLuongCanTru = (questCanTra.loaiNhiemVu == LoaiNhiemVu.GiaoVatPham) ? questCanTra.soLuongCan : 0;

            // Gọi Server trao thưởng & trừ đồ (nếu có) qua RPC của Photon Fusion
            if (Player_Controller.localPlayer != null)
            {
                Player_Controller.localPlayer.RPC_HoanThanhQuest(
                    idVatPhamCanTru,
                    soLuongCanTru,
                    questCanTra.tienThuong,
                    questCanTra.gemThuong,
                    questCanTra.idVatPhamThuong,
                    questCanTra.soLuongVatPhamThuong,
                    questCanTra.expThuong
                );
            }

            // Hiện thông báo hoàn thành
            if (QuestNotifyManager.Instance != null)
            {
                QuestNotifyManager.Instance.ShowQuestComplete(questCanTra.tenNhiemVu, (int)questCanTra.expThuong, (int)questCanTra.tienThuong);
            }

            // Đánh dấu đã hoàn thành vĩnh viễn
            LuuVetNhiemVuDaXong(questCanTra.idNhiemVu);

            // Xóa khỏi danh sách & cập nhật UI
            danhSachNhiemVu.Remove(nv);
            KiemTraTienDo();
        }
        else
        {
            Debug.LogWarning($"<color=red>[Quest Security]</color> Chặn hành vi trả nhiệm vụ sai lệ: {questCanTra?.tenNhiemVu}. Player chưa làm xong hoặc quest không tồn tại!");
        }
    }

    // --- 8. EXPORT DỮ LIỆU SAVE NHIỆM VỤ ---
    public List<QuestSaveData> ExportQuestSaveData()
    {
        List<QuestSaveData> list = new List<QuestSaveData>();
        foreach (var nv in danhSachNhiemVu)
        {
            if (nv.duLieuQuest != null)
            {
                list.Add(new QuestSaveData
                {
                    idNhiemVu = nv.duLieuQuest.idNhiemVu,
                    soLuongHienTai = nv.soLuongHienTai,
                    daDatYeuCau = nv.daDatYeuCau
                });
            }
        }
        return list;
    }

    [Header("Lịch sử nhiệm vụ đã làm xong")]
    public List<int> danhSachDaHoanThanh = new List<int>();

    // Đánh dấu nhiệm vụ đã làm xong
    public void LuuVetNhiemVuDaXong(int questID)
    {
        if (!danhSachDaHoanThanh.Contains(questID))
        {
            danhSachDaHoanThanh.Add(questID);
        }
    }

    // --- 9. IMPORT DỮ LIỆU SAVE NHIỆM VỤ ---
    public void ImportQuestSaveData(List<QuestSaveData> saveList, List<int> completedList = null)
    {
        if (saveList != null)
        {
            danhSachNhiemVu.Clear();
            QuestSO[] tatCaQuestSO = Resources.FindObjectsOfTypeAll<QuestSO>();

            foreach (var itemSave in saveList)
            {
                QuestSO questSO = System.Array.Find(tatCaQuestSO, q => q.idNhiemVu == itemSave.idNhiemVu);
                if (questSO != null)
                {
                    NhiemVuDangLam nv = new NhiemVuDangLam
                    {
                        duLieuQuest = questSO,
                        soLuongHienTai = itemSave.soLuongHienTai,
                        daDatYeuCau = itemSave.daDatYeuCau
                    };
                    danhSachNhiemVu.Add(nv);
                }
            }
            KiemTraTienDo();
        }

        if (completedList != null)
        {
            danhSachDaHoanThanh = completedList;
        }
    }
}