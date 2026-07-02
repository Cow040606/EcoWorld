using UnityEngine;
using TMPro;

public class GemUI : MonoBehaviour
{
    public TextMeshProUGUI[] txtGem;

    void Update()
    {
        if (Player_Controller.localPlayer != null)
        {
            for (int i = 0; i < txtGem.Length; i++)
            {
                if (txtGem[i] != null)
                {
                    txtGem[i].text = ": " + Player_Controller.localPlayer.Gem.ToString();
                }
            }
        }
    }
}