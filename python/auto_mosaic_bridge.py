from __future__ import annotations

import argparse
import json
import os
import shutil
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageFilter, ImageOps

SUPPORTED_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp"}


LANG = os.environ.get("IMAGE_MOSAIC_LANG", "ko")
if LANG.startswith("ja"):
    LANG = "ja"
elif LANG.startswith("zh"):
    LANG = "zh-Hans"
elif LANG.startswith("ko"):
    LANG = "ko"
else:
    LANG = "en"

_MESSAGES = {
    "dependency_missing": {"ko": "자동 검출 의존성을 불러오지 못했습니다. Release 파일이 손상되었을 수 있습니다.", "en": "Automatic detection dependencies could not be loaded. The Release package may be damaged.", "ja": "自動検出の依存関係を読み込めませんでした。Releaseパッケージが破損している可能性があります。", "zh-Hans": "无法加载自动检测依赖项。Release 包可能已损坏。"},
    "base_detection": {"ko": "기본 검열 영역 검출 중... · {provider}", "en": "Detecting primary target regions... · {provider}", "ja": "基本対象領域を検出中... · {provider}", "zh-Hans": "正在检测主要目标区域... · {provider}"},
    "extra_detection": {"ko": "추가 검출 영역 분석 중... · {provider}", "en": "Analyzing extra detection targets... · {provider}", "ja": "追加検出領域を解析中... · {provider}", "zh-Hans": "正在分析附加检测目标... · {provider}"},
    "extra_failed": {"ko": "추가 검출을 완료하지 못했습니다: {error}", "en": "Extra detection could not be completed: {error}", "ja": "追加検出を完了できませんでした: {error}", "zh-Hans": "附加检测未能完成: {error}"},
    "reading": {"ko": "{name}: 이미지 읽는 중... · {provider}", "en": "{name}: reading image... · {provider}", "ja": "{name}: 画像を読み込み中... · {provider}", "zh-Hans": "{name}: 正在读取图像... · {provider}"},
    "organizing": {"ko": "{name}: 검출 결과 정리 중...", "en": "{name}: organizing detections...", "ja": "{name}: 検出結果を整理中...", "zh-Hans": "{name}: 正在整理检测结果..."},
    "none": {"ko": "{name}: 검열 대상 없음", "en": "{name}: no target regions", "ja": "{name}: 対象領域なし", "zh-Hans": "{name}: 无目标区域"},
    "segmenting": {"ko": "{name}: {count}개 부위 픽셀 경계 분리 중...", "en": "{name}: segmenting pixel boundaries for {count} region(s)...", "ja": "{name}: {count}領域のピクセル境界を分離中...", "zh-Hans": "{name}: 正在分离 {count} 个区域的像素边界..."},
    "saving": {"ko": "{name}: 결과 저장 중...", "en": "{name}: saving result...", "ja": "{name}: 結果を保存中...", "zh-Hans": "{name}: 正在保存结果..."},
    "item_done": {"ko": "{name}: 처리 완료", "en": "{name}: complete", "ja": "{name}: 処理完了", "zh-Hans": "{name}: 处理完成"},
    "confidence": {"ko": "confidence는 0.0~1.0 범위여야 합니다.", "en": "confidence must be between 0.0 and 1.0.", "ja": "confidenceは0.0～1.0の範囲で指定してください。", "zh-Hans": "confidence 必须在 0.0 到 1.0 之间。"},
    "padding": {"ko": "padding은 0 이상, strength는 1 이상이어야 합니다.", "en": "padding must be at least 0 and strength at least 1.", "ja": "paddingは0以上、strengthは1以上である必要があります。", "zh-Hans": "padding 必须不小于 0，strength 必须不小于 1。"},
    "device": {"ko": "AI 실행 장치: {provider}", "en": "AI runtime device: {provider}", "ja": "AI実行デバイス: {provider}", "zh-Hans": "AI 运行设备: {provider}"},
    "need_output_file": {"ko": "--input-file에는 --output-file이 필요합니다.", "en": "--input-file requires --output-file.", "ja": "--input-fileには--output-fileが必要です。", "zh-Hans": "--input-file 需要 --output-file。"},
    "done": {"ko": "처리 완료 · {provider}", "en": "Complete · {provider}", "ja": "処理完了 · {provider}", "zh-Hans": "处理完成 · {provider}"},
    "need_output_dir": {"ko": "--input-dir에는 --output-dir이 필요합니다.", "en": "--input-dir requires --output-dir.", "ja": "--input-dirには--output-dirが必要です。", "zh-Hans": "--input-dir 需要 --output-dir。"},
    "missing_dir": {"ko": "입력 폴더가 없습니다: {path}", "en": "Input folder does not exist: {path}", "ja": "入力フォルダーがありません: {path}", "zh-Hans": "输入文件夹不存在: {path}"},
    "no_images": {"ko": "처리할 이미지가 없습니다.", "en": "No images to process.", "ja": "処理する画像がありません。", "zh-Hans": "没有可处理的图像。"},
    "batch_prepare": {"ko": "일괄 처리 준비: {total}개 파일 · {provider}", "en": "Preparing batch: {total} file(s) · {provider}", "ja": "一括処理を準備中: {total}ファイル · {provider}", "zh-Hans": "正在准备批处理: {total} 个文件 · {provider}"},
    "batch_done": {"ko": "일괄 처리 완료: {total}개 파일 · {provider}", "en": "Batch complete: {total} file(s) · {provider}", "ja": "一括処理完了: {total}ファイル · {provider}", "zh-Hans": "批处理完成: {total} 个文件 · {provider}"},
}


