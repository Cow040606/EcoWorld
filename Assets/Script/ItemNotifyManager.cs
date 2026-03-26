using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemNotifyManager : MonoBehaviour
{
    public static ItemNotifyManager Instance;
    
    [Header("Cấu hình UI")]
    public GameObject notifyPrefab; 
    public Transform container;     
    public float thoiGianTonTai = 3f; // Thời gian dòng thông báo tồn tại

    void Awake() 
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowNotify(string itemName, int amount, Sprite icon)
    {
        // Tạo mới dòng thông báo
        GameObject newNotify = Instantiate(notifyPrefab, container);
        
        // Gán dữ liệu (Tìm đúng component để tránh lỗi)
        var textMesh = newNotify.GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null) textMesh.text = $"{itemName} x{amount}";

        var imageIcon = newNotify.transform.Find("Icon")?.GetComponent<Image>();
        if (imageIcon != null) imageIcon.sprite = icon;

        // TỰ XÓA: Lệnh này cực kỳ quan trọng để dọn dẹp bộ nhớ và UI
        Destroy(newNotify, thoiGianTonTai);

        // MẸO: Sau khi Instantiate, ép UI cập nhật lại ngay lập tức để không bị giật
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
    }
}