using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveSlotItemUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI txtSessionName; // Tên phòng / thế giới
    public TextMeshProUGUI txtSaveTime;    // Ngày giờ lưu gần nhất
    public TextMeshProUGUI txtGameTime;    // Giờ trong game
    public Button btnSelect;               // Nút chọn ô Save
    public Button btnDelete;               // Nút xóa ô Save (nếu có)

    private string currentSessionName;
    private System.Action<string> onSelectCallback;
    private System.Action<string> onDeleteCallback;

    public void Setup(WorldSaveData saveData, System.Action<string> selectCallback, System.Action<string> deleteCallback = null)
    {
        if (saveData == null) return;

        currentSessionName = saveData.sessionName;
        onSelectCallback = selectCallback;
        onDeleteCallback = deleteCallback;

        if (txtSessionName != null) txtSessionName.text = saveData.sessionName;
        if (txtSaveTime != null) txtSaveTime.text = "Lưu lúc: " + saveData.lastSaveTime;

        if (txtGameTime != null)
        {
            int hours = Mathf.FloorToInt(saveData.gameTimeInHours) % 24;
            int minutes = Mathf.FloorToInt((saveData.gameTimeInHours - hours) * 60f) % 60;
            txtGameTime.text = $"Giờ game: {hours:D2}:{minutes:D2}";
        }

        if (btnSelect != null)
        {
            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(OnClickSelect);
        }

        if (btnDelete != null)
        {
            btnDelete.onClick.RemoveAllListeners();
            btnDelete.onClick.AddListener(OnClickDelete);
        }
    }

    private void OnClickSelect()
    {
        onSelectCallback?.Invoke(currentSessionName);
    }

    private void OnClickDelete()
    {
        onDeleteCallback?.Invoke(currentSessionName);
    }
}
