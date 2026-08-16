using UnityEngine;

[CreateAssetMenu(fileName = "New_Item", menuName = "Items/Item Create")]
public class Item : ScriptableObject
{
    [Header("--- THÔNG TIN CƠ BẢN ---")]
    public int itemID;
    public string itemName;
    public string Tag;
    public Sprite icon;   
    public bool stackable = true;
    public int value;

    public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
    [Header("Độ hiếm")]
    public ItemRarity rarity = ItemRarity.Common;

    // Lu ít thêm 'VuKhi_CongCu' để Bò dễ phân biệt đồ cầm tay (Cuốc, Rìu) với rác/khoáng sản
    public enum LoaiTrangBi { KhongPhai, Non, DayChuyen, Ao, Giay, Nhan, VuKhi_CongCu }
    
    [Header("--- PHÂN LOẠI TRANG BỊ ---")]
    [Tooltip("Chọn loại trang bị để hệ thống kéo thả nhận diện đúng ô")]
    public LoaiTrangBi loaiTrangBi = LoaiTrangBi.KhongPhai; 

    [Header("--- CHỈ SỐ CỘNG THÊM ---")]
    [Tooltip("Chỉ có tác dụng khi món đồ này được mặc lên người")]
    public float congThemMau;     
    public float congThemStamina;  
    public float congThemTocDo; 

    
    public enum LoaiTieuHao { KhongPhai, HoiMau, HoiTheLuc, SuaGiap }
    [Header("Công cụ tiêu hao")]
    
    [Tooltip("Phân loại xem món đồ này khi bấm chuột phải thì bơm cái gì")]
    public LoaiTieuHao loaiTieuHao = LoaiTieuHao.KhongPhai;
    
    [Tooltip("Thời gian đứng chờ để dùng xong (Ví dụ: 10 giây)")]
    public float thoiGianDung = 0f; 
    
    [Tooltip("Số điểm cộng thêm (Ví dụ: 20 máu, 50 giáp...)")]
    public float luongHoiPhuc = 0f;
    public float congThemGiap;   


    [Header("Mô tả chi tiết")]
    [TextArea(3, 10)] 
    public string description;

    [Header("Mô hình 3D khi cầm trên tay")]
    public GameObject model3DPrefab;

    [Tooltip("Dịch chuyển vị trí của món đồ so với tay nhân vật")]
    public Vector3 viTriCamOffset = Vector3.zero; 
    
    [Tooltip("Xoay món đồ sao cho khớp với lòng bàn tay")]
    public Vector3 gocXoayOffset = Vector3.zero;  
    
    [Tooltip("Kích thước của món đồ")]
    public Vector3 scaleTrenTay = Vector3.one;
}