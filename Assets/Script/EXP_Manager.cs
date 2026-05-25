using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class EXP_Manager : MonoBehaviour
{
    public Slider sliderExp;
    public TextMeshProUGUI ExpText;
    void Update()
    {
        Player_Controller myPlayer = Player_Controller.localPlayer;

        if(sliderExp != null && myPlayer != null) 
        {
            sliderExp.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            ExpText.text = " Lvl " +myPlayer.level.ToString();
        }
        else
        {
            //Debug.LogWarning("Slider hoặc Player_Controller chưa được gán!");
        }
    }
}
