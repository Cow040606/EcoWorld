using UnityEngine;

public class NPC_QuestBridge : MonoBehaviour
{
    // Bắt tín hiệu từ Node Hội thoại và quăng cho Player
    public void GiaoViecChoPlayer(QuestSO quest)
    {
        if (Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.NhanNhiemVu(quest);
            CapNhatThongSoQuestSO(quest); // Tự động cập nhật thông số ngay sau khi nhận nhiệm vụ
        }
    }

    // Sự kiện khi bấm "Done / Trả nhiệm vụ" trong Hội thoại
    public void ThuHoiViecTuPlayer(QuestSO quest)
    {
        if (Player_QuestManager.localQuest != null)
        {
            // [BẢO VỆ KÉP]: Kiểm tra xem thực sự đã làm xong chưa trước khi cho phép trả nhiệm vụ
            if (KiemTraHoanThanhNhiemVu(quest))
            {
                Player_QuestManager.localQuest.TraNhiemVu(quest);
                CapNhatThongSoQuestSO(quest); // Tự động cập nhật thông số ngay sau khi trả nhiệm vụ
            }
            else
            {
                Debug.LogWarning($"<color=red>[LỖI SKIP]</color> Người chơi cố gắng skip/trả nhiệm vụ '{quest.tenNhiemVu}' nhưng chưa đủ điều kiện!");
            }
        }
    }

    // ==========================================
    // THÊM MỚI: DÙNG CHO PHẦN "CONDITIONS" TRONG DIALOGUE EDITOR
    // ==========================================

    // Hàm này check xem nhiệm vụ ĐÃ HOÀN THÀNH chưa
    // HƯỚNG DẪN: Kéo hàm này vào Condition của Option Node "Trả nhiệm vụ"
    public bool KiemTraHoanThanhNhiemVu(QuestSO quest)
    {
        if (quest == null) return false;
        if (Player_QuestManager.localQuest != null)
        {
            // Force check lại tiến độ ngay lúc nói chuyện để số liệu mới nhất
            Player_QuestManager.localQuest.KiemTraTienDo();

            NhiemVuDangLam nv = Player_QuestManager.localQuest.danhSachNhiemVu.Find(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == quest.idNhiemVu);
            if (nv != null)
            {
                return nv.daDatYeuCau; // Chỉ trả về true nếu đã làm xong
            }
        }
        return false;
    }

    // Hàm này check xem Player ĐANG LÀM nhiệm vụ này nhưng CHƯA XONG
    // HƯỚNG DẪN: Kéo hàm này vào Condition của Option Node "Ta đang làm" / "Chưa xong"
    public bool KiemTraChuaHoanThanh(QuestSO quest)
    {
        if (quest == null) return false;
        if (Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.KiemTraTienDo();
            NhiemVuDangLam nv = Player_QuestManager.localQuest.danhSachNhiemVu.Find(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == quest.idNhiemVu);
            if (nv != null)
            {
                return !nv.daDatYeuCau; // Đang làm nhưng chưa xong
            }
        }
        return false;
    }

    // Hàm này check xem nhiệm vụ CHƯA TỪNG ĐƯỢC NHẬN (chưa có trong danh sách đang làm và chưa làm xong)
    // HƯỚNG DẪN: Dùng để làm điều kiện cho câu thoại mở đầu "Cậu có muốn giúp tôi không?"
    public bool KiemTraChuaTungNhanNhiemVu(QuestSO quest)
    {
        if (quest == null) return false;
        if (Player_QuestManager.localQuest != null)
        {
            // Nếu đã từng làm xong và trả rồi -> false (không cho nhận lại)
            if (Player_QuestManager.localQuest.danhSachDaHoanThanh.Contains(quest.idNhiemVu))
            {
                return false; 
            }

            // Nếu đang nằm trong danh sách nhiệm vụ ĐANG LÀM -> false
            bool dangLam = Player_QuestManager.localQuest.danhSachNhiemVu.Exists(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == quest.idNhiemVu);
            if (dangLam)
            {
                return false;
            }

            // Nếu qua 2 ải trên, nghĩa là thực sự chưa từng đụng tới
            return true;
        }
        return false;
    }

