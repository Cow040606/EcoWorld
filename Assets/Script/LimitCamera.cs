using UnityEngine;

public class LimitCamera : MonoBehaviour
{
    public Transform Player;
    public float Height = 30f;

    private void LateUpdate()
    {
        transform.position = Player.position + Vector3.up * Height;
    }
}