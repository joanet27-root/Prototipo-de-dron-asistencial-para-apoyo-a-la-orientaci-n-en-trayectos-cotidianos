using System.Collections.Generic;
using UnityEngine;

public class RouteMover : MonoBehaviour
{
    [System.Serializable]
    public class PoseData
    {
        public string poseName = "Nueva Pose";
        public int poseId = 1;
        public Vector3 position;
        public Vector3 rotationEuler;
    }

    [System.Serializable]
    public class RoutePoint
    {
        public string pointName = "Punto";
        public Vector3 position;
        public Vector3 rotationEuler;

        [Header("Pausa")]
        public bool isPausePoint = false;

        [Header("Precisión")]
        public float positionTolerance = 0.01f;
        public float rotationTolerance = 0.5f;
    }

    [System.Serializable]
    public class RouteData
    {
        public string routeName = "Nueva Ruta";
        public int fromPoseId = 1;
        public int toPoseId = 2;
        public List<RoutePoint> points = new List<RoutePoint>();
    }

    [Header("Poses")]
    public List<PoseData> poses = new List<PoseData>();

    [Header("Rutas")]
    public List<RouteData> routes = new List<RouteData>();

    [Header("Movimiento global")]
    [Tooltip("Velocidad lineal constante para todos los puntos")]
    public float moveSpeed = 1.0f;

    [Tooltip("Velocidad angular constante en grados por segundo")]
    public float rotationSpeed = 180f;

    [Header("Control")]
    public int currentPoseId = 1;
    public int Verde = 1;

    [Tooltip("Si está suficientemente cerca de la pose destino, se considera que ya está ahí")]
    public float poseSnapDistance = 0.05f;

    [Tooltip("Ángulo máximo para considerar que ya está en la rotación de la pose")]
    public float poseSnapAngle = 2f;

    [Header("Teclas directas")]
    public bool enableKeyboardInput = true;
    public KeyCode key1 = KeyCode.Alpha1;
    public KeyCode key2 = KeyCode.Alpha2;
    public KeyCode key3 = KeyCode.Alpha3;
    public KeyCode key4 = KeyCode.Alpha4;
    public KeyCode key5 = KeyCode.Alpha5;
    public KeyCode key6 = KeyCode.Alpha6;
    public KeyCode key7 = KeyCode.Alpha7;
    public KeyCode key8 = KeyCode.Alpha8;
    public KeyCode key9 = KeyCode.Alpha9;

    [Header("Estado runtime")]
    public bool isMoving = false;
    public bool isPausedByGreen = false;
    public int currentRouteIndex = -1;
    public int currentPointIndex = -1;
    public int requestedDestinationPoseId = -1;

    private RouteData activeRoute;

    private void Update()
    {
        HandleKeyboardInput();

        if (!isMoving || activeRoute == null)
            return;

        if (currentPointIndex < 0 || currentPointIndex >= activeRoute.points.Count)
        {
            FinishRoute();
            return;
        }

        RoutePoint point = activeRoute.points[currentPointIndex];

        if (isPausedByGreen)
        {
            if (Verde == 1)
            {
                isPausedByGreen = false;
                AdvanceToNextPoint();
            }
            return;
        }

        MoveToPoint(point);
    }

    private void HandleKeyboardInput()
    {
        if (!enableKeyboardInput)
            return;

        if (Input.GetKeyDown(key1)) RequestGoToPose(1);
        if (Input.GetKeyDown(key2)) RequestGoToPose(2);
        if (Input.GetKeyDown(key3)) RequestGoToPose(3);
        if (Input.GetKeyDown(key4)) RequestGoToPose(4);
        if (Input.GetKeyDown(key5)) RequestGoToPose(5);
        if (Input.GetKeyDown(key6)) RequestGoToPose(6);
        if (Input.GetKeyDown(key7)) RequestGoToPose(7);
        if (Input.GetKeyDown(key8)) RequestGoToPose(8);
        if (Input.GetKeyDown(key9)) RequestGoToPose(9);
    }

