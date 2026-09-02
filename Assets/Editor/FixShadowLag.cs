using UnityEngine;
using UnityEditor;

public class FixShadowLag : MonoBehaviour
{
    [MenuItem("EcoWorld/Fix Shadow Lag (Tat Bong Do Đen Phu)")]
    public static void FixShadows()
    {
        Light[] lights = FindObjectsOfType<Light>();
        int disabledShadows = 0;
        foreach (Light light in lights)
        {
            // Chỉ giữ lại bóng đổ cho mặt trời (Directional Light), tắt hết bóng của đèn con
            if (light.type != LightType.Directional && light.shadows != LightShadows.None)
            {
                light.shadows = LightShadows.None;
                disabledShadows++;
            }
        }
        
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Hoàn tất", $"Đã tắt bóng đổ của {disabledShadows} đèn phụ (Point/Spot light). Tình trạng giật lag bóng đổ URP sẽ chấm dứt! Vui lòng lưu scene (Ctrl+S).", "OK");
    }
}
