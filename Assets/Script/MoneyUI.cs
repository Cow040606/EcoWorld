using UnityEngine;
using TMPro;
using Fusion;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI txtGold;

    void Update()
    {
        // 1. Kiểm tra xem danh sách Runner có tồn tại người nào không (Sửa lỗi sập game)
        if (NetworkRunner.Instances.Count == 0) return;

        var runner = NetworkRunner.Instances[0];
        
        // 2. Chắc chắn Runner đang chạy thì mới lấy dữ liệu
        if (runner != null && runner.IsRunning)
        {
            // Nhờ bước "Đăng ký hộ khẩu" bên kia, hàm này giờ sẽ lấy đúng nhân vật!
            NetworkObject myPlayer = runner.GetPlayerObject(runner.LocalPlayer);

            if (myPlayer != null)
            {
                int currentGold = myPlayer.GetComponent<Player_Controller>().Gold;
                txtGold.text = currentGold.ToString(); // Hiện số lên màn hình
            }
        }
    }
}