using UnityEngine;
using TMPro;

public class HintUIManager : MonoBehaviour
{
    public static HintUIManager instance;

    [Header("UI Hướng Dẫn (Kéo cục Huongdan vào đây)")]
    public GameObject huongDanRoot;
    
    [Header("Các Ô Chữ")]
    [Tooltip("Kéo chữ Label_Input (nút bấm) vào đây")]
    public TextMeshProUGUI textKey;
    [Tooltip("Kéo chữ Label_Action (tác dụng) vào đây")]
    public TextMeshProUGUI textAction;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (huongDanRoot == null) huongDanRoot = this.gameObject; HideHint();
    }

    public void ShowHint(string key, string action)
    {
        if (huongDanRoot != null && !huongDanRoot.activeSelf)
        {
            huongDanRoot.SetActive(true);
        }

        if (textKey != null) textKey.text = key;
        if (textAction != null) textAction.text = action;
    }

    public void HideHint()
    {
        if (huongDanRoot != null && huongDanRoot.activeSelf)
        {
            huongDanRoot.SetActive(false);
        }
    }
}
