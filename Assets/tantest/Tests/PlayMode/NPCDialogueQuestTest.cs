using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class NPCDialogueQuestTest
{
    // Lớp helper Reflection để gọi code của game chính không cần tham chiếu trực tiếp
    public static class Refl
    {
        private static Assembly gameAssembly;
        public static Assembly GameAssembly
        {
            get
            {
                if (gameAssembly == null)
                {
                    gameAssembly = Assembly.Load("Assembly-CSharp");
                }
                return gameAssembly;
            }
        }

        public static Type GetType(string fullName)
        {
            return GameAssembly.GetType(fullName);
        }

        public static object GetStaticField(string typeName, string fieldName)
        {
            Type t = GetType(typeName);
            FieldInfo f = t.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return f.GetValue(null);
        }

        public static object GetField(object obj, string fieldName)
        {
            FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return f.GetValue(obj);
        }

        public static void SetField(object obj, string fieldName, object value)
        {
            FieldInfo f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(obj, value);
        }

        public static object CallMethod(object obj, string methodName, params object[] args)
        {
            MethodInfo m = obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return m.Invoke(obj, args);
        }

        public static object CallStaticMethod(string typeName, string methodName, params object[] args)
        {
            Type t = GetType(typeName);
            MethodInfo m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return m.Invoke(null, args);
        }
    }

    [UnityTest]
    public IEnumerator RunNPCDialogueQuestLineMultipleTimes()
    {
        // 1. Tải scene map1
        Debug.Log("[TEST] Loading map1 scene...");
        SceneManager.LoadScene("Scenes/map1");
        yield return new WaitForSeconds(2.0f); // Chờ load map

        // 2. Chờ Player và các NPC xuất hiện trong Scene
        GameObject player = null;
        float timeOut = 12f;
        while (player == null && timeOut > 0)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null) yield return new WaitForSeconds(0.5f);
            timeOut -= 0.5f;
        }
        Assert.IsNotNull(player, "[TEST] Player was not found after timeout!");
        Debug.Log("[TEST] Player found successfully!");

        // Lấy các singleton instances của game chính
        var localQuest = Refl.GetStaticField("Player_QuestManager", "localQuest");
        var localPlayer = Refl.GetStaticField("Player_Controller", "localPlayer");
        Assert.IsNotNull(localQuest, "[TEST] Player_QuestManager.localQuest is null!");
        Assert.IsNotNull(localPlayer, "[TEST] Player_Controller.localPlayer is null!");

        // Tìm 3 NPC trong scene dựa trên npcID của họ
        var npcTriggers = GameObject.FindObjectsByType(Refl.GetType("NPC_DialogueTrigger"), FindObjectsSortMode.None);
        Assert.Greater(npcTriggers.Length, 0, "[TEST] No NPC_DialogueTrigger components found in the scene!");

        object princeTrigger = null;
        object kingTrigger = null;
        object wizardTrigger = null;

        foreach (var trigger in npcTriggers)
        {
            int npcID = (int)Refl.GetField(trigger, "npcID");
            if (npcID == 3) princeTrigger = trigger;
            else if (npcID == 10) kingTrigger = trigger;
            else if (npcID == 11) wizardTrigger = trigger;
        }

        Assert.IsNotNull(princeTrigger, "[TEST] NPC Prince (ID 3) not found!");
        Assert.IsNotNull(kingTrigger, "[TEST] NPC King (ID 10) not found!");
        Assert.IsNotNull(wizardTrigger, "[TEST] NPC Wizard (ID 11) not found!");
        Debug.Log("[TEST] All 3 NPCs (Prince, King, Wizard) located successfully!");

        // Chạy kiểm thử toàn bộ chuỗi nhiệm vụ 3 lần liên tiếp để đảm bảo không bị lỗi lưu trạng thái hoặc reset
        for (int run = 1; run <= 3; run++)
        {
            Debug.Log($"<color=cyan>[TEST] Starting Run #{run}/3...</color>");

            // Reset trạng thái nhiệm vụ và thông số nhân vật của Player
            ResetPlayerState(localQuest, localPlayer);
            yield return new WaitForSeconds(0.5f);

            // =========================================================================
            // BƯỚC 1: Nói chuyện với Prince để nhận nhiệm vụ giết skeleton (LEVEL UP!!!!)
            // =========================================================================
            Debug.Log("[TEST] Step 1: Interacting with Prince to get KIll ske quest...");
            yield return StartNPCConversation(princeTrigger);
            yield return SimulateDialogueChoice();

            // Kiểm tra: Player đã nhận quest 6 (KIll ske) chưa
            Assert.IsTrue(HasQuest(localQuest, 6), "[TEST] Failed to receive KIll ske quest!");
            Assert.IsFalse(IsQuestCompleted(localQuest, 6), "[TEST] Quest should not be completed yet.");

            // =========================================================================
            // BƯỚC 2: Tiêu diệt 2 Skeletons và quay lại Prince
            // =========================================================================
            Debug.Log("[TEST] Step 2: Simulating killing 2 Skeletons...");
            Refl.CallMethod(localQuest, "TangTienDoNhiemVu", 1, 10, 2); // LoaiNhiemVu.TieuDietQuai = 1, skeleton ID = 10
            Assert.IsTrue(IsQuestCompleted(localQuest, 6), "[TEST] KIll ske quest should be marked completed now.");

            // Nói chuyện với Prince để trả quest 6 và tự động nhận quest 8 (KILL ORC KING)
            Debug.Log("[TEST] Step 2: Interacting with Prince to submit and get bosskill quest...");
            yield return StartNPCConversation(princeTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 6), "[TEST] KIll ske quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 8), "[TEST] Failed to receive bosskill quest!");

            // =========================================================================
            // BƯỚC 3: Tiêu diệt Orc King và quay lại Prince
            // =========================================================================
            Debug.Log("[TEST] Step 3: Simulating killing Orc King...");
            Refl.CallMethod(localQuest, "TangTienDoNhiemVu", 1, 3, 1); // LoaiNhiemVu.TieuDietQuai = 1, Orc King ID = 3
            Assert.IsTrue(IsQuestCompleted(localQuest, 8), "[TEST] bosskill quest should be marked completed.");

            // Nói chuyện với Prince để trả quest 8 và tự động nhận quest 11 (FIND THE KING)
            Debug.Log("[TEST] Step 3: Interacting with Prince to submit and get find king quest...");
            yield return StartNPCConversation(princeTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 8), "[TEST] bosskill quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 11), "[TEST] Failed to receive find king quest!");

            // =========================================================================
            // BƯỚC 4: Đến lâu đài và nói chuyện với King
            // =========================================================================
            Debug.Log("[TEST] Step 4: Interacting with King to submit find king and get LV20 quest...");
            yield return StartNPCConversation(kingTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 11), "[TEST] find king quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 17), "[TEST] Failed to receive LV20 (UPDATE YOUR SELF) quest!");

            // =========================================================================
            // BƯỚC 5: Đạt cấp độ 10 và quay lại King
            // =========================================================================
            Debug.Log("[TEST] Step 5: Simulating leveling up to Level 10...");
            Refl.SetField(localPlayer, "level", 10);
            Refl.CallMethod(localQuest, "KiemTraTienDo");
            Assert.IsTrue(IsQuestCompleted(localQuest, 17), "[TEST] LV20 quest should be marked completed.");

            // Nói chuyện với King để trả quest 17 và nhận quest 12 (DEFEAT KNIGHT HERO)
            Debug.Log("[TEST] Step 5: Interacting with King to submit and get boss2 quest...");
            yield return StartNPCConversation(kingTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 17), "[TEST] LV20 quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 12), "[TEST] Failed to receive boss2 quest!");

            // =========================================================================
            // BƯỚC 6: Tiêu diệt Knight Hero và quay lại King
            // =========================================================================
            Debug.Log("[TEST] Step 6: Simulating killing Knight Hero...");
            Refl.CallMethod(localQuest, "TangTienDoNhiemVu", 1, 2, 1); // LoaiNhiemVu.TieuDietQuai = 1, Knight Hero ID = 2
            Assert.IsTrue(IsQuestCompleted(localQuest, 12), "[TEST] boss2 quest should be marked completed.");

            // Nói chuyện với King để trả quest 12 và nhận quest 13 (Find The Master)
            Debug.Log("[TEST] Step 6: Interacting with King to submit and get talk2 quest...");
            yield return StartNPCConversation(kingTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 12), "[TEST] boss2 quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 13), "[TEST] Failed to receive talk2 quest!");

            // =========================================================================
            // BƯỚC 7: Tìm đến gặp Wizard
            // =========================================================================
            Debug.Log("[TEST] Step 7: Interacting with Wizard to submit talk2 and get daoquang quest...");
            yield return StartNPCConversation(wizardTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 13), "[TEST] talk2 quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 16), "[TEST] Failed to receive daoquang (ores) quest!");

            // =========================================================================
            // BƯỚC 8: Thu thập 20 quặng và quay lại Wizard
            // =========================================================================
            Debug.Log("[TEST] Step 8: Simulating gathering 20 ores...");
            SetInventoryItem(localPlayer, 7, 20); // Ore ItemID = 7, qty = 20
            Refl.CallMethod(localQuest, "KiemTraTienDo");
            Assert.IsTrue(IsQuestCompleted(localQuest, 16), "[TEST] daoquang quest should be marked completed.");

            // Nói chuyện với Wizard để trả quest 16 và nhận quest 18 (Craft 10 Potions)
            Debug.Log("[TEST] Step 8: Interacting with Wizard to submit and get craft2 quest...");
            yield return StartNPCConversation(wizardTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 16), "[TEST] daoquang quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 18), "[TEST] Failed to receive craft2 quest!");

            // =========================================================================
            // BƯỚC 9: Chế tạo 10 bình máu và quay lại Wizard
            // =========================================================================
            Debug.Log("[TEST] Step 9: Simulating crafting 10 potions...");
            // Xóa quặng và đặt potion vào túi đồ để vượt qua bước CheckTienDo của GiaoVatPham nếu có,
            // Đồng thời tăng tiến độ của nhiệm vụ chế tạo (LoaiNhiemVu.CheTao = 6, Potion ID = 1001)
            SetInventoryItem(localPlayer, 0, 0); 
            Refl.CallMethod(localQuest, "TangTienDoNhiemVu", 6, 1001, 10);
            Assert.IsTrue(IsQuestCompleted(localQuest, 18), "[TEST] craft2 quest should be marked completed.");

            // Nói chuyện với Wizard để trả quest 18 và nhận quest 15 (THE END)
            Debug.Log("[TEST] Step 9: Interacting with Wizard to submit and get bossfinal quest...");
            yield return StartNPCConversation(wizardTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 18), "[TEST] craft2 quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 15), "[TEST] Failed to receive bossfinal quest!");

            // =========================================================================
            // BƯỚC 10: Tiêu diệt Boss Cuối (Final Boss) và quay lại Wizard
            // =========================================================================
            Debug.Log("[TEST] Step 10: Simulating killing Final Boss...");
            Refl.CallMethod(localQuest, "TangTienDoNhiemVu", 1, 1, 1); // LoaiNhiemVu.TieuDietQuai = 1, Final Boss ID = 1
            Assert.IsTrue(IsQuestCompleted(localQuest, 15), "[TEST] bossfinal quest should be marked completed.");

            // Nói chuyện với Wizard để trả quest 15
            Debug.Log("[TEST] Step 10: Interacting with Wizard to turn in final quest...");
            yield return StartNPCConversation(wizardTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 15), "[TEST] bossfinal quest was not turned in!");
            Debug.Log($"[TEST] Run #{run}/3 completed successfully!");
        }

        Debug.Log("[TEST] ALL 3 RUNS COMPLETED SUCCESSFULLY! Dialogue & Quests are fully functioning!");
    }

    // Helper: Reset toàn bộ trạng thái nhiệm vụ và level của player
    private void ResetPlayerState(object localQuest, object localPlayer)
    {
        // Xóa danh sách nhiệm vụ đang làm
        var activeList = (System.Collections.IList)Refl.GetField(localQuest, "danhSachNhiemVu");
        activeList.Clear();

        // Xóa danh sách nhiệm vụ đã hoàn thành
        var completedList = (System.Collections.IList)Refl.GetField(localQuest, "danhSachDaHoanThanh");
        completedList.Clear();

        // Đặt level của player về 1
        Refl.SetField(localPlayer, "level", 1);

        // Xóa sạch hòm đồ
        var tuiDo = Refl.GetField(localPlayer, "TuiDo");
        Type oVatPhamType = Refl.GetType("O_VatPham");
        var setMethod = tuiDo.GetType().GetMethod("Set", new Type[] { typeof(int), oVatPhamType });
        int length = (int)tuiDo.GetType().GetProperty("Length").GetValue(tuiDo);
        for (int i = 0; i < length; i++)
        {
            object emptyItem = Activator.CreateInstance(oVatPhamType);
            oVatPhamType.GetField("ItemID").SetValue(emptyItem, 0);
            oVatPhamType.GetField("SoLuong").SetValue(emptyItem, 0);
            oVatPhamType.GetField("UpgradeLevel").SetValue(emptyItem, 0);
            setMethod.Invoke(tuiDo, new object[] { i, emptyItem });
        }

        // Cập nhật lại UI bảng nhiệm vụ
        Refl.CallMethod(localQuest, "KiemTraTienDo");
    }

    // Helper: Thêm vật phẩm vào túi đồ
    private void SetInventoryItem(object localPlayer, int itemId, int qty)
    {
        var tuiDo = Refl.GetField(localPlayer, "TuiDo");
        Type oVatPhamType = Refl.GetType("O_VatPham");
        var setMethod = tuiDo.GetType().GetMethod("Set", new Type[] { typeof(int), oVatPhamType });
        
        object item = Activator.CreateInstance(oVatPhamType);
        oVatPhamType.GetField("ItemID").SetValue(item, itemId);
        oVatPhamType.GetField("SoLuong").SetValue(item, qty);
        oVatPhamType.GetField("UpgradeLevel").SetValue(item, 0);
        
        setMethod.Invoke(tuiDo, new object[] { 0, item });
    }

    // Helper: Kiểm tra player có đang giữ nhiệm vụ nào không
    private bool HasQuest(object localQuest, int questID)
    {
        var activeList = (System.Collections.IList)Refl.GetField(localQuest, "danhSachNhiemVu");
        foreach (var item in activeList)
        {
            var duLieuQuest = Refl.GetField(item, "duLieuQuest");
            int id = (int)Refl.GetField(duLieuQuest, "idNhiemVu");
            if (id == questID) return true;
        }
        return false;
    }

    // Helper: Kiểm tra xem nhiệm vụ cụ thể đã đạt yêu cầu chưa
    private bool IsQuestCompleted(object localQuest, int questID)
    {
        var activeList = (System.Collections.IList)Refl.GetField(localQuest, "danhSachNhiemVu");
        foreach (var item in activeList)
        {
            var duLieuQuest = Refl.GetField(item, "duLieuQuest");
            int id = (int)Refl.GetField(duLieuQuest, "idNhiemVu");
            if (id == questID)
            {
                return (bool)Refl.GetField(item, "daDatYeuCau");
            }
        }
        return false;
    }

    // Helper: Bắt đầu hội thoại với NPC
    private IEnumerator StartNPCConversation(object npcTrigger)
    {
        // Giả lập dangNoiChuyenVoiNPCNay = true
        Refl.SetField(npcTrigger, "dangNoiChuyenVoiNPCNay", true);

        // Lấy cuộc hội thoại
        var cuocHoiThoai = Refl.GetField(npcTrigger, "cuocHoiThoaiCuaNPC");
        var convManagerInstance = Refl.GetStaticField("DialogueEditor.ConversationManager", "Instance");

        // Gọi StartConversation
        Refl.CallMethod(convManagerInstance, "StartConversation", cuocHoiThoai);
        yield return new WaitForSeconds(0.2f);
    }

    // Helper: Giả lập lựa chọn và nhấn phím đi tiếp trong hội thoại cho đến khi kết thúc hội thoại
    private IEnumerator SimulateDialogueChoice()
    {
        var convManagerInstance = Refl.GetStaticField("DialogueEditor.ConversationManager", "Instance");
        Assert.IsNotNull(convManagerInstance, "ConversationManager Instance is null!");

        float timer = 0f;
        while ((bool)Refl.GetField(convManagerInstance, "IsConversationActive") && timer < 10f)
        {
            // Trạng thái m_state = 3 nghĩa là Idle (chờ người chơi tương tác)
            var state = Refl.GetField(convManagerInstance, "m_state");
            if ((int)state == 3)
            {
                var uiOptionsList = (System.Collections.IList)Refl.GetField(convManagerInstance, "m_uiOptions");
                if (uiOptionsList.Count > 0)
                {
                    // Tự động chọn option đầu tiên hợp lệ
                    Refl.CallMethod(convManagerInstance, "PressSelectedOption");
                }
                else
                {
                    // Nếu không có danh sách lựa chọn, tìm xem có button Speech (Continue) hay End trong Scene không và click nó
                    var uiButtons = GameObject.FindObjectsByType(Refl.GetType("DialogueEditor.UIConversationButton"), FindObjectsSortMode.None);
                    if (uiButtons.Length > 0)
                    {
                        // Gọi click
                        Refl.CallMethod(uiButtons[0], "OnButtonPressed");
                    }
                    else
                    {
                        // Fallback: kết thúc cuộc hội thoại trực tiếp
                        Refl.CallMethod(convManagerInstance, "EndConversation");
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        // Chờ thêm 1 chút để các event kết thúc hội thoại hoàn thành
        yield return new WaitForSeconds(0.3f);
    }
}
