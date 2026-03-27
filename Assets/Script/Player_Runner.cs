using Fusion;
using UnityEngine;

// Kế thừa NetworkBehaviour để xài được hàm Spawned siêu xịn
// public class Player_Runner : NetworkBehaviour, IPlayerJoined
public class Player_Runner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] 
    NetworkPrefabRef playerPrefab; 
    NetworkPrefabRef enemyPrefab; 
    public GameObject spawn;
    public GameObject spawn1;
    public GameObject spawn2;
    public GameObject spawnenemy;
    

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer) 
        {
            GameObject[] danhSachSpawn = { spawn, spawn1, spawn2 };
            int viTriNgauNhien = Random.Range(0, danhSachSpawn.Length); 
            GameObject diemSpawnDuocChon = danhSachSpawn[viTriNgauNhien];
            
            Vector3 vitrispawn = diemSpawnDuocChon != null ? diemSpawnDuocChon.transform.position : Vector3.zero;
            Runner.Spawn(playerPrefab, vitrispawn, Quaternion.identity, player);

            if (Runner.IsSharedModeMasterClient)
            {
                Vector3 viTriSpawnQuai = spawnenemy != null ? spawnenemy.transform.position : Vector3.up * 5;
                Runner.Spawn(enemyPrefab, viTriSpawnQuai, Quaternion.identity, null);
            }
        }
    }




    
    // public void PlayerJoined(PlayerRef player)
    // {
    //     // 1. TỰ ĐẺ CHO CHÍNH MÌNH (Local Player)
    //     if (player == Runner.LocalPlayer) 
    //     {
    //         Vector3 vitrispawn = spawn != null ? spawn.transform.position : Vector3.zero;
            
    //         // Lệnh đẻ nhân vật
    //         Runner.Spawn(playerPrefab, vitrispawn, Quaternion.identity, player);
    //         Debug.Log("Đã đẻ Player thành công!");

    //         // 2. CHỈ CHỦ PHÒNG (Master Client) MỚI ĐẺ QUÁI
    //         // Để tránh tình trạng 2 người vào đẻ ra 2 con quái trùng nhau
    //         if (Runner.IsSharedModeMasterClient)
    //         {
    //             Vector3 viTriSpawnQuai = spawnenemy != null ? spawnenemy.transform.position : Vector3.up * 5;
    //             Runner.Spawn(enemyPrefab, viTriSpawnQuai, Quaternion.identity, null);
    //             Debug.Log("Chủ phòng đã đẻ quái!");
    //         }
    //     }
    // }
    


    // public override void Spawned()
    // {
    //     if (Object.HasStateAuthority) 
    //     {
    //         Vector3 vitrispawn = spawn.transform.position;
    //         Vector3 viTriSpawnQuai = spawnenemy.transform.position;
    //         Runner.Spawn(playerPrefab, vitrispawn, Quaternion.identity, Runner.LocalPlayer);
    //         Runner.Spawn(enemyPrefab, viTriSpawnQuai, Quaternion.identity, null);
    //     }
    // }


    // =======================================================
    // 2. CHẠY KHI CÓ KHÁCH VÀO PHÒNG SAU (Đẻ cho thằng bạn)
    // =======================================================
    // public void PlayerJoined(PlayerRef player)
    // {
    //     if (Object.HasStateAuthority) 
    //     {
    //         if (player != Runner.LocalPlayer) 
    //         {
    //             Vector3 vitrispawn = spawn.transform.position;  
    //             Runner.Spawn(playerPrefab, vitrispawn, Quaternion.identity, player);
    //         }
    //     }
    // }
}