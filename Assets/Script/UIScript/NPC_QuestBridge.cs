using UnityEngine;

public class NPC_QuestBridge : MonoBehaviour
{
    // Bắt tín hiệu từ Node Hội thoại và quăng cho Player
    public void GiaoViecChoPlayer(QuestSO quest)
    {
        if (Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.NhanNhiemVu(quest);
        }
    }

    public void ThuHoiViecTuPlayer(QuestSO quest)
    {
        if (Player_QuestManager.localQuest != null)
        {
            Player_QuestManager.localQuest.TraNhiemVu(quest);
        }
    }
}