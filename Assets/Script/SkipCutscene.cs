using UnityEngine;
using UnityEngine.Playables;

public class SkipCutscene : MonoBehaviour
{
    public PlayableDirector director;
    public DialogueUI dialogueUI;

    public GameObject gameplayCamera;
    public GameObject cutsceneCamera;

    public GameObject player;

    public void Skip()
    {
        // nhảy tới cuối Timeline
        director.time = director.duration;
        director.Evaluate();

        // dừng Timeline
        director.Stop();

        // ẩn hội thoại
        dialogueUI.HideDialogue();

        // đổi camera
        gameplayCamera.SetActive(true);
        cutsceneCamera.SetActive(false);

        // trả quyền điều khiển
        player.GetComponent<PlayerMovement>().enabled = true;
    }
}