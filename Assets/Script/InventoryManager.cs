using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion; // Phải có thư viện mạng để đọc NetworkArray

public class InventoryManager : MonoBehaviour 
{
    public static InventoryManager instance;

    [Header("UI Balo")]
    public GameObject khungBalo; 
    public bool trangThaiBalo = false; 

    [Header("Cấu hình Ô UI")]
    public Transform itemHolder;  // Khung chứa các ô (Grid Layout Group)
    public GameObject itemPrefab; // Prefab của 1 ô (có hình, chữ...)

    [Header("Từ Điển Vật Phẩm")]
    public Item[] khoDuLieu; 

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (khungBalo != null) khungBalo.SetActive(false);
    }

    // --- SỬA HÀM NÀY: Giờ nó sẽ đòi nhận cái mảng TuiDo của Player ---
    public void BatTatBalo(NetworkArray<O_VatPham> tuiDoCuaPlayer)
    {
        trangThaiBalo = !trangThaiBalo; 
        
        if (khungBalo != null) khungBalo.SetActive(trangThaiBalo);

        // Nếu balo đang MỞ thì vẽ đồ ra màn hình
        if (trangThaiBalo == true)
        {
            VeBaloRaManHinh(tuiDoCuaPlayer);
        }
    }

    // --- HÀM VẼ UI SIÊU CẤP ---
    public void VeBaloRaManHinh(NetworkArray<O_VatPham> tuiDoCuaPlayer)
    {
        // 1. Xóa sạch sẽ các ô cũ đang có trên màn hình
        foreach (Transform child in itemHolder) { 
            Destroy(child.gameObject); 
        }

        // 2. Quét qua 20 ngăn trong cái túi mạng của Player
        for (int i = 0; i < tuiDoCuaPlayer.Length; i++)
        {
            if (tuiDoCuaPlayer[i].ItemID != 0) // Á chà, ngăn này có đồ!
            {
                // Tra từ điển xem ID này là món gì (Gỗ hay Đá?)
                Item thongTinMonDo = TraCuuItem(tuiDoCuaPlayer[i].ItemID);
                
                if (thongTinMonDo != null)
                {
                    // 3. Đẻ ra 1 cái ô UI mới và nhét nó vào khung itemHolder
                    GameObject oMoi = Instantiate(itemPrefab, itemHolder);

                    // 4. Tìm mấy cái Text và Image trong cái ô đó để thay đổi
                    TextMeshProUGUI itemName = oMoi.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI itemCountText = oMoi.transform.Find("stack").GetComponent<TextMeshProUGUI>();
                    Image itemIMG = oMoi.transform.Find("ItemIcon").GetComponent<Image>();

                    // 5. Đắp dữ liệu từ Từ điển và Mạng lên UI
                    itemName.text = thongTinMonDo.itemName;
                    itemIMG.sprite = thongTinMonDo.icon;
                    
                    if (tuiDoCuaPlayer[i].SoLuong > 1) {
                        itemCountText.text = "x" + tuiDoCuaPlayer[i].SoLuong.ToString();
                    } else {
                        itemCountText.text = ""; // 1 cục thì ẩn số đi cho đẹp
                    }
                }
            }
        }
    }

    // Hàm tra từ điển
    public Item TraCuuItem(int idCanTim)
    {
        foreach (Item monDo in khoDuLieu)
        {
            if (monDo.itemID == idCanTim) return monDo;
        }
        return null; 
    }
}