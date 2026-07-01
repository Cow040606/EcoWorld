using UnityEngine;
using TMPro;

public class GemUI : MonoBehaviour
{
    public TextMeshProUGUI[] txtGold;

    void Update()
    {
        if (Player_Controller.localPlayer != null)
        {
            // Đổi txtGem.Length thành txtGold.Length cho đúng với biến khai báo ở trên
            for (int i = 0; i < txtGold.Length; i++)
            {
                // Thêm [i] để chỉ định đúng phần tử trong mảng
                txtGold[i].text = ": " + Player_Controller.localPlayer.Gold.ToString();
            }
        }
    }
}