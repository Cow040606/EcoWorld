using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public class GlobalOptimizer
{
    static GlobalOptimizer()
    {
        EditorApplication.delayCall += FixURP;
        EditorApplication.delayCall += FixFont;
    }

    static void FixURP()
    {
        UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null) urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;

        if (urpAsset != null)
        {
            SerializedObject so = new SerializedObject(urpAsset);
            SerializedProperty castShadows = so.FindProperty("m_AdditionalLightShadowsSupported");
            if (castShadows != null && castShadows.boolValue == true)
            {
                castShadows.boolValue = false;
                so.ApplyModifiedProperties();
                Debug.Log("[Tối Ưu FPS Cực Hạn] Đã TẮT bóng đổ của toàn bộ các đèn phụ (Point/Spot) tận gốc trong cấu hình URP Asset! Vĩnh viễn hết lag do đèn.");
            }
        }
    }

    static void FixFont()
    {
        // Khắc phục lỗi spam Console liên tục của TextMeshPro
        string[] guids = AssetDatabase.FindAssets("Grenze-SemiBold SDF Atlas t:Texture2D");
        foreach(string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                Debug.Log("[Sửa Lỗi SPAM] Đã bật Read/Write cho font chữ " + path + " để chấm dứt chuỗi báo lỗi đỏ liên tục của TMPro.");
            }
        }
    }
}
