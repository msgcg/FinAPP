using System;
using System.Threading;
using System.Threading.Tasks;
using FinAPP.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FinAPP.Views.Components;

public partial class FinnyPetView : ContentView
{
    // Короткие советы на 1 предложение, не выходящие за рамки бабла
    private readonly string[] _finnyQuotes = new[]
    {
        "Планируй сначала обязательные траты, а потом желания! 🍗",
        "Копилка растёт по монетке — так рождается капитал! 🏦",
        "Никому не сообщай коды из СМС — Финни за безопасность! 🛡️",
        "Сложный процент умножает твои сбережения! ✨",
        "Муррр! Спасибо за заботу и твою внимательность! 🐾",
        "Правило 50/30/20 помогает копить легко и без стресса! 📊",
        "Запиши сегодняшние расходы, чтобы видеть свой прогресс! 📝"
    };
    private int _quoteIndex = 0;
    private string _currentStagePrefix = "finny_baby";
    private string _currentEmotionGif = "finny_baby_idle.gif";
    private string _activeGif = string.Empty;
    private double _baseScale = 1.0;
    private bool _isReacting = false;
    private CancellationTokenSource? _waveCts;

    public FinnyPetView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        ImgPet.HandlerChanged += OnImgPetHandlerChanged;
    }

    private void OnImgPetHandlerChanged(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_activeGif))
        {
            ApplyNativeAnimation(_activeGif);
        }
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        ApplyAnimation(_currentEmotionGif, force: true);
    }

    public void UpdatePet(PetProfile profile, string emotion)
    {
        // 1. Выбор префикса стадии эволюции котика (Малыш, Юниор, Мастер)
        _currentStagePrefix = profile.Stage switch
        {
            GrowthStage.Baby => "finny_baby",
            GrowthStage.Teen => "finny_teen",
            _ => "finny_master"
        };

        // 2. Выбор действия/эмоции
        string action = emotion.ToLowerInvariant() switch
        {
            "proud" => "proud",
            "sad" => "sad",
            "wave" or "surprised" => "wave",
            _ => "idle"
        };

        _currentEmotionGif = $"{_currentStagePrefix}_{action}.gif";

        // Если сейчас не воспроизводится временная реакция на тап, обновляем анимацию
        if (!_isReacting)
        {
            ApplyAnimation(_currentEmotionGif);
        }

        // 3. Накладные аксессуары поверх котика
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

        // 4. Эволюционные стадии роста (ТЗ п. 2.5.10)
        switch (profile.Stage)
        {
            case GrowthStage.Baby:
                _baseScale = 0.95;
                LblStageBadge.Text = "🐾 Финни-Малыш (1 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Teen:
                _baseScale = 1.0;
                LblStageBadge.Text = "⚡ Финни-Юниор (2 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Master:
                _baseScale = 1.06;
                LblStageBadge.Text = "🌟 Финни-Мастер (3 ст.)";
                MasterAura.IsVisible = true;
                break;
        }

        if (!_isReacting)
        {
            PetFrameCard.Scale = _baseScale;
        }
    }

    private void ApplyAnimation(string gifName, bool force = false)
    {
        if (!force && _activeGif == gifName) return;
        _activeGif = gifName;

        // Базовое назначение для MAUI (гарантирует видимость на всех платформах)
        if (ImgPet != null)
        {
            ImgPet.Source = gifName;
            ImgPet.IsAnimationPlaying = true;
        }

        ApplyNativeAnimation(gifName);
    }

    private void ApplyNativeAnimation(string gifName)
    {
#if ANDROID
        try
        {
            if (ImgPet?.Handler?.PlatformView is Android.Widget.ImageView nativeImageView)
            {
                nativeImageView.Post(() =>
                {
                    try
                    {
                        nativeImageView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                        if (OperatingSystem.IsAndroidVersionAtLeast(28))
                        {
                            var context = Android.App.Application.Context;
                            using var stream = context?.Assets?.Open(gifName);
                            if (stream != null)
                            {
                                using var ms = new System.IO.MemoryStream();
                                stream.CopyTo(ms);
                                var byteBuffer = Java.Nio.ByteBuffer.Wrap(ms.ToArray());
                                var source = Android.Graphics.ImageDecoder.CreateSource(byteBuffer);
                                var drawable = Android.Graphics.ImageDecoder.DecodeDrawable(source);
                                
                                nativeImageView.SetImageDrawable(drawable);
                                
                                if (drawable is Android.Graphics.Drawables.AnimatedImageDrawable animDrawable)
                                {
                                    animDrawable.RepeatCount = Android.Graphics.Drawables.AnimatedImageDrawable.RepeatInfinite;
                                    animDrawable.Start();
                                }
                                else if (drawable is Android.Graphics.Drawables.IAnimatable anim)
                                {
                                    anim.Start();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Error in nativeImageView.Post: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Error loading Android AnimatedImageDrawable: {ex.Message}");
        }
#endif
    }

    public void SetSpeechText(string text)
    {
        LblSpeech.Text = text;
    }

    private async Task AnimateBubbleBounce()
    {
        try
        {
            await SpeechBubble.ScaleToAsync(1.06, 90, Easing.CubicOut);
            await SpeechBubble.ScaleToAsync(1.0, 90, Easing.CubicIn);
        }
        catch { }
    }

    private async void OnPetTapped(object? sender, EventArgs e)
    {
        if (_isReacting) return;
        _isReacting = true;

        try
        {
            // 1. Тактильный отклик (вибрация клика)
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch { }

            // 2. Смена цитаты и анимация облачка
            _quoteIndex = (_quoteIndex + 1) % _finnyQuotes.Length;
            SetSpeechText(_finnyQuotes[_quoteIndex]);
            _ = AnimateBubbleBounce();

            // 3. Отменяем предыдущий таймер возврата, если был
            _waveCts?.Cancel();
            _waveCts?.Dispose();
            _waveCts = new CancellationTokenSource();
            var ct = _waveCts.Token;

            // 4. Переключаем на анимацию приветствия текущей стадии
            ApplyAnimation($"{_currentStagePrefix}_wave.gif", force: true);

            // 5. Пружинистый подскок карточки персонажа
            await PetFrameCard.ScaleToAsync(_baseScale * 1.05, 120, Easing.CubicOut);
            await PetFrameCard.ScaleToAsync(_baseScale, 120, Easing.CubicIn);

            // 6. Даем помахать 1.8 секунды, затем возвращаем базовую эмоцию
            await Task.Delay(1800, ct);

            if (!ct.IsCancellationRequested)
            {
                ApplyAnimation(_currentEmotionGif, force: true);
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Error in OnPetTapped: {ex.Message}");
        }
        finally
        {
            _isReacting = false;
        }
    }
}
