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
                // Tra từ điển xem ID này là món gì
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

                    // =======================================================
                    // 6. GẮN MẮT THẦN (TOOLTIP) - Bơm cả Thông tin lẫn Số lượng
                    // =======================================================
                    ItemHover camBien = oMoi.GetComponent<ItemHover>();
                    if (camBien != null)
                    {
                        camBien.thongTinMonDo = thongTinMonDo;
                        
                        // Lấy đúng số lượng của cái ô thứ [i] truyền vào đây:
                        camBien.soLuongDangCo = tuiDoCuaPlayer[i].SoLuong; 
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

    private Player_Controller chuSoHuuBalo;

    public void BatTatBalo(NetworkArray<O_VatPham> tuiDoCuaPlayer, Player_Controller player)
    {
        chuSoHuuBalo = player; // Lưu lại để tí nữa biết ai bán đồ
        trangThaiBalo = !trangThaiBalo;

        // 🚨 MÁY PHÁT HIỆN NÓI DỐI
        if (khungBalo != null) 
        {
            khungBalo.SetActive(trangThaiBalo);
            //Debug.Log("<color=yellow>Đã ra lệnh SetActive thành: " + trangThaiBalo + " | Đối tượng bị bật/tắt là: [" + khungBalo.name + "]</color>");
        }
        else
        {
            //Debug.Log("<color=red>BÁO ĐỘNG ĐỎ: Ô khungBalo đang TRỐNG (Null). Chưa kéo giao diện vào!</color>");
        }

        if (trangThaiBalo)
        {
            VeBaloRaManHinh(tuiDoCuaPlayer);
        }
    }
}