def tr(key: str, **kwargs):
    table = _MESSAGES[key]
    return table.get(LANG, table["en"]).format(**kwargs)


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
        raise RuntimeError(tr("dependency_missing")) from exc


def get_runtime_provider_label():
    """Preload bundled CUDA/cuDNN DLLs and report imgutils' real provider."""
    try:
        import onnxruntime as ort
        if hasattr(ort, "preload_dlls"):
            try:
                # Empty string explicitly searches NVIDIA pip site-packages bundled
                # in the portable runtime (nvidia-cuda-runtime / nvidia-cudnn).
                ort.preload_dlls(directory="")
            except Exception:
                try:
                    ort.preload_dlls()
                except Exception:
                    pass

        from imgutils.utils.onnxruntime import get_onnx_provider
        provider = str(get_onnx_provider())
    except Exception:
        provider = "CPUExecutionProvider"

    if provider == "CUDAExecutionProvider":
        return "GPU (CUDA)"
    if provider == "CPUExecutionProvider":
        return "CPU"
    return provider


def detect(image, args, stage):
    detect_censors, detect_with_nudenet = _load_imgutils()

    stage(0.25, tr("base_detection", provider=args.provider))
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
        stage(0.58, tr("extra_detection", provider=args.provider))
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
            warning = tr("extra_failed", error=exc)

    return detections, warning


def padded_box(box, padding, width, height):
    x0, y0, x1, y1 = box
    return (
        max(0, int(x0) - padding),
        max(0, int(y0) - padding),
        min(width, int(x1) + padding),
        min(height, int(y1) + padding),
    )


def _rect_fallback_mask(size, relative_rect):
    width, height = size
    x, y, w, h = relative_rect
    mask = np.zeros((height, width), dtype=np.uint8)
    x0 = max(0, min(width, x))
    y0 = max(0, min(height, y))
    x1 = max(x0, min(width, x + w))
    y1 = max(y0, min(height, y + h))
    mask[y0:y1, x0:x1] = 255
    return Image.fromarray(mask, mode="L")


def build_precise_region_mask(image, context_box, detection_box):
    """Segment actual foreground pixels inside the detector hint box."""
    cx0, cy0, cx1, cy1 = context_box
    dx0, dy0, dx1, dy1 = detection_box
    crop = image.crop(context_box).convert("RGB")
    width, height = crop.size

    if width < 3 or height < 3:
        return Image.new("L", crop.size, 255)

    rx0 = max(0, min(width - 1, dx0 - cx0))
    ry0 = max(0, min(height - 1, dy0 - cy0))
    rx1 = max(rx0 + 1, min(width, dx1 - cx0))
    ry1 = max(ry0 + 1, min(height, dy1 - cy0))

    gx = max(1, min(width - 2, rx0)) if width > 2 else 0
    gy = max(1, min(height - 2, ry0)) if height > 2 else 0
    gr = max(gx + 1, min(width - 1, rx1))
    gb = max(gy + 1, min(height - 1, ry1))
    gw = gr - gx
    gh = gb - gy

    if gw < 2 or gh < 2:
        return _rect_fallback_mask(crop.size, (rx0, ry0, rx1 - rx0, ry1 - ry0))

    rgb = np.asarray(crop, dtype=np.uint8)
    bgr = cv2.cvtColor(rgb, cv2.COLOR_RGB2BGR)
    grab_mask = np.zeros((height, width), dtype=np.uint8)
    bg_model = np.zeros((1, 65), dtype=np.float64)
    fg_model = np.zeros((1, 65), dtype=np.float64)

    try:
        cv2.grabCut(
            bgr,
            grab_mask,
            (gx, gy, gw, gh),
            bg_model,
            fg_model,
            5,
            cv2.GC_INIT_WITH_RECT,
        )
        binary = np.where(
            (grab_mask == cv2.GC_FGD) | (grab_mask == cv2.GC_PR_FGD),
            255,
            0,
        ).astype(np.uint8)

        bounds = np.zeros_like(binary)
        bounds[ry0:ry1, rx0:rx1] = 255
        binary = cv2.bitwise_and(binary, bounds)

        min_side = max(1, min(rx1 - rx0, ry1 - ry0))
        kernel_size = 3 if min_side < 80 else 5
        kernel = np.ones((kernel_size, kernel_size), dtype=np.uint8)
        binary = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel, iterations=1)
        binary = cv2.morphologyEx(binary, cv2.MORPH_OPEN, kernel, iterations=1)

        detector_area = max(1, (rx1 - rx0) * (ry1 - ry0))
        foreground_area = int(np.count_nonzero(binary))
        if foreground_area < max(8, int(detector_area * 0.02)):
            return _rect_fallback_mask(crop.size, (rx0, ry0, rx1 - rx0, ry1 - ry0))

        return Image.fromarray(binary, mode="L")
    except cv2.error:
        return _rect_fallback_mask(crop.size, (rx0, ry0, rx1 - rx0, ry1 - ry0))


