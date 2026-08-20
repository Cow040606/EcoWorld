using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Upgrade : MonoBehaviour
{
    [Header("UI Elements")]
    public LoCamDo slotTrangBi;
    public LoCamDo slotNguyenLieu;
    public Button btnUpgrade;
    public TMP_Text txtThongBao;
    public TMP_Text txtThongTinNguyenLieu; // Tùy chọn: Để hiện số đá cần thiết

        public bool dangMo = false;

    public void OpenUpgrade()
    {
        dangMo = true;
        gameObject.SetActive(true);
        if (InventoryManager.instance != null && Player_Controller.localPlayer != null)
        {
            InventoryManager.instance.MoBaloTuNgoai(Player_Controller.localPlayer, true);
        }
    }

    public void CloseUpgrade()
    {
        dangMo = false;
        gameObject.SetActive(false);
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.DongBaloTuNgoai();
        }
    }

    private void Start()
    {
        if (btnUpgrade != null)
        {
            btnUpgrade.onClick.AddListener(OnUpgradeClicked);
        }
    }

    private void Update()
    {
        // Liên tục cập nhật chữ hiển thị số nguyên liệu yêu cầu nếu có trang bị trong ô
        if (txtThongTinNguyenLieu != null && slotTrangBi != null && InventoryManager.instance != null)
        {
            int idTrangBi = slotTrangBi.LayIDTrangBiHienTai();
            if (idTrangBi > 0)
            {
                Item thongTinItem = InventoryManager.instance.TraCuuItem(idTrangBi);
                if (thongTinItem != null)
                {
                    int soLuongCan = thongTinItem.soLuongNguyenLieuNangCap * (slotTrangBi.levelDangMac + 1);
                    Item nguyenLieuThongTin = InventoryManager.instance.TraCuuItem(thongTinItem.idNguyenLieuNangCap);
                    string tenNguyenLieu = nguyenLieuThongTin != null ? nguyenLieuThongTin.itemName : "Nguyên liệu";
                    txtThongTinNguyenLieu.text = $"Yêu cầu: {soLuongCan}x {tenNguyenLieu}";
                }
            }
            else
            {
                txtThongTinNguyenLieu.text = "Chưa có trang bị";
            }
        }
    }

    public void OnUpgradeClicked()
    {
        if (slotTrangBi == null || slotNguyenLieu == null) return;
        
        int idTrangBi = slotTrangBi.LayIDTrangBiHienTai();
        int idNguyenLieu = slotNguyenLieu.LayIDTrangBiHienTai();

        if (idTrangBi == 0)
        {
            ShowMessage("Vui lòng đặt trang bị cần nâng cấp vào ô!");
            return;
        }

        Item thongTinTrangBi = InventoryManager.instance.TraCuuItem(idTrangBi);
        if (thongTinTrangBi == null) return;

        int idCanThiet = thongTinTrangBi.idNguyenLieuNangCap;
        int soLuongCanThiet = thongTinTrangBi.soLuongNguyenLieuNangCap * (slotTrangBi.levelDangMac + 1);

        if (idNguyenLieu != idCanThiet)
        {
            Item nguyenLieuCan = InventoryManager.instance.TraCuuItem(idCanThiet);
            string tenNguyenLieu = nguyenLieuCan != null ? nguyenLieuCan.itemName : "nguyên liệu khác";
            ShowMessage($"Sai nguyên liệu! Món này cần {tenNguyenLieu}.");
            return;
        }

        int soLuongHienCo = Player_Controller.localPlayer.DemSoLuongVatPham(idCanThiet);
        if (soLuongHienCo < soLuongCanThiet)
        {
            ShowMessage($"Không đủ nguyên liệu! Cần {soLuongCanThiet}.");
            return;
        }

        Player_Controller.localPlayer.RPC_TruVatPham(idCanThiet, soLuongCanThiet);
        Player_Controller.localPlayer.RPC_NangCapVatPham(idTrangBi, slotTrangBi.levelDangMac);
        slotTrangBi.levelDangMac++;
        
        if (InventoryManager.instance != null) InventoryManager.instance.CapNhatLaiToanBoChiSo();
        LoCamDo.CapNhatToanBoSoLuongTrenTramCheTao();
        
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo)
        {
            InventoryManager.instance.VeBaloRaManHinh(Player_Controller.localPlayer.TuiDo);
        }

        ShowMessage($"Nâng cấp thành công! Trang bị đạt cấp +{slotTrangBi.levelDangMac}");
    }

    private void ShowMessage(string msg)
    {
        if (txtThongBao != null) txtThongBao.text = msg;
        else Debug.Log(msg);
    }
}
