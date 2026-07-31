using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text nameText;
    public TMP_Text dialogueText;

    private int index = 0;

    private readonly string[] speakers =
    {
        "Husband",
        "Wife",
        "Husband",
        "Wife",
        "Husband",
        "Wife",
        "Husband"
    };

    private readonly string[] dialogues =
    {
        "Morning.",
        "Morning. You're finally awake.",
        "Smells good. What's for breakfast?",
        "Just the usual.",
        "After breakfast, I'll go chop some wood.",
        "Don't stay out too late.",
        "I won't."
    };

    public void ShowNextDialogue()
    {
        panel.SetActive(true);

        if (index >= dialogues.Length)
        {
            panel.SetActive(false);
            return;
        }

        nameText.text = speakers[index];
        dialogueText.text = dialogues[index];

        index++;
    }

    public void ResetDialogue()
    {
        index = 0;
        panel.SetActive(false);
    }
    
    public void HideDialogue()
    {
        panel.SetActive(false);
    }
}