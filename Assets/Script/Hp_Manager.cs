using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Hp_Manager : MonoBehaviour
{
    public Slider sliderhp;
    public TextMeshProUGUI HpText;

    void Update()
    {
        Player_Controller myPlayer = Player_Controller.localPlayer;

        if(sliderhp != null && myPlayer != null) 
        {
            sliderhp.value = myPlayer.CurrentHealth / myPlayer.MaxHealth;
            HpText.text = myPlayer.CurrentHealth +" / " + myPlayer.MaxHealth ;
        }
        else
        {
            // Lúc mới bật game nhân vật chưa kịp Spawn ra thì nó sẽ chạy vào đây
            // Bò không nên để Debug.Log ở đây vì Update chạy 60 lần/giây, nó sẽ spam lag banh Console đó!
        }
    }
}