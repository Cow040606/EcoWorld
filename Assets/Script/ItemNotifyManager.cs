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
        
        // 1. Tìm và gán tên vật phẩm (Vào cái object tên "Name")
        var textName = newNotify.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (textName != null) textName.text = itemName;

        // 2. Tìm và gán số lượng (Vào cái object tên "value")
        var textValue = newNotify.transform.Find("value")?.GetComponent<TextMeshProUGUI>();
        if (textValue != null) textValue.text = $"x{amount}";

        // 3. Tìm và gán hình ảnh (Sửa thành "Iconitem" cho khớp ảnh Bò chụp)
        var imageIcon = newNotify.transform.Find("Iconitem")?.GetComponent<Image>();
        if (imageIcon != null) imageIcon.sprite = icon;

        // TỰ XÓA
        Destroy(newNotify, thoiGianTonTai);

        // Ép UI cập nhật lại ngay lập tức
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
    }
}