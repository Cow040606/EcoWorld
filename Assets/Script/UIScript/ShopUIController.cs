using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic; // Bắt buộc để dùng List
using Fusion;

public class ShopUIController : MonoBehaviour
{
    public static ShopUIController instance;
    public GameObject khungGiaoDien; 
    public bool dangMoCraft = false;

    [Header("UI Shop")]
    public GameObject khungShop; 
    
    public bool isShopOpen = false;

    [Header("Cấu hình Ô UI")]
    public Transform itemHolder;  // Khung chứa các ô mặt hàng (Grid Layout Group)
    public GameObject shopItemPrefab; // Prefab của 1 ô (có Tên, Hình, Giá, và Nút Mua)

    [Header("Danh Sách Mặt Hàng (Kéo thả Item vào đây)")]
    // Dùng List để Bò dễ dàng Thêm/Bớt đồ đạc khi có Event
    public List<Item> danhSachMatHang = new List<Item>(); 

    private Player_Controller khachHangHienTai;
    public bool dangmoshop = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        if (khungShop != null) khungShop.SetActive(false);
        khungGiaoDien.SetActive(true);
        khungGiaoDien.SetActive(false);
    }
    // HÀM SIÊU TÌM KIẾM: Đảm bảo 1000% tìm ra Local Player
    private Player_Controller TimKhachHangLocal()
    {
        if (NetworkRunner.Instances.Count == 0) return null;
        var runner = NetworkRunner.Instances[0];
        
        // Thử tìm theo sổ hộ khẩu trước (Cách nhẹ nhàng)
        NetworkObject myPlayer = runner.GetPlayerObject(runner.LocalPlayer);
        if (myPlayer != null) 
        {
            return myPlayer.GetComponent<Player_Controller>();
        }

        // Nếu sổ hộ khẩu bị lỗi, dùng Radar quét toàn bộ màn hình! (Cách trâu bò)
        Player_Controller[] tatCaNhanVat = FindObjectsOfType<Player_Controller>();
        foreach (var p in tatCaNhanVat)
        {
            // Nếu đúng là nhân vật do máy Bò điều khiển (InputAuthority) thì lụm ngay!
            if (p.Object != null && p.HasInputAuthority) 
            {
                return p;
            }
        }

        return null; // Quét rách bản đồ không thấy thì mới chịu thua
    }

    // --- HÀM BẬT TẮT SHOP KHI NÓI CHUYỆN VỚI NPC ---
    public void BatTatShop(Player_Controller khachHang)
    {
        khachHangHienTai = khachHang; 
        isShopOpen = !isShopOpen;

        if (khungShop != null) khungShop.SetActive(isShopOpen);

        if (isShopOpen)
        {
            VeShopRaManHinh();
        }
    }

    // --- HÀM VẼ GIAN HÀNG RA MÀN HÌNH ---
    public void VeShopRaManHinh()
    {
        // 1. Dọn dẹp sạch sẽ gian hàng cũ
        foreach (Transform child in itemHolder) { Destroy(child.gameObject); }

        // 2. LẤY THÔNG TIN TÚI ĐỒ CỦA PLAYER HIỆN TẠI ĐỂ ĐẾM SỐ LƯỢNG
        Player_Controller khachHang = TimKhachHangLocal();

        // 3. Bày đồ mới ra bán
        foreach (Item matHang in danhSachMatHang)
        {
            if (matHang == null) continue;
            GameObject oMoi = Instantiate(shopItemPrefab, itemHolder);

            // TÌM CÁC THÀNH PHẦN UI TRONG PREFAB (Bò nhớ kiểm tra tên cho đúng nha)
            var itemName = oMoi.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            var itemPrice = oMoi.transform.Find("Price")?.GetComponent<TextMeshProUGUI>(); 
            var itemIMG = oMoi.transform.Find("ItemIcon")?.GetComponent<Image>();
            var nutMua = oMoi.transform.Find("BuyButton")?.GetComponent<Button>();
            var nutBan = oMoi.transform.Find("SellButton")?.GetComponent<Button>();

            // --- ĐOẠN TÍNH TOÁN SỐ LƯỢNG ĐANG CÓ ---
            int soLuongThucTe = 0;
            if (khachHang != null)
            {
                // Quét trong túi đồ của Player xem có món này không
                for (int i = 0; i < khachHang.TuiDo.Length; i++)
                {
                    if (khachHang.TuiDo[i].ItemID == matHang.itemID)
                    {
                        soLuongThucTe += khachHang.TuiDo[i].SoLuong;
                    }
                }
            }

            // --- BƠM DỮ LIỆU CHO TOOLTIP ---
            ItemHover camBien = oMoi.GetComponent<ItemHover>();
            if (camBien != null)
            {
                camBien.thongTinMonDo = matHang;
                camBien.soLuongDangCo = soLuongThucTe; 
            }

            // Gán dữ liệu lên hình ảnh và chữ
            if (itemName != null) itemName.text = matHang.itemName;
            if (itemIMG != null) itemIMG.sprite = matHang.icon;
            if (itemPrice != null) itemPrice.text = matHang.value.ToString() + " Xu"; 

            // Gắn chức năng cho Nút Mua
            if (nutMua != null)
            {
                int idMua = matHang.itemID;
                int giaMua = matHang.value;
                string tenMon = matHang.itemName;

                nutMua.onClick.AddListener(() => {
                    if (khachHang != null)
                    {
                        khachHang.RPC_MuaVatPham(idMua, giaMua);
                        UnityEngine.Debug.Log($"Đã gửi yêu cầu mua hàng: {tenMon} - Giá: {giaMua}");
                        Invoke("VeShopRaManHinh", 0.1f);
                    }
                });
            }
            if(nutBan != null)
            {
                int idMua = matHang.itemID;
                int giaban = matHang.value /2;
                string tenMon = matHang.itemName;

                nutBan.onClick.AddListener(() => {
                    if (khachHang != null)
                    {
                        khachHang.RPC_BanVatPham(idMua, giaban);
                        UnityEngine.Debug.Log($"Đã gửi yêu cầu Bán hàng: {tenMon} - Giá: {giaban}");
                        Invoke("VeShopRaManHinh", 0.1f);
                    }
                });
            }
        }
    }

    // =========================================================
    // HÀM CHUẨN BỊ CHO EVENT (Giáng sinh, Lễ Tết...)
    // =========================================================
    public void ThayDoiShopTheoEvent(List<Item> danhSachMoi)
    {
        danhSachMatHang = danhSachMoi;
        if (isShopOpen) 
        {
            VeShopRaManHinh();
        }
    }
    public void OpenShop() 
    { 
        if (NetworkRunner.Instances.Count > 0)
        {
            var runner = NetworkRunner.Instances[0];
            NetworkObject myPlayer = runner.GetPlayerObject(runner.LocalPlayer);
            if (myPlayer != null)
            {
                khachHangHienTai = myPlayer.GetComponent<Player_Controller>();
            }
        }

        isShopOpen = true; 
        khungShop.SetActive(true); 
        VeShopRaManHinh(); 
    }

    public void CloseShop() 
    { 
        isShopOpen = false; 
        khungShop.SetActive(false); 
    }
    public void BatTatCraft()
    {
        dangMoCraft = !dangMoCraft;
        if (khungGiaoDien != null)
        {
            khungGiaoDien.SetActive(dangMoCraft);
        }
    }
}