using UnityEngine;
using UnityEngine.EventSystems; 

public class ItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Item thongTinMonDo; 
    
    // THÊM BIẾN NÀY ĐỂ NHẬN SỐ LƯỢNG TỪ BALO TRUYỀN VÀO
    [HideInInspector] public int soLuongDangCo = 0; 

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (thongTinMonDo != null && TooltipManager.instance != null)
        {
            // Truyền thêm con số lượng qua cho Tổng đài Tooltip
            TooltipManager.instance.HienThiTooltip(thongTinMonDo, soLuongDangCo);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.AnTooltip();
        }
    }
    
    private void OnDisable()
    {
        if (TooltipManager.instance != null) TooltipManager.instance.AnTooltip();
    }
}