    // =========================================================================
    // HÀM VOID DÙNG CHO EVENT (XUẤT HIỆN TRONG DROPDOWN) ĐỂ CẬP NHẬT THAM SỐ
    // =========================================================================

    // Gọi hàm này ở Event của Root Node để cập nhật trạng thái Quest 1 (Skeletons) vào Parameter của Dialogue
    // HƯỚNG DẪN: Tạo 3 biến Bool trong tab Parameters của Dialogue Editor: "ChuaNhanQ1", "DangLamQ1", "DaXongQ1"
    public void CapNhatThongSoQuest1(QuestSO quest)
    {
        if (Player_QuestManager.localQuest != null && DialogueEditor.ConversationManager.Instance != null)
        {
            Player_QuestManager.localQuest.KiemTraTienDo();
            
            bool chuaNhan = KiemTraChuaTungNhanNhiemVu(quest);
            bool dangLam = KiemTraChuaHoanThanh(quest);
            bool daXong = KiemTraHoanThanhNhiemVu(quest);

            DialogueEditor.ConversationManager.Instance.SetBool("ChuaNhanQ1", chuaNhan);
            DialogueEditor.ConversationManager.Instance.SetBool("DangLamQ1", dangLam);
            DialogueEditor.ConversationManager.Instance.SetBool("DaXongQ1", daXong);

            Debug.Log($"[Dialogue Bridge] Cập nhật Quest 1: ChuaNhan={chuaNhan}, DangLam={dangLam}, DaXong={daXong}");
        }
    }

    // Gọi hàm này ở Event của Root Node hoặc sau khi trả Quest 1 để cập nhật trạng thái Quest 2 (Orc King)
    // HƯỚNG DẪN: Tạo 2 biến Bool trong tab Parameters của Dialogue Editor: "DangLamQ2", "DaXongQ2"
    public void CapNhatThongSoQuest2(QuestSO quest)
    {
        if (Player_QuestManager.localQuest != null && DialogueEditor.ConversationManager.Instance != null)
        {
            Player_QuestManager.localQuest.KiemTraTienDo();

            bool dangLam = KiemTraChuaHoanThanh(quest);
            bool daXong = KiemTraHoanThanhNhiemVu(quest);

            DialogueEditor.ConversationManager.Instance.SetBool("DangLamQ2", dangLam);
            DialogueEditor.ConversationManager.Instance.SetBool("DaXongQ2", daXong);

            Debug.Log($"[Dialogue Bridge] Cập nhật Quest 2: DangLam={dangLam}, DaXong={daXong}");
        }
    }

    // =========================================================================
    // HÀM TỰ ĐỘNG CẬP NHẬT PARAMETER THEO TÊN FILE QUEST (DÙNG CHO MỌI QUEST KHÁC)
    // =========================================================================
    // HƯỚNG DẪN: Tạo các biến Bool trong tab Parameters của Dialogue Editor theo cú pháp:
    // "[Tên_File_Quest]_ChuaNhan", "[Tên_File_Quest]_DangLam", "[Tên_File_Quest]_DaXong"
    // Ví dụ với file "LV20.asset": "LV20_ChuaNhan", "LV20_DangLam", "LV20_DaXong"
    // Ví dụ với file "craft.asset": "craft_ChuaNhan", "craft_DangLam", "craft_DaXong"
    public void CapNhatThongSoQuestSO(QuestSO quest)
    {
        if (quest == null || Player_QuestManager.localQuest == null || DialogueEditor.ConversationManager.Instance == null) return;

        Player_QuestManager.localQuest.KiemTraTienDo();

        string questName = quest.name; // Lấy tên của file asset

        bool chuaNhan = KiemTraChuaTungNhanNhiemVu(quest);
        bool dangLam = KiemTraChuaHoanThanh(quest);
        bool daXong = KiemTraHoanThanhNhiemVu(quest);

        DialogueEditor.ConversationManager.Instance.SetBool(questName + "_ChuaNhan", chuaNhan);
        DialogueEditor.ConversationManager.Instance.SetBool(questName + "_DangLam", dangLam);
        DialogueEditor.ConversationManager.Instance.SetBool(questName + "_DaXong", daXong);

        Debug.Log($"[Dialogue Bridge] Auto-Set Parameter: {questName}_ChuaNhan={chuaNhan}, {questName}_DangLam={dangLam}, {questName}_DaXong={daXong}");
    }

