using UnityEngine;

[CreateAssetMenu(fileName = "CongThuc_Moi", menuName = "Items/Cong Thuc Che Tao")]
public class CraftingRecipe : ScriptableObject
{
    [Header("--- SẢN PHẨM NHẬN ĐƯỢC ---")]
    public Item monDoThuDuoc;
    public int soLuongThuDuoc = 1;

    [Header("--- ĐIỀU KIỆN CHẾ TẠO ---")]
    public int giaTienXu = 0; 

    [System.Serializable]
    public struct NguyenLieu
    {
        public Item monDo;
        public int soLuongCan;
    }

    [Header("Danh sách nguyên liệu (Tối đa 3 món)")]
    public NguyenLieu[] danhSachNguyenLieu = new NguyenLieu[3];
}