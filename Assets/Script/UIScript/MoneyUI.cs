using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI[] txtGold;

    void Update()
    {
        if (Player_Controller.localPlayer != null)
        {
            for (int i = 0; i < txtGold.Length; i++)
            {
                if (txtGold[i] != null)
                    txtGold[i].text = ": " + Player_Controller.localPlayer.Gold.ToString();
            }
        }
    }

    // ⚠️ CHUYỂN HÀM BÁN ĐỒ SANG ĐÂY
    public void Click_NutBanGo()
    {
        // Kiểm tra xem túi có món đồ ID = 1 (Gỗ) không thì mới bán được nhé!
        if (Player_Controller.localPlayer != null)
        {
            // Gọi nhân vật của mình thực hiện lệnh bán (ID gỗ là 1, giá 10 Gold)
            Player_Controller.localPlayer.RPC_BanVatPham(1, 10);
        }
    }
}