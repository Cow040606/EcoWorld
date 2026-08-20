using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

#region Data Models
[Serializable]
public class ItemSaveData
{
    public int itemID;
    public int soLuong;
    public int upgradeLevel;
}

[Serializable]
public class QuestSaveData
{
    public int idNhiemVu;
    public int soLuongHienTai;
    public bool daDatYeuCau;
}

[Serializable]
public class PlayerSaveData
{
    public string playerName;
    public int gold;
    public int gem;
    public float health;
    public float stamina;
    public int level;
    public float expCurrent;
    public int availablePoints;
    public int diemSucManh;
    public int diemTheLuc;
    public int diemNhanhNhen;
    public int diemMau;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ;
    public List<ItemSaveData> tuiDo = new List<ItemSaveData>();
    public List<int> hotbarIDs = new List<int>();
    public List<QuestSaveData> quests = new List<QuestSaveData>();
}

[Serializable]
public class BackpackSaveData
{
    public float posX, posY, posZ;
    public List<ItemSaveData> items = new List<ItemSaveData>();
}

[Serializable]
public class WorldSaveData
{
    public string sessionName;
    public string lastSaveTime;
    public float gameTimeInHours;
    public List<PlayerSaveData> playersData = new List<PlayerSaveData>();
    public List<BackpackSaveData> droppedBackpacks = new List<BackpackSaveData>();
}
#endregion

