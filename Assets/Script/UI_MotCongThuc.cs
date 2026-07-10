using UnityEngine;
using TMPro;

public class UI_MotCongThuc : MonoBehaviour
{
    public TextMeshProUGUI txtTenVaNguyenLieu;
    private CraftingRecipe congThucHienTai;

    // Trạm chế tạo sẽ gọi hàm này để đổ dữ liệu vào UI
    public void HienThiThongTin(CraftingRecipe ct)
    {
        congThucHienTai = ct;
        if (ct == null || ct.monDoThuDuoc == null) return;

        // Xây dựng chuỗi chữ. Ví dụ: "Rìu Sắt (Cần: 3 Gỗ, 2 Sắt)"
        string chuoi = $"{ct.monDoThuDuoc.itemName} (Cần: ";
        
        foreach (var nl in ct.danhSachNguyenLieu)
        {
            if (nl.monDo != null && nl.soLuongCan > 0)
            {
                chuoi += $"{nl.soLuongCan} {nl.monDo.itemName}, ";
            }
        }
        
        if (ct.giaTienXu > 0) chuoi += $"{ct.giaTienXu} Xu, ";

        // Cắt bỏ dấu phẩy bị dư ở cuối chuỗi
        chuoi = chuoi.TrimEnd(',', ' ') + ")";
        txtTenVaNguyenLieu.text = chuoi;
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