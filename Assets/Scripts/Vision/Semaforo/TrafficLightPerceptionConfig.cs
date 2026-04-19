using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TrafficLightRouteROI
{
    public string name = "ROI Ruta";
    public int fromPoseId;
    public int toPoseId;

    [Tooltip("ROI normalizada en pantalla (0..1)")]
    public Rect normalizedROI = new Rect(0.3f, 0.1f, 0.3f, 0.4f);
}

public class TrafficLightPerceptionConfig : MonoBehaviour
{
    public List<TrafficLightRouteROI> routeROIs = new List<TrafficLightRouteROI>();

    public bool TryGetROI(int fromPoseId, int toPoseId, out Rect roi)
    {
        for (int i = 0; i < routeROIs.Count; i++)
        {
            if (routeROIs[i].fromPoseId == fromPoseId && routeROIs[i].toPoseId == toPoseId)
            {
                roi = routeROIs[i].normalizedROI;
                return true;
            }
        }

        roi = default;
        return false;
    }
}