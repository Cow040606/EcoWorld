using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    [Header("Thành phần UI")]
    public TextMeshProUGUI txtTen;
    public TextMeshProUGUI txtGia;
    public TextMeshProUGUI txtMoTa;

    public TextMeshProUGUI txtSoLuong;
    private RectTransform rectTransform;

    void Awake()
    {
        if (instance == null) instance = this;
        rectTransform = GetComponent<RectTransform>();
        
        // Tắt đi ngay khi vào game
        gameObject.SetActive(false); 
    }

    void Update()
    {
        // Nếu đang hiện thì bắt nó bay theo chuột
        if (gameObject.activeSelf)
        {
            DiChuyenTheoChuot();
        }
    }

    public void HienThiTooltip(Item monDo, int soLuong) 
    {
        // 1. KHIÊN BẢO VỆ: Nếu di chuột vào ô trống (không có đồ), tắt bảng và ngừng chạy code ngay!
        if (monDo == null) 
        {
            gameObject.SetActive(false);
            return;
        }

        // 2. MÁY PHÁT HIỆN NÓI DỐI: In ra Console xem có nhận được data thật không

        txtTen.text = monDo.itemName;
        txtGia.text = "Giá: " + monDo.value.ToString() + " Xu";
        txtMoTa.text = monDo.description;

        // HIỆN SỐ LƯỢNG LÊN MÀN HÌNH
        if (txtSoLuong != null)
        {
            if (soLuong > 0) 
            {
                txtSoLuong.text = "Đang sở hữu: " + soLuong.ToString();
            }
            else 
            {
                txtSoLuong.text = "Đang sở hữu: 0"; 
            }
        }

        gameObject.SetActive(true);
        DiChuyenTheoChuot(); 
    }

    public void AnTooltip()
    {
        gameObject.SetActive(false);
    }

    private void DiChuyenTheoChuot()
    {
        Vector2 viTriChuot = Input.mousePosition;
        
        // 1. Chia màn hình làm 4 góc để xác định vị trí chuột
        float nuaManHinhX = Screen.width / 2f;
        float nuaManHinhY = Screen.height / 2f;

        // Nếu chuột ở nửa Phải -> Tâm Pivot dời sang Phải (1). Nửa Trái -> dời sang Trái (0)
        float pivotX = viTriChuot.x > nuaManHinhX ? 1f : 0f;
        
        // Nếu chuột ở nửa Trên -> Tâm Pivot dời lên Trên (1). Nửa Dưới -> dời xuống Dưới (0)
        float pivotY = viTriChuot.y > nuaManHinhY ? 1f : 0f;

        rectTransform.pivot = new Vector2(pivotX, pivotY);

        // 2. Đẩy cái bảng ra xa con trỏ chuột 15 pixel để Bò không bị che mất Item đang nhìn
        float offsetX = pivotX == 1f ? -15f : 15f;
        float offsetY = pivotY == 1f ? -15f : 15f;

        // 3. Chốt tọa độ cuối cùng
        transform.position = new Vector2(viTriChuot.x + offsetX, viTriChuot.y + offsetY);
    }
}