import cv2
import numpy as np
from fastapi import FastAPI, UploadFile, File, Form
from fastapi.responses import JSONResponse
from traffic_light import analyze_traffic_light_bgr

app = FastAPI()


@app.post("/analyze_traffic_light")
async def analyze_traffic_light(
    image: UploadFile = File(...),
    from_pose_id: int = Form(...),
    to_pose_id: int = Form(...),
    roi_x: float = Form(...),
    roi_y: float = Form(...),
    roi_w: float = Form(...),
    roi_h: float = Form(...)
):
    image_bytes = await image.read()
    np_buffer = np.frombuffer(image_bytes, dtype=np.uint8)
    image_bgr = cv2.imdecode(np_buffer, cv2.IMREAD_COLOR)

    if image_bgr is None:
        return JSONResponse(
            status_code=400,
            content={"error": "No se pudo decodificar la imagen"}
        )

    result = analyze_traffic_light_bgr(image_bgr, roi_x, roi_y, roi_w, roi_h)
    result["from_pose_id"] = from_pose_id
    result["to_pose_id"] = to_pose_id

    print(
    f"from={from_pose_id} to={to_pose_id} "
    f"state={result['traffic_light']['state']} "
    f"green={result['green']} "
    f"conf={result['traffic_light']['confidence']:.3f} "
    f"bbox={result['traffic_light']['bbox']}"
)
    

    return JSONResponse(content=result)