using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneArcadeHoldController : MonoBehaviour
{
    [Header("Visual rotors (optional)")]
    public DroneRotor[] rotors;

    [Header("Altitude Hold")]
    public bool holdAltitude = true;
    public float targetAltitude = 1.2f;          // altura objetivo inicial (m)
    public float altitudeChangeSpeed = 1.5f;     // m/s con Shift/Ctrl
    public float maxUpAccel = 20f;               // límite aceleración vertical
    public float altitudeKp = 8f;                // P
    public float altitudeKd = 4f;                // D
    public float minAltitude = 0.2f;             // no bajar de aquí

    [Header("Horizontal Movement")]
    public float maxSpeed = 4.0f;                // m/s
    public float accel = 10.0f;                  // aceleración horizontal (m/s^2)
    public float horizontalDamping = 3.0f;       // freno cuando no hay input

    [Header("Rotation / Tilt (visual + heading)")]
    public float yawSpeedDeg = 90f;              // Q/E
    public float maxTiltDeg = 12f;               // inclinación máxima
    public float tiltResponsiveness = 6f;        // rapidez de inclinación

    [Header("Rigidbody Tuning")]
    public float linearDrag = 1.0f;
    public float angularDrag = 3.0f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = linearDrag;
        rb.angularDrag = angularDrag;

        // Inicializa target a la altura actual si prefieres:
        // targetAltitude = Mathf.Max(minAltitude, rb.position.y);
    }

    void Update()
    {
        // Shift/Ctrl ajusta la altura objetivo (altitude hold)
        float up = Input.GetKey(KeyCode.LeftShift) ? 1f : 0f;
        float down = Input.GetKey(KeyCode.LeftControl) ? 1f : 0f;

        if (holdAltitude)
        {
            targetAltitude += (up - down) * altitudeChangeSpeed * Time.deltaTime;
            targetAltitude = Mathf.Max(minAltitude, targetAltitude);
        }

        // Rotores visuales: si quieres que “aceleren” con el esfuerzo, podemos mapearlo simple.
        // Por ahora los dejamos en un valor fijo “bonito” cuando está volando.
        float visualThrottle = holdAltitude ? 0.6f : 0f;
        if (rotors != null)
        {
            for (int i = 0; i < rotors.Length; i++)
                if (rotors[i] != null) rotors[i].SetThrottle(visualThrottle);
        }
    }

    void FixedUpdate()
    {
        float forwardIn =
            (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f) -
            (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);

        float rightIn =
            (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f) -
            (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);

        // Q/E yaw
        float yawIn =
            (Input.GetKey(KeyCode.E) ? 1f : 0f) -
            (Input.GetKey(KeyCode.Q) ? 1f : 0f);

        // 1) Yaw (rumbo)
        if (Mathf.Abs(yawIn) > 0.001f)
        {
            float yawDelta = yawIn * yawSpeedDeg * Time.fixedDeltaTime;
            rb.MoveRotation(Quaternion.AngleAxis(yawDelta, Vector3.up) * rb.rotation);
        }

        // 2) Movimiento horizontal explícito (en el plano XZ)
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 rgt = transform.right; rgt.y = 0f; rgt.Normalize();

        Vector3 desiredVel = (fwd * forwardIn + rgt * rightIn);
        if (desiredVel.sqrMagnitude > 1f) desiredVel.Normalize();
        desiredVel *= maxSpeed;

        Vector3 vel = rb.velocity;
        Vector3 velXZ = new Vector3(vel.x, 0f, vel.z);

        Vector3 velError = desiredVel - velXZ;
        Vector3 accelCmd = Vector3.ClampMagnitude(velError * accel, accel);

        rb.AddForce(accelCmd, ForceMode.Acceleration);

        // Freno si no hay input
        if (Mathf.Abs(forwardIn) < 0.001f && Mathf.Abs(rightIn) < 0.001f)
        {
            Vector3 damp = -velXZ * horizontalDamping;
            rb.AddForce(damp, ForceMode.Acceleration);
        }

        // 3) Altitude hold: controla Y sin depender del tilt
        if (holdAltitude)
        {
            float y = rb.position.y;
            float vy = rb.velocity.y;

            float err = targetAltitude - y;
            float desiredAy = altitudeKp * err - altitudeKd * vy;   // PD
            desiredAy = Mathf.Clamp(desiredAy, -maxUpAccel, maxUpAccel);

            // Fuerza necesaria: F = m*(g + a)
            float forceY = rb.mass * (Physics.gravity.magnitude + desiredAy);
            rb.AddForce(Vector3.up * forceY, ForceMode.Force);
        }

        // 4) Tilt visual según input (solo estética/lectura)
        float targetPitch = +forwardIn * maxTiltDeg;
        float targetRoll = -rightIn * maxTiltDeg;

        Quaternion yawOnly = Quaternion.Euler(0f, rb.rotation.eulerAngles.y, 0f);
        Quaternion tiltLocal = Quaternion.Euler(targetPitch, 0f, targetRoll);
        Quaternion targetRot = yawOnly * tiltLocal;

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, tiltResponsiveness * Time.fixedDeltaTime));
    }
}