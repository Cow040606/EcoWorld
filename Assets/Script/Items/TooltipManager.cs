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

    public TextMeshProUGUI txtRarity;
    public Image imgIcon;
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

    public void HienThiTooltip(Item monDo) 
    {
        txtTen.text = monDo.itemName;
        txtGia.text = "Giá: " + monDo.value.ToString() + " Xu";
        txtMoTa.text = monDo.description;

        if (imgIcon != null && monDo.icon != null)
        {
            imgIcon.sprite = monDo.icon;
        }

        if (txtRarity != null)
        {
            txtRarity.text = "Độ hiếm: " + monDo.rarity.ToString();
            switch (monDo.rarity)
            {
                case Item.ItemRarity.Common: txtRarity.color = Color.white; break;
                case Item.ItemRarity.Uncommon: txtRarity.color = Color.green; break;
                case Item.ItemRarity.Rare: txtRarity.color = Color.blue; break;
                case Item.ItemRarity.Epic: txtRarity.color = new Color(0.6f, 0.2f, 0.8f); break; // Tím
                case Item.ItemRarity.Legendary: txtRarity.color = new Color(1f, 0.6f, 0f); break; // Cam
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