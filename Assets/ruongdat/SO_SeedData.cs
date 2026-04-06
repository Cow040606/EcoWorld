using UnityEngine;

[CreateAssetMenu(fileName = "NewSeedData", menuName = "Farming/Seed Data")]
public class SO_SeedData : ScriptableObject
{
    [Header("Định danh Item (Khớp với ID trong TuiDo)")]
    public int SeedItemID;         // Ép về kiểu INT (VD: 101)
    public int HarvestItemID;      // Ép về kiểu INT (VD: 201)
    
    [Header("Chỉ số nông nghiệp")]
    public int HarvestYield = 3;   
    public float GrowTimeSeconds = 300f; 

    [Header("Hình ảnh (Prefabs)")]
    public GameObject SeedlingPrefab; 
    public GameObject MaturePrefab;   
}