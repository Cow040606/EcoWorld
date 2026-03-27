using UnityEngine;

public class SimpleWaterMove : MonoBehaviour
{
    public float speedX = 0.05f;
    public float speedY = 0.03f;

    Renderer rend;
    Vector2 offset;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        offset.x += speedX * Time.deltaTime;
        offset.y += speedY * Time.deltaTime;
        rend.material.SetTextureOffset("_BaseMap", offset);
    }
}