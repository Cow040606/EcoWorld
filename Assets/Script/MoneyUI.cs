using UnityEngine;
using TMPro;
using Fusion;

public class MoneyUI : MonoBehaviour
{
    public TextMeshProUGUI txtGold;

    void Update()
    {
        // Ch? ch?y n?u mình là ng??i ch?i Local
        if (NetworkRunner.Instances[0] == null) return;

        var runner = NetworkRunner.Instances[0];
        NetworkObject myPlayer = runner.GetPlayerObject(runner.LocalPlayer);

        if (myPlayer != null)
        {
            // L?y component Player_Controller ?? ??c bi?n Gold
            int currentGold = myPlayer.GetComponent<Player_Controller>().Gold;
            txtGold.text = currentGold.ToString(); // Hi?n s? lên màn hình
        }
    }
}