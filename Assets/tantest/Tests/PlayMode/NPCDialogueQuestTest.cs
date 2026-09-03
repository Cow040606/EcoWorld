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
    // L?p helper Reflection d? g?i code c?a game chính không c?n tham chi?u tr?c ti?p
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
        // 1. T?i scene map1
        // Debug.Log("[TEST] Loading map1 scene...");
        SceneManager.LoadScene("Scenes/map1");
        yield return new WaitForSeconds(2.0f); // Ch? load map

        // 2. Ch? Player và các NPC xu?t hi?n trong Scene
        GameObject player = null;
        float timeOut = 12f;
        while (player == null && timeOut > 0)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null) yield return new WaitForSeconds(0.5f);
            timeOut -= 0.5f;
        }
        Assert.IsNotNull(player, "[TEST] Player was not found after timeout!");
        // Debug.Log("[TEST] Player found successfully!");

        // L?y các singleton instances c?a game chính
        var localQuest = Refl.GetStaticField("Player_QuestManager", "localQuest");
        var localPlayer = Refl.GetStaticField("Player_Controller", "localPlayer");
        Assert.IsNotNull(localQuest, "[TEST] Player_QuestManager.localQuest is null!");
        Assert.IsNotNull(localPlayer, "[TEST] Player_Controller.localPlayer is null!");

        // T́m 3 NPC trong scene d?a trên npcID c?a h?
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
        // Debug.Log("[TEST] All 3 NPCs (Prince, King, Wizard) located successfully!");

        // Ch?y ki?m th? toàn b? chu?i nhi?m v? 3 l?n liên ti?p d? d?m b?o không b? l?i luu tr?ng thái ho?c reset
        for (int run = 1; run <= 3; run++)
        {
            // Debug.Log($"<color=cyan>[TEST] Starting Run #{run}/3...</color>");

            // Reset tr?ng thái nhi?m v? và thông s? nhân v?t c?a Player
            ResetPlayerState(localQuest, localPlayer);
            yield return new WaitForSeconds(0.5f);

            // =========================================================================
            // BUỚC 8: Thu thập 20 quặng và quay lại Wizard (Bỏ qua chế thuốc, đi thẳng đến Boss Final)
            // =========================================================================
            // Debug.Log("[TEST] Step 8: Simulating gathering 20 ores...");
            SetInventoryItem(localPlayer, 7, 20); // Ore ItemID = 7, qty = 20
            Refl.CallMethod(localQuest, "KiemTraTienDo");
            Assert.IsTrue(IsQuestCompleted(localQuest, 16), "[TEST] daoquang quest should be marked completed.");

            // Nói chuyện với Wizard để trả quest 16 và nhận trực tiếp quest 15 (THE END / Boss Final)
            // Debug.Log("[TEST] Step 8: Interacting with Wizard to submit daoquang and get bossfinal quest directly...");
            yield return StartNPCConversation(wizardTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 16), "[TEST] daoquang quest was not turned in!");
            Assert.IsTrue(HasQuest(localQuest, 15), "[TEST] Failed to receive bossfinal quest directly!");

            // =========================================================================
            // BUỚC 9: Tiêu diệt Boss Cuối (Final Boss) và quay lại Wizard
            // =========================================================================
            // Debug.Log("[TEST] Step 9: Simulating killing Final Boss...");
            SetInventoryItem(localPlayer, 0, 0); 
            Refl.CallMethod(localQuest, "TangTienDoNhiemVu", 1, 1, 1); // LoaiNhiemVu.TieuDietQuai = 1, Final Boss ID = 1
            Assert.IsTrue(IsQuestCompleted(localQuest, 15), "[TEST] bossfinal quest should be marked completed.");

            // Nói chuyện với Wizard để trả quest 15
            // Debug.Log("[TEST] Step 9: Interacting with Wizard to turn in final quest...");
            yield return StartNPCConversation(wizardTrigger);
            yield return SimulateDialogueChoice();

            Assert.IsFalse(HasQuest(localQuest, 15), "[TEST] bossfinal quest was not turned in!");
            // Debug.Log($"[TEST] Run #{run}/3 completed successfully!");
        }

        // Debug.Log("[TEST] ALL 3 RUNS COMPLETED SUCCESSFULLY! Dialogue & Quests are fully functioning!");
    }

    // Helper: Reset toàn b? tr?ng thái nhi?m v? và level c?a player
    private void ResetPlayerState(object localQuest, object localPlayer)
    {
        // Xóa danh sách nhi?m v? dang làm
        var activeList = (System.Collections.IList)Refl.GetField(localQuest, "danhSachNhiemVu");
        activeList.Clear();

        // Xóa danh sách nhi?m v? dă hoàn thành
        var completedList = (System.Collections.IList)Refl.GetField(localQuest, "danhSachDaHoanThanh");
        completedList.Clear();

        // Đ?t level c?a player v? 1
        Refl.SetField(localPlayer, "level", 1);

        // Xóa s?ch ḥm d?
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

        // C?p nh?t l?i UI b?ng nhi?m v?
        Refl.CallMethod(localQuest, "KiemTraTienDo");
    }

    // Helper: Thêm v?t ph?m vào túi d?
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

    // Helper: Ki?m tra player có dang gi? nhi?m v? nào không
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

    // Helper: Ki?m tra xem nhi?m v? c? th? dă d?t yêu c?u chua
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

    // Helper: B?t d?u h?i tho?i v?i NPC
    private IEnumerator StartNPCConversation(object npcTrigger)
    {
        // Gi? l?p dangNoiChuyenVoiNPCNay = true
        Refl.SetField(npcTrigger, "dangNoiChuyenVoiNPCNay", true);

        // L?y cu?c h?i tho?i
        var cuocHoiThoai = Refl.GetField(npcTrigger, "cuocHoiThoaiCuaNPC");
        var convManagerInstance = Refl.GetStaticField("DialogueEditor.ConversationManager", "Instance");

        // G?i StartConversation
        Refl.CallMethod(convManagerInstance, "StartConversation", cuocHoiThoai);
        yield return new WaitForSeconds(0.2f);
    }

    // Helper: Gi? l?p l?a ch?n và nh?n phím di ti?p trong h?i tho?i cho d?n khi k?t thúc h?i tho?i
    private IEnumerator SimulateDialogueChoice()
    {
        var convManagerInstance = Refl.GetStaticField("DialogueEditor.ConversationManager", "Instance");
        Assert.IsNotNull(convManagerInstance, "ConversationManager Instance is null!");

        float timer = 0f;
        while ((bool)Refl.GetField(convManagerInstance, "IsConversationActive") && timer < 10f)
        {
            // Tr?ng thái m_state = 3 nghia là Idle (ch? ngu?i choi tuong tác)
            var state = Refl.GetField(convManagerInstance, "m_state");
            if ((int)state == 3)
            {
                var uiOptionsList = (System.Collections.IList)Refl.GetField(convManagerInstance, "m_uiOptions");
                if (uiOptionsList.Count > 0)
                {
                    // T? d?ng ch?n option d?u tiên h?p l?
                    Refl.CallMethod(convManagerInstance, "PressSelectedOption");
                }
                else
                {
                    // N?u không có danh sách l?a ch?n, t́m xem có button Speech (Continue) hay End trong Scene không và click nó
                    var uiButtons = GameObject.FindObjectsByType(Refl.GetType("DialogueEditor.UIConversationButton"), FindObjectsSortMode.None);
                    if (uiButtons.Length > 0)
                    {
                        // G?i click
                        Refl.CallMethod(uiButtons[0], "OnButtonPressed");
                    }
                    else
                    {
                        // Fallback: k?t thúc cu?c h?i tho?i tr?c ti?p
                        Refl.CallMethod(convManagerInstance, "EndConversation");
                    }
                }
            }
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;
        }

        // Ch? thêm 1 chút d? các event k?t thúc h?i tho?i hoàn thành
        yield return new WaitForSeconds(0.3f);
    }
}
