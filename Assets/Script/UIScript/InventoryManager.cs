using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion; // Phải có thư viện mạng để đọc NetworkArray
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour 
{
    public static InventoryManager instance;

    [Header("UI Balo")]
    public GameObject khungBalo;
    public GameObject khungStats; // UI Stats trong Balo 
    public bool trangThaiBalo = false; 

    [Header("Cấu hình Ô UI")]
    public Transform itemHolder;  // Khung chứa các ô (Grid Layout Group)
    public GameObject itemPrefab; // Prefab của 1 ô (có hình, chữ...)

    [Header("Từ Điển Vật Phẩm")]
    public Item[] khoDuLieu; 
    [Header("Các ô trang bị trên UI (Kéo thả từ Inspector)")]
    public LoCamDo slotMu;
    public LoCamDo slotAo;
    public LoCamDo slotQuan; 
    public LoCamDo slotVuKhi;
    
    // THÊM 3 Ô MỚI NÀY VÀO ĐỂ HỨNG ĐỒ CỦA BÒ NÈ:
    public LoCamDo slotDayChuyen;
    public LoCamDo slotGiay;
    public LoCamDo slotNhan;

    // Hàm gọi cập nhật chỉ số
    public void CapNhatLaiToanBoChiSo()
    {
        // 1. Lấy ID từ TẤT CẢ các lỗ cắm đồ (Lỗ nào trống nó tự ra số 0)
        int idMu = slotMu != null ? slotMu.LayIDTrangBiHienTai() : 0;
        int idAo = slotAo != null ? slotAo.LayIDTrangBiHienTai() : 0;
        int idQuan = slotQuan != null ? slotQuan.LayIDTrangBiHienTai() : 0;
        int idVuKhi = slotVuKhi != null ? slotVuKhi.LayIDTrangBiHienTai() : 0;
        
        int idDayChuyen = slotDayChuyen != null ? slotDayChuyen.LayIDTrangBiHienTai() : 0;
        int idGiay = slotGiay != null ? slotGiay.LayIDTrangBiHienTai() : 0;
        int idNhan = slotNhan != null ? slotNhan.LayIDTrangBiHienTai() : 0;

        // 2. Gom TẤT CẢ vào 1 mảng
        O_VatPham[] danhSachDoDangMac = new O_VatPham[] 
        { 
            new O_VatPham { ItemID = idMu, UpgradeLevel = (slotMu != null ? slotMu.levelDangMac : 0) }, new O_VatPham { ItemID = idAo, UpgradeLevel = (slotAo != null ? slotAo.levelDangMac : 0) }, new O_VatPham { ItemID = idQuan, UpgradeLevel = (slotQuan != null ? slotQuan.levelDangMac : 0) }, new O_VatPham { ItemID = idVuKhi, UpgradeLevel = (slotVuKhi != null ? slotVuKhi.levelDangMac : 0) }, new O_VatPham { ItemID = idDayChuyen, UpgradeLevel = (slotDayChuyen != null ? slotDayChuyen.levelDangMac : 0) }, new O_VatPham { ItemID = idGiay, UpgradeLevel = (slotGiay != null ? slotGiay.levelDangMac : 0) }, new O_VatPham { ItemID = idNhan, UpgradeLevel = (slotNhan != null ? slotNhan.levelDangMac : 0) } 
        };

        // 3. Quăng cái mảng cho Player tính toán
        if (Player_Controller.localPlayer != null)
        {
            Player_Controller.localPlayer.RPC_CapNhatChiSoTrangBi(danhSachDoDangMac);
        }
    }

    [Header("Danh sách UI cần ẩn khi mở Balo")]
    public GameObject[] danhSachUI_CanAn; 
    private Dictionary<GameObject, bool> triNhoUI = new Dictionary<GameObject, bool>();
    public enum TabBalo { TrangBi, NguyenLieu, CongCu }

    // 2. Gắn Header vào đúng cái biến sẽ hiển thị ra Inspector
    [Header("Phân loại Tab Balo")]
    public TabBalo tabHienTai = TabBalo.NguyenLieu;

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
        // Xóa sạch sẽ các ô cũ đang hiển thị
        foreach (Transform child in itemHolder) { Destroy(child.gameObject); }

        // Lấy danh sách các món đồ đang được trang bị / cầm trên Hotbar để ẩn đi trong Balo
        List<int> danhSachDoDaGan = new List<int>();
        if (chuSoHuuBalo != null)
        {
            for (int j = 0; j < chuSoHuuBalo.HotbarIDs.Length; j++)
            {
                if (chuSoHuuBalo.HotbarIDs[j] != 0) danhSachDoDaGan.Add(chuSoHuuBalo.HotbarIDs[j]);
            }
        }
        if (slotMu != null && slotMu.idDangMac != 0) danhSachDoDaGan.Add(slotMu.idDangMac);
        if (slotAo != null && slotAo.idDangMac != 0) danhSachDoDaGan.Add(slotAo.idDangMac);
        if (slotQuan != null && slotQuan.idDangMac != 0) danhSachDoDaGan.Add(slotQuan.idDangMac);
        if (slotVuKhi != null && slotVuKhi.idDangMac != 0) danhSachDoDaGan.Add(slotVuKhi.idDangMac);
        if (slotDayChuyen != null && slotDayChuyen.idDangMac != 0) danhSachDoDaGan.Add(slotDayChuyen.idDangMac);
        if (slotGiay != null && slotGiay.idDangMac != 0) danhSachDoDaGan.Add(slotGiay.idDangMac);
        if (slotNhan != null && slotNhan.idDangMac != 0) danhSachDoDaGan.Add(slotNhan.idDangMac);

        for (int i = 0; i < tuiDoCuaPlayer.Length; i++)
        {
            if (tuiDoCuaPlayer[i].ItemID != 0)
            {
                int currentID = tuiDoCuaPlayer[i].ItemID;

                // Nếu món này đã gắn vào người/hotbar, bỏ qua không vẽ để "ẩn" đi, đồng thời xóa khỏi danh sách chờ để lỡ có món thứ 2 giống hệt thì vẫn vẽ
                if (danhSachDoDaGan.Contains(currentID))
                {
                    danhSachDoDaGan.Remove(currentID);
                    continue; 
                }

                Item thongTinMonDo = TraCuuItem(currentID);
                if (thongTinMonDo != null)
                {
                    // =======================================
                    // BỘ LỌC PHÂN LOẠI TỰ ĐỘNG
                    // =======================================
                    bool duocPhepHienThi = false;

                    if (tabHienTai == TabBalo.NguyenLieu && thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.KhongPhai) 
                    {
                        duocPhepHienThi = true; // Tab Nguyên liệu chỉ hiện đồ KhongPhai
                    }
                    else if (tabHienTai == TabBalo.CongCu && thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.VuKhi_CongCu) 
                    {
                        duocPhepHienThi = true; // Tab Công cụ chỉ hiện VuKhi_CongCu
                    }
                    else if (tabHienTai == TabBalo.TrangBi && 
                            (thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.Non || 
                             thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.Ao || 
                             thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.Giay || 
                             thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.DayChuyen || 
                             thongTinMonDo.loaiTrangBi == Item.LoaiTrangBi.Nhan)) 
                    {
                        duocPhepHienThi = true; // Tab Trang bị hiện các món giáp/trang sức
                    }

                    // ĐÚNG TAB THÌ MỚI ĐƯỢC VẼ RA NGOÀI!
                    if (duocPhepHienThi)
                    {
                        // (Mấy dòng code cũ đẻ Prefab và gán dữ liệu của Bò nhét hết vào trong hàm if này nhé)
                        GameObject oMoi = Instantiate(itemPrefab, itemHolder);
                        oMoi.GetComponent<SlotItemUI>().SetData(thongTinMonDo, tuiDoCuaPlayer[i].SoLuong, tuiDoCuaPlayer[i].UpgradeLevel);
                        
                        ItemHover camBien = oMoi.GetComponent<ItemHover>();
                        KeoThaItem cucKeoTha = oMoi.GetComponent<KeoThaItem>();
                        if (cucKeoTha != null) { cucKeoTha.idMonDoDangKeo = thongTinMonDo.itemID; cucKeoTha.levelMonDoDangKeo = tuiDoCuaPlayer[i].UpgradeLevel; }
                        if (camBien != null)
                        {
                            camBien.thongTinMonDo = thongTinMonDo;
                            camBien.soLuongDangCo = tuiDoCuaPlayer[i].SoLuong;
                              camBien.upgradeLevel = tuiDoCuaPlayer[i].UpgradeLevel; 
                        }
                    }
                }
            }
        }
    }

    public void BamChuyenTab(int idTab)
    {
        tabHienTai = (TabBalo)idTab;
        
        // Nếu Balo đang mở thì load lại hình ảnh ngay lập tức
        if (trangThaiBalo && chuSoHuuBalo != null)
        {
            VeBaloRaManHinh(chuSoHuuBalo.TuiDo);
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
        chuSoHuuBalo = player; 
        trangThaiBalo = !trangThaiBalo;

        if (khungStats != null && trangThaiBalo) khungStats.SetActive(true);

        foreach (GameObject ui in danhSachUI_CanAn)
        {
            if (ui != null)
            {
                ui.SetActive(!trangThaiBalo);
            }
        }

        if (trangThaiBalo)
        {
            VeBaloRaManHinh(tuiDoCuaPlayer);
        }

        if (khungBalo != null) khungBalo.SetActive(trangThaiBalo);
    }

    public void MoBaloTuNgoai(Player_Controller player, bool anKhungStats = false)
    {
        chuSoHuuBalo = player;
        if (khungStats != null) khungStats.SetActive(!anKhungStats);
        
        if (!trangThaiBalo)
        {
            trangThaiBalo = true;
            foreach (GameObject ui in danhSachUI_CanAn) { if (ui != null) ui.SetActive(false); }
            VeBaloRaManHinh(player.TuiDo);
            if (khungBalo != null) khungBalo.SetActive(true);
        }
    }

    public void DongBaloTuNgoai()
    {
        if (khungStats != null) khungStats.SetActive(true); // Khôi phục lại
        
        if (trangThaiBalo)
        {
            trangThaiBalo = false;
            foreach (GameObject ui in danhSachUI_CanAn) { if (ui != null) ui.SetActive(true); }
            if (khungBalo != null) khungBalo.SetActive(false);
        }
    }
}
