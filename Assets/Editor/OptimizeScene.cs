using UnityEngine;
using UnityEditor;

public class OptimizeScene : MonoBehaviour
{
    [MenuItem("EcoWorld/Optimize Current Scene (map1)")]
    public static void Optimize()
    {
        int optimizedLights = 0;
        int staticCount = 0;
        int terrainCount = 0;

        // 1. Optimize Lights
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            // Set realtime lights (except directional) to Mixed to save draw calls
            if (light.type != LightType.Directional && light.lightmapBakeType == LightmapBakeType.Realtime)
            {
                light.lightmapBakeType = LightmapBakeType.Mixed;
                optimizedLights++;
            }
            
            // Optionally reduce shadow resolution for smaller lights
            if (light.type == LightType.Point && light.range < 10f)
            {
                light.shadows = LightShadows.None;
            }
        }
        // Debug.Log($"[Optimization] Changed {optimizedLights} realtime lights to Mixed mode.");

        // 2. Terrain Optimization
        Terrain[] terrains = FindObjectsOfType<Terrain>();
        foreach (Terrain terrain in terrains)
        {
            // Tweak terrain settings for better performance
            terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, 10f);
            terrain.basemapDistance = Mathf.Min(terrain.basemapDistance, 250f);
            terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance, 100f);
            terrain.treeDistance = Mathf.Min(terrain.treeDistance, 500f);
            terrainCount++;
        }
        // Debug.Log($"[Optimization] Optimized {terrainCount} terrains.");

        // 3. Mark environment objects as static for Static Batching
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            // Look for common environment object names
            if ((objName.Contains("house") || objName.Contains("tree") || objName.Contains("rock") || 
                 objName.Contains("building") || objName.Contains("wall") || objName.Contains("prop") || objName.Contains("env")) 
                && !obj.isStatic)
            {
                // Set flags for batching and culling
                GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.NavigationStatic);
                staticCount++;
            }
        }
        // Debug.Log($"[Optimization] Marked {staticCount} environment objects as Static for Batching.");

        // Mark scene as dirty so the user can save it
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("Optimization Complete", 
            $"Optimized {optimizedLights} lights.\n" +
            $"Optimized {terrainCount} terrains.\n" +
            $"Marked {staticCount} objects as static.\n\n" +
            "Please go to Window > Rendering > Lighting and click 'Generate Lighting' to bake the lights, then save the scene (Ctrl+S).", 
            "OK");
    }
}
