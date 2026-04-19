using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrafficLightOverlayUI : MonoBehaviour
{
    public RectTransform overlayRoot;
    public Image trafficLightBox;
    public TMP_Text trafficLightLabel;

    public Color greenColor = Color.green;
    public Color redColor = Color.red;
    public Color unknownColor = Color.yellow;

    public void Hide()
    {
        if (trafficLightBox != null) trafficLightBox.gameObject.SetActive(false);
        if (trafficLightLabel != null) trafficLightLabel.gameObject.SetActive(false);
    }

    public void Show(Rect normalizedBBox, string state)
    {
        if (overlayRoot == null || trafficLightBox == null || trafficLightLabel == null)
            return;

        trafficLightBox.gameObject.SetActive(true);
        trafficLightLabel.gameObject.SetActive(true);

        Color c = unknownColor;
        if (state == "green") c = greenColor;
        else if (state == "red") c = redColor;

        trafficLightBox.color = new Color(c.r, c.g, c.b, 0.15f);
        var outline = trafficLightBox.GetComponent<Outline>();
        if (outline != null) outline.effectColor = c;

        trafficLightLabel.text = $"Semaforo: {state.ToUpper()}";
        trafficLightLabel.color = c;

        float rootW = overlayRoot.rect.width;
        float rootH = overlayRoot.rect.height;

        float xMin = normalizedBBox.xMin * rootW;
        float yMin = normalizedBBox.yMin * rootH;
        float xMax = normalizedBBox.xMax * rootW;
        float yMax = normalizedBBox.yMax * rootH;

        float width = xMax - xMin;
        float height = yMax - yMin;

        trafficLightBox.rectTransform.anchorMin = new Vector2(0, 1);
        trafficLightBox.rectTransform.anchorMax = new Vector2(0, 1);
        trafficLightBox.rectTransform.pivot = new Vector2(0, 1);
        trafficLightBox.rectTransform.anchoredPosition = new Vector2(xMin, -yMin);
        trafficLightBox.rectTransform.sizeDelta = new Vector2(width, height);

        trafficLightLabel.rectTransform.anchorMin = new Vector2(0, 1);
        trafficLightLabel.rectTransform.anchorMax = new Vector2(0, 1);
        trafficLightLabel.rectTransform.pivot = new Vector2(0, 1);
        trafficLightLabel.rectTransform.anchoredPosition = new Vector2(xMin, -yMin - 4f);
    }

    public Image debugROIBox;
    public bool showDebugROI = true;

    public void ShowDebugROI(Rect normalizedROI)
    {
        if (!showDebugROI || overlayRoot == null || debugROIBox == null)
            return;

        debugROIBox.gameObject.SetActive(true);

        float rootW = overlayRoot.rect.width;
        float rootH = overlayRoot.rect.height;

        float xMin = normalizedROI.xMin * rootW;
        float yMin = normalizedROI.yMin * rootH;
        float xMax = normalizedROI.xMax * rootW;
        float yMax = normalizedROI.yMax * rootH;

        float width = xMax - xMin;
        float height = yMax - yMin;

        debugROIBox.rectTransform.anchorMin = new Vector2(0, 1);
        debugROIBox.rectTransform.anchorMax = new Vector2(0, 1);
        debugROIBox.rectTransform.pivot = new Vector2(0, 1);
        debugROIBox.rectTransform.anchoredPosition = new Vector2(xMin, -yMin);
        debugROIBox.rectTransform.sizeDelta = new Vector2(width, height);
    }

    public void HideDebugROI()
    {
        if (debugROIBox != null)
            debugROIBox.gameObject.SetActive(false);
    }
}