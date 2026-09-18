using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace FinAPP.Services
{
    public class AudioService : IAudioService
    {
        private static IAudioService? _instance;
        public static IAudioService Instance => _instance ??= new AudioService();

        private const string PrefSfxKey = "audio_sfx_enabled";
        private const string PrefMusicKey = "audio_music_enabled";

        private bool _isSfxEnabled;
        private bool _isMusicEnabled;
        private bool _isInitialized;
        private readonly object _lock = new();

#if ANDROID
        private Android.Media.SoundPool? _soundPool;
        private Android.Media.MediaPlayer? _bgmPlayer;
        private readonly Dictionary<string, int> _soundIds = new(StringComparer.OrdinalIgnoreCase);
#endif

        public bool IsSfxEnabled
        {
            get => _isSfxEnabled;
            set
            {
                if (_isSfxEnabled != value)
                {
                    _isSfxEnabled = value;
                    Preferences.Default.Set(PrefSfxKey, value);
                }
            }
        }

        public bool IsMusicEnabled
        {
            get => _isMusicEnabled;
            set
            {
                if (_isMusicEnabled != value)
                {
                    _isMusicEnabled = value;
                    Preferences.Default.Set(PrefMusicKey, value);
                    if (value)
                    {
                        ResumeMusic();
                    }
                    else
                    {
                        StopMusic();
                    }
                }
            }
        }

        public AudioService()
        {
            _isSfxEnabled = Preferences.Default.Get(PrefSfxKey, true);
            _isMusicEnabled = Preferences.Default.Get(PrefMusicKey, true);

#if ANDROID
            _ = InitializeAndroidAudioAsync();
#endif
        }

#if ANDROID
        private async Task InitializeAndroidAudioAsync()
        {
            if (_isInitialized) return;

            try
            {
                var audioDir = Path.Combine(FileSystem.CacheDirectory, "audio");
                Directory.CreateDirectory(audioDir);

                string[] audioFiles =
                [
                    "sfx_money.mp3",
                    "sfx_success.mp3",
                    "sfx_error.mp3",
                    "sfx_meow.mp3",
                    "sfx_purr.mp3",
                    "bgm_idle.mp3"
                ];

                foreach (var file in audioFiles)
                {
                    var targetPath = Path.Combine(audioDir, file);
                    try
                    {
                        using var srcStream = await FileSystem.OpenAppPackageFileAsync($"audio/{file}");
                        if (!File.Exists(targetPath) || new FileInfo(targetPath).Length != srcStream.Length)
                        {
                            using var dstStream = File.Create(targetPath);
                            await srcStream.CopyToAsync(dstStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Audio asset copy error {file}: {ex.Message}");
                    }
                }

                // Initialize SoundPool for SFX
                var audioAttributes = new Android.Media.AudioAttributes.Builder()
                    ?.SetUsage(Android.Media.AudioUsageKind.Game)
                    ?.SetContentType(Android.Media.AudioContentType.Sonification)
                    ?.Build();

                if (audioAttributes != null)
                {
                    _soundPool = new Android.Media.SoundPool.Builder()
                        ?.SetMaxStreams(6)
                        ?.SetAudioAttributes(audioAttributes)
                        ?.Build();
                }

                if (_soundPool != null)
                {
                    foreach (var file in audioFiles)
                    {
                        if (file.StartsWith("sfx_", StringComparison.OrdinalIgnoreCase))
                        {
                            var targetPath = Path.Combine(audioDir, file);
                            if (File.Exists(targetPath))
                            {
                                int soundId = _soundPool.Load(targetPath, 1);
                                string keyName = Path.GetFileNameWithoutExtension(file);
                                _soundIds[keyName] = soundId;
                                _soundIds[file] = soundId;
                            }
                        }
                    }
                }

                _isInitialized = true;

                // Start music if enabled
                if (_isMusicEnabled)
                {
                    PlayMusic("bgm_idle");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService initialization failed: {ex.Message}");
            }
        }
#endif

        public void PlaySfx(string soundName)
        {
            if (!_isSfxEnabled || string.IsNullOrWhiteSpace(soundName)) return;

#if ANDROID
            try
            {
                lock (_lock)
                {
                    string key = soundName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) 
                        ? Path.GetFileNameWithoutExtension(soundName) 
                        : soundName;

                    if (_soundPool != null && _soundIds.TryGetValue(key, out int soundId) && soundId > 0)
                    {
                        _soundPool.Play(soundId, 1.0f, 1.0f, 1, 0, 1.0f);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaySfx error {soundName}: {ex.Message}");
            }
#endif
        }

        public void PlayMusic(string musicName = "bgm_idle")
        {
            if (!_isMusicEnabled) return;

#if ANDROID
            try
            {
                lock (_lock)
                {
                    var audioDir = Path.Combine(FileSystem.CacheDirectory, "audio");
                    string fileName = musicName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) 
                        ? musicName 
                        : $"{musicName}.mp3";
                    string filePath = Path.Combine(audioDir, fileName);

                    if (!File.Exists(filePath)) return;

                    if (_bgmPlayer == null)
                    {
                        _bgmPlayer = new Android.Media.MediaPlayer();
                        _bgmPlayer.SetAudioAttributes(
                            new Android.Media.AudioAttributes.Builder()
                                ?.SetUsage(Android.Media.AudioUsageKind.Game)
                                ?.SetContentType(Android.Media.AudioContentType.Music)
                                ?.Build());
                    }
                    else
                    {
                        _bgmPlayer.Reset();
                    }

                    _bgmPlayer.SetDataSource(filePath);
                    _bgmPlayer.Looping = true;
                    _bgmPlayer.SetVolume(0.35f, 0.35f);
                    _bgmPlayer.Prepare();
                    _bgmPlayer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlayMusic error {musicName}: {ex.Message}");
            }
#endif
        }

        public void StopMusic()
        {
#if ANDROID
            try
            {
                lock (_lock)
                {
                    if (_bgmPlayer != null && _bgmPlayer.IsPlaying)
                    {
                        _bgmPlayer.Pause();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"StopMusic error: {ex.Message}");
            }
#endif
        }

        public void ResumeMusic()
        {
            if (!_isMusicEnabled) return;

#if ANDROID
            try
            {
                lock (_lock)
                {
                    if (_bgmPlayer != null)
                    {
                        _bgmPlayer.Start();
                    }
                    else
                    {
                        PlayMusic("bgm_idle");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ResumeMusic error: {ex.Message}");
            }
#endif
        }
    }
}
