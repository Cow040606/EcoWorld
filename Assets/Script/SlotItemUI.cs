using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SlotItemUI : MonoBehaviour 
{
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI itemCount;
    public Image itemIcon;
    
    // Hàm này giúp đổ dữ liệu từ Item vào cái Ô hiển thị trên màn hình
    public void SetData(Item thongTin, int soLuong, int level = 0)
    {
        if (itemName != null) 
        {
            if (level > 0) itemName.text = thongTin.itemName + " (+" + level + ")";
            else itemName.text = thongTin.itemName;
        }
        if (itemIcon != null) itemIcon.sprite = thongTin.icon;
        if (itemCount != null) itemCount.text = (soLuong > 1) ? "x" + soLuong : "";
    }
}
