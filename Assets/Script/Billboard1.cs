using UnityEngine;

public class Billboar1 : MonoBehaviour
{
    private Camera cam; // Đã chuyển thành private, tự động gán qua code

    void LateUpdate()
    {
        // 1. Tự động tìm Camera nếu biến cam đang trống
        if (cam == null)
        {
            // Lấy camera từ Local Player (người chơi trên máy này)
            if (Player_Controller.localPlayer != null && Player_Controller.localPlayer.playerCamera != null)
            {
                cam = Player_Controller.localPlayer.playerCamera;
            }

            // Dùng Fallback (Dự phòng): Tìm Camera mặc định của Scene
            if (cam == null)
            {
                cam = Camera.main;
            }

            // Nếu player và camera vẫn chưa kịp spawn, bỏ qua frame này để tránh lỗi NullReference
            if (cam == null) return;
        }

        // 2. Ép thanh máu xoay mặt nhìn vuông góc với Camera
        transform.LookAt(transform.position + cam.transform.forward);
    }
}