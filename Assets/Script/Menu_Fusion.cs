// using Fusion;
// using UnityEngine;
// using System.Threading.Tasks;
// using TMPro;
// using Fusion.Photon.Realtime; // Bắt buộc phải có dòng này!

// public class Menu_Fusion : MonoBehaviour
// {
//     private NetworkRunner runner;
//     public TMP_InputField inputSessionName;
//     public GameObject coopPlayer;
//     public GameObject menu;

//     async void KetNoi(GameMode cheDo)
//     {
//         // 1. ĐUỔI VIỆC ANH CŨ ĐÚNG QUY TRÌNH (QUAN TRỌNG NHẤT)
//         if (runner != null)
//         {
//             // Phải Shutdown để Server xóa tên mình ra khỏi phòng cũ
//             await runner.Shutdown(); 
//             Destroy(runner.gameObject);
//         }

//         // Kiểm tra xem Bò có quên nhập tên không
//         if (string.IsNullOrEmpty(inputSessionName.text))
//         {
//             Debug.LogError("<color=red>Bà mụ:</color> Bò ơi, gõ cái tên phòng vào đã!");
//             return;
//         }

//         // 2. CẤP "CHỨNG MINH NHÂN DÂN" GIẢ (GUID)
//         // Dòng này giúp Server phân biệt 2 cửa sổ trên cùng 1 máy Bò
//         var idRieng = new AuthenticationValues(System.Guid.NewGuid().ToString());

//         // 3. THUÊ ANH MỚI
//         GameObject runnerObject = new GameObject("TienTrinhFusion");
//         runner = runnerObject.AddComponent<NetworkRunner>();

//         Debug.Log("<color=green>Bà mụ:</color> Đang phi vào phòng: " + inputSessionName.text.Trim());

//         // 4. RA LỆNH KẾT NỐI
//         await runner.StartGame(new StartGameArgs()
//         {
//             GameMode = cheDo,
//             SessionName = inputSessionName.text.Trim(),
//             AuthValues = idRieng, // <--- CHIÊU CUỐI FIX LỖI 104
//             Scene = SceneRef.FromIndex(1),
//             SceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>()
//         });
//     }

//     public void BamNut_ChoiDon() { KetNoi(GameMode.Single); }
//     public void BamNut_TaoPhong() { KetNoi(GameMode.Host); }
//     public void BamNut_Coop() { coopPlayer.SetActive(true); 
//         menu.SetActive(false); } 
//         public void BamNut_Menu() { coopPlayer.SetActive(false); 
//         menu.SetActive(true); } 
//     public void BamNut_VaoPhong() { KetNoi(GameMode.Client); }
//     public void BamNut_VaoNhanh() { KetNoi(GameMode.AutoHostOrClient); }
// }