using UnityEngine;
using System.Collections.Generic;

public class GlobalSeedDatabase : MonoBehaviour
{
    public static GlobalSeedDatabase Instance;
    
    // Khởi tạo sẵn list để không bao giờ bị Null
    public List<SO_SeedData> AllSeeds = new List<SO_SeedData>(); 
    
    private static Dictionary<int, SO_SeedData> _lookup = new Dictionary<int, SO_SeedData>();

    private void Awake() 
    {
        Instance = this;
        
        // Xóa data cũ đề phòng trường hợp load lại Scene
        _lookup.Clear();

        if (AllSeeds != null)
        {
            foreach(var s in AllSeeds) 
            {
                // Chỉ nạp những hạt giống đã được kéo thả đàng hoàng, bỏ qua các ô "None"
                if (s != null) 
                {
                    _lookup[s.SeedItemID] = s;
                }
            }
        }
    }

    public static SO_SeedData GetSeed(int id) 
    {
        _lookup.TryGetValue(id, out var data);
        return data;
    }
}