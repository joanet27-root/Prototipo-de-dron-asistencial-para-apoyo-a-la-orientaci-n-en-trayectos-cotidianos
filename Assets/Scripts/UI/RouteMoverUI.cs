using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RouteMoverUI : MonoBehaviour
{
    [Header("Referencia principal")]
    public RouteMover mover;

    [Header("Botones destino")]
    public Button btnCasa;
    public Button btnEscuela;
    public Button btnCasaAbuela;
    public Button btnStop;

    [Header("Botones semaforo")]
    public Button btnVerde0;
    public Button btnVerde1;

    [Header("Textos")]
    public TMP_Text currentPoseText;
    public TMP_Text movementStateText;
    public TMP_Text consoleText;
    public TMP_Text titleText;

    [Header("IDs de poses")]
    public int casaPoseId = 1;
    public int escuelaPoseId = 2;
    public int abuelaPoseId = 3;

    [Header("Colores botones")]
    public Color normalColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    public Color selectedColor = new Color(0.55f, 0.85f, 0.55f, 1f);
    public Color movingToColor = new Color(0.95f, 0.8f, 0.35f, 1f);
    public Color disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);

    private string lastConsoleMessage = "Sistema listo";

    private void Start()
    {
        if (btnCasa != null) btnCasa.onClick.AddListener(OnCasaClicked);
        if (btnEscuela != null) btnEscuela.onClick.AddListener(OnEscuelaClicked);
        if (btnCasaAbuela != null) btnCasaAbuela.onClick.AddListener(OnAbuelaClicked);
        if (btnStop != null) btnStop.onClick.AddListener(OnStopClicked);

        if (btnVerde0 != null) btnVerde0.onClick.AddListener(() => SetVerde(0));
        if (btnVerde1 != null) btnVerde1.onClick.AddListener(() => SetVerde(1));

        if (titleText != null)
            titleText.text = "Control de destinos";

        RefreshUI(true);
    }

    private void Update()
    {
        RefreshUI(false);
    }

    private void OnCasaClicked()
    {
        RequestDestination(casaPoseId, "Casa");
    }

    private void OnEscuelaClicked()
    {
        RequestDestination(escuelaPoseId, "Escuela");
    }

    private void OnAbuelaClicked()
    {
        RequestDestination(abuelaPoseId, "Casa Abuela");
    }

    private void OnStopClicked()
    {
        if (mover == null)
            return;

        mover.StopMovement();
        SetConsole("Movimiento detenido");
        RefreshUI(false);
    }

    private void SetVerde(int value)
    {
        if (mover == null)
            return;

        mover.Verde = value;
        SetConsole(value == 1 ? "Semaforo manual: VERDE = 1" : "Semaforo manual: VERDE = 0");
        RefreshUI(false);
    }

    private void RequestDestination(int poseId, string poseName)
    {
        if (mover == null)
            return;

        if (mover.isMoving)
        {
            SetConsole("Orden ignorada: ya se esta moviendo");
            return;
        }

        RouteMover.PoseData pose = mover.GetPoseById(poseId);
        if (pose == null)
        {
            SetConsole($"Destino no disponible: {poseName}");
            return;
        }

        if (mover.IsAlreadyAtPose(pose))
        {
            mover.currentPoseId = poseId;
            SetConsole($"Ya esta en {poseName}");
            RefreshUI(false);
            return;
        }

        if (mover.currentPoseId == poseId)
        {
            SetConsole($"Ya esta en {poseName}");
            RefreshUI(false);
            return;
        }

        RouteMover.RouteData route = mover.GetRoute(mover.currentPoseId, poseId);
        if (route == null)
        {
            SetConsole($"No existe ruta desde {GetPoseName(mover.currentPoseId)} hasta {poseName}");
            RefreshUI(false);
            return;
        }

        mover.RequestGoToPose(poseId);
        SetConsole($"Moviendose hacia {poseName}");
        RefreshUI(false);
    }

    private void RefreshUI(bool forceConsoleRefresh)
    {
        if (mover == null)
        {
            if (currentPoseText != null) currentPoseText.text = "Pose actual: sin referencia";
            if (movementStateText != null) movementStateText.text = "Estado: sin RouteMover";
            if (consoleText != null) consoleText.text = "Asigna la referencia a RouteMover";
            return;
        }

        string currentPoseName = GetPoseName(mover.currentPoseId);

        if (currentPoseText != null)
            currentPoseText.text = $"Pose actual: {currentPoseName}";

        if (movementStateText != null)
            movementStateText.text = $"Estado: {BuildMovementStateText()}";

        if (consoleText != null)
        {
            if (mover.isPausedByGreen)
            {
                consoleText.text = "Esperando semaforo en verde";
            }
            else if (mover.isMoving)
            {
                string targetName = GetPoseName(mover.requestedDestinationPoseId);
                consoleText.text = $"Moviendose hacia {targetName}";
            }
            else if (forceConsoleRefresh)
            {
                consoleText.text = lastConsoleMessage;
            }
            else if (!mover.isMoving && !mover.isPausedByGreen)
            {
                if (lastConsoleMessage.StartsWith("Moviendose hacia"))
                    consoleText.text = $"Ruta completada: {currentPoseName}";
                else
                    consoleText.text = lastConsoleMessage;
            }
        }

        UpdateButtonVisuals();
        UpdateButtonInteractableState();
    }

    private string BuildMovementStateText()
    {
        if (mover.isPausedByGreen)
            return "Pausado por semaforo";

        if (mover.isMoving)
        {
            string targetName = GetPoseName(mover.requestedDestinationPoseId);
            return $"En movimiento hacia {targetName}";
        }

        return "En reposo";
    }

    private void UpdateButtonInteractableState()
    {
        bool canSendDestination = mover != null && !mover.isMoving;

        if (btnCasa != null) btnCasa.interactable = canSendDestination;
        if (btnEscuela != null) btnEscuela.interactable = canSendDestination;
        if (btnCasaAbuela != null) btnCasaAbuela.interactable = canSendDestination;

        if (btnStop != null) btnStop.interactable = mover != null && mover.isMoving;
    }

    private void UpdateButtonVisuals()
    {
        UpdateDestinationButtonColor(btnCasa, casaPoseId);
        UpdateDestinationButtonColor(btnEscuela, escuelaPoseId);
        UpdateDestinationButtonColor(btnCasaAbuela, abuelaPoseId);

        if (btnVerde0 != null)
            SetButtonColor(btnVerde0, mover != null && mover.Verde == 0 ? selectedColor : normalColor);

        if (btnVerde1 != null)
            SetButtonColor(btnVerde1, mover != null && mover.Verde == 1 ? selectedColor : normalColor);
    }

    private void UpdateDestinationButtonColor(Button button, int poseId)
    {
        if (button == null || mover == null)
            return;

        if (!button.interactable)
        {
            if (mover.isMoving && mover.requestedDestinationPoseId == poseId)
                SetButtonColor(button, movingToColor);
            else
                SetButtonColor(button, disabledColor);

            return;
        }

        if (mover.currentPoseId == poseId)
        {
            SetButtonColor(button, selectedColor);
            return;
        }

        SetButtonColor(button, normalColor);
    }

    private void SetButtonColor(Button button, Color color)
    {
        if (button == null || button.image == null)
            return;

        button.image.color = color;
    }

    private void SetConsole(string message)
    {
        lastConsoleMessage = message;

        if (consoleText != null)
            consoleText.text = message;
    }

    private string GetPoseName(int poseId)
    {
        if (poseId == casaPoseId) return "Casa";
        if (poseId == escuelaPoseId) return "Escuela";
        if (poseId == abuelaPoseId) return "Casa Abuela";

        if (mover != null)
        {
            RouteMover.PoseData pose = mover.GetPoseById(poseId);
            if (pose != null && !string.IsNullOrWhiteSpace(pose.poseName))
                return pose.poseName;
        }

        return $"Pose {poseId}";
    }
}