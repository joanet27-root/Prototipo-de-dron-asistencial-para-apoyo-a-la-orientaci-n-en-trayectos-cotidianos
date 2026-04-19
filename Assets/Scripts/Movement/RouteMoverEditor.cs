using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RouteMover))]
public class RouteMoverEditor : Editor
{
    private RouteMover mover;

    private int selectedRouteIndex = 0;

    private bool showPoses = true;
    private bool showRoutes = true;
    private bool showRuntime = true;

    private void OnEnable()
    {
        mover = (RouteMover)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Route Mover", EditorStyles.boldLabel);

        DrawTopSettings();

        EditorGUILayout.Space();
        showRuntime = EditorGUILayout.Foldout(showRuntime, "Estado y control");
        if (showRuntime)
        {
            DrawRuntimeSection();
        }

        EditorGUILayout.Space();
        showPoses = EditorGUILayout.Foldout(showPoses, "Poses");
        if (showPoses)
        {
            DrawPosesSection();
        }

        EditorGUILayout.Space();
        showRoutes = EditorGUILayout.Foldout(showRoutes, "Rutas");
        if (showRoutes)
        {
            DrawRoutesSection();
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(mover);
        }
    }

    private void DrawTopSettings()
    {
        mover.currentPoseId = EditorGUILayout.IntField("Current Pose ID", mover.currentPoseId);
        mover.Verde = EditorGUILayout.IntSlider("Verde", mover.Verde, 0, 1);

        mover.enableKeyboardInput = EditorGUILayout.Toggle("Enable Keyboard Input", mover.enableKeyboardInput);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movimiento global", EditorStyles.boldLabel);
        mover.moveSpeed = EditorGUILayout.FloatField("Move Speed", mover.moveSpeed);
        mover.rotationSpeed = EditorGUILayout.FloatField("Rotation Speed", mover.rotationSpeed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Detección de pose", EditorStyles.boldLabel);
        mover.poseSnapDistance = EditorGUILayout.FloatField("Pose Snap Distance", mover.poseSnapDistance);
        mover.poseSnapAngle = EditorGUILayout.FloatField("Pose Snap Angle", mover.poseSnapAngle);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Parar"))
        {
            mover.StopMovement();
        }

        if (GUILayout.Button("Refrescar currentPose"))
        {
            RefreshCurrentPoseFromNearest();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawRuntimeSection()
    {
        EditorGUILayout.LabelField("Moviendo", mover.isMoving ? "Sí" : "No");
        EditorGUILayout.LabelField("Pausa por Verde", mover.isPausedByGreen ? "Sí" : "No");
        EditorGUILayout.LabelField("Ruta actual", mover.currentRouteIndex >= 0 ? mover.currentRouteIndex.ToString() : "-");
        EditorGUILayout.LabelField("Punto actual", mover.currentPointIndex >= 0 ? mover.currentPointIndex.ToString() : "-");

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Ir a pose 1")) mover.RequestGoToPose(1);
        if (GUILayout.Button("Ir a pose 2")) mover.RequestGoToPose(2);
        if (GUILayout.Button("Ir a pose 3")) mover.RequestGoToPose(3);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPosesSection()
    {
        if (GUILayout.Button("Añadir pose"))
        {
            Undo.RecordObject(mover, "Add Pose");
            mover.poses.Add(new RouteMover.PoseData
            {
                poseName = "Nueva Pose",
                poseId = mover.poses.Count + 1,
                position = mover.transform.position,
                rotationEuler = mover.transform.rotation.eulerAngles
            });
            EditorUtility.SetDirty(mover);
        }

        EditorGUILayout.Space();

        for (int i = 0; i < mover.poses.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            mover.poses[i].poseName = EditorGUILayout.TextField("Nombre", mover.poses[i].poseName);
            mover.poses[i].poseId = EditorGUILayout.IntField("Pose ID", mover.poses[i].poseId);

            EditorGUILayout.LabelField("Posición", mover.poses[i].position.ToString("F3"));
            EditorGUILayout.LabelField("Rotación", mover.poses[i].rotationEuler.ToString("F3"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Guardar pose actual"))
            {
                Undo.RecordObject(mover, "Save Pose From Transform");
                mover.SaveCurrentTransformAsPose(i);
                EditorUtility.SetDirty(mover);
            }

            if (GUILayout.Button("Ir a pose"))
            {
                Undo.RecordObject(mover.transform, "Move To Pose");
                mover.MoveObjectToPose(i);
                EditorUtility.SetDirty(mover.transform);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fijar currentPose aquí"))
            {
                Undo.RecordObject(mover, "Set Current Pose");
                mover.currentPoseId = mover.poses[i].poseId;
                EditorUtility.SetDirty(mover);
            }

            if (GUILayout.Button("Borrar pose"))
            {
                Undo.RecordObject(mover, "Delete Pose");
                mover.poses.RemoveAt(i);
                EditorUtility.SetDirty(mover);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawRoutesSection()
    {
        if (GUILayout.Button("Añadir ruta"))
        {
            Undo.RecordObject(mover, "Add Route");
            mover.routes.Add(new RouteMover.RouteData
            {
                routeName = "Nueva Ruta",
                fromPoseId = 1,
                toPoseId = 2
            });
            selectedRouteIndex = mover.routes.Count - 1;
            EditorUtility.SetDirty(mover);
        }

        if (mover.routes.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay rutas creadas.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        string[] routeNames = new string[mover.routes.Count];
        for (int i = 0; i < mover.routes.Count; i++)
        {
            var r = mover.routes[i];
            routeNames[i] = $"{i}: {r.routeName} ({r.fromPoseId} -> {r.toPoseId})";
        }

        selectedRouteIndex = Mathf.Clamp(selectedRouteIndex, 0, mover.routes.Count - 1);
        selectedRouteIndex = EditorGUILayout.Popup("Ruta seleccionada", selectedRouteIndex, routeNames);

        RouteMover.RouteData route = mover.routes[selectedRouteIndex];

        EditorGUILayout.BeginVertical("box");

        route.routeName = EditorGUILayout.TextField("Nombre ruta", route.routeName);
        route.fromPoseId = EditorGUILayout.IntField("From Pose ID", route.fromPoseId);
        route.toPoseId = EditorGUILayout.IntField("To Pose ID", route.toPoseId);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Guardar punto actual"))
        {
            Undo.RecordObject(mover, "Add Route Point");
            mover.AddCurrentTransformAsPoint(selectedRouteIndex);
            EditorUtility.SetDirty(mover);
        }

        if (GUILayout.Button("Ejecutar esta ruta"))
        {
            mover.StartRoute(route);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Borrar ruta"))
        {
            Undo.RecordObject(mover, "Delete Route");
            mover.routes.RemoveAt(selectedRouteIndex);
            selectedRouteIndex = Mathf.Clamp(selectedRouteIndex - 1, 0, Mathf.Max(0, mover.routes.Count - 1));
            EditorUtility.SetDirty(mover);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }

        if (GUILayout.Button("Limpiar puntos"))
        {
            Undo.RecordObject(mover, "Clear Route Points");
            route.points.Clear();
            EditorUtility.SetDirty(mover);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Puntos de la ruta", EditorStyles.boldLabel);

        for (int i = 0; i < route.points.Count; i++)
        {
            var p = route.points[i];

            EditorGUILayout.BeginVertical("helpbox");

            p.pointName = EditorGUILayout.TextField("Nombre", p.pointName);

            EditorGUILayout.LabelField("Posición", p.position.ToString("F3"));
            EditorGUILayout.LabelField("Rotación", p.rotationEuler.ToString("F3"));

            p.isPausePoint = EditorGUILayout.Toggle("Es pausa", p.isPausePoint);
            p.positionTolerance = EditorGUILayout.FloatField("Position Tolerance", p.positionTolerance);
            p.rotationTolerance = EditorGUILayout.FloatField("Rotation Tolerance", p.rotationTolerance);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Actualizar con pose actual"))
            {
                Undo.RecordObject(mover, "Update Point From Current Transform");
                p.position = mover.transform.position;
                p.rotationEuler = mover.transform.rotation.eulerAngles;
                EditorUtility.SetDirty(mover);
            }

            if (GUILayout.Button("Ir a este punto"))
            {
                Undo.RecordObject(mover.transform, "Move To Route Point");
                mover.MoveObjectToPoint(selectedRouteIndex, i);
                EditorUtility.SetDirty(mover.transform);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Subir") && i > 0)
            {
                Undo.RecordObject(mover, "Move Point Up");
                var temp = route.points[i - 1];
                route.points[i - 1] = route.points[i];
                route.points[i] = temp;
                EditorUtility.SetDirty(mover);
            }

            if (GUILayout.Button("Bajar") && i < route.points.Count - 1)
            {
                Undo.RecordObject(mover, "Move Point Down");
                var temp = route.points[i + 1];
                route.points[i + 1] = route.points[i];
                route.points[i] = temp;
                EditorUtility.SetDirty(mover);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Borrar punto"))
            {
                Undo.RecordObject(mover, "Delete Route Point");
                mover.RemovePoint(selectedRouteIndex, i);
                EditorUtility.SetDirty(mover);
                EditorGUILayout.EndVertical();
                break;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void RefreshCurrentPoseFromNearest()
    {
        if (mover.poses == null || mover.poses.Count == 0)
            return;

        float bestDistance = float.MaxValue;
        int bestPoseId = mover.currentPoseId;

        for (int i = 0; i < mover.poses.Count; i++)
        {
            float d = Vector3.Distance(mover.transform.position, mover.poses[i].position);
            if (d < bestDistance)
            {
                bestDistance = d;
                bestPoseId = mover.poses[i].poseId;
            }
        }

        Undo.RecordObject(mover, "Refresh Current Pose");
        mover.currentPoseId = bestPoseId;
        EditorUtility.SetDirty(mover);
    }
}