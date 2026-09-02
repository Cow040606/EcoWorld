using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class AutoFixShadowLag
{
    static AutoFixShadowLag()
    {
        EditorApplication.hierarchyChanged += CheckLights;
        CheckLights();
    }

    static void CheckLights()
    {
        if (Application.isPlaying) return;

        // Tìm tất cả các đèn trong scene
        Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        int disabledCount = 0;
        
        foreach (Light l in allLights)
        {
            // Nếu không phải là Mặt trời (Directional) và đang bật bóng đổ
            if (l.type != LightType.Directional && l.shadows != LightShadows.None)
            {
                l.shadows = LightShadows.None;
                disabledCount++;
                EditorUtility.SetDirty(l.gameObject);
            }
        }
        
        if (disabledCount > 0)
        {
            Debug.Log("[Tối Ưu FPS] Đã tự động tắt bóng đổ của " + disabledCount + " đèn phụ (Point/Spot Light) trong Scene để chống lag văng game.");
        }
    }
}
