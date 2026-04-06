using UnityEngine;

public class ItemNotifySlot : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private float timer = 0;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Destroy(gameObject, 3.5f); // Tổng thời gian sống
    }

    void Update()
    {
        timer += Time.deltaTime;
        // Nếu sống được hơn 2.5 giây thì bắt đầu mờ dần
        if (timer > 2.5f)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, 0, Time.deltaTime * 5f);
        }
    }
}