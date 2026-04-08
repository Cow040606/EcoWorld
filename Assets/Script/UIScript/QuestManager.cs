using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static QuestManager instance;
    public GameObject khungnhiemvu;
    public bool isQuest_Open; 
    public GameObject txtBangNhiemVu;


    void Awake()
    {
        if (instance == null) instance = this;
    }



    void Start()
    {
        if (khungnhiemvu != null) khungnhiemvu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Battatbangnhiemvu()
    {
        isQuest_Open = !isQuest_Open;
        
        if (khungnhiemvu != null) 
        {
            khungnhiemvu.SetActive(isQuest_Open);
        }
        if (isQuest_Open)
        {
            Debug.Log("Đang mở bảng lên! Chạy code vẽ item trong Balo ra đây...");
        }
    }
}
