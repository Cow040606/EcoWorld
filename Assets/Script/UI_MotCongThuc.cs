using UnityEngine;
using UnityEngine.UI; // Bắt buộc phải có cái này để dùng Image
using TMPro;

public class UI_MotCongThuc : MonoBehaviour
{
    [Header("Thông tin kết quả")]
    public TextMeshProUGUI txtTenMonDo;
    public Image imgKetQua;
    public TextMeshProUGUI txtGiaTien; // Dành cho xu (nếu có, không có cứ để trống ngoài Inspector)

    [Header("Danh sách 3 ô nguyên liệu")]
    public GameObject[] khungNguyenLieu = new GameObject[3]; // Dùng để Bật/Tắt nguyên cụm (hình + chữ)
    public Image[] imgNguyenLieu = new Image[3];             // Nơi truyền hình icon nguyên liệu vào
    public TextMeshProUGUI[] txtSoLuongNL = new TextMeshProUGUI[3]; // Truyền chữ x2, x3...

    private CraftingRecipe congThucHienTai;

    // Trạm chế tạo sẽ gọi hàm này để đổ dữ liệu vào UI
    public void HienThiThongTin(CraftingRecipe ct)
    {
        congThucHienTai = ct;
        if (ct == null || ct.monDoThuDuoc == null) return;

        // 1. Gán Tên và Hình ảnh món đồ thu được
        if (txtTenMonDo != null) txtTenMonDo.text = ct.monDoThuDuoc.itemName;
        if (imgKetQua != null) imgKetQua.sprite = ct.monDoThuDuoc.icon;

        // 2. Hiển thị Tiền xu (nếu công thức có yêu cầu)
        if (txtGiaTien != null)
        {
            txtGiaTien.gameObject.SetActive(ct.giaTienXu > 0);
            txtGiaTien.text = ct.giaTienXu + " Xu";
        }

        // 3. TẮT SẠCH 3 ô nguyên liệu đi trước 
        // (Để xíu nữa công thức cần mấy món thì chỉ bật bấy nhiêu ô)
        for (int i = 0; i < 3; i++)
        {
            if (khungNguyenLieu[i] != null) khungNguyenLieu[i].SetActive(false);
        }

        // 4. Duyệt qua nguyên liệu của công thức và BẬT đúng số ô lên
        if (ct.danhSachNguyenLieu != null)
        {
            for (int i = 0; i < ct.danhSachNguyenLieu.Length; i++)
            {
                if (i >= 3) break; // Khung UI của bò chỉ hỗ trợ tối đa 3 nguyên liệu
                
                var nl = ct.danhSachNguyenLieu[i];

                if (nl.monDo != null && nl.soLuongCan > 0)
                {
                    // Bật ô UI lên
                    if (khungNguyenLieu[i] != null) khungNguyenLieu[i].SetActive(true);
                    
                    // Gán hình ảnh và số lượng
                    if (imgNguyenLieu[i] != null) imgNguyenLieu[i].sprite = nl.monDo.icon;
                    if (txtSoLuongNL[i] != null) txtSoLuongNL[i].text = "x" + nl.soLuongCan;
                }
            }
        }
    }

    // Gắn hàm này vào sự kiện OnClick() của cái Nút Chế Tạo
    public void BamNutCheTao()
    {
        if (Player_Controller.localPlayer == null || congThucHienTai == null) return;
        Player_Controller player = Player_Controller.localPlayer;

        // Trích xuất ID và Số lượng của 3 nguyên liệu
        int id1 = 0, sl1 = 0, id2 = 0, sl2 = 0, id3 = 0, sl3 = 0;

        if (congThucHienTai.danhSachNguyenLieu.Length > 0 && congThucHienTai.danhSachNguyenLieu[0].monDo != null)
        { id1 = congThucHienTai.danhSachNguyenLieu[0].monDo.itemID; sl1 = congThucHienTai.danhSachNguyenLieu[0].soLuongCan; }
        
        if (congThucHienTai.danhSachNguyenLieu.Length > 1 && congThucHienTai.danhSachNguyenLieu[1].monDo != null)
        { id2 = congThucHienTai.danhSachNguyenLieu[1].monDo.itemID; sl2 = congThucHienTai.danhSachNguyenLieu[1].soLuongCan; }
        
        if (congThucHienTai.danhSachNguyenLieu.Length > 2 && congThucHienTai.danhSachNguyenLieu[2].monDo != null)
        { id3 = congThucHienTai.danhSachNguyenLieu[2].monDo.itemID; sl3 = congThucHienTai.danhSachNguyenLieu[2].soLuongCan; }

        // KIỂM TRA ĐỦ ĐỒ CHƯA
        if (congThucHienTai.giaTienXu > 0 && player.Gold < congThucHienTai.giaTienXu) { Debug.Log("<color=red>Thiếu Xu!</color>"); return; }
        if (id1 > 0 && player.DemSoLuongVatPham(id1) < sl1) { Debug.Log("<color=red>Thiếu nguyên liệu 1!</color>"); return; }
        if (id2 > 0 && player.DemSoLuongVatPham(id2) < sl2) { Debug.Log("<color=red>Thiếu nguyên liệu 2!</color>"); return; }
        if (id3 > 0 && player.DemSoLuongVatPham(id3) < sl3) { Debug.Log("<color=red>Thiếu nguyên liệu 3!</color>"); return; }

        // ĐỦ ĐỒ -> RÈN THÔI!
        Debug.Log("<color=green>Chế tạo thành công!</color>");
        player.RPC_ThucHienCheTao(congThucHienTai.monDoThuDuoc.itemID, congThucHienTai.soLuongThuDuoc, id1, sl1, id2, sl2, id3, sl3, congThucHienTai.giaTienXu);
    }
}