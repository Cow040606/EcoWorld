using UnityEngine;
using UnityEngine.UI;

public class FixSliderZero : MonoBehaviour
{
    private Slider mySlider;
    
    [Header("Kéo đối tượng Fill vào đây")]
    public Image fillImage; 

    void Awake()
    {
        mySlider = GetComponent<Slider>();
    }

    void Start()
    {
        // Gắn tai nghe: Mỗi khi thanh trượt thay đổi giá trị là gọi hàm KiemTra
        mySlider.onValueChanged.AddListener(KiemTraGiaTri);
        
        // Chạy ép kiểm tra 1 lần lúc mới vào game
        KiemTraGiaTri(mySlider.value); 
    }

    void KiemTraGiaTri(float giatri)
    {
        // NẾU GIÁ TRỊ VỀ 0 -> Tàng hình luôn cái Fill
        if (giatri <= mySlider.minValue) // Dùng minValue cho an toàn lỡ Bò set min < 0
        {
            fillImage.enabled = false;
        }
        else
        {
            fillImage.enabled = true; // Lớn hơn mức Min thì hiện lại
        }
    }
}