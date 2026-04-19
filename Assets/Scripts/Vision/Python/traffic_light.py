import cv2
import numpy as np


def clamp_roi(roi_x, roi_y, roi_w, roi_h, img_w, img_h):
    x1 = int(max(0, min(img_w - 1, roi_x * img_w)))
    y1 = int(max(0, min(img_h - 1, roi_y * img_h)))
    x2 = int(max(1, min(img_w, (roi_x + roi_w) * img_w)))
    y2 = int(max(1, min(img_h, (roi_y + roi_h) * img_h)))
    return x1, y1, x2, y2


def _largest_valid_contour(mask, min_area=20, max_area=3000):
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    best = None
    best_area = 0

    for c in contours:
        area = cv2.contourArea(c)
        if area < min_area or area > max_area:
            continue

        x, y, w, h = cv2.boundingRect(c)
        if w <= 0 or h <= 0:
            continue

        # Evita manchas demasiado anchas tipo césped o fondo
        aspect = w / float(h)
        if aspect < 0.25 or aspect > 2.5:
            continue

        if area > best_area:
            best = c
            best_area = area

    return best, best_area


def analyze_traffic_light_bgr(image_bgr, roi_x, roi_y, roi_w, roi_h):
    h, w = image_bgr.shape[:2]
    x1, y1, x2, y2 = clamp_roi(roi_x, roi_y, roi_w, roi_h, w, h)

    roi = image_bgr[y1:y2, x1:x2].copy()
    if roi.size == 0:
        return {
            "green": 0,
            "traffic_light": {
                "detected": False,
                "state": "unknown",
                "confidence": 0.0,
                "bbox": None
            },
            "description": "ROI vacia"
        }
    cv2.imshow("ROI", roi)
    cv2.waitKey(1)

    roi_h_px, roi_w_px = roi.shape[:2]
    hsv = cv2.cvtColor(roi, cv2.COLOR_BGR2HSV)

    # Rangos HSV
    lower_green = np.array([35, 50, 50], dtype=np.uint8)
    upper_green = np.array([95, 255, 255], dtype=np.uint8)

    # Rojo más permisivo
    lower_red1 = np.array([0, 50, 40], dtype=np.uint8)
    upper_red1 = np.array([15, 255, 255], dtype=np.uint8)

    lower_red2 = np.array([160, 50, 40], dtype=np.uint8)
    upper_red2 = np.array([179, 255, 255], dtype=np.uint8)

    full_green = cv2.inRange(hsv, lower_green, upper_green)
    red1 = cv2.inRange(hsv, lower_red1, upper_red1)
    red2 = cv2.inRange(hsv, lower_red2, upper_red2)
    full_red = cv2.bitwise_or(red1, red2)

    kernel = np.ones((3, 3), np.uint8)
    full_green = cv2.morphologyEx(full_green, cv2.MORPH_OPEN, kernel)
    full_red = cv2.morphologyEx(full_red, cv2.MORPH_OPEN, kernel)

    # Dividir ROI en 3 bandas verticales:
    # superior = rojo, media = amarillo, inferior = verde
    y_red_end = int(roi_h_px * 0.40)
    y_green_start = int(roi_h_px * 0.45)

    red_band = np.zeros_like(full_red)
    green_band = np.zeros_like(full_green)

    red_band[:y_red_end, :] = full_red[:y_red_end, :]
    green_band[y_green_start:, :] = full_green[y_green_start:, :]

    best_red, red_area = _largest_valid_contour(red_band, min_area=20, max_area=2500)
    best_green, green_area = _largest_valid_contour(green_band, min_area=20, max_area=2500)

    state = "unknown"
    green_value = 0
    confidence = 0.0
    detected = False
    bbox = None
    chosen = None

    # Regla simple de decisión
    if best_green is not None and green_area > red_area * 1.2:
        state = "green"
        green_value = 1
        chosen = best_green
        confidence = min(1.0, green_area / 400.0)
    elif best_red is not None and red_area > green_area * 1.2:
        state = "red"
        green_value = 0
        chosen = best_red
        confidence = min(1.0, red_area / 400.0)

    if chosen is not None:
        detected = True
        bx, by, bw_box, bh_box = cv2.boundingRect(chosen)

        gx1 = (x1 + bx) / w
        gy1 = (y1 + by) / h
        gx2 = (x1 + bx + bw_box) / w
        gy2 = (y1 + by + bh_box) / h
        bbox = [gx1, gy1, gx2, gy2]

    description = {
        "green": "Semaforo en verde",
        "red": "Semaforo en rojo",
        "unknown": "Semaforo no detectado con claridad"
    }[state]

    print(
    f"ROI normalized: x={roi_x:.3f}, y={roi_y:.3f}, w={roi_w:.3f}, h={roi_h:.3f} | "
    f"pixels: x1={x1}, y1={y1}, x2={x2}, y2={y2}, "
    f"crop_w={x2-x1}, crop_h={y2-y1}, img_w={w}, img_h={h}"
    )

    return {
        "green": green_value,
        "traffic_light": {
            "detected": detected,
            "state": state,
            "confidence": float(confidence),
            "bbox": bbox
        },
        "description": description
    }