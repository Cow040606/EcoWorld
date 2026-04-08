using UnityEngine;
using UnityEngine.UI;

public class UI_HotBar : MonoBehaviour
{
    public static UI_HotBar Instance;

    [Header("Kéo 4 cái Ô TỔNG (Thanh_Hotbar 1, 2, 3, 4) vào đây")]
    public GameObject[] danhSachO_Hotbar; // MỚI: Cái này để tắt/bật nguyên cả cái ô vuông

    [Header("Kéo 4 cái KHUNG SÁNG vào đây")]
    public GameObject[] danhSachKhungSang; 

    [Header("Kéo 4 cái ICON ITEM vào đây")]
    public Image[] danhSachIcon; 

    private void Awake()
    {
        Instance = this;
    }

    // Hàm này chạy mỗi khi Bò bấm phím 1 2 3 4
    public void HighlightSlot(int slotIndex)
    {
        // 1. Tắt sạch khung sáng trước cho chắc cú
        for (int i = 0; i < danhSachKhungSang.Length; i++)
        {
            if (danhSachKhungSang[i] != null) danhSachKhungSang[i].SetActive(false);
        }

        // 2. NẾU ĐANG RÚT VŨ KHÍ (Bấm 1, 2, 3, 4)
        if (slotIndex >= 0 && slotIndex < danhSachKhungSang.Length)
        {
            // Bật khung sáng cho cái ô đang chọn
            if (danhSachKhungSang[slotIndex] != null) danhSachKhungSang[slotIndex].SetActive(true);

            // // TÀNG HÌNH 3 Ô CÒN LẠI, CHỈ GIỮ LẠI Ô ĐANG CHỌN
            // if (danhSachO_Hotbar != null && danhSachO_Hotbar.Length > 0)
            // {
            //     for (int i = 0; i < danhSachO_Hotbar.Length; i++)
            //     {
            //         if (danhSachO_Hotbar[i] != null) 
            //         {
            //             // Nếu 'i' bằng với ô đang chọn thì = true (Hiện), khác thì = false (Tàng hình)
            //             danhSachO_Hotbar[i].SetActive(i == slotIndex);
            //         }
            //     }
            // }
        }
        // 3. NẾU BẤM CẤT VŨ KHÍ ĐI (Tay không: slotIndex = -1)
        else 
        {
            // HIỆN LẠI TOÀN BỘ 4 Ô để Bò còn nhìn thấy đường mà chọn vũ khí khác
            if (danhSachO_Hotbar != null && danhSachO_Hotbar.Length > 0)
            {
                for (int i = 0; i < danhSachO_Hotbar.Length; i++)
                {
                    if (danhSachO_Hotbar[i] != null) danhSachO_Hotbar[i].SetActive(true);
                }
            }
        }
    }

    // --- HÀM THAY ĐỔI HÌNH ẢNH MÓN ĐỒ (Giữ nguyên của Bò) ---
    public void CapNhatHinhAnhSlot(int slotIndex, Sprite hinhAnhItem)
    {
        if (slotIndex >= 0 && slotIndex < danhSachIcon.Length)
        {
            if (hinhAnhItem != null)
            {
                danhSachIcon[slotIndex].sprite = hinhAnhItem;
                danhSachIcon[slotIndex].color = Color.white; 
            }
            else
            {
                danhSachIcon[slotIndex].sprite = null;
                danhSachIcon[slotIndex].color = new Color(1f, 1f, 1f, 0f); 
            }
        }
    }
}