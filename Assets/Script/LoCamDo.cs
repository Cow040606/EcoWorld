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

    [Header("Hi?u ?ng sáng dèn")]
    public GameObject khungSang; 

    [Header("Cài d?t Nh?p Nháy")]
    public float tocDo = 3f;           
    public float doMoThapNhat = 0.2f;  
    private Image anhVien; 

    [Header("Hi?n th? hình ?nh & S? lu?ng")]
    public Image anhIcon;
    public TextMeshProUGUI txtSoLuong; 
    public int idDangMac = 0;
    public int levelDangMac = 0;

    void Awake()
    {
        if (!danhSachTatCaCacLo.Contains(this)) danhSachTatCaCacLo.Add(this);
        if (khungSang != null) anhVien = khungSang.GetComponent<Image>();
    }

    void Start()
    {
        if (idDangMac == 0)
        {
            XoaDoKhoiO();
        }
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
                int levelDoVat = doBiKeo.levelMonDoDangKeo;
                Item thongTin = InventoryManager.instance.TraCuuItem(idDoVat);

                if (KiemTraHopLe(thongTin))
                {
                    XuLyGanDo(idDoVat, levelDoVat);
                    Destroy(doBiKeo.gameObject);
                }
                else
                {
                    // Debug.Log("<color=red>Sai lo?i ô r?i Bò oi!</color>");
                }
            }
        }
    }

    private void XuLyGanDo(int idDoVat, int levelDoVat = 0)
    {
        if (Player_Controller.localPlayer == null) return;

        if (loaiCuaO == LoaiLo.Hotbar)
        {
            Player_Controller.localPlayer.RPC_GanVaoHotbar(slotIndex, idDoVat);
        }
        else 
        {
            if (loaiCuaO == LoaiLo.NguyenLieuCheTao)
            {
                foreach (var lo in danhSachTatCaCacLo)
                {
                    if (lo != this && lo.loaiCuaO == LoaiLo.NguyenLieuCheTao && lo.idDangMac == idDoVat)
                    {
                        lo.XoaDoKhoiO(); 
                    }
                }
            }

            if (anhIcon != null && InventoryManager.instance != null)
            {
                Item thongTin = InventoryManager.instance.TraCuuItem(idDoVat);
                if (thongTin != null)
                {
                    anhIcon.sprite = thongTin.icon; 
                    anhIcon.enabled = true;
                    
                    Color mauHienTai = Color.white; 
                    anhIcon.color = mauHienTai;
                    
                    idDangMac = idDoVat;
                    levelDangMac = levelDoVat; 

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

        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo && Player_Controller.localPlayer != null)
        {
            InventoryManager.instance.VeBaloRaManHinh(Player_Controller.localPlayer.TuiDo);
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
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (idDangMac != 0 || loaiCuaO == LoaiLo.Hotbar)
            {
                if (loaiCuaO == LoaiLo.Hotbar && Player_Controller.localPlayer != null)
                {
                    Player_Controller.localPlayer.RPC_GanVaoHotbar(slotIndex, 0);
                }
                else
                {
                    XoaDoKhoiO();
                    if (InventoryManager.instance != null) InventoryManager.instance.CapNhatLaiToanBoChiSo();
                }

                if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo && Player_Controller.localPlayer != null)
                {
                    InventoryManager.instance.VeBaloRaManHinh(Player_Controller.localPlayer.TuiDo);
                }
            }
        }
    }

    public void XoaDoKhoiO()
    {
        idDangMac = 0; 
        levelDangMac = 0;
        
        if (anhIcon != null)
        {
            anhIcon.sprite = null;
            anhIcon.enabled = false;
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
                lo.txtSoLuong.text = $"Sl: {soLuongMoi}x";
            }
        }
    }
}
