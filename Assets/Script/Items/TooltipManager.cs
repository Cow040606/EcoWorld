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
        txtTen.text = monDo.itemName;
        txtGia.text = "Giá: " + monDo.value.ToString() + " Xu";
        txtMoTa.text = monDo.description;

        // HIỆN SỐ LƯỢNG LÊN MÀN HÌNH
        if (txtSoLuong != null)
        {
            // Nếu có đồ thì hiện, còn nếu = 0 thì báo là Chưa có
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
        float tiLeX = viTriChuot.x / Screen.width;
        float tiLeY = viTriChuot.y / Screen.height;
        float tamX = tiLeX > 0.5f ? 1.05f : -0.05f;
        float tamY = tiLeY > 0.5f ? 1.05f : -0.05f;

        // Đảo tâm Pivot liên tục
        rectTransform.pivot = new Vector2(tamX, tamY);

        // Kéo bảng đi theo chuột
        transform.position = viTriChuot;
    }
}