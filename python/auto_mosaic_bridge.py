from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageOps

SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}
DEFAULT_MODEL = Path(__file__).resolve().parent / "models" / "ntd11_anime_nsfw_segm_v5.pt"


def parse_args():
    p = argparse.ArgumentParser()
    source = p.add_mutually_exclusive_group(required=True)
    source.add_argument("--input-file", type=Path)
    source.add_argument("--input-dir", type=Path)
    target = p.add_mutually_exclusive_group(required=True)
    target.add_argument("--output-file", type=Path)
    target.add_argument("--output-dir", type=Path)
    p.add_argument("--mode", choices=["mosaic", "black", "blur"], default="mosaic")
    p.add_argument("--strength", type=int, default=20)
    p.add_argument("--confidence", type=float, default=0.25)
    p.add_argument("--padding", type=int, default=10)
    p.add_argument("--detector", choices=["auto", "imgutils", "ntd11"], default="auto")
    p.add_argument("--include-nipple", action="store_true")
    p.add_argument("--include-anus", action="store_true")
    p.add_argument("--include-testicles", action="store_true")
    p.add_argument("--ntd11-model", type=Path)
    p.add_argument("--copy-undetected", action="store_true")
    return p.parse_args()


def _load_imgutils():
    try:
        from imgutils.detect import detect_censors, detect_with_nudenet
        return detect_censors, detect_with_nudenet
    except ImportError as exc:
        raise RuntimeError(
            "dghs-imgutils가 설치되어 있지 않습니다. 자동 모자이크 > Python 의존성 설치를 실행하세요."
        ) from exc


def _detect_ntd11(image, model_path: Path, labels: set[str], confidence: float):
    try:
        from ultralytics import YOLO
    except ImportError as exc:
        raise RuntimeError("NTD11을 사용하려면 ultralytics가 필요합니다: pip install ultralytics") from exc
    if not model_path.is_file():
        raise RuntimeError(f"NTD11 모델 파일을 찾을 수 없습니다: {model_path}")
    model = _detect_ntd11._cache.get(str(model_path))
    if model is None:
        model = YOLO(str(model_path))
        _detect_ntd11._cache[str(model_path)] = model
    detections = []
    for result in model.predict(source=image, conf=confidence, iou=0.7, verbose=False):
        boxes = result.boxes
        if boxes is None:
            continue
        for i in range(len(boxes)):
            x0, y0, x1, y1 = boxes.xyxy[i].tolist()
            class_id = int(boxes.cls[i].item())
            score = float(boxes.conf[i].item())
            label = str(model.names[class_id])
            if label in labels:
                detections.append(((int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))), label, score))
    return detections


_detect_ntd11._cache = {}


def detect(image, args):
    model_path = (args.ntd11_model or DEFAULT_MODEL).resolve()
    base_labels = {"pussy", "penis"}
    if args.include_nipple:
        base_labels.add("nipple_f")

    if args.detector == "ntd11":
        labels = {"pussy", "penis"}
        if args.include_nipple:
            labels.add("nipples")
        if args.include_anus:
            labels.add("anus")
        if args.include_testicles:
            labels.add("testicles")
        return _detect_ntd11(image, model_path, labels, args.confidence), None

    detect_censors, detect_with_nudenet = _load_imgutils()
    detections = [
        (box, label, float(score))
        for box, label, score in detect_censors(image, level="s", conf_threshold=args.confidence)
        if label in base_labels
    ]
    warning = None

    if args.detector == "auto" and (args.include_anus or args.include_testicles):
        ntd_labels = set()
        if args.include_anus:
            ntd_labels.add("anus")
        if args.include_testicles:
            ntd_labels.add("testicles")
        if model_path.is_file():
            try:
                detections.extend(_detect_ntd11(image, model_path, ntd_labels, args.confidence))
            except RuntimeError as exc:
                warning = str(exc)
        elif args.include_anus:
            try:
                fallback = detect_with_nudenet(image, score_threshold=args.confidence)
                detections.extend(
                    (box, label, float(score))
                    for box, label, score in fallback
                    if label in {"ANUS_EXPOSED", "ANUS_COVERED"}
                )
                if args.include_testicles:
                    warning = "NTD11 모델이 없어 testicles 검출은 건너뛰었습니다."
            except Exception as exc:
                warning = f"추가 검출 폴백 실패: {exc}"
        else:
            warning = "NTD11 모델이 없어 testicles 검출은 건너뛰었습니다."
    return detections, warning