def apply_effect(image, detections, mode, strength, padding):
    result = image.copy()

    for box, _label, _score in detections:
        detection_box = padded_box(box, 0, image.width, image.height)
        context_padding = max(8, padding)
        context_box = padded_box(box, context_padding, image.width, image.height)
        x0, y0, x1, y1 = context_box
        if x1 <= x0 or y1 <= y0:
            continue

        crop = image.crop(context_box)
        if min(crop.size) < 4:
            continue

        if mode == "black":
            effected = Image.new(crop.mode, crop.size, "black")
        elif mode == "blur":
            effected = crop.filter(ImageFilter.GaussianBlur(radius=max(1, strength)))
        else:
            block = max(2, min(strength, min(crop.size)))
            small = crop.resize(
                (max(1, crop.width // block), max(1, crop.height // block)),
                Image.Resampling.BOX,
            )
            effected = small.resize(crop.size, Image.Resampling.NEAREST)

        mask = build_precise_region_mask(image, context_box, detection_box)
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

    stage(0.02, tr("reading", name=input_path.name, provider=args.provider))
    with Image.open(input_path) as opened:
        image = ImageOps.exif_transpose(opened).copy()

    detections, warning = detect(image, args, stage)
    if not detections:
        stage(0.90, tr("organizing", name=input_path.name))
        if args.copy_undetected:
            output_path.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(input_path, output_path)
        stage(1.0, tr("none", name=input_path.name))
        return {"status": "undetected", "count": 0, "provider": args.provider, "warning": warning}

    stage(0.72, tr("segmenting", name=input_path.name, count=len(detections)))
    result = apply_effect(image, detections, args.mode, args.strength, args.padding)

    stage(0.90, tr("saving", name=input_path.name))
    save_image(result, output_path)
    stage(1.0, tr("item_done", name=input_path.name))
    return {"status": "success", "count": len(detections), "provider": args.provider, "warning": warning}


def emit(payload, exit_code=0):
    print(json.dumps(payload, ensure_ascii=False), flush=True)
    raise SystemExit(exit_code)


def main():
    args = parse_args()
    if not 0.0 <= args.confidence <= 1.0:
        emit({"status": "error", "error": tr("confidence")}, 2)
    if args.padding < 0 or args.strength < 1:
        emit({"status": "error", "error": tr("padding")}, 2)

    args.provider = get_runtime_provider_label()
    emit_progress(1, tr("device", provider=args.provider))

    if args.input_file:
        if not args.output_file:
            emit({"status": "error", "error": tr("need_output_file")}, 2)
        try:
            result = process_one(args.input_file.resolve(), args.output_file.resolve(), args, 5, 98)
            emit_progress(100, tr("done", provider=args.provider))
            emit(result)
        except Exception as exc:
            emit({"status": "error", "provider": args.provider, "error": str(exc)}, 1)

    if not args.output_dir:
        emit({"status": "error", "error": tr("need_output_dir")}, 2)
    input_dir = args.input_dir.resolve()
    output_dir = args.output_dir.resolve()
    if not input_dir.is_dir():
        emit({"status": "error", "error": tr("missing_dir", path=input_dir)}, 2)
    output_dir.mkdir(parents=True, exist_ok=True)

    files = sorted(
        (p for p in input_dir.iterdir() if p.is_file() and p.suffix.lower() in SUPPORTED_EXTENSIONS),
        key=lambda p: p.name.lower(),
    )
    if not files:
        emit_progress(100, tr("no_images"))
        emit({"status": "success", "provider": args.provider, "count": 0, "processed": 0, "undetected": 0, "errors": 0})

    processed = undetected = errors = total_count = 0
    warnings = []
    total = len(files)
    emit_progress(2, tr("batch_prepare", total=total, provider=args.provider))

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

    emit_progress(100, tr("batch_done", total=total, provider=args.provider))
    emit({
        "status": "success" if errors == 0 else "partial",
        "provider": args.provider,
        "count": total_count,
        "processed": processed,
        "undetected": undetected,
        "errors": errors,
        "warning": "; ".join(dict.fromkeys(warnings)) if warnings else None,
    })


if __name__ == "__main__":
    main()
