import os
import subprocess
import shutil
from PIL import Image, ImageDraw, ImageFilter
import numpy as np

FFMPEG = r"C:\adb_run\bin\ffmpeg.exe" if os.path.exists(r"C:\adb_run\bin\ffmpeg.exe") else "ffmpeg"

def extract_frames(video_path, fps=13, start=0.0, duration=None):
    temp_dir = f"scratch/frames_{os.path.basename(video_path).split('.')[0]}"
    os.makedirs(temp_dir, exist_ok=True)
    pattern = os.path.join(temp_dir, "frame_%04d.png")
    
    cmd = [FFMPEG, "-y"]
    if start is not None:
        cmd.extend(["-ss", str(start)])
    if duration is not None:
        cmd.extend(["-t", str(duration)])
    cmd.extend(["-i", video_path, "-vf", f"fps={fps}", pattern])
    
    subprocess.run(cmd, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    files = sorted([os.path.join(temp_dir, f) for f in os.listdir(temp_dir) if f.endswith(".png")])
    frames = [Image.open(f).convert("RGBA") for f in files]
    return frames, temp_dir

def clean_master_wave_frame(img: Image.Image) -> Image.Image:
    arr = np.array(img, dtype=np.float32)
    h, w, _ = arr.shape
    
    # Candidate white background (> 235 on all channels)
    is_white = (arr[:, :, 0] > 235) & (arr[:, :, 1] > 235) & (arr[:, :, 2] > 235)
    bin_im = Image.fromarray(np.where(is_white, 0, 255).astype(np.uint8), mode="L").copy()
    
    # 1. Border floodfill
    for x in range(0, w, 4):
        if bin_im.getpixel((x, 0)) == 0: ImageDraw.floodfill(bin_im, (x, 0), 128)
        if bin_im.getpixel((x, h - 1)) == 0: ImageDraw.floodfill(bin_im, (x, h - 1), 128)
    for y in range(0, h, 4):
        if bin_im.getpixel((0, y)) == 0: ImageDraw.floodfill(bin_im, (0, y), 128)
        if bin_im.getpixel((w - 1, y)) == 0: ImageDraw.floodfill(bin_im, (w - 1, y), 128)
        
    bin_arr = np.array(bin_im)
    unreached = (bin_arr == 0)
    marked = bin_im.copy()
    unreached_coords = np.argwhere(unreached)
    
    # For wave (1080x1920):
    # Eyes and teeth are strictly within X in [240, 670], Y in [400, 740]
    # Trapped background is under elbow (Y > 950) or between paw/cheek (X > 670)
    for y, x in unreached_coords:
        if marked.getpixel((x, y)) == 0:
            ImageDraw.floodfill(marked, (x, y), 200)
            cmask = np.array(marked) == 200
            pts = np.argwhere(cmask)
            ymin, xmin = pts.min(axis=0)
            ymax, xmax = pts.max(axis=0)
            cx, cy = (xmin + xmax) // 2, (ymin + ymax) // 2
            
            is_face_feature = (240 <= cx <= 670) and (400 <= cy <= 740)
            if not is_face_feature:
                ImageDraw.floodfill(bin_im, (x, y), 128)
                
            ImageDraw.floodfill(marked, (x, y), 201)
            
    hard_alpha = np.where(np.array(bin_im) == 128, 0, 255).astype(np.uint8)
    alpha_im = Image.fromarray(hard_alpha, mode="L").filter(ImageFilter.GaussianBlur(0.8))
    rgba = np.dstack([arr[:, :, :3], np.array(alpha_im)]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")

def clean_master_sad_frame(img: Image.Image) -> Image.Image:
    arr = np.array(img, dtype=np.float32)
    h, w, _ = arr.shape
    
    is_white = (arr[:, :, 0] > 235) & (arr[:, :, 1] > 235) & (arr[:, :, 2] > 235)
    bin_im = Image.fromarray(np.where(is_white, 0, 255).astype(np.uint8), mode="L").copy()
    
    # 1. Border floodfill
    for x in range(0, w, 4):
        if bin_im.getpixel((x, 0)) == 0: ImageDraw.floodfill(bin_im, (x, 0), 128)
        if bin_im.getpixel((x, h - 1)) == 0: ImageDraw.floodfill(bin_im, (x, h - 1), 128)
    for y in range(0, h, 4):
        if bin_im.getpixel((0, y)) == 0: ImageDraw.floodfill(bin_im, (0, y), 128)
        if bin_im.getpixel((w - 1, y)) == 0: ImageDraw.floodfill(bin_im, (w - 1, y), 128)
        
    bin_arr = np.array(bin_im)
    unreached = (bin_arr == 0)
    marked = bin_im.copy()
    unreached_coords = np.argwhere(unreached)
    
    # For sad (720x1280):
    # Eyes are in the face: Y in [400, 700], X in [180, 560]
    # Trapped background is below the waist: Y > 800
    for y, x in unreached_coords:
        if marked.getpixel((x, y)) == 0:
            ImageDraw.floodfill(marked, (x, y), 200)
            cmask = np.array(marked) == 200
            pts = np.argwhere(cmask)
            ymin, xmin = pts.min(axis=0)
            ymax, xmax = pts.max(axis=0)
            cx, cy = (xmin + xmax) // 2, (ymin + ymax) // 2
            
            is_face_feature = (180 <= cx <= 560) and (400 <= cy <= 750)
            if not is_face_feature:
                ImageDraw.floodfill(bin_im, (x, y), 128)
                
            ImageDraw.floodfill(marked, (x, y), 201)
            
    hard_alpha = np.where(np.array(bin_im) == 128, 0, 255).astype(np.uint8)
    alpha_im = Image.fromarray(hard_alpha, mode="L").filter(ImageFilter.GaussianBlur(0.8))
    rgba = np.dstack([arr[:, :, :3], np.array(alpha_im)]).astype(np.uint8)
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
        canvas = Image.new("RGBA", target_size, (0, 0, 0, 0))
        offset_y = tgt_h - new_h - 8
        canvas.paste(resized, (0, max(0, offset_y)), resized)
        processed.append(canvas)
    return processed

def save_transparent_gif(frames, output_path, fps=13, loop=0):
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
        transparency=255,
        disposal=2,
        optimize=True,
        loop=loop
    )
    print(f"[OK] Saved GIF: {output_path} ({os.path.getsize(output_path)} bytes, {len(frames)} frames)")

def build_master_animations():
    img_dir = os.path.join("FinAPP", "Resources", "Images")
    raw_dir = os.path.join("FinAPP", "Resources", "Raw")
    os.makedirs(img_dir, exist_ok=True)
    os.makedirs(raw_dir, exist_ok=True)
    
    # 1. finny_master_wave
    print("\n--- 1. Processing finny_master_wave ---")
    raw_wave_frames, dir_wave = extract_frames(
        "sources/70322129_1789635247890053.mp4",
        fps=13, start=1.2, duration=2.4
    )
    clean_wave = [clean_master_wave_frame(f) for f in raw_wave_frames]
    resized_wave = crop_and_resize_frames(clean_wave, target_size=(360, 480))
    
    wave_img_path = os.path.join(img_dir, "finny_master_wave.gif")
    wave_raw_path = os.path.join(raw_dir, "finny_master_wave.gif")
    save_transparent_gif(resized_wave, wave_img_path, fps=13, loop=0)
    shutil.copy2(wave_img_path, wave_raw_path)
    print(f"Synced wave to Raw: {wave_raw_path}")
    
    # 2. finny_master_sad
    print("\n--- 2. Processing finny_master_sad from new video ---")
    raw_sad_frames, dir_sad = extract_frames(
        "sources/171991838_1789999841913639.mp4",
        fps=13, start=0.0, duration=2.25
    )
    clean_sad = [clean_master_sad_frame(f) for f in raw_sad_frames]
    resized_sad = crop_and_resize_frames(clean_sad, target_size=(360, 480))
    
    sad_img_path = os.path.join(img_dir, "finny_master_sad.gif")
    sad_raw_path = os.path.join(raw_dir, "finny_master_sad.gif")
    save_transparent_gif(resized_sad, sad_img_path, fps=13, loop=0)
    shutil.copy2(sad_img_path, sad_raw_path)
    print(f"Synced sad to Raw: {sad_raw_path}")
    
    print("\n[SUCCESS] Master animations updated successfully!")

if __name__ == "__main__":
    build_master_animations()
