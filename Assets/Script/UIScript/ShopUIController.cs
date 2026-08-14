using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic; // Bắt buộc để dùng List
using Fusion;
using UnityEngine.EventSystems;

public class ShopUIController : MonoBehaviour
{
    public static ShopUIController instance;
    public GameObject khungGiaoDien; 
    public bool dangMoCraft = false;

    [Header("UI Shop")]
    public GameObject khungShop; 
    
    [Header("Các UI cần ẩn khi mở Shop")]
    public List<GameObject> uiCanAnKhiMoShop = new List<GameObject>();

    public bool isShopOpen = false;

    [Header("Cấu hình Ô UI")]
    public Transform itemHolder;  // Khung chứa các ô mặt hàng (Grid Layout Group)
    public GameObject shopItemPrefab; // Prefab của 1 ô (có Tên, Hình, Giá, và Nút Mua)

    [Header("Danh Sách Mặt Hàng (Kéo thả Item vào đây)")]
    // Dùng List để Bò dễ dàng Thêm/Bớt đồ đạc khi có Event
    public List<Item> danhSachMatHang = new List<Item>(); 

    [Header("UI Bảng Chi Tiết")]
    public TextMeshProUGUI txtTenChiTiet;
    public Image imgIconChiTiet;
    public TextMeshProUGUI txtGiaChiTiet;
    public TextMeshProUGUI txtDoHiemChiTiet;
    public TextMeshProUGUI txtMoTaChiTiet;
    
    [Header("UI Tổng Tiền Mua/Bán")]
    public TextMeshProUGUI txtTongTienMua;
    public TextMeshProUGUI txtTongTienBan;

    [Header("UI Trạng Thái")]
    public TextMeshProUGUI txtStatus;

    [Header("Hệ Thống Số Lượng")]
    public TMP_InputField txtSoLuongMuaBan; // Đổi thành TMP_InputField để có thể gõ chữ
    public Button btnCong;
    public Button btnTru;
    public Button btnMax;
    public Button btnMin;
    public Button btnBuy;
    public Button btnSell;

    private Item selectedItem;
    private int currentQuantity = 1;
    private int playerCurrentQuantity = 0;

    private Player_Controller khachHangHienTai;

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

        foreach (GameObject ui in uiCanAnKhiMoShop)
        {
            if (ui != null) ui.SetActive(!isShopOpen);
        }

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

            // --- XÓA ITEM HOVER ĐỂ KHÔNG HIỆN TOOLTIP ---
            ItemHover camBien = oMoi.GetComponent<ItemHover>();
            if (camBien != null)
            {
                Destroy(camBien);
            }

            // Gán dữ liệu lên hình ảnh và chữ
            if (itemName != null) itemName.text = matHang.itemName;
            if (itemIMG != null) itemIMG.sprite = matHang.icon;
            if (itemPrice != null) itemPrice.text = matHang.value.ToString() + " Coin"; 

            // BIẾN Ô ĐỒ THÀNH NÚT CHỌN
            Button btnChon = oMoi.GetComponent<Button>();
            if (btnChon == null) btnChon = oMoi.AddComponent<Button>();

            if (btnChon != null)
            {
                Item captureItem = matHang;
                int captureSoLuong = soLuongThucTe;
                btnChon.onClick.AddListener(() => {
                    ChonMatHang(captureItem, captureSoLuong);
                });
            }

            // HIỆU ỨNG PHÓNG TO KHI LIA CHUỘT
            EventTrigger trigger = oMoi.GetComponent<EventTrigger>();
            if (trigger == null) trigger = oMoi.AddComponent<EventTrigger>();
            trigger.triggers.Clear(); // Xoá sạch phòng khi bị lặp

