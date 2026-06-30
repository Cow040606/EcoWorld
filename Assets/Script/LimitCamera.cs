using UnityEngine;

public class LimitCamera : MonoBehaviour
{
    public GameObject Player;

    void LateUpdate()
    {
        transform.position = new Vector3(
    Player.transform.position.x,
    Player.transform.position.y + 15,
    Player.transform.position.z

        );

        Debug.Log("Player: " + Player.transform.position);
        Debug.Log("Camera: " + transform.position);
    }
}