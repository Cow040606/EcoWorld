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
        
        // 1. Tìm và gán tên vật phẩm theo đúng cấu trúc của Prefab Pickupinfo mới
        var textName = newNotify.transform.Find("Content/Info/Label_ItemName")?.GetComponent<TextMeshProUGUI>();
        
        // 2. Tìm và gán số lượng (Sử dụng cục Label_Action để hiển thị số lượng cộng thêm)
        var textValue = newNotify.transform.Find("Content/Info/Input_Action/Label_Action")?.GetComponent<TextMeshProUGUI>();
        
        if (textName != null) textName.text = itemName;
        if (textValue != null) textValue.text = "+" + amount;

        // 3. Tìm và gán hình ảnh Icon
        var imageIcon = newNotify.transform.Find("Icon/ICON")?.GetComponent<Image>();
        if (imageIcon != null) imageIcon.sprite = icon;

        // TỰ XÓA sau vài giây
        Destroy(newNotify, thoiGianTonTai);

        // Ép UI cập nhật lại ngay lập tức để xếp hàng cho đẹp
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
    }
}