            Vector3 originalScale = Vector3.one;

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { oMoi.transform.localScale = originalScale * 1.1f; });
            trigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { oMoi.transform.localScale = originalScale; });
            trigger.triggers.Add(entryExit);

            // Ẩn 2 nút cũ trên prefab đi
            if (nutMua != null) nutMua.gameObject.SetActive(false);
            if (nutBan != null) nutBan.gameObject.SetActive(false);
        }
        
        CapNhatSuKienCacNut();
    }

    private void CapNhatSuKienCacNut()
    {
        if (txtSoLuongMuaBan != null)
        {
            txtSoLuongMuaBan.onValueChanged.RemoveAllListeners();
            txtSoLuongMuaBan.onValueChanged.AddListener(OnInputSoLuongThayDoi);
            txtSoLuongMuaBan.onEndEdit.RemoveAllListeners();
            txtSoLuongMuaBan.onEndEdit.AddListener(OnInputKetThucGo);
        }

        if (btnCong != null) { btnCong.onClick.RemoveAllListeners(); btnCong.onClick.AddListener(() => ThayDoiSoLuong(1)); }
        if (btnTru != null) { btnTru.onClick.RemoveAllListeners(); btnTru.onClick.AddListener(() => ThayDoiSoLuong(-1)); }
        if (btnMax != null) { btnMax.onClick.RemoveAllListeners(); btnMax.onClick.AddListener(() => ThayDoiSoLuong(999)); }
        if (btnMin != null) { btnMin.onClick.RemoveAllListeners(); btnMin.onClick.AddListener(() => ThayDoiSoLuong(-999)); }
        
        if (btnBuy != null) { btnBuy.onClick.RemoveAllListeners(); btnBuy.onClick.AddListener(MuaVatPhamHienTai); }
        if (btnSell != null) { btnSell.onClick.RemoveAllListeners(); btnSell.onClick.AddListener(BanVatPhamHienTai); }
    }

    public void ChonMatHang(Item item, int soLuongDangCo)
    {
        selectedItem = item;
        playerCurrentQuantity = soLuongDangCo;
        currentQuantity = 1;

        if (txtTenChiTiet != null) txtTenChiTiet.text = item.itemName;
        if (imgIconChiTiet != null) imgIconChiTiet.sprite = item.icon;
        if (txtGiaChiTiet != null) txtGiaChiTiet.text = item.value.ToString();
        if (txtMoTaChiTiet != null) txtMoTaChiTiet.text = item.description;

        if (txtStatus != null) txtStatus.text = "";

        if (txtDoHiemChiTiet != null)
        {
            txtDoHiemChiTiet.text = "Rarity: " + item.rarity.ToString();
            switch (item.rarity)
            {
                case Item.ItemRarity.Common: txtDoHiemChiTiet.color = Color.white; break;
                case Item.ItemRarity.Uncommon: txtDoHiemChiTiet.color = Color.green; break;
                case Item.ItemRarity.Rare: txtDoHiemChiTiet.color = Color.blue; break;
                case Item.ItemRarity.Epic: txtDoHiemChiTiet.color = new Color(0.6f, 0.2f, 0.8f); break;
                case Item.ItemRarity.Legendary: txtDoHiemChiTiet.color = new Color(1f, 0.6f, 0f); break;
            }
        }

        HienThiSoLuong();
    }

    private void ThayDoiSoLuong(int thayDoi)
    {
        if (selectedItem == null) return;
        
        if (thayDoi == 999) currentQuantity = 99;
        else if (thayDoi == -999) currentQuantity = 1;
        else currentQuantity += thayDoi;

        if (currentQuantity < 1) currentQuantity = 1;
        if (currentQuantity > 99) currentQuantity = 99;

        HienThiSoLuong();
    }

    private void OnInputSoLuongThayDoi(string noiDung)
    {
        if (string.IsNullOrEmpty(noiDung)) return; // Cho phép xoá trống khi đang gõ
        if (int.TryParse(noiDung, out int giaTri))
        {
            if (giaTri < 1) giaTri = 1;
            if (giaTri > 99) giaTri = 99;
            currentQuantity = giaTri;
        }
    }

    private void OnInputKetThucGo(string noiDung)
    {
        if (string.IsNullOrEmpty(noiDung) || !int.TryParse(noiDung, out int giaTri))
        {
            currentQuantity = 1;
        }
        else
        {
            currentQuantity = Mathf.Clamp(giaTri, 1, 99);
        }
        HienThiSoLuong(); // Cập nhật lại giao diện khi gõ xong
    }

    private void HienThiSoLuong()
    {
        if (txtSoLuongMuaBan != null)
        {
            txtSoLuongMuaBan.text = currentQuantity.ToString();
        }

        if (selectedItem != null)
        {
            int tongTienMua = selectedItem.value * currentQuantity;
            int tongTienBan = (selectedItem.value / 2) * currentQuantity;

            if (txtTongTienMua != null)
            {
                txtTongTienMua.text = "-" + tongTienMua + " Xu";
                txtTongTienMua.color = Color.red;
            }
            if (txtTongTienBan != null)
            {
                txtTongTienBan.text = "+" + tongTienBan + " Xu";
                txtTongTienBan.color = Color.green;
            }
        }
    }

    private void MuaVatPhamHienTai()
    {
        if (selectedItem == null) 
        {
            if (txtStatus != null) { txtStatus.text = "Invalid item!"; txtStatus.color = Color.red; }
            return;
        }

        Player_Controller khachHang = TimKhachHangLocal();
        if (khachHang != null)
        {
            int tongGiaHienTai = selectedItem.value * currentQuantity;
            if (khachHang.Gold < tongGiaHienTai)
            {
                if (txtStatus != null) { txtStatus.text = "Not enough money!"; txtStatus.color = Color.red; }
                return;
            }

            khachHang.RPC_MuaVatPham(selectedItem.itemID, selectedItem.value, currentQuantity);
            if (txtStatus != null) { txtStatus.text = "Purchase successful!"; txtStatus.color = Color.green; }
            
            Invoke("VeShopRaManHinh", 0.1f);
        }
    }

    private void BanVatPhamHienTai()
    {
        if (selectedItem == null) 
        {
            if (txtStatus != null) { txtStatus.text = "Invalid item!"; txtStatus.color = Color.red; }
            return;
        }

        Player_Controller khachHang = TimKhachHangLocal();
        if (khachHang != null)
        {
            int soLuongCoTheBan = 0;
            for (int i = 0; i < khachHang.TuiDo.Length; i++)
            {
                if (khachHang.TuiDo[i].ItemID == selectedItem.itemID)
                {
                    soLuongCoTheBan += khachHang.TuiDo[i].SoLuong;
                }
            }

            if (soLuongCoTheBan < currentQuantity)
            {
                if (txtStatus != null) { txtStatus.text = "Not enough items!"; txtStatus.color = Color.red; }
                return;
            }

            int soLuongThucBan = Mathf.Min(currentQuantity, soLuongCoTheBan);

            if (soLuongThucBan > 0)
            {
                int giaBan = selectedItem.value / 2;
                khachHang.RPC_BanVatPham(selectedItem.itemID, giaBan, soLuongThucBan);
                if (txtStatus != null) { txtStatus.text = "Sell successful!"; txtStatus.color = Color.green; }
                Invoke("VeShopRaManHinh", 0.1f);
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

        foreach (GameObject ui in uiCanAnKhiMoShop)
        {
            if (ui != null) ui.SetActive(false);
        }

        VeShopRaManHinh(); 
    }

    public void CloseShop() 
    { 
        isShopOpen = false; 
        khungShop.SetActive(false); 

        foreach (GameObject ui in uiCanAnKhiMoShop)
        {
            if (ui != null) ui.SetActive(true);
        }
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







