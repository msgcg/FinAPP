"""
Generate smooth, high-quality initial animated GIFs for FinAPP
Creates:
- FinAPP/Resources/Images/finny_idle.gif
- FinAPP/Resources/Images/finny_wave.gif
- FinAPP/Resources/Images/finny_proud.gif
- FinAPP/Resources/Images/finny_sad.gif
"""

import math
import os
import numpy as np
from PIL import Image, ImageChops, ImageEnhance
from process_finny_gif import save_transparent_gif, crop_and_resize_frames

def create_idle_frames(base_img, num_frames=20, target_size=(448, 600)):
    # Base breathing & subtle sway loop
    frames = []
    w, h = base_img.size
    
    for i in range(num_frames):
        t = (i / num_frames) * 2 * math.pi
        # Sinusoidal breathing: expands slightly vertically, contracts slightly horizontally
        scale_y = 1.0 + 0.022 * math.sin(t)
        scale_x = 1.0 - 0.012 * math.sin(t)
        
        # Subtle horizontal sway
        sway_angle = 1.2 * math.sin(t)
        
        new_w = int(w * scale_x)
        new_h = int(h * scale_y)
        
        # Resize with anchor at bottom
        resized = base_img.resize((new_w, new_h), Image.Resampling.LANCZOS)
        
        # Canvas
        canvas = Image.new("RGBA", (w + 40, h + 40), (0, 0, 0, 0))
        # Place anchored at bottom-center
        offset_x = (canvas.width - new_w) // 2
        offset_y = canvas.height - 20 - new_h
        canvas.paste(resized, (offset_x, offset_y), resized)
        
        # Rotate slightly anchored near bottom
        rotated = canvas.rotate(sway_angle, resample=Image.Resampling.BICUBIC, center=(canvas.width // 2, canvas.height - 20))
        
        frames.append(rotated)
        
    return crop_and_resize_frames(frames, target_size=target_size)

def create_wave_frames(base_img, num_frames=16, target_size=(448, 600)):
    # Cheerful bouncing wave loop
    frames = []
    w, h = base_img.size
    
    for i in range(num_frames):
        t = (i / num_frames) * 2 * math.pi
        # Double-bounce rhythm
        bounce_y = -12 * abs(math.sin(t))
        tilt = 3.5 * math.sin(t)
        scale = 1.0 + 0.03 * math.sin(t * 2)
        
        new_w = int(w * scale)
        new_h = int(h * scale)
        resized = base_img.resize((new_w, new_h), Image.Resampling.LANCZOS)
        
        canvas = Image.new("RGBA", (w + 60, h + 60), (0, 0, 0, 0))
        offset_x = (canvas.width - new_w) // 2
        offset_y = int(canvas.height - 30 - new_h + bounce_y)
        canvas.paste(resized, (offset_x, offset_y), resized)
        
        rotated = canvas.rotate(tilt, resample=Image.Resampling.BICUBIC, center=(canvas.width // 2, canvas.height - 30))
        frames.append(rotated)
        
    return crop_and_resize_frames(frames, target_size=target_size)

def create_proud_frames(base_img, num_frames=18, target_size=(448, 600)):
    # Confident chest swell and subtle glow pulse
    frames = []
    w, h = base_img.size
    
    for i in range(num_frames):
        t = (i / num_frames) * 2 * math.pi
        scale_y = 1.0 + 0.035 * (0.5 + 0.5 * math.sin(t))
        scale_x = 1.0 + 0.015 * (0.5 + 0.5 * math.sin(t))
        tilt = 1.5 * math.sin(t)
        
        new_w = int(w * scale_x)
        new_h = int(h * scale_y)
        resized = base_img.resize((new_w, new_h), Image.Resampling.LANCZOS)
        
        canvas = Image.new("RGBA", (w + 50, h + 50), (0, 0, 0, 0))
        offset_x = (canvas.width - new_w) // 2
        offset_y = canvas.height - 25 - new_h
        canvas.paste(resized, (offset_x, offset_y), resized)
        
        rotated = canvas.rotate(tilt, resample=Image.Resampling.BICUBIC, center=(canvas.width // 2, canvas.height - 25))
        frames.append(rotated)
        
    return crop_and_resize_frames(frames, target_size=target_size)

def create_sad_frames(base_img, num_frames=20, target_size=(448, 600)):
    # Drooping, slow heavy breathing
    frames = []
    w, h = base_img.size
    
    for i in range(num_frames):
        t = (i / num_frames) * 2 * math.pi
        scale_y = 1.0 - 0.02 * (0.5 + 0.5 * math.sin(t))
        scale_x = 1.0 + 0.01 * (0.5 + 0.5 * math.sin(t))
        drop_y = 4 * (0.5 + 0.5 * math.sin(t))
        
        new_w = int(w * scale_x)
        new_h = int(h * scale_y)
        resized = base_img.resize((new_w, new_h), Image.Resampling.LANCZOS)
        
        canvas = Image.new("RGBA", (w + 40, h + 40), (0, 0, 0, 0))
        offset_x = (canvas.width - new_w) // 2
        offset_y = int(canvas.height - 20 - new_h + drop_y)
        canvas.paste(resized, (offset_x, offset_y), resized)
        
        frames.append(canvas)
        
    return crop_and_resize_frames(frames, target_size=target_size)

def main():
    base_happy = Image.open("FinAPP/Resources/Images/finny_happy.png").convert("RGBA")
    base_proud = Image.open("FinAPP/Resources/Images/finny_proud.png").convert("RGBA")
    base_sad = Image.open("FinAPP/Resources/Images/finny_sad.png").convert("RGBA")
    
    out_dir = os.path.join("FinAPP", "Resources", "Images")
    
    print("Generating finny_idle.gif...")
    idle_frames = create_idle_frames(base_happy, num_frames=20)
    save_transparent_gif(idle_frames, os.path.join(out_dir, "finny_idle.gif"), fps=12)
    
    print("Generating finny_wave.gif...")
    wave_frames = create_wave_frames(base_happy, num_frames=16)
    save_transparent_gif(wave_frames, os.path.join(out_dir, "finny_wave.gif"), fps=14)
    
    print("Generating finny_proud.gif...")
    proud_frames = create_proud_frames(base_proud, num_frames=18)
    save_transparent_gif(proud_frames, os.path.join(out_dir, "finny_proud.gif"), fps=12)
    
    print("Generating finny_sad.gif...")
    sad_frames = create_sad_frames(base_sad, num_frames=20)
    save_transparent_gif(sad_frames, os.path.join(out_dir, "finny_sad.gif"), fps=10)
    
    print("All initial animated GIFs successfully created!")

if __name__ == "__main__":
    main()
