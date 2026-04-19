using UnityEngine;

public class DroneRotor : MonoBehaviour
{
    [Header("Visual")]
    public float maxRpm = 2500f;
    public bool clockwise = true;

    [Header("Runtime (read-only)")]
    [Range(0f, 1f)]
    public float throttle01; // 0..1

    public float CurrentRpm => throttle01 * maxRpm;

    void Update()
    {
        // Rotación visual sobre su propio eje Y (local)
        float dir = clockwise ? 1f : -1f;
        float degPerSec = (CurrentRpm / 60f) * 360f;
        transform.Rotate(0f, dir * degPerSec * Time.deltaTime, 0f, Space.Self);
    }

    public void SetThrottle(float t01)
    {
        throttle01 = Mathf.Clamp01(t01);
    }
}
