"""
Enhanced Finny Animation Processor
Converts MP4/WebM/GIF animations into optimized transparent GIFs for FinAPP.
Uses border flood-fill background isolation to 100% preserve character clothing,
hoodies, jackets, and white muzzles. Supports edge anti-aliasing and selective despill.
"""

import os
import sys
import argparse
import subprocess
import tempfile
import shutil
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def extract_frames_ffmpeg(video_path, fps=14, start=None, duration=None):
    temp_dir = tempfile.mkdtemp(prefix="finny_frames_")
    pattern = os.path.join(temp_dir, "frame_%04d.png")
    
    cmd = ["ffmpeg", "-y"]
    if start is not None:
        cmd.extend(["-ss", str(start)])
    if duration is not None:
        cmd.extend(["-t", str(duration)])
    cmd.extend(["-i", video_path, "-vf", f"fps={fps}", pattern])
    
    try:
        subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        frame_files = sorted([os.path.join(temp_dir, f) for f in os.listdir(temp_dir) if f.startswith("frame_") and f.endswith(".png")])
        if not frame_files:
            raise RuntimeError("No frames extracted")
        frames = [Image.open(f).convert("RGBA") for f in frame_files]
        return frames, temp_dir
    except Exception as e:
        shutil.rmtree(temp_dir, ignore_errors=True)
        raise RuntimeError(f"FFmpeg extraction failed: {e}")

def detect_bg_color(frame: Image.Image):
    """Samples the 4 corners to detect background color."""
    arr = np.array(frame)
    corners = np.concatenate([
        arr[:15, :15, :3].reshape(-1, 3),
        arr[:15, -15:, :3].reshape(-1, 3),
        arr[-15:, :15, :3].reshape(-1, 3),
        arr[-15:, -15:, :3].reshape(-1, 3)
    ], axis=0)
    median_color = np.median(corners, axis=0)
    r, g, b = median_color
    
    if g > r + 18 and g > b + 18:
        return "green", median_color
    elif r > 215 and g > 215 and b > 215:
        return "white", median_color
    else:
        return "custom", median_color

def remove_background_floodfill(img: Image.Image, bg_type="green", key_rgb=None, tolerance=46, feather_radius=0.8) -> Image.Image:
    """
    Removes background using flood-fill strictly from image borders.
    Protects character's internal green clothes and white muzzle.
    """
    arr = np.array(img, dtype=np.float32)
    h, w, _ = arr.shape
    
    if bg_type == "green":
        if key_rgb is None:
            key_rgb = (55, 200, 75)
        dist = np.sqrt(np.sum((arr[:, :, :3] - key_rgb)**2, axis=2))
        is_bg_candidate = (dist < tolerance)
    else:
        # white background
        is_bg_candidate = (arr[:, :, 0] > 236) & (arr[:, :, 1] > 236) & (arr[:, :, 2] > 236)
        
    raw_mask = np.where(is_bg_candidate, 0, 255).astype(np.uint8)
    # Must use .copy() so ImageDraw.floodfill can mutate the buffer
    bin_im = Image.fromarray(raw_mask, mode="L").copy()
    
    # Flood-fill candidate background pixels connected to any outer edge
    step = 4
    for x in range(0, w, step):
        if bin_im.getpixel((x, 0)) == 0:
            ImageDraw.floodfill(bin_im, (x, 0), 128)
        if bin_im.getpixel((x, h - 1)) == 0:
            ImageDraw.floodfill(bin_im, (x, h - 1), 128)
    for y in range(0, h, step):
        if bin_im.getpixel((0, y)) == 0:
            ImageDraw.floodfill(bin_im, (0, y), 128)
        if bin_im.getpixel((w - 1, y)) == 0:
            ImageDraw.floodfill(bin_im, (w - 1, y), 128)
            
    bin_arr = np.array(bin_im)
    is_exterior = (bin_arr == 128)
    hard_alpha = np.where(is_exterior, 0, 255).astype(np.uint8)
    
    # Smooth edge feathering
    if feather_radius > 0:
        alpha_im = Image.fromarray(hard_alpha, mode="L").filter(ImageFilter.GaussianBlur(feather_radius))
        alpha_arr = np.array(alpha_im, dtype=np.float32)
    else:
        alpha_arr = hard_alpha.astype(np.float32)
        
    r = arr[:, :, 0]
    g = arr[:, :, 1]
    b = arr[:, :, 2]
    
    # Despill green halo ONLY on semi-transparent edge pixels
    if bg_type == "green":
        edge_mask = (alpha_arr > 10) & (alpha_arr < 245)
        g_clamped = np.minimum(g, (r + b) / 1.65)
        g = np.where(edge_mask, g_clamped, g)
        
    rgba = np.dstack([
        np.uint8(np.clip(r, 0, 255)),
        np.uint8(np.clip(g, 0, 255)),
        np.uint8(np.clip(b, 0, 255)),
        np.uint8(np.clip(alpha_arr, 0, 255))
    ])
    return Image.fromarray(rgba, "RGBA")

