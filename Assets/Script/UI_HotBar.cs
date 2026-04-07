using UnityEngine;
using UnityEngine.UI;

public class UI_HotBar : MonoBehaviour
{
    public static UI_HotBar Instance;

    [Header("Kéo 9 cái KHUNG SÁNG (hoặc ô UI) vào đây")]
    public GameObject[] danhSachKhungSang; 

    private void Awake()
    {
        Instance = this;
    }

    // Hàm này sẽ được Player gọi mỗi khi đổi slot
    public void HighlightSlot(int slotIndex)
    {
        // Tắt hết toàn bộ khung sáng đi
        for (int i = 0; i < danhSachKhungSang.Length; i++)
        {
            if (danhSachKhungSang[i] != null)
            {
                danhSachKhungSang[i].SetActive(false);
            }
        }

        // Chỉ bật duy nhất cái khung sáng ở vị trí đang chọn
        if (slotIndex >= 0 && slotIndex < danhSachKhungSang.Length)
        {
            if (danhSachKhungSang[slotIndex] != null)
            {
                danhSachKhungSang[slotIndex].SetActive(true);
            }
        }
    }
}