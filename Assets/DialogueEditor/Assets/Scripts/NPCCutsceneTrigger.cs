using UnityEngine;
using UnityEngine.Playables;

public class NPCCutsceneTrigger : MonoBehaviour
{
    public TimeManager timeManager;
    
    [Header("Danh sách Cutscene")]
    public PlayableDirector cutscene2; 
    public PlayableDirector cutscene3; 

    // Gọi hàm này khi trả xong nhiệm vụ
    public void PlayStoryCutscenes()
    {
        // Đổi trời tối
        if (timeManager != null) timeManager.SetNightForCutscene();

        // Chạy Cutscene 2 trước
        if (cutscene2 != null)
        {
            cutscene2.Play();
        }
    }
}