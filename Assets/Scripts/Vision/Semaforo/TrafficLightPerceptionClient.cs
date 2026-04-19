using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;

public class TrafficLightPerceptionClient : MonoBehaviour
{
    [Serializable]
    public class TrafficLightData
    {
        public bool detected;
        public string state;
        public float confidence;
        public float[] bbox;
    }

    [Serializable]
    public class TrafficLightResponse
    {
        public int green;
        public TrafficLightData traffic_light;
        public string description;
    }

    [Header("Debug ROI")]
    public bool showDebugROI = true;
    public int debugFromPoseId = 1;
    public int debugToPoseId = 2;
    public Color debugROIColor = Color.cyan;

    public Camera agentCamera;
    public RouteMover routeMover;
    public TrafficLightPerceptionConfig perceptionConfig;
    public TrafficLightOverlayUI overlayUI;
    public RouteMoverUI routeMoverUI;

    [Header("HTTP")]
    public string endpoint = "http://127.0.0.1:8000/analyze_traffic_light";
    public float intervalSeconds = 0.2f;
    public int captureWidth = 640;
    public int captureHeight = 360;
    public int jpgQuality = 75;

    [Header("Control")]
    public bool enablePerception = true;

    private float timer = 0f;
    private bool requestInFlight = false;
    private Texture2D captureTexture;
    private RenderTexture captureRT;

    private void Start()
    {
        captureRT = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
        captureTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);

        if (overlayUI != null)
            overlayUI.Hide();
    }

    private void Update()
    {
        if (!enablePerception || agentCamera == null || routeMover == null || perceptionConfig == null)
            return;

        if (requestInFlight)
            return;

        if (routeMover.requestedDestinationPoseId < 0)
        {
            if (overlayUI != null) overlayUI.Hide();
            return;
        }

        // SOLO percibir cuando el punto actual de la ruta es un punto de pausa
        // o cuando ya está efectivamente pausado esperando semáforo.
        bool shouldPerceive =
            routeMover.IsCurrentTargetPausePoint() ||
            routeMover.isPausedByGreen;

        if (!shouldPerceive)
        {
            timer = 0f;
            if (overlayUI != null) overlayUI.Hide();
            return;
        }

        int fromPoseId = routeMover.currentPoseId;
        int toPoseId = routeMover.requestedDestinationPoseId;

        if (!perceptionConfig.TryGetROI(fromPoseId, toPoseId, out Rect roi))
        {
            if (overlayUI != null) overlayUI.Hide();
            return;
        }

        timer += Time.deltaTime;
        if (timer < intervalSeconds)
            return;

        timer = 0f;

        StartCoroutine(SendFrameCoroutine(fromPoseId, toPoseId, roi));
    }

    private IEnumerator SendFrameCoroutine(int fromPoseId, int toPoseId, Rect roi)
    {
        requestInFlight = true;

        byte[] imageBytes = CaptureCameraJPG();

        WWWForm form = new WWWForm();
        form.AddField("from_pose_id", fromPoseId);
        form.AddField("to_pose_id", toPoseId);
        form.AddField("roi_x", roi.x.ToString(CultureInfo.InvariantCulture));
        form.AddField("roi_y", roi.y.ToString(CultureInfo.InvariantCulture));
        form.AddField("roi_w", roi.width.ToString(CultureInfo.InvariantCulture));
        form.AddField("roi_h", roi.height.ToString(CultureInfo.InvariantCulture));
        form.AddBinaryData("image", imageBytes, "frame.jpg", "image/jpeg");

        using UnityWebRequest req = UnityWebRequest.Post(endpoint, form);
        req.timeout = 5;

        yield return req.SendWebRequest();

        requestInFlight = false;

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("Perception HTTP error: " + req.error);
            if (overlayUI != null) overlayUI.Hide();
            yield break;
        }

        string json = req.downloadHandler.text;

        TrafficLightResponse response = null;
        try
        {
            response = JsonUtility.FromJson<TrafficLightResponse>(json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("JSON parse error: " + e.Message + "\n" + json);
            if (overlayUI != null) overlayUI.Hide();
            yield break;
        }

        if (response == null || response.traffic_light == null)
        {
            if (overlayUI != null) overlayUI.Hide();
            yield break;
        }

        Debug.Log(
            $"TrafficLight state={response.traffic_light.state}, " +
            $"green={response.green}, " +
            $"detected={response.traffic_light.detected}, " +
            $"conf={response.traffic_light.confidence}"
        );

        Debug.Log(
            $"Sending ROI from={fromPoseId} to={toPoseId} " +
            $"x={roi.x:F3} y={roi.y:F3} w={roi.width:F3} h={roi.height:F3}"
        );

        // Actualiza siempre el valor que RouteMover usa para decidir en el pause point
        routeMover.Verde = response.green;

        if (response.traffic_light.detected &&
            response.traffic_light.bbox != null &&
            response.traffic_light.bbox.Length == 4)
        {
            Rect bbox = Rect.MinMaxRect(
                response.traffic_light.bbox[0],
                response.traffic_light.bbox[1],
                response.traffic_light.bbox[2],
                response.traffic_light.bbox[3]
            );

            if (overlayUI != null)
                overlayUI.Show(bbox, response.traffic_light.state);
        }
        else
        {
            if (overlayUI != null)
                overlayUI.Hide();
        }
    }

    private byte[] CaptureCameraJPG()
    {
        RenderTexture previousRT = RenderTexture.active;
        RenderTexture previousTarget = agentCamera.targetTexture;

        agentCamera.targetTexture = captureRT;
        agentCamera.Render();

        RenderTexture.active = captureRT;
        captureTexture.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
        captureTexture.Apply();

        agentCamera.targetTexture = previousTarget;
        RenderTexture.active = previousRT;

        return captureTexture.EncodeToJPG(jpgQuality);
    }

    private void OnDestroy()
    {
        if (captureRT != null) captureRT.Release();
    }

    private void OnDrawGizmos()
    {
        if (!showDebugROI || agentCamera == null || perceptionConfig == null)
            return;

        if (!perceptionConfig.TryGetROI(debugFromPoseId, debugToPoseId, out Rect roi))
            return;

        DrawViewportRectGizmo(agentCamera, roi, debugROIColor);
    }

    private void DrawViewportRectGizmo(Camera cam, Rect normalizedRect, Color color)
    {
        Vector3 p1 = cam.ViewportToWorldPoint(new Vector3(normalizedRect.xMin, 1f - normalizedRect.yMin, cam.nearClipPlane + 0.05f));
        Vector3 p2 = cam.ViewportToWorldPoint(new Vector3(normalizedRect.xMax, 1f - normalizedRect.yMin, cam.nearClipPlane + 0.05f));
        Vector3 p3 = cam.ViewportToWorldPoint(new Vector3(normalizedRect.xMax, 1f - normalizedRect.yMax, cam.nearClipPlane + 0.05f));
        Vector3 p4 = cam.ViewportToWorldPoint(new Vector3(normalizedRect.xMin, 1f - normalizedRect.yMax, cam.nearClipPlane + 0.05f));

        Gizmos.color = color;
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
}