using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Đổi thành private vì giờ mình để code tự tìm, không cần kéo thả nữa
    private Camera mainCamera; 

    void LateUpdate()
    {
        // Nếu chưa có camera (do nhân vật chưa kịp Spawn), tự động đi tìm
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null) return; 
        }
        
        // Đã tìm thấy camera thì ép xoay mặt theo nó
        transform.forward = mainCamera.transform.forward;
    }
}