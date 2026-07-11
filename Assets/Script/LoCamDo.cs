using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI; 
using TMPro; 

public class LoCamDo : MonoBehaviour, IDropHandler, IPointerClickHandler 
{
    public static List<LoCamDo> danhSachTatCaCacLo = new List<LoCamDo>();

    public enum LoaiLo { Hotbar, Non, DayChuyen, Ao, Giay, Nhan, VuKhi_CongCu, NguyenLieuCheTao }
    public LoaiLo loaiCuaO; 
    public int slotIndex;   

    [Header("Hiệu ứng sáng đèn")]
    public GameObject khungSang; 

    [Header("Cài đặt Nhấp Nháy")]
    public float tocDo = 3f;           
    public float doMoThapNhat = 0.2f;  
    private Image anhVien; 

    [Header("Hiển thị hình ảnh & Số lượng")]
    public Image anhIcon;
    public TextMeshProUGUI txtSoLuong; 
    public int idDangMac = 0;

    void Awake()
    {
        if (!danhSachTatCaCacLo.Contains(this)) danhSachTatCaCacLo.Add(this);
        if (khungSang != null) anhVien = khungSang.GetComponent<Image>();
    }

    void OnDestroy()
    {
        if (danhSachTatCaCacLo.Contains(this)) danhSachTatCaCacLo.Remove(this);
    }

    void Update()
    {
        if (khungSang != null && khungSang.activeSelf && anhVien != null)
        {
            Color mau = anhVien.color;
            float nhipTho = Mathf.PingPong(Time.time * tocDo, 1f);
            mau.a = Mathf.Lerp(doMoThapNhat, 1f, nhipTho);
            anhVien.color = mau;
        }
    }

    public bool KiemTraHopLe(Item thongTinItem)
    {
        if (thongTinItem == null) return false;
        
        // ========================================================
        // Đã tháo chốt chặn! Cứ là ô chế tạo thì cho phép thả đồ vào
        // ========================================================
        if (loaiCuaO == LoaiLo.NguyenLieuCheTao) return true;

        if (loaiCuaO == LoaiLo.Hotbar && (thongTinItem.loaiTrangBi == Item.LoaiTrangBi.VuKhi_CongCu || thongTinItem.loaiTrangBi == Item.LoaiTrangBi.KhongPhai))
            return true;
        else if (loaiCuaO == LoaiLo.Non && thongTinItem.loaiTrangBi == Item.LoaiTrangBi.Non)
            return true;
        else if (loaiCuaO == LoaiLo.DayChuyen && thongTinItem.loaiTrangBi == Item.LoaiTrangBi.DayChuyen)
            return true;
        else if (loaiCuaO == LoaiLo.Ao && thongTinItem.loaiTrangBi == Item.LoaiTrangBi.Ao)
            return true;
        else if (loaiCuaO == LoaiLo.Giay && thongTinItem.loaiTrangBi == Item.LoaiTrangBi.Giay)
            return true;
        else if (loaiCuaO == LoaiLo.Nhan && thongTinItem.loaiTrangBi == Item.LoaiTrangBi.Nhan)
            return true;

        return false; 
    }

    public void KiemTraHopLeVaBatSang(Item thongTinItem)
    {
        if (khungSang == null) return;
        if (KiemTraHopLe(thongTinItem)) khungSang.SetActive(true);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            KeoThaItem doBiKeo = eventData.pointerDrag.GetComponent<KeoThaItem>();
            if (doBiKeo != null && InventoryManager.instance != null)
            {
                int idDoVat = doBiKeo.idMonDoDangKeo;
                Item thongTin = InventoryManager.instance.TraCuuItem(idDoVat);

                if (KiemTraHopLe(thongTin))
                {
                    XuLyGanDo(idDoVat);
                    doBiKeo.transform.SetParent(doBiKeo.canvasGoc); 
                }
                else
                {
                    Debug.Log("<color=red>Sai loại ô rồi Bò ơi!</color>");
                }
            }
        }
    }

    private void XuLyGanDo(int idDoVat)
    {
        if (Player_Controller.localPlayer == null) return;

        if (loaiCuaO == LoaiLo.Hotbar)
        {
            Player_Controller.localPlayer.RPC_GanVaoHotbar(slotIndex, idDoVat);
        }
        else 
        {
            // ========================================================
            // TÌM VÀ XÓA ĐỒ Ở Ô CŨ TRƯỚC KHI GẮN VÀO Ô MỚI
            // ========================================================
            if (loaiCuaO == LoaiLo.NguyenLieuCheTao)
            {
                foreach (var lo in danhSachTatCaCacLo)
                {
                    // Nếu thấy một lỗ khác (cũng là lỗ chế tạo) đang cầm món đồ này -> Tẩy trắng nó!
                    if (lo != this && lo.loaiCuaO == LoaiLo.NguyenLieuCheTao && lo.idDangMac == idDoVat)
                    {
                        lo.XoaDoKhoiO(); 
                    }
                }
            }

            // Tiến hành gắn đồ vào ô hiện tại
            if (anhIcon != null && InventoryManager.instance != null)
            {
                Item thongTin = InventoryManager.instance.TraCuuItem(idDoVat);
                if (thongTin != null)
                {
                    anhIcon.sprite = thongTin.icon; 
                    
                    Color mauHienTai = anhIcon.color;
                    mauHienTai.a = 1f; 
                    anhIcon.color = mauHienTai;
                    
                    idDangMac = idDoVat; 

                    if (txtSoLuong != null)
                    {
                        int soLuongHienCo = Player_Controller.localPlayer.DemSoLuongVatPham(idDoVat);
                        if (loaiCuaO == LoaiLo.NguyenLieuCheTao)
                        {
                            txtSoLuong.text = $"Soluong: {soLuongHienCo}";
                        }
                        else
                        {
                            txtSoLuong.text = ""; 
                        }
                    }
                }
            }
        }
        
        if (loaiCuaO != LoaiLo.Hotbar && InventoryManager.instance != null)
        {
            InventoryManager.instance.CapNhatLaiToanBoChiSo();
        }
    }

    public int LayIDTrangBiHienTai()
    {
        return idDangMac; 
    }

    public void TatSang()
    {
        if (khungSang != null) khungSang.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Nhận diện cú click chuột phải để gỡ đồ
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (loaiCuaO == LoaiLo.NguyenLieuCheTao && idDangMac != 0)
            {
                XoaDoKhoiO();
            }
        }
    }

    public void XoaDoKhoiO()
    {
        idDangMac = 0; 
        
        if (anhIcon != null)
        {
            anhIcon.sprite = null;
            Color mauTrongSuot = anhIcon.color;
            mauTrongSuot.a = 0f; 
            anhIcon.color = mauTrongSuot;
        }

        if (txtSoLuong != null)
        {
            txtSoLuong.text = ""; 
        }
    }

    public static void CapNhatToanBoSoLuongTrenTramCheTao()
    {
        if (Player_Controller.localPlayer == null) return;
        
        foreach (var lo in danhSachTatCaCacLo)
        {
            if (lo.loaiCuaO == LoaiLo.NguyenLieuCheTao && lo.txtSoLuong != null && lo.idDangMac > 0)
            {
                int soLuongMoi = Player_Controller.localPlayer.DemSoLuongVatPham(lo.idDangMac);
                lo.txtSoLuong.text = $"Soluong: {soLuongMoi}";
            }
        }
    }
}