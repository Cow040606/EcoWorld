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
                {
                    txtGold[i].text = ": " + Player_Controller.localPlayer.Gold.ToString();
                }
            }
        }
    }
}