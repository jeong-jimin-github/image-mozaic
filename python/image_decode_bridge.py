from __future__ import annotations

import argparse
import os
import struct
from pathlib import Path

from PIL import Image, ImageOps

MAGIC = b"IMBG"


def parse_args():
    parser = argparse.ArgumentParser()
    parser.add_argument("--input-file", type=Path, required=True)
    parser.add_argument("--output-file", type=Path, required=True)
    return parser.parse_args()


def main():
    args = parse_args()
    source = args.input_file.resolve()
    target = args.output_file.resolve()

    if not source.is_file():
        raise FileNotFoundError(source)

    target.parent.mkdir(parents=True, exist_ok=True)
    temp = target.with_suffix(target.suffix + ".tmp")

    try:
        with Image.open(source) as opened:
            image = ImageOps.exif_transpose(opened)
            image.load()
            if image.mode != "RGBA":
                image = image.convert("RGBA")

            width, height = image.size
            pixels = image.tobytes("raw", "BGRA")

        with temp.open("wb") as fp:
            fp.write(MAGIC)
            fp.write(struct.pack("<II", width, height))
            fp.write(pixels)

        os.replace(temp, target)
        print(f"{width}x{height}")
    finally:
        try:
            if temp.exists():
                temp.unlink()
        except OSError:
            pass


if __name__ == "__main__":
    main()
