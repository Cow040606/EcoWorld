using UnityEngine;

public class WaterScroll : MonoBehaviour
{
    public float normalSpeed = 0.1f;
    public float baseSpeed = 0.05f;

    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        float baseOffset = Time.time * baseSpeed;
        float normalOffset = Time.time * normalSpeed;

        mat.SetTextureOffset("_BaseMap", new Vector2(baseOffset, 0));
        mat.SetTextureOffset("_BumpMap", new Vector2(0, normalOffset));
    }
}