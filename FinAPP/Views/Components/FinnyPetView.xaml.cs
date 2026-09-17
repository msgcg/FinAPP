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
        // 1. Определение базового файла спрайта с учетом эмоции и цвета куртки
        string colorSuffix = profile.Outfit switch
        {
            OutfitType.RoyalBlue => "_blue",
            OutfitType.RubyRed => "_ruby",
            _ => ""
        };

        string bodyImageName = $"finny_{emotion}{colorSuffix}.png";
        ImgPetBody.Source = bodyImageName;

        // 2. Определение слоя аксессуара
        switch (profile.Accessory)
        {
            case AccessoryType.Sunglasses:
                ImgPetAccessory.Source = "acc_sunglasses.png";
                ImgPetAccessory.IsVisible = true;
                break;
            case AccessoryType.AcademicCap:
                ImgPetAccessory.Source = "acc_academic.png";
                ImgPetAccessory.IsVisible = true;
                break;
            case AccessoryType.Crown:
                ImgPetAccessory.Source = "acc_crown.png";
                ImgPetAccessory.IsVisible = true;
                break;
            default:
                ImgPetAccessory.IsVisible = false;
                break;
        }

        // 3. Стадия развития (масштаб и бейдж)
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
            PetContainer.TranslationY = 0;
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

    private async void StartIdleAnimation()
    {
        StopIdleAnimation();
        _animCts = new CancellationTokenSource();
        var token = _animCts.Token;

        try
        {
            while (!token.IsCancellationRequested)
            {
                await PetContainer.TranslateToAsync(0, -6, 1100, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
                await PetContainer.TranslateToAsync(0, 0, 1100, Easing.SinInOut);
                if (token.IsCancellationRequested) break;
            }
        }
        catch (TaskCanceledException)
        {
            // cancel animation
        }
    }

    private void StopIdleAnimation()
    {
        _animCts?.Cancel();
        _animCts?.Dispose();
        _animCts = null;
    }

    private async void OnPetTapped(object? sender, EventArgs e)
    {
        // Реакция на нажатие: прыжок Финни и смена реплики
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch { }

        _quoteIndex = (_quoteIndex + 1) % _finnyQuotes.Length;
        SetSpeechText(_finnyQuotes[_quoteIndex]);

        // Анимация подпрыгивания
        await PetContainer.TranslateToAsync(0, -22, 160, Easing.CubicOut);
        await PetContainer.TranslateToAsync(0, 0, 220, Easing.BounceOut);
    }
}