    // Cập nhật thông số cho tất cả QuestSO có trong game vào Dialogue parameters
    public void CapNhatTatCaThongSoQuest()
    {
        if (Player_QuestManager.localQuest == null || DialogueEditor.ConversationManager.Instance == null) return;

        // Force check lại tiến độ tất cả nhiệm vụ một lần trước khi cập nhật
        Player_QuestManager.localQuest.KiemTraTienDo();

        QuestSO[] tatCaQuestSO = Resources.FindObjectsOfTypeAll<QuestSO>();
        foreach (var quest in tatCaQuestSO)
        {
            if (quest != null)
            {
                CapNhatThongSoQuestSOInternal(quest);
            }
        }
    }

    private void CapNhatThongSoQuestSOInternal(QuestSO quest)
    {
        if (quest == null || Player_QuestManager.localQuest == null || DialogueEditor.ConversationManager.Instance == null) return;

        string questName = quest.name; // Lấy tên của file asset

        bool chuaNhan = KiemTraChuaTungNhanNhiemVuInternal(quest);
        bool dangLam = KiemTraChuaHoanThanhInternal(quest);
        bool daXong = KiemTraHoanThanhNhiemVuInternal(quest);

        DialogueEditor.ConversationManager.Instance.SetBool(questName + "_ChuaNhan", chuaNhan);
        DialogueEditor.ConversationManager.Instance.SetBool(questName + "_DangLam", dangLam);
        DialogueEditor.ConversationManager.Instance.SetBool(questName + "_DaXong", daXong);

        Debug.Log($"[Dialogue Bridge] Auto-Set Parameter Internal: {questName}_ChuaNhan={chuaNhan}, {questName}_DangLam={dangLam}, {questName}_DaXong={daXong}");
    }

    private bool KiemTraHoanThanhNhiemVuInternal(QuestSO quest)
    {
        if (quest == null) return false;
        if (Player_QuestManager.localQuest != null)
        {
            NhiemVuDangLam nv = Player_QuestManager.localQuest.danhSachNhiemVu.Find(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == quest.idNhiemVu);
            if (nv != null)
            {
                return nv.daDatYeuCau;
            }
        }
        return false;
    }

    private bool KiemTraChuaHoanThanhInternal(QuestSO quest)
    {
        if (quest == null) return false;
        if (Player_QuestManager.localQuest != null)
        {
            NhiemVuDangLam nv = Player_QuestManager.localQuest.danhSachNhiemVu.Find(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == quest.idNhiemVu);
            if (nv != null)
            {
                return !nv.daDatYeuCau;
            }
        }
        return false;
    }

    private bool KiemTraChuaTungNhanNhiemVuInternal(QuestSO quest)
    {
        if (quest == null) return false;
        if (Player_QuestManager.localQuest != null)
        {
            if (Player_QuestManager.localQuest.danhSachDaHoanThanh.Contains(quest.idNhiemVu))
            {
                return false; 
            }

            bool dangLam = Player_QuestManager.localQuest.danhSachNhiemVu.Exists(x => x.duLieuQuest != null && x.duLieuQuest.idNhiemVu == quest.idNhiemVu);
            if (dangLam)
            {
                return false;
            }

            return true;
        }
        return false;
    }
}