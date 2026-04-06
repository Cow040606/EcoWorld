    using UnityEngine;

    [CreateAssetMenu(fileName = "NewSeedData", menuName = "Farming/Seed Data")]
    public class SO_SeedData : ScriptableObject
    {
        public string SeedItemID;         // VD: "seed_tomato"
        public string HarvestItemID;      // VD: "crop_tomato"
        public int HarvestYield = 3;      // Số lượng thu hoạch
        public float GrowTimeSeconds = 300f; // 5 phút

        [Header("Visuals")]
        public GameObject SeedlingPrefab; // Cây con
        public GameObject MaturePrefab;   // Cây trưởng thành
    }