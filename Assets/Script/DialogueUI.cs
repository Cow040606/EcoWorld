using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    private int index = 0;

    private string[] speakers;
    private string[] dialogues;

    private void Start()
    {
        panel.SetActive(false);
    }

    // Load hội thoại mới
    public void LoadDialogue(string[] newSpeakers, string[] newDialogues)
    {
        speakers = newSpeakers;
        dialogues = newDialogues;

        index = 0;
        panel.SetActive(false);
    }

    // Hiện câu tiếp theo
    public void ShowNextDialogue()
    {
        if (speakers == null || dialogues == null)
            return;

        if (index >= dialogues.Length)
        {
            HideDialogue();
            return;
        }

        panel.SetActive(true);

        nameText.text = speakers[index];
        dialogueText.text = dialogues[index];

        index++;
    }

    public void HideDialogue()
    {
        panel.SetActive(false);
    }

    public void ResetDialogue()
    {
        index = 0;
        panel.SetActive(false);
    }
}