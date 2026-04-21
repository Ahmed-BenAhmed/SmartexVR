#!/usr/bin/env python3
"""
Generate printable marker images + a PDF sheet + a machines CSV for every
device listed in markers.yaml.

    python generate.py [--config markers.yaml] [--out out/]

The QR payload encodes `smartex://machine/<device_id>` so a phone camera can
also pick it up as a deep link — useful as a fallback when Vuforia isn't
loaded.

Image format (tuned for Vuforia image-target recommendations):
  - 1024×1024 px PNG
  - High contrast (black on white), wide quiet zone, device label underneath
  - Small corner fiducials to give Vuforia extra feature points beyond the
    QR squares themselves — Vuforia scores pure QRs poorly because all the
    gradient is in one frequency band.

Nothing here talks to Vuforia's VWS API. That needs a server-access-key per
database; we generate the bitmaps + a CSV and a human does the upload.
"""
from __future__ import annotations

import argparse
import csv
import os
import sys
from pathlib import Path

try:
    import qrcode
    from qrcode.constants import ERROR_CORRECT_H
    from PIL import Image, ImageDraw, ImageFont
    import yaml
    from reportlab.lib.pagesizes import A4
    from reportlab.lib.units import mm
    from reportlab.pdfgen import canvas
except ImportError as e:
    print(f"Missing dependency: {e}. Run: pip install -r requirements.txt", file=sys.stderr)
    sys.exit(1)


TARGET_SIZE   = 1024          # px — Vuforia likes >= 320; we overshoot for print DPI.
QUIET_ZONE    = 64            # px of white margin on every side.
LABEL_HEIGHT  = 120           # px reserved under the QR for device label.
FIDUCIAL_SIZE = 48            # corner fiducial squares — help Vuforia lock on.


def load_markers(path: Path) -> list[dict]:
    with path.open("r", encoding="utf-8") as f:
        data = yaml.safe_load(f)
    markers = data.get("markers") or []
    if not markers:
        raise SystemExit(f"No markers found in {path}")
    # Sanity-check required keys up front so errors are local.
    for m in markers:
        for k in ("device_id", "machine_id", "display_name"):
            if k not in m:
                raise SystemExit(f"Marker is missing '{k}': {m}")
    return markers


def pick_font(size: int) -> ImageFont.ImageFont:
    # Try a few common fonts before falling back to the bundled default.
    for name in ("DejaVuSans-Bold.ttf", "Arial Bold.ttf", "arialbd.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except (OSError, IOError):
            continue
    return ImageFont.load_default()


def render_marker(device_id: str, display_name: str, out_path: Path) -> None:
    payload = f"smartex://machine/{device_id}"

    qr = qrcode.QRCode(error_correction=ERROR_CORRECT_H, box_size=16, border=0)
    qr.add_data(payload)
    qr.make(fit=True)
    qr_img = qr.make_image(fill_color="black", back_color="white").convert("RGB")

    # Fit the QR into the inner area (outside the quiet zone, above the label).
    inner = TARGET_SIZE - 2 * QUIET_ZONE
    qr_area = inner - LABEL_HEIGHT
    qr_img = qr_img.resize((qr_area, qr_area), Image.NEAREST)

    canvas_img = Image.new("RGB", (TARGET_SIZE, TARGET_SIZE), "white")
    canvas_img.paste(qr_img, (QUIET_ZONE, QUIET_ZONE))

    draw = ImageDraw.Draw(canvas_img)

    # Corner fiducials — black squares in each corner, outside the QR. These
    # give Vuforia extra high-contrast feature points.
    for cx, cy in [
        (QUIET_ZONE // 2, QUIET_ZONE // 2),
        (TARGET_SIZE - QUIET_ZONE // 2, QUIET_ZONE // 2),
        (QUIET_ZONE // 2, TARGET_SIZE - QUIET_ZONE // 2 - LABEL_HEIGHT),
        (TARGET_SIZE - QUIET_ZONE // 2, TARGET_SIZE - QUIET_ZONE // 2 - LABEL_HEIGHT),
    ]:
        half = FIDUCIAL_SIZE // 2
        draw.rectangle([cx - half, cy - half, cx + half, cy + half], fill="black")

    # Label band under the QR.
    label_y = TARGET_SIZE - QUIET_ZONE - LABEL_HEIGHT
    draw.rectangle([0, label_y, TARGET_SIZE, TARGET_SIZE], fill="white")

    font_big   = pick_font(58)
    font_small = pick_font(34)

    name_bbox = draw.textbbox((0, 0), display_name, font=font_big)
    name_w    = name_bbox[2] - name_bbox[0]
    draw.text(((TARGET_SIZE - name_w) / 2, label_y + 10), display_name, fill="black", font=font_big)

    id_bbox = draw.textbbox((0, 0), device_id, font=font_small)
    id_w    = id_bbox[2] - id_bbox[0]
    draw.text(((TARGET_SIZE - id_w) / 2, label_y + 80), device_id, fill="black", font=font_small)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    canvas_img.save(out_path, "PNG", optimize=True)


def render_pdf_sheet(markers: list[dict], out_dir: Path, pdf_path: Path) -> None:
    """Lay out all marker PNGs into an A4 PDF, 2 across × 3 down per page."""
    c = canvas.Canvas(str(pdf_path), pagesize=A4)
    page_w, page_h = A4
    cols, rows    = 2, 3
    margin        = 15 * mm
    cell_w        = (page_w - 2 * margin) / cols
    cell_h        = (page_h - 2 * margin) / rows
    img_side      = min(cell_w, cell_h) - 10 * mm

    for idx, m in enumerate(markers):
        cell = idx % (cols * rows)
        if idx != 0 and cell == 0:
            c.showPage()
        col = cell % cols
        row = cell // cols
        x = margin + col * cell_w + (cell_w - img_side) / 2
        y = page_h - margin - (row + 1) * cell_h + (cell_h - img_side) / 2
        img_path = out_dir / f"{m['device_id']}.png"
        c.drawImage(str(img_path), x, y, width=img_side, height=img_side)
    c.save()


def write_mapping_csv(markers: list[dict], csv_path: Path) -> None:
    with csv_path.open("w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["vuforia_target_name", "device_id", "machine_id", "display_name"])
        for m in markers:
            # We adopt device_id as the Vuforia target name — Module B's
            # recognizer maps TargetName 1-1 to deviceId.
            w.writerow([m["device_id"], m["device_id"], m["machine_id"], m["display_name"]])


def main() -> int:
    p = argparse.ArgumentParser(description="Generate Vuforia-friendly printable markers.")
    p.add_argument("--config", default="markers.yaml", type=Path)
    p.add_argument("--out",    default="out",          type=Path)
    args = p.parse_args()

    markers = load_markers(args.config)
    args.out.mkdir(parents=True, exist_ok=True)

    for m in markers:
        png_path = args.out / f"{m['device_id']}.png"
        render_marker(m["device_id"], m["display_name"], png_path)
        print(f"  wrote {png_path}")

    pdf_path = args.out / "markers.pdf"
    render_pdf_sheet(markers, args.out, pdf_path)
    print(f"  wrote {pdf_path}")

    csv_path = args.out / "machines_database_spec.csv"
    write_mapping_csv(markers, csv_path)
    print(f"  wrote {csv_path}")

    print(f"\nDone. {len(markers)} marker(s) generated in {args.out}/.")
    print("Next: upload each .png to Vuforia Target Manager under database")
    print("'SmartexMachines' (or whatever ARConfig.vuforiaDatabaseName is set to).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
