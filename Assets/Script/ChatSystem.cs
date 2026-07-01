using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections; // BẮT BUỘC THÊM ĐỂ DÙNG HIỆU ỨNG THỜI GIAN (COROUTINE)

public class ChatSystem : NetworkBehaviour
{
    [Header("UI Components")]
    
    public TextMeshProUGUI textMessage;
    public TMP_InputField inputFieldMessage;
    public Button buttonSend;
    public GameObject ChatSys;
    
    // Biến quản lý hiệu ứng làm mờ
    private CanvasGroup chatCanvasGroup;
    private Coroutine fadeCoroutine;

    // Biến tĩnh dùng chung toàn game
    public static bool IsChatting = false;
    
    // Bản sao CỤC BỘ của máy Bò, giúp xử lý tin nhắn từ mọi người mượt mà nhất
    public static ChatSystem LocalInstance;

    public override void Spawned()
    {
        
        if (HasInputAuthority)
        {
            ChatSys.SetActive(true);
            LocalInstance = this; // Khẳng định: Đây là UI trên màn hình của mình!
            IsChatting = false;

            ChatSys = GameObject.Find("ChatPanel");
            
            // TỰ ĐỘNG THÊM CANVAS GROUP (Vũ khí làm mờ UI)
            if (ChatSys != null)
            {
                chatCanvasGroup = ChatSys.GetComponent<CanvasGroup>();
                if (chatCanvasGroup == null) chatCanvasGroup = ChatSys.AddComponent<CanvasGroup>();
            }

            textMessage = GameObject.Find("TextMessage").GetComponent<TextMeshProUGUI>();
            inputFieldMessage = GameObject.Find("InputField Message").GetComponent<TMP_InputField>();
            buttonSend = GameObject.Find("Send").GetComponent<Button>();

            buttonSend.onClick.AddListener(SendMessageChat);

            // Bắt đầu game: Chỉ TẮT Ô NHẬP LIỆU để không che màn hình, nhưng KHÔNG TẮT PANEL
            inputFieldMessage.gameObject.SetActive(false);
            buttonSend.gameObject.SetActive(false);
            
            // Ép tàng hình khung chat ngay từ đầu
            if (chatCanvasGroup != null) chatCanvasGroup.alpha = 0f; 
            ChatSys.SetActive(false);
        }
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        // Bật chat (/)
        if (!IsChatting && Keyboard.current != null && Keyboard.current.slashKey.wasPressedThisFrame)
        {
            OpenChat();
        }
        // Gửi bằng Enter
        else if (IsChatting && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            SendMessageChat();
        }
        // Tắt bằng Escape
        else if (IsChatting && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseChat();
        }
    }

    public void SendMessageChat()
    {
        var message = inputFieldMessage.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            Player_Data data = GetComponent<Player_Data>();
            string tenHienThi = (data != null) ? data.tenTrenMang.ToString() : "Người chơi";
    
            var text = $"<color=yellow>{tenHienThi}</color>: <color=white>{message}</color>";
            RpcChat(text);
        }
        
        CloseChat();
    }

    void OpenChat()
    {
        IsChatting = true;
        ChatSys.SetActive(true);
        // Sáng 100% ngay lập tức
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 1f;

        // CHỈ HIỆN 2 Ô NHẬP LIỆU
        inputFieldMessage.gameObject.SetActive(true);
        buttonSend.gameObject.SetActive(true);
        inputFieldMessage.ActivateInputField();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseChat()
    {
        IsChatting = false;
        inputFieldMessage.text = ""; 

        // CHỈ ẨN 2 Ô NHẬP LIỆU - Giữ nguyên ChatPanel để đọc chữ
        inputFieldMessage.gameObject.SetActive(false);
        buttonSend.gameObject.SetActive(false);
        
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Kích hoạt đồng hồ đếm ngược 4s
        WakeUpChatUI();
    }

    // ===============================================
    // PHẦN XỬ LÝ MẠNG VÀ HIỆU ỨNG
    // ===============================================

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcChat(string message)
    {
        // Khi Server gửi lệnh này về mọi máy, ta nhờ LocalInstance trên mỗi máy tự in ra màn hình
        // Điều này ngăn chặn việc 1 tin nhắn bị in ra 10 lần nếu có 10 người chơi
        if (LocalInstance != null && LocalInstance.textMessage != null)
        {
            LocalInstance.textMessage.text += message + "\n";
            LocalInstance.WakeUpChatUI(); // Đánh thức UI sáng lên
        }
    }

    // Hàm gọi khung chat sáng lên rồi tự mờ
    public void WakeUpChatUI()
    {
        // Sáng rực rỡ
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 1f;

        // Nếu KHÔNG BẬT KHUNG GÕ CHỮ thì mới cho phép đếm ngược mờ đi
        if (!IsChatting)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutRoutine());
        }
    }

    // Luồng thời gian song song (Coroutine)
    private IEnumerator FadeOutRoutine()
    {
        // Đứng chờ 4 giây cho người ta đọc tin nhắn
        yield return new WaitForSeconds(4f);

        float duration = 1.5f; // Thời gian hiệu ứng mờ dần (1.5 giây)
        float time = 0f;

        // Vòng lặp từ từ giảm độ Alpha từ 1 về 0
        while (time < duration)
        {
            time += Time.deltaTime;
            if (chatCanvasGroup != null)
            {
                // Lerp giúp giảm số đều và mượt mà
                chatCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            }
            yield return null; // Đợi khung hình tiếp theo
        }

        // Chốt hạ tàng hình 100%
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 0f;
        ChatSys.SetActive(false);
    }
}