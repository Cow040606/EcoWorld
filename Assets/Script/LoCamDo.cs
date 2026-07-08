using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI; // BẮT BUỘC THÊM CÁI NÀY ĐỂ ĐIỀU KHIỂN ĐỘ MỜ HÌNH ẢNH

public class LoCamDo : MonoBehaviour, IDropHandler
{
    public static List<LoCamDo> danhSachTatCaCacLo = new List<LoCamDo>();

    public enum LoaiLo { Hotbar, Non, DayChuyen, Ao, Giay, Nhan, VuKhi_CongCu }
    public LoaiLo loaiCuaO; 
    public int slotIndex;   

    [Header("Hiệu ứng sáng đèn")]
    public GameObject khungSang; 

    [Header("Cài đặt Nhấp Nháy")]
    public float tocDo = 3f;           
    public float doMoThapNhat = 0.2f;  
    private Image anhVien; // Lưu lại cái component Image của khung sáng
    [Header("Hiển thị hình ảnh")]
    public Image anhIcon;
    public int idDangMac = 0;

    void Awake()
    {
        if (!danhSachTatCaCacLo.Contains(this)) danhSachTatCaCacLo.Add(this);
        
        // Lấy component Image từ cái khung sáng (nếu Bò đã kéo khungSang vào)
        if (khungSang != null) anhVien = khungSang.GetComponent<Image>();
    }

    void OnDestroy()
    {
        if (danhSachTatCaCacLo.Contains(this)) danhSachTatCaCacLo.Remove(this);
    }

    // ==========================================
    // HÀM UPDATE NÀY SẼ LÀM KHUNG NHẤP NHÁY
    // ==========================================
    void Update()
    {
        // Chỉ nhấp nháy khi khung đang được bật (SetActive = true)
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

        return false; // Nếu không lọt vào các điều kiện trên thì từ chối!
    }

    // ==========================================
    // 2. KHI DI CHUỘT QUA -> XÉT DUYỆT ĐỂ BẬT ĐÈN
    // ==========================================
    public void KiemTraHopLeVaBatSang(Item thongTinItem)
    {
        if (khungSang == null) return;
        
        if (KiemTraHopLe(thongTinItem)) 
        {
            khungSang.SetActive(true);
        }
    }

    // ==========================================
    // 3. KHI THẢ CHUỘT -> XÉT DUYỆT LẦN CUỐI RỒI MỚI NHẬN ĐỒ
    // ==========================================
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            KeoThaItem doBiKeo = eventData.pointerDrag.GetComponent<KeoThaItem>();
            if (doBiKeo != null && InventoryManager.instance != null)
            {
                int idDoVat = doBiKeo.idMonDoDangKeo;
                Item thongTin = InventoryManager.instance.TraCuuItem(idDoVat);

                // CHẶN NGAY TẠI ĐÂY: Có hợp lệ mới cho chạy hàm gán đồ!
                if (KiemTraHopLe(thongTin))
                {
                    XuLyGanDo(idDoVat);
                    doBiKeo.transform.SetParent(doBiKeo.canvasGoc); 
                }
                else
                {
                    Debug.Log("<color=red>Sai lỗ rồi Bò ơi! Không nhét vào đây được!</color>");
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
            // --- XỬ LÝ CẬP NHẬT HÌNH ẢNH TRANG BỊ TẠI CHỖ ---
            if (anhIcon != null && InventoryManager.instance != null)
            {
                Item thongTin = InventoryManager.instance.TraCuuItem(idDoVat);
                if (thongTin != null)
                {
                    anhIcon.sprite = thongTin.icon; 
                    
                    Color mauHienTai = anhIcon.color;
                    mauHienTai.a = 1f; 
                    anhIcon.color = mauHienTai;
                    
                    idDangMac = idDoVat; // <--- CHÈN DÒNG NÀY ĐỂ LƯU LẠI ID
                }
            }
        }
        
        
        // --- CHÈN THÊM ĐOẠN NÀY VÀO CUỐI HÀM NÈ BÒ ---
        // Báo cho InventoryManager tính lại chỉ số mỗi khi Bò mặc 1 món đồ mới vào
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
}