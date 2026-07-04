using UnityEngine;
using TMPro;

public class GemUI : MonoBehaviour
{
    public TextMeshProUGUI[] txtGold;

    void Update()
    {
        if (Player_Controller.localPlayer != null)
        {
            // ⚠️ Lặp qua từng ô text trong mảng để cập nhật
            for (int i = 0; i < txtGold.Length; i++)
            {
                if (txtGold[i] != null)
                {
                    txtGold[i].text = ": " + Player_Controller.localPlayer.Gold.ToString();
                }
            }
        }
    }
}