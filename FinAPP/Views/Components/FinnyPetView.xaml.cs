using System;
using System.Threading;
using System.Threading.Tasks;
using FinAPP.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FinAPP.Views.Components;

public partial class FinnyPetView : ContentView
{
    private CancellationTokenSource? _animCts;
    private readonly string[] _finnyQuotes = new[]
    {
        "Мяу! Сначала планируем обязательные траты, а потом желания! 🍗",
        "Копилка наполняется по монетке — так растут большие сбережения! 🏦",
        "Никогда не сообщай коды из СМС незнакомцам! Финни за безопасность! 🛡️",
        "Сложный процент — это магия: деньги работают на тебя! ✨",
        "Муррр! Спасибо за заботу и вкусный обед! 🐾",
        "Правило 50/30/20 помогает копить легко и без стресса! 📊"
    };
    private int _quoteIndex = 0;

    public FinnyPetView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        StartIdleAnimation();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        StopIdleAnimation();
    }

    public void UpdatePet(PetProfile profile, string emotion)
    {
        // 1. Определение цвета куртки (суффикс для тела и лапки)
        string colorSuffix = profile.Outfit switch
        {
            OutfitType.RoyalBlue => "_blue",
            OutfitType.RubyRed => "_ruby",
            _ => ""
        };

        // 2. Обновление спрайтов слоев
        ImgTail.Source = "finny_tail.png";
        ImgBody.Source = $"finny_body{colorSuffix}.png";
        ImgArmWave.Source = $"finny_arm_wave{colorSuffix}.png";

        // Выбор эмоции головы (happy, proud, sad, surprised)
        string validEmotion = emotion.ToLowerInvariant() switch
        {
            "proud" => "proud",
            "sad" => "sad",
            "surprised" => "surprised",
            _ => "happy"
        };
        ImgHead.Source = $"finny_head_{validEmotion}.png";

        // 3. Определение слоя аксессуара
        switch (profile.Accessory)
        {
            case AccessoryType.Sunglasses:
                ImgAccessory.Source = "acc_sunglasses.png";
                ImgAccessory.IsVisible = true;
                break;
            case AccessoryType.AcademicCap:
                ImgAccessory.Source = "acc_academic.png";
                ImgAccessory.IsVisible = true;
                break;
            case AccessoryType.Crown:
                ImgAccessory.Source = "acc_crown.png";
                ImgAccessory.IsVisible = true;
                break;
            default:
                ImgAccessory.IsVisible = false;
                break;
        }

        // 4. Стадия развития (масштаб и бейдж)
        switch (profile.Stage)
        {
            case GrowthStage.Baby:
                PetContainer.Scale = 0.88;
                LblStageBadge.Text = "🐾 Малыш (1 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Teen:
                PetContainer.Scale = 0.98;
                LblStageBadge.Text = "🚀 Подросток (2 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Master:
                PetContainer.Scale = 1.08;
                LblStageBadge.Text = "🌟 Финни-Мастер (3 ст.)";
                MasterAura.IsVisible = true;
                break;
        }

        if (!profile.AnimationsEnabled)
        {
            StopIdleAnimation();
            ResetBonesToDefault();
        }
        else if (_animCts == null || _animCts.IsCancellationRequested)
        {
            StartIdleAnimation();
        }
    }

    public void SetSpeechText(string text)
    {
        LblSpeech.Text = text;
    }

    private void ResetBonesToDefault()
    {
        ImgArmWave.Rotation = 0;
        ImgTail.Rotation = 0;
        ImgHead.Rotation = 0;
        ImgHead.TranslationY = 0;
        ImgAccessory.Rotation = 0;
        ImgAccessory.TranslationY = 0;
        ImgBody.ScaleX = 1.0;
        ImgBody.ScaleY = 1.0;
        PetContainer.TranslationY = 0;
    }

    private void StartIdleAnimation()
    {
        StopIdleAnimation();
        _animCts = new CancellationTokenSource();
        var ct = _animCts.Token;

        // Запуск 3 независимых несинхронных циклов для живой скелетной анимации
        _ = RunArmWaveLoop(ct);
        _ = RunTailWagLoop(ct);
        _ = RunBreathingHeadBobLoop(ct);
    }

    private async Task RunArmWaveLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Плавный взмах лапкой в плечевом суставе
                await ImgArmWave.RotateToAsync(20, 850, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;
                await ImgArmWave.RotateToAsync(-8, 850, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;
            }
        }
        catch (TaskCanceledException) { }
        catch { }
    }

    private async Task RunTailWagLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Плавное виляние хвостиком у основания
                await ImgTail.RotateToAsync(15, 1100, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;
                await ImgTail.RotateToAsync(-12, 1100, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;
            }
        }
        catch (TaskCanceledException) { }
        catch { }
    }

    private async Task RunBreathingHeadBobLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Фаза вдоха: туловище расширяется, голова слегка приподнимается и наклоняется
                var bIn1 = ImgBody.ScaleYToAsync(1.025, 1600, Easing.SinInOut);
                var bIn2 = ImgBody.ScaleXToAsync(0.99, 1600, Easing.SinInOut);
                var hIn1 = ImgHead.RotateToAsync(2.5, 1600, Easing.SinInOut);
                var hIn2 = ImgHead.TranslateToAsync(0, -2, 1600, Easing.SinInOut);
                if (ImgAccessory.IsVisible)
                {
                    _ = ImgAccessory.RotateToAsync(2.5, 1600, Easing.SinInOut);
                    _ = ImgAccessory.TranslateToAsync(0, -2, 1600, Easing.SinInOut);
                }
                await Task.WhenAll(bIn1, bIn2, hIn1, hIn2);
                if (ct.IsCancellationRequested) break;

                // Фаза выдоха: опускание туловища и возврат
                var bOut1 = ImgBody.ScaleYToAsync(1.0, 1600, Easing.SinInOut);
                var bOut2 = ImgBody.ScaleXToAsync(1.0, 1600, Easing.SinInOut);
                var hOut1 = ImgHead.RotateToAsync(-2.5, 1600, Easing.SinInOut);
                var hOut2 = ImgHead.TranslateToAsync(0, 0, 1600, Easing.SinInOut);
                if (ImgAccessory.IsVisible)
                {
                    _ = ImgAccessory.RotateToAsync(-2.5, 1600, Easing.SinInOut);
                    _ = ImgAccessory.TranslateToAsync(0, 0, 1600, Easing.SinInOut);
                }
                await Task.WhenAll(bOut1, bOut2, hOut1, hOut2);
                if (ct.IsCancellationRequested) break;
            }
        }
        catch (TaskCanceledException) { }
        catch { }
    }

    private void StopIdleAnimation()
    {
        _animCts?.Cancel();
        _animCts?.Dispose();
        _animCts = null;
    }

    private async void OnPetTapped(object? sender, EventArgs e)
    {
        // Тактильный отклик (вибрация клика)
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch { }

        // Смена цитаты и анимация облачка
        _quoteIndex = (_quoteIndex + 1) % _finnyQuotes.Length;
        SetSpeechText(_finnyQuotes[_quoteIndex]);

        _ = SpeechBubble.ScaleToAsync(1.08, 120, Easing.CubicOut)
            .ContinueWith(_ => MainThread.BeginInvokeOnMainThread(async () =>
            {
                await SpeechBubble.ScaleToAsync(1.0, 120, Easing.CubicIn);
            }));

        // Профессиональная интерактивная реакция:
        // 1. Радостное ускоренное махание лапкой (3 взмаха с высокой амплитудой)
        // 2. Кивок головой
        // 3. Энергичное виляние хвостом
        var armTask = Task.Run(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ImgArmWave.RotateToAsync(32, 140, Easing.CubicOut);
                    await ImgArmWave.RotateToAsync(-12, 140, Easing.CubicIn);
                });
            }
        });

        var tailTask = Task.Run(async () =>
        {
            for (int i = 0; i < 3; i++)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await ImgTail.RotateToAsync(22, 140, Easing.CubicOut);
                    await ImgTail.RotateToAsync(-16, 140, Easing.CubicIn);
                });
            }
        });

        var headNodTask = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await ImgHead.TranslateToAsync(0, -6, 130, Easing.CubicOut);
            await ImgHead.TranslateToAsync(0, 0, 180, Easing.BounceOut);
        });

        await Task.WhenAll(armTask, tailTask, headNodTask);
    }
}
