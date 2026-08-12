using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class EXP_Manager : MonoBehaviour
{
    public Slider sliderExp;
    public Slider sliderExp2;
    public TextMeshProUGUI ExpText;
    void Update()
    {
        Player_Controller myPlayer = Player_Controller.localPlayer;

        if(sliderExp != null && myPlayer != null) 
        {
            sliderExp.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            sliderExp2.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            ExpText.text = myPlayer.level.ToString();
        }
    }
}
