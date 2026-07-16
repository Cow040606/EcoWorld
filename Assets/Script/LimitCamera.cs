using UnityEngine;

public class LimitCamera : MonoBehaviour
{
    public Transform Player;
    public float Height = 30f;

    private void LateUpdate()
    {
        // 1. Nếu Player bị trống (chưa gán hoặc nhân vật vừa bị hủy)
        if (Player == null)
        {
            // Tự động tìm nhân vật của người chơi trên mạng (localPlayer)
            if (Player_Controller.localPlayer != null)
            {
                Player = Player_Controller.localPlayer.transform;
            }
            else
            {
                // Nếu nhân vật chưa kịp Spawn ra, thì thoát hàm để không bị báo lỗi đỏ
                return; 
            }
        }

        // 2. Chạy logic bám theo nhân vật
        transform.position = Player.position + Vector3.up * Height;
    }
}