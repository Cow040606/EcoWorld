using UnityEngine;

public class DialogueDatabase : MonoBehaviour
{
    public DialogueUI dialogueUI;

    //================ CUTSCENE 1 ================

    public void LoadCutscene1()
    {
        string[] speakers =
        {
            "Husband",
            "Wife",
            "Husband",
            "Wife",
            "Husband",
            "Wife",
            "Husband"
        };

        string[] dialogues =
        {
            "Morning.",
            "Morning. You're finally awake.",
            "Smells good. What's for breakfast?",
            "Just the usual.",
            "After breakfast, I'll go chop some wood.",
            "Don't stay out too late.",
            "I won't."
        };

        dialogueUI.LoadDialogue(speakers, dialogues);
    }

    //================ CUTSCENE 2 ================

    public void LoadCutscene2()
    {
        string[] speakers =
        {
            "Wife",
            "Husband",
            "Wife",
            "Husband",
            "Wife",
            "Husband",
            "Wife",
            "Husband"
        };

        string[] dialogues =
        {
            "You're back.",
            "Yeah. Took me a little longer than I expected.",
            "I was starting to worry.",
            "Sorry.\nThe trees were farther than usual.",
            "Well... you're home now.\nCome inside.\nDinner's almost ready.",
            "Smells delicious already.",
            "I hope you still say that after you taste it.",
            "I'm sure I will."
        };

        dialogueUI.LoadDialogue(speakers, dialogues);
    }
}