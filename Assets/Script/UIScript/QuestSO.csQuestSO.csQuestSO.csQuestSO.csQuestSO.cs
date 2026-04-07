using UnityEngine;

[CreateAssetMenu(fileName = "NhiemVu_Moi", menuName = "Tạo nhiệm vụ/Nhiệm Vụ")]
public class QuestSO : ScriptableObject
{
    public int idNhiemVu;
    public string tenNhiemVu;
    
    [Header("Yêu Cầu Hoàn Thành")]
    public int idVatPhamCanTim; // Bắt người chơi đi tìm ID này (VD: 1 = Gỗ)
    public int soLuongCan;      // Cần gom bao nhiêu cái

    [Header("Phần Thưởng")]
    public int tienThuong;      // Thưởng bao nhiêu Gold
}