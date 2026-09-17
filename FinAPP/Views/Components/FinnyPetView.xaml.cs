using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FinAPP.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;

namespace FinAPP.Views.Components;

public partial class FinnyPetView : ContentView
{
    // Короткие советы на 1 предложение, не выходящие за рамки бабла
    private readonly string[] _finnyQuotes = new[]
    {
        "Планируй сначала обязательные траты, а потом желания!",
        "Копилка растёт по монетке — так рождается капитал!",
        "Никому не сообщай коды из СМС — Финни за безопасность!",
        "Сложный процент умножает твои сбережения со временем!",
        "Муррр! Спасибо за заботу и твою внимательность!",
        "Правило 50/30/20 помогает копить легко и без стресса!",
        "Запиши сегодняшние расходы, чтобы видеть свой прогресс!"
    };
    private int _quoteIndex = 0;
    private string _currentStagePrefix = "finny_baby";
    private string _currentEmotionGif = "finny_baby_idle.gif";
    private string _activeGif = string.Empty;
    private double _baseScale = 1.0;
    private bool _isReacting = false;
    private CancellationTokenSource? _waveCts;

    // Потокобезопасный кэш HTML с Base64 данными анимаций для мгновенного переключения без лагов
    private static readonly ConcurrentDictionary<string, string> s_htmlCache = new();

    public FinnyPetView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PetWebView.HandlerChanged += OnPetWebViewHandlerChanged;
    }

    private void OnPetWebViewHandlerChanged(object? sender, EventArgs e)
    {
        ConfigurePlatformWebView();
    }

    private void ConfigurePlatformWebView()
    {
#if ANDROID
        try
        {
            if (PetWebView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                nativeWebView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                nativeWebView.VerticalScrollBarEnabled = false;
                nativeWebView.HorizontalScrollBarEnabled = false;
                nativeWebView.SetScrollContainer(false);
                if (nativeWebView.Settings != null)
                {
                    nativeWebView.Settings.DisplayZoomControls = false;
                    nativeWebView.Settings.SetSupportZoom(false);
                    nativeWebView.Settings.BuiltInZoomControls = false;
                    nativeWebView.Settings.UseWideViewPort = false;
                    nativeWebView.Settings.LoadWithOverviewMode = false;
                    nativeWebView.Settings.JavaScriptEnabled = false;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Android WebView config error: {ex.Message}");
        }
#endif
#if IOS || MACCATALYST
        try
        {
            if (PetWebView.Handler?.PlatformView is WebKit.WKWebView wkWeb)
            {
                wkWeb.Opaque = false;
                wkWeb.BackgroundColor = UIKit.UIColor.Clear;
                if (wkWeb.ScrollView != null)
                {
                    wkWeb.ScrollView.ScrollEnabled = false;
                    wkWeb.ScrollView.Bounces = false;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] iOS WKWebView config error: {ex.Message}");
        }
#endif
#if WINDOWS
        try
        {
            if (PetWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 winWebView)
            {
                winWebView.DefaultBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Windows WebView2 config error: {ex.Message}");
        }
#endif
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        ConfigurePlatformWebView();
        ApplyAnimation(_currentEmotionGif, force: true);

        // В фоне прогреваем кэш остальных анимаций текущей стадии для мгновенного переключения
        _ = Task.Run(PreloadStageGifsAsync);
    }

    private async Task PreloadStageGifsAsync()
    {
        try
        {
            var actions = new[] { "idle", "wave", "proud", "sad" };
            foreach (var action in actions)
            {
                var gif = $"{_currentStagePrefix}_{action}.gif";
                await GetOrLoadHtmlAsync(gif);
            }
        }
        catch { }
    }

    public void UpdatePet(PetProfile profile, string emotion)
    {
        var prevPrefix = _currentStagePrefix;

        // 1. Выбор префикса стадии эволюции котика (Малыш, Юниор, Мастер)
        _currentStagePrefix = profile.Stage switch
        {
            GrowthStage.Baby => "finny_baby",
            GrowthStage.Teen => "finny_teen",
            _ => "finny_master"
        };

        if (prevPrefix != _currentStagePrefix)
        {
            _ = Task.Run(PreloadStageGifsAsync);
        }

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
                LblStageBadge.Text = "Финни-Малыш (1 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Teen:
                _baseScale = 1.0;
                LblStageBadge.Text = "Финни-Юниор (2 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Master:
                _baseScale = 1.06;
                LblStageBadge.Text = "Финни-Мастер (3 ст.)";
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

        _ = LoadAndRenderGifAsync(gifName);
    }

    private async Task LoadAndRenderGifAsync(string gifName)
    {
        try
        {
            var html = await GetOrLoadHtmlAsync(gifName);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!string.IsNullOrEmpty(html))
                {
                    PetWebView.Source = new HtmlWebViewSource { Html = html };
                    PetWebView.IsVisible = true;
                    ImgPetFallback.IsVisible = false;
                    ConfigurePlatformWebView();
                }
                else
                {
                    // Резервный режим, если файл не удалось прочесть через WebView
                    ImgPetFallback.Source = gifName;
                    ImgPetFallback.IsVisible = true;
                    PetWebView.IsVisible = false;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] LoadAndRenderGifAsync error: {ex.Message}");
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ImgPetFallback.Source = gifName;
                ImgPetFallback.IsVisible = true;
                PetWebView.IsVisible = false;
            });
        }
    }

    private static async Task<string> GetOrLoadHtmlAsync(string gifName)
    {
        if (s_htmlCache.TryGetValue(gifName, out var cachedHtml))
        {
            return cachedHtml;
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(gifName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());

            var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"">
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
            background: transparent !important;
            background-color: transparent !important;
        }}
        html, body {{
            width: 100%;
            height: 100%;
            margin: 0;
            padding: 0;
            overflow: hidden;
            background: transparent !important;
            background-color: transparent !important;
            display: flex;
            justify-content: center;
            align-items: center;
            user-select: none;
            -webkit-user-select: none;
        }}
        img {{
            max-width: 100%;
            max-height: 100%;
            width: auto;
            height: auto;
            object-fit: contain;
            display: block;
            pointer-events: none;
        }}
    </style>
</head>
<body>
    <img src=""data:image/gif;base64,{base64}"" alt=""Finny"" />
</body>
</html>";

            s_htmlCache[gifName] = html;
            return html;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Error loading {gifName}: {ex.Message}");
            return string.Empty;
        }
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
