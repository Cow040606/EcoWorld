using UnityEngine;
using System.Collections.Generic;

public class UI_TramCheTao : MonoBehaviour
{
    [Header("1. Bỏ tất cả Công Thức của game vào đây")]
    public CraftingRecipe[] khoCongThuc;

    [Header("2. Kéo 3 ô Lỗ Cắm Đồ vào đây")]
    public LoCamDo[] baONguyenLieu = new LoCamDo[3];

    [Header("3. Nơi sinh ra danh sách UI")]
    public Transform khungChuaDanhSach; 
    public GameObject prefabMotCongThuc; 

    private int idCu1, idCu2, idCu3; // Dùng để theo dõi xem người chơi có đổi đồ trong lỗ không

    void Update()
    {
        // Lấy ID hiện tại trong 3 lỗ cắm đồ
        int id1 = baONguyenLieu[0] != null ? baONguyenLieu[0].LayIDTrangBiHienTai() : 0;
        int id2 = baONguyenLieu[1] != null ? baONguyenLieu[1].LayIDTrangBiHienTai() : 0;
        int id3 = baONguyenLieu[2] != null ? baONguyenLieu[2].LayIDTrangBiHienTai() : 0;

        // Nếu người chơi vừa nhét đồ mới vào hoặc rút đồ ra -> Cập nhật lại danh sách!
        if (id1 != idCu1 || id2 != idCu2 || id3 != idCu3)
        {
            idCu1 = id1; idCu2 = id2; idCu3 = id3;
            LocCongThuc(id1, id2, id3);
        }
    }

    void LocCongThuc(int id1, int id2, int id3)
    {
        // Xóa sạch danh sách cũ trên màn hình
        foreach (Transform child in khungChuaDanhSach) Destroy(child.gameObject);

        // Nếu cả 3 ô đều trống không -> Không hiện gì cả
        if (id1 == 0 && id2 == 0 && id3 == 0) return;

        // Duyệt qua toàn bộ công thức trong game
        foreach (var ct in khoCongThuc)
        {
            if (KiemTraCongThucCoChuaID(ct, id1, id2, id3))
            {
                // Nếu công thức này CÓ YÊU CẦU nguyên liệu đang nằm trong 3 lỗ -> Vẽ nó ra màn hình!
                GameObject go = Instantiate(prefabMotCongThuc, khungChuaDanhSach);
                go.GetComponent<UI_MotCongThuc>().HienThiThongTin(ct);
            }
        }
    }

    // Hàm phụ: Kiểm tra xem món đồ trong lỗ cắm có nằm trong Công Thức hay không
    bool KiemTraCongThucCoChuaID(CraftingRecipe ct, int id1, int id2, int id3)
    {
        // Yêu cầu: Nếu đã bỏ đồ vào lỗ nào, thì công thức BẮT BUỘC phải chứa đồ của lỗ đó
        if (id1 > 0 && !ChuaNguyenLieu(ct, id1)) return false;
        if (id2 > 0 && !ChuaNguyenLieu(ct, id2)) return false;
        if (id3 > 0 && !ChuaNguyenLieu(ct, id3)) return false;
        return true; 
    }

    bool ChuaNguyenLieu(CraftingRecipe ct, int idNL)
    {
        foreach (var nl in ct.danhSachNguyenLieu)
        {
            if (nl.monDo != null && nl.monDo.itemID == idNL) return true;
        }
        return false;
    }
}