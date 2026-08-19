using UnityEngine;

public class LocationTrigger : MonoBehaviour
{
    [Header("Tên Vùng Đất")]
    public string locationName = "Rừng Sương Mù";

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem vật thể chạm vào có nằm trong con trỏ của Player hay không
        // GetComponentInParent giúp tìm lên tới gốc của nhân vật (giải quyết vụ vướng chân leftFoot_trigger)
        Player_Controller player = other.GetComponentInParent<Player_Controller>();

        if (player != null)
        {
            // RẤT QUAN TRỌNG: Kiểm tra xem đây có phải là nhân vật CỦA MÌNH đang điều khiển không.
            // Nếu không có dòng này, người chơi khác chạy vào vùng đất, máy mình cũng sẽ hiện thông báo!
            if (player == Player_Controller.localPlayer)
            {
                if (LocationNotifyManager.Instance != null)
                {
                    LocationNotifyManager.Instance.ShowLocation(locationName);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Player_Controller player = other.GetComponentInParent<Player_Controller>();
        if (player != null && player == Player_Controller.localPlayer)
        {
            if (LocationNotifyManager.Instance != null)
            {
                LocationNotifyManager.Instance.ResetLastLocation();
            }
        }
    }
}