def padded_box(box, padding, width, height):
    x0, y0, x1, y1 = box
    return (
        max(0, int(x0) - padding),
        max(0, int(y0) - padding),
        min(width, int(x1) + padding),
        min(height, int(y1) + padding),
    )


def apply_effect(image, areas, mode, strength):
    result = image.copy()
    for box in areas:
        x0, y0, x1, y1 = box
        if x1 <= x0 or y1 <= y0:
            continue
        if mode == "black" or min(x1 - x0, y1 - y0) < 4:
            ImageDraw.Draw(result).rectangle(box, fill="black")
            continue
        crop = result.crop(box)
        if mode == "blur":
            crop = crop.filter(ImageFilter.GaussianBlur(radius=max(1, strength)))
        else:
            block = max(2, min(strength, min(crop.size)))
            small = crop.resize(
                (max(1, crop.width // block), max(1, crop.height // block)),
                Image.Resampling.BOX,
            )
            crop = small.resize(crop.size, Image.Resampling.NEAREST)
        result.paste(crop, (x0, y0))
    return result


def save_image(image, output_path: Path):
    output_path.parent.mkdir(parents=True, exist_ok=True)
    suffix = output_path.suffix.lower()
    kwargs = {}
    if suffix in {".jpg", ".jpeg"}:
        if image.mode in {"RGBA", "P"}:
            image = image.convert("RGB")
        kwargs = {"quality": 95, "subsampling": 0}
    elif suffix == ".webp":
        kwargs = {"quality": 95}
    image.save(output_path, **kwargs)


def process_one(input_path: Path, output_path: Path, args):
    with Image.open(input_path) as opened:
        image = ImageOps.exif_transpose(opened).copy()
    detections, warning = detect(image, args)
    if not detections:
        if args.copy_undetected:
            output_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(input_path, output_path)
        return {"status": "undetected", "count": 0, "warning": warning}
    areas = [padded_box(box, args.padding, image.width, image.height) for box, _, _ in detections]
    result = apply_effect(image, areas, args.mode, args.strength)
    save_image(result, output_path)
    return {"status": "success", "count": len(areas), "warning": warning}


def emit(payload, exit_code=0):
    print(json.dumps(payload, ensure_ascii=False))
    raise SystemExit(exit_code)


def main():
    args = parse_args()
    if not 0.0 <= args.confidence <= 1.0:
        emit({"status": "error", "error": "confidence는 0.0~1.0 범위여야 합니다."}, 2)
    if args.padding < 0 or args.strength < 1:
        emit({"status": "error", "error": "padding은 0 이상, strength는 1 이상이어야 합니다."}, 2)

    if args.input_file:
        if not args.output_file:
            emit({"status": "error", "error": "--input-file에는 --output-file이 필요합니다."}, 2)
        try:
            result = process_one(args.input_file.resolve(), args.output_file.resolve(), args)
            emit(result)
        except Exception as exc:
            emit({"status": "error", "error": str(exc)}, 1)

    if not args.output_dir:
        emit({"status": "error", "error": "--input-dir에는 --output-dir이 필요합니다."}, 2)
    input_dir = args.input_dir.resolve()
    output_dir = args.output_dir.resolve()
    if not input_dir.is_dir():
        emit({"status": "error", "error": f"입력 폴더가 없습니다: {input_dir}"}, 2)
    output_dir.mkdir(parents=True, exist_ok=True)

    files = sorted(
        (p for p in input_dir.iterdir() if p.is_file() and p.suffix.lower() in SUPPORTED_EXTENSIONS),
        key=lambda p: p.name.lower(),
    )
    processed = undetected = errors = total_count = 0
    warnings = []
    for path in files:
        try:
            item = process_one(path, output_dir / path.name, args)
            if item["status"] == "success":
                processed += 1
                total_count += item["count"]
            else:
                undetected += 1
            if item.get("warning"):
                warnings.append(item["warning"])
        except Exception as exc:
            errors += 1
            print(f"{path.name}: {exc}", file=sys.stderr)

    emit({
        "status": "success" if errors == 0 else "partial",
        "count": total_count,
        "processed": processed,
        "undetected": undetected,
        "errors": errors,
        "warning": "; ".join(dict.fromkeys(warnings)) if warnings else None,
    })


if __name__ == "__main__":
    main()
