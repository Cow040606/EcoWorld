using UnityEngine;
using UnityEngine.EventSystems; 

public class ItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public Item thongTinMonDo; 
    
    [HideInInspector] public int soLuongDangCo = 0; 

    // --- 1. THÊM BIẾN NÀY ĐỂ CẢ SERVER BIẾT CHUỘT ĐANG CHỈ VÀO AI ---
    public static int itemID_DangDiChuot = 0; 

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (thongTinMonDo != null && TooltipManager.instance != null)
        {
            TooltipManager.instance.HienThiTooltip(thongTinMonDo);
            
            // --- 2. GÁN ID KHI CHUỘT CHẠM VÀO ĐỒ ---
            // (Lu ít đoán biến ID trong file Item của Bò tên là itemID, nếu khác thì Bò sửa lại xíu nha)
            itemID_DangDiChuot = thongTinMonDo.itemID; 
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.instance != null)
        {
            TooltipManager.instance.AnTooltip();
        }

        // --- 3. XÓA TRÍ NHỚ KHI CHUỘT RỜI ĐI ---
        itemID_DangDiChuot = 0;
    }
    
    private void OnDisable()
    {
        if (TooltipManager.instance != null) TooltipManager.instance.AnTooltip();
        
        // Đề phòng trường hợp đang lia chuột mà Bò bấm B tắt Balo đột ngột
        itemID_DangDiChuot = 0;
    }
}
