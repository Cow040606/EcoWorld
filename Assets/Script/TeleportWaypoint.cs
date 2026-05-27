using UnityEngine;

public class TeleportWaypoint : MonoBehaviour
{
    [Header("Điểm Dịch Chuyển (Kéo Object đích vào đây)")]
    // Thay đổi: Dùng Transform thay vì Vector3
    public Transform diemDichChuyen; 

    // Hàm này sẽ được gọi khi Bò click chuột vào cái icon trên Map
    public void Click_DichChuyenNhanVat()
    {
        // Kiểm tra xem Bò đã kéo Object vào chưa, chống lỗi văng game
        if (diemDichChuyen == null)
        {
            Debug.Log("<color=red>Lỗi: Bò chưa kéo Object đích vào nút dịch chuyển này!</color>");
            return;
        }

        if (Player_Controller.localPlayer != null)
        {
            // Lấy tọa độ (position) của cái Object đó truyền vào hàm
            Player_Controller.localPlayer.ThucHienDichChuyen(diemDichChuyen.position);
            
            // TODO: Bò có thể gọi lệnh đóng Map ở đây nếu muốn
        }
        else
        {
            Debug.Log("Không tìm thấy Nhân vật của bạn!");
        }
    }
}