public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SaveManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SaveManager");
                    _instance = go.AddComponent<SaveManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Đường Dẫn Save Tùy Chỉnh (Để rỗng = Mặc định AppData)")]
    [Tooltip("Để rỗng nếu muốn lưu trong AppData, hoặc điền đường dẫn tùy chỉnh (Ví dụ: D:/MySaves)")]
    public string customSavePath = "";

    public string SaveFolderPath
    {
        get
        {
            if (!string.IsNullOrEmpty(customSavePath))
            {
                return customSavePath;
            }
            // Mặc định lưu trực tiếp vào thư mục Saves nằm ngay trong thư mục game/Project
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../Saves"));
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureDirectoryExists();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(SaveFolderPath))
        {
            Directory.CreateDirectory(SaveFolderPath);
        }
    }

    private string GetSavePath(string sessionName)
    {
        string safeName = string.Join("_", sessionName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(SaveFolderPath, safeName + ".json");
    }

    public bool HasSaveFile(string sessionName)
    {
        if (string.IsNullOrEmpty(sessionName)) return false;
        return File.Exists(GetSavePath(sessionName));
    }

    // --- LƯU THẾ GIỚI & PLAYER ---
    public void SaveGame(string sessionName, Player_Controller localPlayer)
    {
        if (string.IsNullOrEmpty(sessionName) || localPlayer == null) return;

        EnsureDirectoryExists();
        string path = GetSavePath(sessionName);

        WorldSaveData saveData = new WorldSaveData();
        if (File.Exists(path))
        {
            try
            {
                string existingJson = File.ReadAllText(path);
                saveData = JsonUtility.FromJson<WorldSaveData>(existingJson) ?? new WorldSaveData();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SaveManager]: Error reading existing save file, creating new. " + ex.Message);
            }
        }

        saveData.sessionName = sessionName;
        saveData.lastSaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // 1. Lưu thời gian game
        if (TimeManager.Instance != null && TimeManager.Instance.Service != null)
        {
            saveData.gameTimeInHours = (float)TimeManager.Instance.Service.CurrentTime.TimeOfDay.TotalHours;
        }

        // 2. Thu thập dữ liệu người chơi
        PlayerSaveData pData = ExtractPlayerData(localPlayer);
        int pIndex = saveData.playersData.FindIndex(p => p.playerName == pData.playerName);
        if (pIndex >= 0)
        {
            saveData.playersData[pIndex] = pData;
        }
        else
        {
            saveData.playersData.Add(pData);
        }

        // 3. Ghi file JSON
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(path, json);
            Debug.Log($"<color=green>[SaveManager]:</color> Đã lưu game thành công vào {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError("[SaveManager]: Error writing save file: " + ex.Message);
        }
    }

    // --- TẢI THẾ GIỚI & PLAYER ---
    public WorldSaveData LoadGame(string sessionName, Player_Controller localPlayer)
    {
        if (!HasSaveFile(sessionName)) return null;

        string path = GetSavePath(sessionName);
        try
        {
            string json = File.ReadAllText(path);
            WorldSaveData saveData = JsonUtility.FromJson<WorldSaveData>(json);

            if (saveData != null)
            {
                // Khôi phục thời gian game
                if (TimeManager.Instance != null && TimeManager.Instance.Service != null && saveData.gameTimeInHours > 0)
                {
                    TimeManager.Instance.Service.SetTime((int)saveData.gameTimeInHours);
                }

                // Khôi phục người chơi
                if (localPlayer != null)
                {
                    string pName = PlayerPrefs.GetString("TenNhanVat", "VoDanh");
                    PlayerSaveData pData = saveData.playersData.Find(p => p.playerName == pName);
                    if (pData != null)
                    {
                        ApplyPlayerData(localPlayer, pData);
                    }
                }
            }

            return saveData;
        }
        catch (Exception ex)
        {
            Debug.LogError("[SaveManager]: Error loading save file: " + ex.Message);
            return null;
        }
    }

    // --- LẤY DANH SÁCH TẤT CẢ FILE SAVE NGOÀI MENU ---
    public List<WorldSaveData> GetAllSaves()
    {
        EnsureDirectoryExists();
        List<WorldSaveData> saveList = new List<WorldSaveData>();
        string[] files = Directory.GetFiles(SaveFolderPath, "*.json");

        foreach (string file in files)
        {
            try
            {
                string json = File.ReadAllText(file);
                WorldSaveData data = JsonUtility.FromJson<WorldSaveData>(json);
                if (data != null && !string.IsNullOrEmpty(data.sessionName))
                {
                    saveList.Add(data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SaveManager]: Skip corrupted save file: " + file + " Error: " + ex.Message);
            }
        }

        // Sắp xếp theo ngày lưu mới nhất
        saveList.Sort((a, b) => string.Compare(b.lastSaveTime, a.lastSaveTime, StringComparison.Ordinal));
        return saveList;
    }

    // --- XÓA FILE SAVE ---
    public void DeleteSave(string sessionName)
    {
        if (HasSaveFile(sessionName))
        {
            File.Delete(GetSavePath(sessionName));
        }
    }

    #region Helper Processing
    private PlayerSaveData ExtractPlayerData(Player_Controller p)
    {
        PlayerSaveData data = new PlayerSaveData();
        data.playerName = PlayerPrefs.GetString("TenNhanVat", "VoDanh");
        data.gold = p.Gold;
        data.gem = p.Gem;
        data.health = p.CurrentHealth;
        data.stamina = p.CurrentStamina;

        // Lưu Cấp độ & Điểm thuộc tính cộng thêm
        data.level = p.level;
        data.expCurrent = p.ExpCurrent;
        data.availablePoints = p.AvailablePoints;
        data.diemSucManh = p.DiemSucManh;
        data.diemTheLuc = p.DiemTheLuc;
        data.diemNhanhNhen = p.DiemNhanhNhen;
        data.diemMau = p.DiemMau;

        data.posX = p.transform.position.x;
        data.posY = p.transform.position.y;
        data.posZ = p.transform.position.z;

        data.rotX = p.transform.eulerAngles.x;
        data.rotY = p.transform.eulerAngles.y;
        data.rotZ = p.transform.eulerAngles.z;

        // Lưu túi đồ
        if (p.TuiDo.Length > 0)
        {
            for (int i = 0; i < p.TuiDo.Length; i++)
            {
                data.tuiDo.Add(new ItemSaveData { itemID = p.TuiDo[i].ItemID, soLuong = p.TuiDo[i].SoLuong, upgradeLevel = p.TuiDo[i].UpgradeLevel });
            }
        }

        // Lưu Hotbar
        if (p.HotbarIDs.Length > 0)
        {
            for (int i = 0; i < p.HotbarIDs.Length; i++)
            {
                data.hotbarIDs.Add(p.HotbarIDs[i]);
            }
        }

        // Lưu Nhiệm Vụ
        if (Player_QuestManager.localQuest != null)
        {
            data.quests = Player_QuestManager.localQuest.ExportQuestSaveData();
        }

        return data;
    }

    private void ApplyPlayerData(Player_Controller p, PlayerSaveData data)
    {
        if (p == null || data == null) return;

        p.Gold = data.gold;
        p.Gem = data.gem;
        if (data.health > 0) p.CurrentHealth = data.health;
        if (data.stamina > 0) p.CurrentStamina = data.stamina;

        // Khôi phục Cấp độ & Điểm thuộc tính cộng thêm
        p.level = data.level;
        p.ExpCurrent = data.expCurrent;
        p.AvailablePoints = data.availablePoints;
        p.DiemSucManh = data.diemSucManh;
        p.DiemTheLuc = data.diemTheLuc;
        p.DiemNhanhNhen = data.diemNhanhNhen;
        p.DiemMau = data.diemMau;

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.CapNhatLaiToanBoChiSo();
        }

        if (UI_StatsManager.instance != null)
        {
            UI_StatsManager.instance.CapNhatThongTinTrenBang();
        }

        // Khôi phục vị trí (nếu không phải vị trí chết 0,0,0)
        if (data.posX != 0 || data.posY != 0 || data.posZ != 0)
        {
            p.transform.position = new Vector3(data.posX, data.posY, data.posZ);
            p.transform.eulerAngles = new Vector3(data.rotX, data.rotY, data.rotZ);
        }

        // Khôi phục túi đồ
        if (data.tuiDo != null && p.TuiDo.Length > 0)
        {
            for (int i = 0; i < Mathf.Min(p.TuiDo.Length, data.tuiDo.Count); i++)
            {
                p.TuiDo.Set(i, new O_VatPham { ItemID = data.tuiDo[i].itemID, SoLuong = data.tuiDo[i].soLuong, UpgradeLevel = data.tuiDo[i].upgradeLevel });
            }
        }

        // Khôi phục Hotbar
        if (data.hotbarIDs != null && p.HotbarIDs.Length > 0)
        {
            for (int i = 0; i < Mathf.Min(p.HotbarIDs.Length, data.hotbarIDs.Count); i++)
            {
                p.HotbarIDs.Set(i, data.hotbarIDs[i]);
            }
        }

        // Khôi phục Nhiệm vụ
        if (Player_QuestManager.localQuest != null && data.quests != null)
        {
            Player_QuestManager.localQuest.ImportQuestSaveData(data.quests);
        }
    }
    #endregion
}
