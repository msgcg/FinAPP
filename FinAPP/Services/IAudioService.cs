namespace FinAPP.Services
{
    public interface IAudioService
    {
        bool IsSfxEnabled { get; set; }
        bool IsMusicEnabled { get; set; }

        void PlaySfx(string soundName);
        void PlayMusic(string musicName = "bgm_idle");
        void StopMusic();
        void ResumeMusic();
    }
}
