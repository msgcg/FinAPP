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
    // Живые, увлекательные и практичные советы Финни для детей
    private readonly string[] _finnyQuotes = new[]
    {
        "Муррр! Знаешь секрет? Если не купить ненужную жвачку 10 раз — вот тебе и новая крутая игра!",
        "Кошачий лайфхак: сначала кормим обязательные конверты, а вкусняшки — на десерт!",
        "Монетки в копилке не спят, они приближают твою заветную мечту! Проверь прогресс цели!",
        "Никому и никогда не говори секретные коды из СМС — даже если пишут от имени котика!",
        "Правило умного кота: перед покупкой игрушки посчитай до 10. Если всё ещё хочется — бери!",
        "Сложный процент — это как снежный ком из монет: катится по времени и становится огромным!",
        "Составляй список перед походом в магазин — так монетки не разбегутся на случайные сладости!",
        "Мяу! Сэкономленный рубль — это заработанный рубль! Ты отлично справляешься!",
        "Погладь меня ещё разок! А потом загляни в магазин заботы — там есть лакомства!",
        "Копилка — это твой личный сундук с сокровищами. Заглядывай в неё чаще!",
        "Карманные деньги любят счёт и порядок. Попробуй записать сегодняшние траты!",
        "У каждого великого миллионера всё начиналось с маленькой детской копилки!",
        "Если потерял карту — сразу скажи родителям, они заблокируют её за пару секунд!",
        "Игрушка надоест через пару дней, а достигнутая цель останется с тобой надолго!",
        "Мур-мяу! Финансовая грамотность — это суперсила, которая останется с тобой навсегда!",
        "Хочешь накопить быстрее? Решай финансовые задания и получай монетные награды!"
    };
    private int _quoteIndex = 0;
    private string _currentStagePrefix = "finny_baby";
    private string _currentEmotionGif = "finny_baby_idle.gif";
    private string _activeGif = string.Empty;
    private double _baseScale = 1.0;
    private bool _isReacting = false;
    private CancellationTokenSource? _waveCts;

    // Делегат для обновления текста в облачке мыслей на главном экране
    public Action<string>? SpeechTextChanged { get; set; }

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
        ConfigurePlatformWebView(PetWebView);
    }

    public static void ConfigurePlatformWebView(WebView webView)
    {
#if ANDROID
        try
        {
            if (webView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
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
            if (webView.Handler?.PlatformView is WebKit.WKWebView wkWeb)
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
            if (webView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 winWebView)
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
        ConfigurePlatformWebView(PetWebView);
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

        // 3. Сменная платформа / подиум под ногами котика
        UpdatePlatform(profile.Platform);

        // 4. Эволюционные стадии роста (ТЗ п. 2.5.10)
        switch (profile.Stage)
        {
            case GrowthStage.Baby:
                _baseScale = 0.96;
                LblStageBadge.Text = "Малыш (1 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Teen:
                _baseScale = 1.0;
                LblStageBadge.Text = "Юниор (2 ст.)";
                MasterAura.IsVisible = false;
                break;
            case GrowthStage.Master:
                _baseScale = 1.05;
                LblStageBadge.Text = "Мастер (3 ст.)";
                MasterAura.IsVisible = true;
                break;
        }

        if (!_isReacting)
        {
            PetContainer.Scale = _baseScale;
        }
    }

    public void UpdatePlatform(PetPlatformType platform)
    {
        switch (platform)
        {
            case PetPlatformType.Emerald:
                PlatformBg.Color = Color.FromArgb("#00796B");
                PlatformBase.Stroke = Color.FromArgb("#69F0AE");
                PlatformShadow.Brush = Color.FromArgb("#00E676");
                PlatformDecor.Text = "◆  КРИСТАЛЛ  ◆";
                PlatformDecor.TextColor = Color.FromArgb("#E0F2F1");
                break;
            case PetPlatformType.Stars:
                PlatformBg.Color = Color.FromArgb("#F57F17");
                PlatformBase.Stroke = Color.FromArgb("#FFE082");
                PlatformShadow.Brush = Color.FromArgb("#FFD700");
                PlatformDecor.Text = "★  ЗВЁЗДЫ  ★";
                PlatformDecor.TextColor = Color.FromArgb("#FFF8E1");
                break;
            case PetPlatformType.Flowers:
                PlatformBg.Color = Color.FromArgb("#2E7D32");
                PlatformBase.Stroke = Color.FromArgb("#B9F6CA");
                PlatformShadow.Brush = Color.FromArgb("#69F0AE");
                PlatformDecor.Text = "✤  ПОЛЯНКА  ✤";
                PlatformDecor.TextColor = Color.FromArgb("#E8F5E9");
                break;
            case PetPlatformType.Cosmic:
                PlatformBg.Color = Color.FromArgb("#4A148C");
                PlatformBase.Stroke = Color.FromArgb("#B388FF");
                PlatformShadow.Brush = Color.FromArgb("#7C4DFF");
                PlatformDecor.Text = "✦  КОСМОС  ✦";
                PlatformDecor.TextColor = Color.FromArgb("#EDE7F6");
                break;
            case PetPlatformType.Cloud:
                PlatformBg.Color = Color.FromArgb("#0288D1");
                PlatformBase.Stroke = Color.FromArgb("#E1F5FE");
                PlatformShadow.Brush = Color.FromArgb("#40C4FF");
                PlatformDecor.Text = "☁  ОБЛАКО  ☁";
                PlatformDecor.TextColor = Color.FromArgb("#F5FBFF");
                break;
        }
    }

    public void PlayAction(string action)
    {
        ApplyAnimation($"{_currentStagePrefix}_{action}.gif", force: true);
    }

    public void ReplayCurrent()
    {
        ApplyAnimation(_currentEmotionGif, force: true);
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
                    // Adding a timestamp ensures WebView parses fresh HTML and plays 1 complete animation cycle
                    var stampedHtml = html.Replace("</html>", $"<!-- {DateTime.UtcNow.Ticks} --></html>");
                    PetWebView.Source = new HtmlWebViewSource { Html = stampedHtml };
                    PetWebView.IsVisible = true;
                    ImgPetFallback.IsVisible = false;
                    ConfigurePlatformWebView(PetWebView);
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

    public static async Task<string> GetOrLoadHtmlAsync(string gifName)
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

            // Важно: justify-content: flex-start прижимает Финни к левому краю кадра,
            // благодаря чему левый срез хвоста идеально совпадает с границей экрана и выглядит естественно!
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
            justify-content: flex-start;
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
            margin-left: 0;
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
        SpeechTextChanged?.Invoke(text);
    }

    public void NextQuote()
    {
        _quoteIndex = (_quoteIndex + 1) % _finnyQuotes.Length;
        SetSpeechText(_finnyQuotes[_quoteIndex]);
    }

    public void TapPet()
    {
        OnPetTapped(this, EventArgs.Empty);
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

            // 2. Смена цитаты Финни
            _quoteIndex = (_quoteIndex + 1) % _finnyQuotes.Length;
            SetSpeechText(_finnyQuotes[_quoteIndex]);

            // 3. Отменяем предыдущий таймер возврата, если был
            _waveCts?.Cancel();
            _waveCts?.Dispose();
            _waveCts = new CancellationTokenSource();
            var ct = _waveCts.Token;

            // 4. Переключаем на анимацию приветствия текущей стадии
            ApplyAnimation($"{_currentStagePrefix}_wave.gif", force: true);

            // 5. Пружинистый подскок персонажа
            await PetContainer.ScaleToAsync(_baseScale * 1.05, 120, Easing.CubicOut);
            await PetContainer.ScaleToAsync(_baseScale, 120, Easing.CubicIn);

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
