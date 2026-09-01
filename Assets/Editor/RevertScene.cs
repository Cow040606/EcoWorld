using UnityEngine;
using UnityEditor;

public class RevertScene : MonoBehaviour
{
    [MenuItem("EcoWorld/Revert Optimizations (Hoan Tac)")]
    public static void Revert()
    {
        // 1. Revert Lights back to Realtime
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type != LightType.Directional && light.lightmapBakeType == LightmapBakeType.Mixed)
            {
                light.lightmapBakeType = LightmapBakeType.Realtime;
                light.shadows = LightShadows.Soft; 
            }
        }
        
        // 2. Remove Static Flags from environment objects
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            if (objName.Contains("house") || objName.Contains("tree") || objName.Contains("rock") || 
                 objName.Contains("building") || objName.Contains("wall") || objName.Contains("prop") || objName.Contains("env"))
            {
                obj.isStatic = false;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Revert Complete", "Đã hoàn tác các thay đổi ánh sáng và static. Vui lòng lưu scene (Ctrl+S)", "OK");
    }
}