    public void RequestGoToPose(int destinationPoseId)
    {
        PoseData destinationPose = GetPoseById(destinationPoseId);
        if (destinationPose == null)
        {
            Debug.LogWarning($"No existe una pose con poseId = {destinationPoseId}");
            return;
        }

        if (IsAlreadyAtPose(destinationPose))
        {
            currentPoseId = destinationPoseId;
            StopMovement();
            return;
        }

        if (currentPoseId == destinationPoseId)
        {
            StopMovement();
            return;
        }

        RouteData route = GetRoute(currentPoseId, destinationPoseId);
        if (route == null)
        {
            Debug.LogWarning($"No existe ruta desde pose {currentPoseId} hasta pose {destinationPoseId}");
            return;
        }

        if (route.points == null || route.points.Count == 0)
        {
            Debug.LogWarning($"La ruta '{route.routeName}' no tiene puntos.");
            return;
        }

        StartRoute(route);
    }

    public void StartRoute(RouteData route)
    {
        if (route == null || route.points == null || route.points.Count == 0)
            return;

        activeRoute = route;
        currentRouteIndex = routes.IndexOf(route);
        currentPointIndex = 0;
        requestedDestinationPoseId = route.toPoseId;
        isMoving = true;
        isPausedByGreen = false;
    }

    public void StopMovement()
    {
        isMoving = false;
        isPausedByGreen = false;
        activeRoute = null;
        currentRouteIndex = -1;
        currentPointIndex = -1;
        requestedDestinationPoseId = -1;
    }

