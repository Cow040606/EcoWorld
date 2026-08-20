using UnityEngine;

public enum LoaiNhiemVu
{
    GiaoVatPham = 0,    // Tìm / Thu thập / Giao vật phẩm có trong túi đồ
    TieuDietQuai = 1,   // Tiêu diệt quái vật (targetID = ID quái, 0 = quái bất kỳ)
    CauCa = 2,          // Câu cá (targetID = ID cá, 0 = cá bất kỳ)
    ThuHoach = 3,       // Trồng / Thu hoạch cây trồng (targetID = ID nông sản, 0 = nông sản bất kỳ)
    TroChuyenNPC = 4,   // Trò chuyện / Gặp NPC (targetID = ID NPC)
    TichLuyTien = 5,    // Tích lũy đủ số Gold hiện có
    CheTao = 6,         // Chế tạo vật phẩm (targetID = ID vật phẩm chế tạo, 0 = bất kỳ)
    DatCapDo = 7        // THÊM MỚI: Đạt đến cấp độ yêu cầu (soLuongCan = Cấp độ cần đạt)
}

[CreateAssetMenu(fileName = "NhiemVu_Moi", menuName = "Tạo nhiệm vụ/Nhiệm Vụ")]
public class QuestSO : ScriptableObject
{
    public int idNhiemVu;
    public string tenNhiemVu;
    [TextArea(2, 4)]
    public string moTaNhiemVu;

    [Header("NPC Liên Quan")]
    [Tooltip("ID của NPC liên quan đến nhiệm vụ này (dùng để hiển thị dấu ! trên đầu NPC khi đang nhận quest)")]
    public int npcID;

    [Header("Loại Nhiệm Vụ")]
    public LoaiNhiemVu loaiNhiemVu = LoaiNhiemVu.GiaoVatPham;

    [Header("Yêu Cầu Hoàn Thành")]
    [Tooltip("ID của Vật phẩm / Quái / Cá / Crop / NPC tùy thuộc Loại Nhiệm Vụ. Đặt 0 nếu chấp nhận loại bất kỳ.")]
    public int targetID;        // ID mục tiêu
    public int soLuongCan = 1;  // Cần gom / làm bao nhiêu

    // Thuộc tính tương thích ngược cho code cũ
    public int idVatPhamCanTim
    {
        get => targetID;
        set => targetID = value;
    }

    [Header("Phần Thưởng")]
    public int tienThuong;              // Thưởng bao nhiêu Gold
    public int gemThuong;               // Thưởng bao nhiêu Gem

    [Tooltip("Thưởng bao nhiêu Kinh Nghiệm (EXP)")]
    public float expThuong;             // ĐÃ THÊM BIẾN NÀY ĐỂ THƯỞNG EXP

    public int idVatPhamThuong;         // ID vật phẩm thưởng (0 = không thưởng item)
    public int soLuongVatPhamThuong = 1; // Số lượng vật phẩm thưởng
}