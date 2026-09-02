using UnityEngine;
using System.Collections.Generic;

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
                // Debug.LogWarning($"<color=red>[LỖI SKIP]</color> Người chơi cố gắng skip/trả nhiệm vụ '{quest.tenNhiemVu}' nhưng chưa đủ điều kiện!");
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

            // Debug.Log($"[Dialogue Bridge] Cập nhật Quest 1: ChuaNhan={chuaNhan}, DangLam={dangLam}, DaXong={daXong}");
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

            // Debug.Log($"[Dialogue Bridge] Cập nhật Quest 2: DangLam={dangLam}, DaXong={daXong}");
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

        // 1. Gán theo chuẩn cũ mặc định để tương thích ngược
        SetBoolIfParameterExists(questName + "_ChuaNhan", chuaNhan);
        SetBoolIfParameterExists(questName + "_DangLam", dangLam);
        SetBoolIfParameterExists(questName + "_DaXong", daXong);

        // 2. Tìm kiếm thông minh dựa trên danh sách tham số đang có trong Conversation
        SmartMapQuestStatus(questName, chuaNhan, dangLam, daXong);

        // Debug.Log($"[Dialogue Bridge] Auto-Set Parameter: {questName}_ChuaNhan={chuaNhan}, {questName}_DangLam={dangLam}, {questName}_DaXong={daXong}");
    }

    // Cập nhật thông số cho tất cả QuestSO có trong game vào Dialogue parameters
    public void CapNhatTatCaThongSoQuest()
    {
        if (Player_QuestManager.localQuest == null || DialogueEditor.ConversationManager.Instance == null) return;

        // Force check lại tiến độ tất cả nhiệm vụ một lần trước khi cập nhật
        Player_QuestManager.localQuest.KiemTraTienDo();

        // 1. Quét qua các nhiệm vụ đang thực hiện (chắc chắn có trong memory và quan trọng nhất)
        foreach (var nv in Player_QuestManager.localQuest.danhSachNhiemVu)
        {
            if (nv != null && nv.duLieuQuest != null)
            {
                CapNhatThongSoQuestSOInternal(nv.duLieuQuest);
            }
        }

        // 2. Quét qua tất cả QuestSO trong bộ nhớ dự phòng
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

        // 1. Gán theo chuẩn cũ mặc định
        SetBoolIfParameterExists(questName + "_ChuaNhan", chuaNhan);
        SetBoolIfParameterExists(questName + "_DangLam", dangLam);
        SetBoolIfParameterExists(questName + "_DaXong", daXong);

        // 2. Tìm kiếm thông minh dựa trên danh sách tham số đang có trong Conversation
        SmartMapQuestStatus(questName, chuaNhan, dangLam, daXong);

        // Debug.Log($"[Dialogue Bridge] Auto-Set Parameter Internal: {questName}_ChuaNhan={chuaNhan}, {questName}_DangLam={dangLam}, {questName}_DaXong={daXong}");
    }

    // ==========================================
    // THÊM MỚI: CÁC HÀM HỖ TRỢ BẢN ĐỒ THAM SỐ THÔNG MINH
    // ==========================================
    private void SetBoolIfParameterExists(string paramName, bool value)
    {
        if (DialogueEditor.ConversationManager.Instance == null) return;
        List<string> paramNames = DialogueEditor.ConversationManager.Instance.GetParameterNames();
        if (paramNames.Contains(paramName))
        {
            DialogueEditor.ConversationManager.Instance.SetBool(paramName, value);
        }
    }

    private void SmartMapQuestStatus(string questName, bool chuaNhan, bool dangLam, bool daXong)
    {
        if (DialogueEditor.ConversationManager.Instance == null) return;

        List<string> paramNames = DialogueEditor.ConversationManager.Instance.GetParameterNames();
        foreach (string param in paramNames)
        {
            string statusType;
            if (IsMatchingQuest(questName, param, out statusType))
            {
                if (statusType == "chuanhan")
                {
                    DialogueEditor.ConversationManager.Instance.SetBool(param, chuaNhan);
                    // Debug.Log($"[Smart Quest Map] Mapped parameter '{param}' to quest '{questName}' ChuaNhan={chuaNhan}");
                }
                else if (statusType == "danglam")
                {
                    DialogueEditor.ConversationManager.Instance.SetBool(param, dangLam);
                    // Debug.Log($"[Smart Quest Map] Mapped parameter '{param}' to quest '{questName}' DangLam={dangLam}");
                }
                else if (statusType == "daxong")
                {
                    DialogueEditor.ConversationManager.Instance.SetBool(param, daXong);
                    // Debug.Log($"[Smart Quest Map] Mapped parameter '{param}' to quest '{questName}' DaXong={daXong}");
                }
            }
        }
    }

    private bool IsMatchingQuest(string questName, string paramName, out string statusType)
    {
        statusType = null;
        
        string nQuest = NormalizeString(questName);
        string nParam = NormalizeString(paramName);
        
        // Xử lý các lỗi chính tả phổ biến (ví dụ: "bosskill" vs "boss skill")
        nQuest = nQuest.Replace("bossskill", "bosskill");
        nParam = nParam.Replace("bossskill", "bosskill");
        
        // Kiểm tra xem tên tham số có chứa tên nhiệm vụ đã chuẩn hóa hay không
        bool hasQuestName = nParam.Contains(nQuest);
        
        // Trường hợp đặc biệt nếu questName là một phần của tham số hoặc ngược lại
        if (!hasQuestName)
        {
            if (nQuest == "killske" && nParam.Contains("killske")) hasQuestName = true;
            if (nQuest == "bosskill" && nParam.Contains("bosskill")) hasQuestName = true;
        }
        
        if (!hasQuestName) return false;
        
        // Phân loại trạng thái nhiệm vụ dựa trên từ khóa trong tên tham số
        if (nParam.Contains("chuanhan") || nParam.Contains("notstarted") || nParam.Contains("chuathuchien"))
        {
            statusType = "chuanhan";
            return true;
        }
        else if (nParam.Contains("danglam") || nParam.Contains("inprogress") || nParam.Contains("chuanhoanthanh") || nParam.Contains("chuaxong"))
        {
            statusType = "danglam";
            return true;
        }
        else if (nParam.Contains("daxong") || nParam.Contains("hoanthanh") || nParam.Contains("completed") || nParam.Contains("complete") || nParam.Contains("done") || nParam.Contains("xong"))
        {
            statusType = "daxong";
            return true;
        }
        
        return false;
    }

    private string NormalizeString(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLower().Trim();
        s = s.Replace("_", "").Replace(" ", "").Replace("-", "");
        s = RemoveSign4VietnameseString(s);
        return s;
    }

    private string RemoveSign4VietnameseString(string str)
    {
        string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
            "đ",
            "é","è","ẻ","ẽ","ẹ","ê","ế","ề","ể","ễ","ệ",
            "í","ì","ỉ","ĩ","ị",
            "ó","ò","ỏ","õ","ọ","ô","ố","ồ","ổ","ỗ","ộ","ơ","ớ","ờ","ở","ỡ","ợ",
            "ú","ù","ủ","ũ","ụ","ư","ứ","ừ","ử","ữ","ự",
            "ý","ỳ","ỷ","ỹ","ỵ",};
        string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
            "d",
            "e","e","e","e","e","e","e","e","e","e","e",
            "i","i","i","i","i",
            "o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o","o",
            "u","u","u","u","u","u","u","u","u","u","u",
            "y","y","y","y","y",};
        for (int i = 0; i < arr1.Length; i++)
        {
            str = str.Replace(arr1[i], arr2[i]);
            str = str.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
        }
        return str;
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