    private void MoveToPoint(RoutePoint point)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            point.position,
            moveSpeed * Time.deltaTime
        );

        Quaternion targetRotation = Quaternion.Euler(point.rotationEuler);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        bool posReached = Vector3.Distance(transform.position, point.position) <= point.positionTolerance;
        bool rotReached = Quaternion.Angle(transform.rotation, targetRotation) <= point.rotationTolerance;

        if (posReached && rotReached)
        {
            transform.position = point.position;
            transform.rotation = targetRotation;

            if (point.isPausePoint)
            {
                if (Verde == 1)
                {
                    AdvanceToNextPoint();
                }
                else
                {
                    isPausedByGreen = true;
                }
            }
            else
            {
                AdvanceToNextPoint();
            }
        }
    }

    private void AdvanceToNextPoint()
    {
        currentPointIndex++;

        if (activeRoute == null)
            return;

        if (currentPointIndex >= activeRoute.points.Count)
        {
            FinishRoute();
        }
    }

    private void FinishRoute()
    {
        if (activeRoute != null)
        {
            PoseData destinationPose = GetPoseById(activeRoute.toPoseId);
            if (destinationPose != null)
            {
                transform.position = destinationPose.position;
                transform.rotation = Quaternion.Euler(destinationPose.rotationEuler);
            }

            currentPoseId = activeRoute.toPoseId;
        }

        StopMovement();
    }

    public RouteData GetRoute(int fromPoseId, int toPoseId)
    {
        for (int i = 0; i < routes.Count; i++)
        {
            if (routes[i].fromPoseId == fromPoseId && routes[i].toPoseId == toPoseId)
                return routes[i];
        }
        return null;
    }

    public PoseData GetPoseById(int poseId)
    {
        for (int i = 0; i < poses.Count; i++)
        {
            if (poses[i].poseId == poseId)
                return poses[i];
        }
        return null;
    }

    public bool IsAlreadyAtPose(PoseData pose)
    {
        if (pose == null)
            return false;

        bool closeEnough = Vector3.Distance(transform.position, pose.position) <= poseSnapDistance;
        bool angleEnough = Quaternion.Angle(transform.rotation, Quaternion.Euler(pose.rotationEuler)) <= poseSnapAngle;
        return closeEnough && angleEnough;
    }

    public void ForceSetCurrentPose(int poseId)
    {
        PoseData pose = GetPoseById(poseId);
        if (pose == null)
            return;

        currentPoseId = poseId;
        transform.position = pose.position;
        transform.rotation = Quaternion.Euler(pose.rotationEuler);
        StopMovement();
    }

    public void SaveCurrentTransformAsPose(int poseIndex)
    {
        if (poseIndex < 0 || poseIndex >= poses.Count)
            return;

        poses[poseIndex].position = transform.position;
        poses[poseIndex].rotationEuler = transform.rotation.eulerAngles;
    }

    public void AddCurrentTransformAsPoint(int routeIndex)
    {
        if (routeIndex < 0 || routeIndex >= routes.Count)
            return;

        RoutePoint p = new RoutePoint
        {
            pointName = "Punto " + (routes[routeIndex].points.Count + 1),
            position = transform.position,
            rotationEuler = transform.rotation.eulerAngles,
            isPausePoint = false,
            positionTolerance = 0.01f,
            rotationTolerance = 0.5f
        };

        routes[routeIndex].points.Add(p);
    }

    public void RemovePoint(int routeIndex, int pointIndex)
    {
        if (routeIndex < 0 || routeIndex >= routes.Count)
            return;

        if (pointIndex < 0 || pointIndex >= routes[routeIndex].points.Count)
            return;

        routes[routeIndex].points.RemoveAt(pointIndex);
    }

    public void MoveObjectToPoint(int routeIndex, int pointIndex)
    {
        if (routeIndex < 0 || routeIndex >= routes.Count)
            return;

        if (pointIndex < 0 || pointIndex >= routes[routeIndex].points.Count)
            return;

        RoutePoint p = routes[routeIndex].points[pointIndex];
        transform.position = p.position;
        transform.rotation = Quaternion.Euler(p.rotationEuler);
    }

    public void MoveObjectToPose(int poseIndex)
    {
        if (poseIndex < 0 || poseIndex >= poses.Count)
            return;

        transform.position = poses[poseIndex].position;
        transform.rotation = Quaternion.Euler(poses[poseIndex].rotationEuler);
    }

    private void OnDrawGizmos()
    {
        DrawPoses();
        DrawRoutes();
    }

    private void DrawPoses()
    {
        Gizmos.color = Color.yellow;

        for (int i = 0; i < poses.Count; i++)
        {
            Gizmos.DrawSphere(poses[i].position, 0.08f);

            Quaternion rot = Quaternion.Euler(poses[i].rotationEuler);
            Vector3 forward = rot * Vector3.forward;
            Gizmos.DrawLine(poses[i].position, poses[i].position + forward * 0.3f);
        }
    }

    private void DrawRoutes()
    {
        for (int r = 0; r < routes.Count; r++)
        {
            RouteData route = routes[r];
            if (route.points == null || route.points.Count == 0)
                continue;

            for (int i = 0; i < route.points.Count; i++)
            {
                RoutePoint p = route.points[i];
                Gizmos.color = p.isPausePoint ? Color.red : Color.cyan;
                Gizmos.DrawCube(p.position, Vector3.one * 0.05f);

                Quaternion rot = Quaternion.Euler(p.rotationEuler);
                Vector3 forward = rot * Vector3.forward;
                Gizmos.DrawLine(p.position, p.position + forward * 0.2f);

                if (i < route.points.Count - 1)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(route.points[i].position, route.points[i + 1].position);
                }
            }

            PoseData fromPose = GetPoseById(route.fromPoseId);
            PoseData toPose = GetPoseById(route.toPoseId);

            if (fromPose != null && route.points.Count > 0)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(fromPose.position, route.points[0].position);
            }

            if (toPose != null && route.points.Count > 0)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(route.points[route.points.Count - 1].position, toPose.position);
            }
        }
    }

    public bool HasActiveRoute()
    {
        return activeRoute != null;
    }

    public bool IsCurrentTargetPausePoint()
    {
        if (activeRoute == null)
            return false;

        if (currentPointIndex < 0 || currentPointIndex >= activeRoute.points.Count)
            return false;

        return activeRoute.points[currentPointIndex].isPausePoint;
    }

    public RoutePoint GetCurrentTargetPoint()
    {
        if (activeRoute == null)
            return null;

        if (currentPointIndex < 0 || currentPointIndex >= activeRoute.points.Count)
            return null;

        return activeRoute.points[currentPointIndex];
    }
}