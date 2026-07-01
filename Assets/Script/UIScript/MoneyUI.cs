using UnityEngine;
using TMPro;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI[] txtGem; 

    void Update()
    {
        if (Player_Controller.localPlayer != null)
        {
            for (int i = 0; i < txtGem.Length; i++)
            {
                // Thêm [i] để chỉ định đúng phần tử trong mảng
                txtGem[i].text = ": " + Player_Controller.localPlayer.Gem.ToString();
            }
        }
    }
}