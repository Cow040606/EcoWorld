using UnityEngine;

// Dòng dưới đây chính là lệnh tạo ra menu chuột phải mà bạn đang tìm
[CreateAssetMenu(fileName = "NewSeedData", menuName = "Farming/Seed Data")] 
public class SO_SeedData : ScriptableObject
{
    [Header("Định danh Item (Khớp với ID trong TuiDo)")]
    public int SeedItemID;
    public int HarvestItemID;
    
    [Header("Chỉ số nông nghiệp")]
    public int HarvestYield = 3;   
    public float GrowTimeSeconds = 300f; 

    [Header("Hình ảnh (Prefabs)")]
    public GameObject SeedlingPrefab; 
    public GameObject MaturePrefab;   
}