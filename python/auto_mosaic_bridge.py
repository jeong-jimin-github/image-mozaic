from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageOps

SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}


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
    p.add_argument("--include-nipple", action="store_true")
    p.add_argument("--include-anus", action="store_true")
    p.add_argument("--include-testicles", action="store_true")
    p.add_argument("--copy-undetected", action="store_true")
    return p.parse_args()


def emit_progress(value: int, message: str):
    payload = {"type": "progress", "value": max(0, min(100, int(value))), "message": message}
    print(json.dumps(payload, ensure_ascii=False), flush=True)


def _load_imgutils():
    try:
        from imgutils.detect import detect_censors, detect_with_nudenet
        return detect_censors, detect_with_nudenet
    except ImportError as exc:
        raise RuntimeError("자동 검출 의존성을 불러오지 못했습니다. Release 파일이 손상되었을 수 있습니다.") from exc


def detect(image, args, stage):
    detect_censors, detect_with_nudenet = _load_imgutils()

    stage(0.25, "기본 검열 영역 검출 중...")
    labels = {"pussy", "penis"}
    if args.include_nipple:
        labels.add("nipple_f")

    detections = [
        (box, label, float(score))
        for box, label, score in detect_censors(image, level="s", conf_threshold=args.confidence)
        if label in labels
    ]

    warning = None
    if args.include_anus or args.include_testicles:
        stage(0.58, "추가 검출 영역 분석 중...")
        try:
            fallback = detect_with_nudenet(image, score_threshold=args.confidence)
            extra_labels = set()
            if args.include_anus:
                extra_labels.update({"ANUS_EXPOSED", "ANUS_COVERED"})
            if args.include_testicles:
                extra_labels.update({"MALE_GENITALIA_EXPOSED", "MALE_GENITALIA_COVERED"})

            for box, label, score in fallback:
                normalized = str(label).upper()
                if normalized in extra_labels:
                    detections.append((box, normalized, float(score)))
        except Exception as exc:
            warning = f"추가 검출을 완료하지 못했습니다: {exc}"

    return detections, warning


def padded_box(box, padding, width, height):
    x0, y0, x1, y1 = box
    return (
        max(0, int(x0) - padding),
        max(0, int(y0) - padding),
        min(width, int(x1) + padding),
        min(height, int(y1) + padding),
    )


def build_region_mask(size, label: str):
    """Create a non-rectangular mask centered on the detected anatomy.

    The detectors return bounding boxes, not segmentation masks. Instead of
    censoring every corner of that box, use an anatomy-shaped mask so pixels
    outside the detected part remain untouched.
    """
    width, height = size
    mask = Image.new("L", size, 0)
    draw = ImageDraw.Draw(mask)

    inset = max(1, int(min(width, height) * 0.04))
    bounds = (inset, inset, max(inset + 1, width - inset - 1), max(inset + 1, height - inset - 1))
    normalized = str(label).lower()

    if "penis" in normalized or "male_genitalia" in normalized:
        radius = max(2, min(width, height) // 2)
        draw.rounded_rectangle(bounds, radius=radius, fill=255)
    else:
        draw.ellipse(bounds, fill=255)

    feather = max(1, int(min(width, height) * 0.035))
    if feather > 1:
        mask = mask.filter(ImageFilter.GaussianBlur(radius=feather))
    return mask


def apply_effect(image, detections, mode, strength, padding):
    result = image.copy()

    for box, label, _score in detections:
        x0, y0, x1, y1 = padded_box(box, padding, image.width, image.height)
        if x1 <= x0 or y1 <= y0:
            continue

        crop = result.crop((x0, y0, x1, y1))
        if min(crop.size) < 4:
            continue

        if mode == "black":
            effected = crop.copy()
            ImageDraw.Draw(effected).rectangle((0, 0, crop.width, crop.height), fill="black")
        elif mode == "blur":
            effected = crop.filter(ImageFilter.GaussianBlur(radius=max(1, strength)))
        else:
            block = max(2, min(strength, min(crop.size)))
            small = crop.resize(
                (max(1, crop.width // block), max(1, crop.height // block)),
                Image.Resampling.BOX,
            )
            effected = small.resize(crop.size, Image.Resampling.NEAREST)

        mask = build_region_mask(crop.size, label)
        result.paste(effected, (x0, y0), mask)

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


def process_one(input_path: Path, output_path: Path, args, start=5, end=95):
    span = max(1, end - start)

    def stage(fraction, message):
        emit_progress(start + int(span * max(0.0, min(1.0, fraction))), message)

    stage(0.02, f"{input_path.name}: 이미지 읽는 중...")
    with Image.open(input_path) as opened:
        image = ImageOps.exif_transpose(opened).copy()

    detections, warning = detect(image, args, stage)
    if not detections:
        stage(0.90, f"{input_path.name}: 검출 결과 정리 중...")
        if args.copy_undetected:
            output_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(input_path, output_path)
        stage(1.0, f"{input_path.name}: 검열 대상 없음")
        return {"status": "undetected", "count": 0, "warning": warning}

    stage(0.72, f"{input_path.name}: {len(detections)}개 검출 부위에 마스크 적용 중...")
    result = apply_effect(image, detections, args.mode, args.strength, args.padding)

    stage(0.90, f"{input_path.name}: 결과 저장 중...")
    save_image(result, output_path)
    stage(1.0, f"{input_path.name}: 처리 완료")
    return {"status": "success", "count": len(detections), "warning": warning}


def emit(payload, exit_code=0):
    print(json.dumps(payload, ensure_ascii=False), flush=True)
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
            emit_progress(2, "자동 검열 준비 중...")
            result = process_one(args.input_file.resolve(), args.output_file.resolve(), args, 5, 98)
            emit_progress(100, "처리 완료")
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
    if not files:
        emit_progress(100, "처리할 이미지가 없습니다.")
        emit({"status": "success", "count": 0, "processed": 0, "undetected": 0, "errors": 0})

    processed = undetected = errors = total_count = 0
    warnings = []
    total = len(files)
    emit_progress(2, f"일괄 처리 준비: {total}개 파일")

    for index, path in enumerate(files):
        start = 3 + int(94 * index / total)
        end = 3 + int(94 * (index + 1) / total)
        try:
            item = process_one(path, output_dir / path.name, args, start, end)
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

    emit_progress(100, f"일괄 처리 완료: {total}개 파일")
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
