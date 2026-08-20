using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager instance;

    [Header("Thành phần UI Cơ bản")]
    public TextMeshProUGUI txtTen;
    public TextMeshProUGUI txtGia;
    public TextMeshProUGUI txtMoTa;
    public TextMeshProUGUI txtRarity;
    public Image imgIcon;

    [Header("Chữ hiển thị chỉ số (Kéo Label_Stat_Text vào)")]
    public TextMeshProUGUI txtMau;
    public TextMeshProUGUI txtStamina;
    public TextMeshProUGUI txtGiap;
    public TextMeshProUGUI txtSatThuong;

    [Header("Khối chứa chỉ số (Kéo HUD_Stat_Base vào để ẩn/hiện)")]
    public GameObject khoiMau;
    public GameObject khoiStamina;
    public GameObject khoiGiap;
    public GameObject khoiSatThuong;

    private RectTransform rectTransform;

    void Awake()
    {
        if (instance == null) instance = this;
        rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false); 
    }

    void Update()
    {
        if (gameObject.activeSelf)
        {
            DiChuyenTheoChuot();
        }
    }

    public void HienThiTooltip(Item monDo, int upgradeLevel = 0) 
    {
        float heSo = 1f + (0.1f * upgradeLevel);
        string chuoiLevel = upgradeLevel > 0 ? $" (+{upgradeLevel})" : "";
        
        if (txtTen != null) txtTen.text = monDo.itemName + chuoiLevel;
        if (txtGia != null) txtGia.text = "Giá: " + monDo.value.ToString() + " Xu";
        if (txtMoTa != null) txtMoTa.text = monDo.description;

        if (imgIcon != null && monDo.icon != null) imgIcon.sprite = monDo.icon;

        if (txtRarity != null)
        {
            txtRarity.text = "Độ hiếm: " + monDo.rarity.ToString();
            switch (monDo.rarity)
            {
                case Item.ItemRarity.Common: txtRarity.color = Color.white; break;
                case Item.ItemRarity.Uncommon: txtRarity.color = Color.green; break;
                case Item.ItemRarity.Rare: txtRarity.color = Color.blue; break;
                case Item.ItemRarity.Epic: txtRarity.color = new Color(0.6f, 0.2f, 0.8f); break; 
                case Item.ItemRarity.Legendary: txtRarity.color = new Color(1f, 0.6f, 0f); break; 
            }
        }

        // Xử lý Máu
        if (khoiMau != null) khoiMau.SetActive(monDo.congThemMau > 0);
        if (txtMau != null && monDo.congThemMau > 0) txtMau.text = $"{monDo.congThemMau * heSo}";

        // Xử lý Stamina
        if (khoiStamina != null) khoiStamina.SetActive(monDo.congThemStamina > 0);
        if (txtStamina != null && monDo.congThemStamina > 0) txtStamina.text = $"{monDo.congThemStamina * heSo}";

        // Xử lý Giáp
        if (khoiGiap != null) khoiGiap.SetActive(monDo.congThemGiap > 0);
        if (txtGiap != null && monDo.congThemGiap > 0) txtGiap.text = $"{monDo.congThemGiap * heSo}";

        // Xử lý Sát thương
        if (khoiSatThuong != null) khoiSatThuong.SetActive(monDo.congThemSatThuong > 0);
        if (txtSatThuong != null && monDo.congThemSatThuong > 0) txtSatThuong.text = $"{monDo.congThemSatThuong * heSo}";

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

        rectTransform.pivot = new Vector2(tamX, tamY);
        transform.position = viTriChuot;
    }
}
