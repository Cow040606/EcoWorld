using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI txtGold;

    void Update()
    {
        // Gọi thẳng tên Chủ tịch từ danh bạ VIP ra xài, không trượt đi đâu được!
        if (Player_Controller.localPlayer != null)
        {
            // Lu ít thêm dấu ": " vào cho nó giống thiết kế ban đầu của Bò nhé
            txtGold.text = ": " + Player_Controller.localPlayer.Gold.ToString();
        }
    }
}