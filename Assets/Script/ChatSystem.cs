using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections; 

public class ChatSystem : NetworkBehaviour
{
    [Header("Chat Log Settings")]
    public GameObject chatLogItemPrefab; 
    public Transform chatContentParent;  

    [Header("UI Components")]
    public TMP_InputField inputFieldMessage;
    public Button buttonSend; 
    public GameObject ChatSys;
    
    private CanvasGroup chatCanvasGroup;
    private Coroutine fadeCoroutine;

    public static bool IsChatting = false;
    public static ChatSystem LocalInstance;

    private float lastSendTime = 0f; // BỘ ĐẾM CHỐNG KẸT NÚT ENTER

    public override void Spawned()
    {
        LocalInstance = this; 
        IsChatting = false;

        if (ChatSys != null)
        {
            ChatSys.SetActive(true); 
            chatCanvasGroup = ChatSys.GetComponent<CanvasGroup>();
            if (chatCanvasGroup == null) chatCanvasGroup = ChatSys.AddComponent<CanvasGroup>();
        }

        if (inputFieldMessage != null)
        {
            inputFieldMessage.gameObject.SetActive(false);
            // BỎ cái onSubmit đi để tránh đụng độ với nút Enter trong hàm Update
        }

        if (buttonSend != null)
        {
            buttonSend.onClick.AddListener(SendMessageChat);
            buttonSend.gameObject.SetActive(false);
        }
        
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 0f; 
        if (ChatSys != null) ChatSys.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        bool pressEnter = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        bool pressSlash = Keyboard.current.slashKey.wasPressedThisFrame;
        bool pressEsc = Keyboard.current.escapeKey.wasPressedThisFrame;

        if (!IsChatting)
        {
            // Bật chat: Bấm / hoặc Enter (Cách lần gửi cuối 0.2s để không bị lặp nút)
            if ((pressEnter || pressSlash) && Time.time - lastSendTime > 0.2f)
            {
                OpenChat();
            }
        }
        else
        {
            // Đang chat: Bấm Enter để gửi, Esc để tắt
            if (pressEnter)
            {
                SendMessageChat();
            }
            else if (pressEsc)
            {
                CloseChat();
            }
        }
    }

    public void SendMessageChat()
    {
        if (inputFieldMessage == null) return;

        var message = inputFieldMessage.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            string tenHienThi = "Người chơi";
            if (Player_Controller.localPlayer != null)
            {
                Player_Data data = Player_Controller.localPlayer.GetComponent<Player_Data>();
                if (data != null) tenHienThi = data.tenTrenMang.ToString();
            }
            
            var text = $"<color=yellow>{tenHienThi}</color>: <color=white>{message}</color>";
            RpcChat(text);
        }
        
        lastSendTime = Time.time; // Cập nhật thời gian vừa gửi xong
        CloseChat();
    }

    void OpenChat()
    {
        IsChatting = true;
        
        if (ChatSys != null) ChatSys.SetActive(true);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 1f;

        if (inputFieldMessage != null)
        {
            inputFieldMessage.gameObject.SetActive(true);
            inputFieldMessage.ActivateInputField();
        }

        if (buttonSend != null) buttonSend.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseChat()
    {
        IsChatting = false;

        if (inputFieldMessage != null)
        {
            inputFieldMessage.text = ""; 
            inputFieldMessage.gameObject.SetActive(false);
        }

        if (buttonSend != null) buttonSend.gameObject.SetActive(false);
        
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        WakeUpChatUI();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcChat(string message)
    {
        if (LocalInstance != null)
        {
            if (LocalInstance.chatLogItemPrefab != null && LocalInstance.chatContentParent != null)
            {
                GameObject newChatLog = Instantiate(LocalInstance.chatLogItemPrefab, LocalInstance.chatContentParent);
                TextMeshProUGUI chatText = newChatLog.GetComponentInChildren<TextMeshProUGUI>();
                if (chatText != null) chatText.text = message;
            }

            LocalInstance.WakeUpChatUI(); 
        }
    }

    public void WakeUpChatUI()
    {
        if (ChatSys != null) ChatSys.SetActive(true);
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 1f;

        if (!IsChatting)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(4f);
        float duration = 1.5f; 
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            if (chatCanvasGroup != null) chatCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null; 
        }

        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 0f;
        if (ChatSys != null) ChatSys.SetActive(false);
    }
}
