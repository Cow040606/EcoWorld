using UnityEngine;

[CreateAssetMenu(fileName = "TimeSetting", menuName = "TimeSetting")]
public class TimeSetting : ScriptableObject
{
    public float timeMultiplier = 2000f;
    public float startHour = 12f;
    public float sunriseHour = 6f;
    public float sunsetHour = 18f;
}
