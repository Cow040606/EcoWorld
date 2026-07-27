using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera; 

    void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;

            if (mainCamera == null) return; 
        }
        
        // Đã tìm thấy camera thì ép xoay mặt theo nó
        transform.forward = mainCamera.transform.forward;
    }
}