def find_global_bbox(frames, alpha_thresh=25):
    min_x, min_y = 100000, 100000
    max_x, max_y = 0, 0
    found = False
    
    for frame in frames:
        arr = np.array(frame)
        mask = arr[:, :, 3] > alpha_thresh
        if np.any(mask):
            found = True
            y_indices, x_indices = np.where(mask)
            min_x = min(min_x, int(np.min(x_indices)))
            max_x = max(max_x, int(np.max(x_indices)))
            min_y = min(min_y, int(np.min(y_indices)))
            max_y = max(max_y, int(np.max(y_indices)))
            
    if not found:
        return 0, 0, frames[0].width, frames[0].height
        
    return min_x, min_y, max_x, max_y

def crop_and_resize_frames(frames, target_size=(360, 480), pad=12):
    min_x, min_y, max_x, max_y = find_global_bbox(frames)
    
    W, H = frames[0].width, frames[0].height
    x1 = max(0, min_x - pad)
    y1 = max(0, min_y - pad)
    x2 = min(W, max_x + pad)
    y2 = min(H, max_y + pad)
    
    crop_w = max(1, x2 - x1)
    crop_h = max(1, y2 - y1)
    
    tgt_w, tgt_h = target_size
    scale = min(tgt_w / crop_w, tgt_h / crop_h)
    new_w = max(1, int(crop_w * scale))
    new_h = max(1, int(crop_h * scale))
    
    processed = []
    for frame in frames:
        cropped = frame.crop((x1, y1, x2, y2))
        resized = cropped.resize((new_w, new_h), Image.Resampling.LANCZOS)
        
        # Center in canvas anchored slightly above bottom
        canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
        offset_x = (tgt_w - new_w) // 2
        offset_y = tgt_h - new_h - 8
        canvas.paste(resized, (offset_x, max(0, offset_y)), resized)
        processed.append(canvas)
        
    return processed

def save_transparent_gif(frames, output_path, fps=14, loop=0):
    duration_ms = int(1000 / fps)
    gif_frames = []
    
    for frame in frames:
        alpha = frame.split()[3]
        mask = Image.eval(alpha, lambda a: 255 if a <= 128 else 0)
        
        rgb_img = frame.convert("RGB")
        p_frame = rgb_img.convert("P", palette=Image.Palette.ADAPTIVE, colors=254)
        p_frame.paste(255, mask)
        p_frame.info["transparency"] = 255
        gif_frames.append(p_frame)
        
    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    gif_frames[0].save(
        output_path,
        save_all=True,
        append_images=gif_frames[1:],
        duration=duration_ms,
        loop=loop,
        transparency=255,
        disposal=2,
        optimize=True
    )
    print(f"[OK] Saved GIF ({len(frames)} frames, {fps} fps): {output_path} ({os.path.getsize(output_path)} bytes)")

def process_video_to_gif(video_path, output_path, fps=14, start=None, duration=None, key_mode="auto", tolerance=46, target_size=(360, 480)):
    frames, temp_dir = extract_frames_ffmpeg(video_path, fps=fps, start=start, duration=duration)
    try:
        bg_type, detected_rgb = detect_bg_color(frames[0])
        print(f"Detected BG: {bg_type} {tuple(detected_rgb.astype(int))}")
        
        if key_mode == "auto":
            key_mode = bg_type
            
        print(f"Applying border flood-fill isolation (mode={key_mode}, tol={tolerance})...")
        frames = [
            remove_background_floodfill(
                f,
                bg_type=key_mode,
                key_rgb=detected_rgb if key_mode == "green" else None,
                tolerance=tolerance
            )
            for f in frames
        ]
            
        frames = crop_and_resize_frames(frames, target_size=target_size)
        save_transparent_gif(frames, output_path, fps=fps)
    finally:
        if temp_dir and os.path.exists(temp_dir):
            shutil.rmtree(temp_dir, ignore_errors=True)

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("input")
    parser.add_argument("--output", "-o")
    parser.add_argument("--name")
    parser.add_argument("--fps", type=int, default=14)
    parser.add_argument("--start", type=float, default=None)
    parser.add_argument("--duration", type=float, default=None)
    parser.add_argument("--key", default="auto")
    parser.add_argument("--tolerance", type=int, default=46)
    args = parser.parse_args()
    
    if args.output:
        out = args.output
    elif args.name:
        out = os.path.join("FinAPP", "Resources", "Images", f"{args.name}.gif")
    else:
        out = "output.gif"
        
    process_video_to_gif(
        args.input, out,
        fps=args.fps, start=args.start, duration=args.duration,
        key_mode=args.key, tolerance=args.tolerance
    )
