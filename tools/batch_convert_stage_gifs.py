import os
import shutil
from process_finny_gif import process_video_to_gif

out_img_dir = os.path.join("FinAPP", "Resources", "Images")
out_raw_dir = os.path.join("FinAPP", "Resources", "Raw")
os.makedirs(out_img_dir, exist_ok=True)
os.makedirs(out_raw_dir, exist_ok=True)

conversions = [
    # 1. Baby stage (Малыш - chubby toddler kitten, big head, large round eyes, oversized teal hoodie, no jeans)
    {
        "name": "finny_baby_idle",
        "video": "sources/951400593_1789636534254383.mp4",
        "start": 0.2, "duration": 2.2, "fps": 13, "key": "white"
    },
    {
        "name": "finny_baby_wave",
        "video": "sources/458996030_1789636552002045.mp4",
        "start": 0.2, "duration": 2.5, "fps": 13, "key": "green"
    },
    {
        "name": "finny_baby_proud",
        "video": "sources/532084969_1789636568567184.mp4",
        "start": 0.2, "duration": 2.2, "fps": 13, "key": "green"
    },
    {
        "name": "finny_baby_sad",
        "video": "sources/368697115_1789636573906018.mp4",
        "start": 0.2, "duration": 2.4, "fps": 13, "key": "green"
    },

    # 2. Teen stage (Юниор - school-age cat in blue jeans and white sneakers)
    {
        "name": "finny_teen_idle",
        "video": "sources/756636224_1789636388080481.mp4",
        "start": 0.2, "duration": 2.2, "fps": 13, "key": "green"
    },
    {
        "name": "finny_teen_wave",
        "video": "sources/142589458_1789636279739845.mp4",
        "start": 0.2, "duration": 2.4, "fps": 13, "key": "green"
    },
    {
        "name": "finny_teen_proud",
        "video": "sources/633314252_1789636297174330.mp4",
        "start": 0.2, "duration": 2.4, "fps": 13, "key": "green"
    },
    {
        "name": "finny_teen_sad",
        "video": "sources/586104840_1789636400198492.mp4",
        "start": 0.2, "duration": 2.4, "fps": 12, "key": "green"
    },

    # 3. Master stage (Мастер - adult hero cat with Moscow Finance lime pants)
    {
        "name": "finny_master_idle",
        "video": "sources/847602321_1789635248081390.mp4",
        "start": 0.1, "duration": 1.7, "fps": 12, "key": "green"
    },
    {
        "name": "finny_master_wave",
        "video": "sources/70322129_1789635247890053.mp4",
        "start": 1.2, "duration": 2.4, "fps": 13, "key": "white"
    },
    {
        "name": "finny_master_proud",
        "video": "sources/847602321_1789635248081390.mp4",
        "start": 1.8, "duration": 2.2, "fps": 13, "key": "green"
    },
    {
        "name": "finny_master_sad",
        "video": "sources/586104840_1789636324786831.mp4",
        "start": 0.2, "duration": 2.3, "fps": 12, "key": "green"
    }
]

print(f"Starting batch conversion of {len(conversions)} GIF animations...")
for idx, c in enumerate(conversions, 1):
    target_img_path = os.path.join(out_img_dir, f"{c['name']}.gif")
    target_raw_path = os.path.join(out_raw_dir, f"{c['name']}.gif")
    print(f"\n[{idx}/{len(conversions)}] Converting {c['name']} from {c['video']} (start={c['start']}s, dur={c['duration']}s)...")
    process_video_to_gif(
        video_path=c['video'],
        output_path=target_img_path,
        fps=c['fps'],
        start=c['start'],
        duration=c['duration'],
        key_mode=c['key']
    )
    # Copy to Resources/Raw for Android asset access in WebView
    shutil.copy2(target_img_path, target_raw_path)
    print(f"  -> Synced to Raw: {target_raw_path}")

# Also copy baby animations as base fallbacks (finny_idle.gif, finny_wave.gif, finny_proud.gif, finny_sad.gif)
for act in ["idle", "wave", "proud", "sad"]:
    src_img = os.path.join(out_img_dir, f"finny_baby_{act}.gif")
    dst_img = os.path.join(out_img_dir, f"finny_{act}.gif")
    dst_raw = os.path.join(out_raw_dir, f"finny_{act}.gif")
    if os.path.exists(src_img):
        shutil.copy2(src_img, dst_img)
        shutil.copy2(src_img, dst_raw)
        print(f"Copied fallback {src_img} -> {dst_img} & {dst_raw}")

print("\nAll stage animations successfully processed and deployed to Resources/Images and Resources/Raw!")
