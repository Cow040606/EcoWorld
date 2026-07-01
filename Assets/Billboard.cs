using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Cứ để public để Bò kéo thả trong Inspector như Bò đã làm
    public Camera mainCamera; 

    void LateUpdate()
    {
        if (mainCamera == null) return;
        
        // Ép xoay mặt theo Camera
        transform.forward = mainCamera.transform.forward;
    }
}