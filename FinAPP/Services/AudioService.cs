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
        private const string PrefAudioVersionKey = "audio_assets_version";
        private const int CurrentAudioVersion = 13;

        private bool _isSfxEnabled;
        private bool _isMusicEnabled;
        private bool _isInitialized;
        private readonly object _lock = new();

#if ANDROID
        private Android.Media.SoundPool? _soundPool;
        private Android.Media.MediaPlayer? _bgmPlayer;
        private Android.Media.MediaPlayer? _meowPlayer;
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

                int savedVersion = Preferences.Default.Get(PrefAudioVersionKey, 0);
                bool forceRefresh = savedVersion < CurrentAudioVersion;

                string[] audioFiles =
                [
                    "sfx_money.mp3",
                    "sfx_success.mp3",
                    "sfx_error.mp3",
                    "sfx_meow.wav",
                    "sfx_meow.mp3",
                    "sfx_purr.mp3",
                    "bgm_idle.mp3"
                ];

                foreach (var file in audioFiles)
                {
                    var targetPath = Path.Combine(audioDir, file);
                    try
                    {
                        if (forceRefresh && File.Exists(targetPath))
                        {
                            try { File.Delete(targetPath); } catch { }
                        }

                        if (!File.Exists(targetPath))
                        {
                            using var srcStream = await FileSystem.OpenAppPackageFileAsync($"audio/{file}");
                            using var dstStream = File.Create(targetPath);
                            await srcStream.CopyToAsync(dstStream);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Audio asset copy error {file}: {ex.Message}");
                    }
                }

                if (forceRefresh)
                {
                    Preferences.Default.Set(PrefAudioVersionKey, CurrentAudioVersion);
                }

                // Initialize SoundPool for SFX on Media/Music stream (matches volume rocker)
                var audioAttributes = new Android.Media.AudioAttributes.Builder()
                    ?.SetUsage(Android.Media.AudioUsageKind.Media)
                    ?.SetContentType(Android.Media.AudioContentType.Music)
                    ?.Build();

                if (audioAttributes != null)
                {
                    _soundPool = new Android.Media.SoundPool.Builder()
                        ?.SetMaxStreams(8)
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

                // Pre-prepare dedicated instant player for sfx_meow
                try
                {
                    var meowPath = Path.Combine(audioDir, "sfx_meow.wav");
                    if (!File.Exists(meowPath)) meowPath = Path.Combine(audioDir, "sfx_meow.mp3");
                    if (File.Exists(meowPath))
                    {
                        _meowPlayer = new Android.Media.MediaPlayer();
                        _meowPlayer.SetAudioAttributes(
                            new Android.Media.AudioAttributes.Builder()
                                ?.SetUsage(Android.Media.AudioUsageKind.Media)
                                ?.SetContentType(Android.Media.AudioContentType.Music)
                                ?.Build());
                        _meowPlayer.SetDataSource(meowPath);
                        _meowPlayer.SetVolume(1.0f, 1.0f);
                        _meowPlayer.Prepare();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"_meowPlayer init error: {ex.Message}");
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
                    string key = soundName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || soundName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                        ? Path.GetFileNameWithoutExtension(soundName) 
                        : soundName;

                    // 1. Быстрый гарантированный путь для кошачьего мяуканья
                    if (key.Equals("sfx_meow", StringComparison.OrdinalIgnoreCase) && _meowPlayer != null)
                    {
                        try
                        {
                            if (_meowPlayer.IsPlaying)
                            {
                                _meowPlayer.SeekTo(0);
                            }
                            _meowPlayer.Start();
                            return;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"_meowPlayer Play error: {ex.Message}");
                        }
                    }

                    // 2. Основной SoundPool
                    int streamId = 0;
                    if (_soundPool != null && _soundIds.TryGetValue(key, out int soundId) && soundId > 0)
                    {
                        streamId = _soundPool.Play(soundId, 1.0f, 1.0f, 1, 0, 1.0f);
                    }

                    // 3. Fallback через MediaPlayer при нулевом результате SoundPool
                    if (streamId == 0)
                    {
                        PlaySfxFallback(soundName);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaySfx error {soundName}: {ex.Message}");
                PlaySfxFallback(soundName);
            }
#endif
        }

#if ANDROID
        private void PlaySfxFallback(string soundName)
        {
            try
            {
                var audioDir = Path.Combine(FileSystem.CacheDirectory, "audio");
                string baseName = Path.GetFileNameWithoutExtension(soundName);
                string filePath = Path.Combine(audioDir, $"{baseName}.wav");
                if (!File.Exists(filePath)) filePath = Path.Combine(audioDir, $"{baseName}.mp3");

                if (!File.Exists(filePath)) return;

                var player = new Android.Media.MediaPlayer();
                player.SetAudioAttributes(
                    new Android.Media.AudioAttributes.Builder()
                        ?.SetUsage(Android.Media.AudioUsageKind.Media)
                        ?.SetContentType(Android.Media.AudioContentType.Music)
                        ?.Build());
                player.SetDataSource(filePath);
                player.SetVolume(1.0f, 1.0f);
                player.Completion += (s, e) =>
                {
                    try
                    {
                        player.Stop();
                        player.Release();
                        player.Dispose();
                    }
                    catch { }
                };
                player.Prepare();
                player.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PlaySfxFallback error {soundName}: {ex.Message}");
            }
        }
#endif

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
