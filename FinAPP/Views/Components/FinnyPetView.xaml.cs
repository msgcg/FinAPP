using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FinAPP.Models;
using FinAPP.Services;
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
    private long _lastTapTime = 0;
    private bool _isReacting = false;
    private CancellationTokenSource? _waveCts;

    // Делегат для обновления текста в облачке мыслей на главном экране
    public Action<string>? SpeechTextChanged { get; set; }

    // Потокобезопасный кэш бинарных байтов GIF для мгновенной генерации уникальных тактов в памяти
    private static readonly ConcurrentDictionary<string, byte[]> s_gifBytesCache = new();

#if ANDROID
    private class PetNativeTouchListener : Java.Lang.Object, Android.Views.View.IOnTouchListener
    {
        private readonly Action _onTap;
        private float _downX;
        private float _downY;
        private long _downTime;

        public PetNativeTouchListener(Action onTap)
        {
            _onTap = onTap;
        }

        public bool OnTouch(Android.Views.View? v, Android.Views.MotionEvent? e)
        {
            if (e == null) return false;

            switch (e.ActionMasked)
            {
                case Android.Views.MotionEventActions.Down:
                    _downX = e.GetX();
                    _downY = e.GetY();
                    _downTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    return true;

                case Android.Views.MotionEventActions.Up:
                    float dx = Math.Abs(e.GetX() - _downX);
                    float dy = Math.Abs(e.GetY() - _downY);
                    long duration = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _downTime;

                    if (dx < 50 && dy < 50 && duration < 700)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try
                            {
                                _onTap?.Invoke();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Pet touch callback error: {ex.Message}");
                            }
                        });
                    }
                    return true;

                case Android.Views.MotionEventActions.Cancel:
                    return true;
            }

            return false;
        }
    }
#endif

    public FinnyPetView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        PetWebView.HandlerChanged += OnPetWebViewHandlerChanged;
        TouchOverlayBorder.HandlerChanged += OnTouchOverlayHandlerChanged;
        PetContainer.HandlerChanged += OnPetContainerHandlerChanged;
    }

    private void OnPetWebViewHandlerChanged(object? sender, EventArgs e)
    {
        ConfigurePlatformWebView(PetWebView, () => TapPet());
    }

    private void OnTouchOverlayHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (TouchOverlayBorder.Handler?.PlatformView is Android.Views.View nativeOverlay)
        {
            nativeOverlay.Clickable = true;
            nativeOverlay.SetOnTouchListener(new PetNativeTouchListener(() => TapPet()));
        }
#endif
    }

    private void OnPetContainerHandlerChanged(object? sender, EventArgs e)
    {
#if ANDROID
        if (PetContainer.Handler?.PlatformView is Android.Views.View nativeGrid)
        {
            nativeGrid.Clickable = true;
            nativeGrid.SetOnTouchListener(new PetNativeTouchListener(() => TapPet()));
        }
#endif
    }

    public static void ConfigurePlatformWebView(WebView webView, Action? onTapped = null)
    {
#if ANDROID
        try
        {
            if (webView.Handler?.PlatformView is Android.Webkit.WebView nativeWebView)
            {
                nativeWebView.SetBackgroundColor(Android.Graphics.Color.Transparent);
                nativeWebView.VerticalScrollBarEnabled = false;
                nativeWebView.HorizontalScrollBarEnabled = false;
                nativeWebView.Focusable = false;
                nativeWebView.FocusableInTouchMode = false;

                if (onTapped != null)
                {
                    nativeWebView.Clickable = true;
                    nativeWebView.SetOnTouchListener(new PetNativeTouchListener(onTapped));
                }
                else
                {
                    nativeWebView.Clickable = false;
                    nativeWebView.SetOnTouchListener(null);
                }

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
        ConfigurePlatformWebView(PetWebView, () => TapPet());
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
                await GetRawGifBytesAsync(gif);
            }
        }
        catch { }
    }

    public void UpdatePet(PetProfile profile, string emotion)
    {
        PetName = string.IsNullOrWhiteSpace(profile.PetName) ? "Финни" : profile.PetName;
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

        // 4. Сменный рабочий стол перед Финни
        UpdateDesk(profile.Desk);

        // 5. Эволюционные стадии роста (ТЗ п. 2.5.10)
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

    public void UpdateDesk(PetDeskType desk)
    {
        switch (desk)
        {
            case PetDeskType.Modern:
                ImgDeskOverlay.Source = "desk_modern.png";
                ImgDeskOverlay.IsVisible = true;
                break;
            case PetDeskType.Artisan:
                ImgDeskOverlay.Source = "desk_artisan.png";
                ImgDeskOverlay.IsVisible = true;
                break;
            case PetDeskType.Market:
                ImgDeskOverlay.Source = "desk_market.png";
                ImgDeskOverlay.IsVisible = true;
                break;
            case PetDeskType.Maker:
                ImgDeskOverlay.Source = "desk_maker.png";
                ImgDeskOverlay.IsVisible = true;
                break;
            case PetDeskType.Reading:
                ImgDeskOverlay.Source = "desk_reading.png";
                ImgDeskOverlay.IsVisible = true;
                break;
            case PetDeskType.Botanical:
                ImgDeskOverlay.Source = "desk_botanical.png";
                ImgDeskOverlay.IsVisible = true;
                break;
            default:
                ImgDeskOverlay.Source = null;
                ImgDeskOverlay.IsVisible = false;
                break;
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
                PlatformDecor.Text = "◆   ◆   ◆   ◆   ◆   ◆   ◆";
                PlatformDecor.TextColor = Color.FromArgb("#E0F2F1");
                break;
            case PetPlatformType.Stars:
                PlatformBg.Color = Color.FromArgb("#F57F17");
                PlatformBase.Stroke = Color.FromArgb("#FFE082");
                PlatformShadow.Brush = Color.FromArgb("#FFD700");
                PlatformDecor.Text = "★   ★   ★   ★   ★   ★   ★";
                PlatformDecor.TextColor = Color.FromArgb("#FFF8E1");
                break;
            case PetPlatformType.Flowers:
                PlatformBg.Color = Color.FromArgb("#2E7D32");
                PlatformBase.Stroke = Color.FromArgb("#B9F6CA");
                PlatformShadow.Brush = Color.FromArgb("#69F0AE");
                PlatformDecor.Text = "✤   ✤   ✤   ✤   ✤   ✤   ✤";
                PlatformDecor.TextColor = Color.FromArgb("#E8F5E9");
                break;
            case PetPlatformType.Cosmic:
                PlatformBg.Color = Color.FromArgb("#4A148C");
                PlatformBase.Stroke = Color.FromArgb("#B388FF");
                PlatformShadow.Brush = Color.FromArgb("#7C4DFF");
                PlatformDecor.Text = "✦   ✦   ✦   ✦   ✦   ✦   ✦";
                PlatformDecor.TextColor = Color.FromArgb("#EDE7F6");
                break;
            case PetPlatformType.Cloud:
                PlatformBg.Color = Color.FromArgb("#0288D1");
                PlatformBase.Stroke = Color.FromArgb("#E1F5FE");
                PlatformShadow.Brush = Color.FromArgb("#40C4FF");
                PlatformDecor.Text = "✧   ✧   ✧   ✧   ✧   ✧   ✧";
                PlatformDecor.TextColor = Color.FromArgb("#F5FBFF");
                break;
        }
    }

    public void PlayAction(string action)
    {
        _waveCts?.Cancel();
        _waveCts?.Dispose();
        _waveCts = new CancellationTokenSource();
        var ct = _waveCts.Token;

        _ = LoadAndRenderGifAsync($"{_currentStagePrefix}_{action}.gif");

        // После 1 такта возвращаем текущую базовую эмоцию котика
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1800, ct);
                if (!ct.IsCancellationRequested)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _ = LoadAndRenderGifAsync(_currentEmotionGif);
                    });
                }
            }
            catch { }
        });
    }

    public void ReplayCurrent()
    {
        _ = LoadAndRenderGifAsync(_currentEmotionGif);
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
            var html = await GetFreshHtmlAsync(gifName);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!string.IsNullOrEmpty(html))
                {
                    PetWebView.Source = new HtmlWebViewSource { Html = html };
                    PetWebView.IsVisible = true;
                    ImgPetFallback.IsVisible = false;
                    ConfigurePlatformWebView(PetWebView, () => TapPet());
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

    public static async Task<byte[]?> GetRawGifBytesAsync(string gifName)
    {
        if (s_gifBytesCache.TryGetValue(gifName, out var cachedBytes))
        {
            return cachedBytes;
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(gifName);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();
            s_gifBytesCache[gifName] = bytes;
            return bytes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Error loading raw {gifName}: {ex.Message}");
            return null;
        }
    }

    public static async Task<string> GetFreshHtmlAsync(string gifName)
    {
        var raw = await GetRawGifBytesAsync(gifName);
        if (raw == null || raw.Length == 0)
        {
            return string.Empty;
        }

        var uniqueBytes = GifAnimationHelper.CreateUniqueGifBytes(raw);
        var base64 = Convert.ToBase64String(uniqueBytes);
        return GifAnimationHelper.GenerateFinnyHtml(base64);
    }

    public string PetName { get; set; } = "Финни";

    public void SetSpeechText(string text)
    {
        string formatted = string.IsNullOrEmpty(text) ? "" : text.Replace("Финни", PetName);
        SpeechTextChanged?.Invoke(formatted);
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
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - _lastTapTime < 350) return;
        _lastTapTime = now;
        _isReacting = true;

        try
        {
            // 1. Звук мяуканья при тапе по котику Финни
            try
            {
                AudioService.Instance.PlaySfx("sfx_meow");
            }
            catch { }

            // 2. Тактильный отклик (изолированно)
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch { }

            // 3. Смена цитаты Финни
            if (_currentEmotionGif.EndsWith("_sad.gif"))
            {
                SetSpeechText("Мяу... Животик урчит или мне грустно! Давай заглянем в магазин заботы или поиграем!");
            }
            else
            {
                _quoteIndex = (_quoteIndex + 1) % _finnyQuotes.Length;
                SetSpeechText(_finnyQuotes[_quoteIndex]);
            }

            // 3. Отменяем предыдущий таймер возврата, если был
            _waveCts?.Cancel();
            _waveCts?.Dispose();
            _waveCts = new CancellationTokenSource();
            var ct = _waveCts.Token;

            // 4. Переключаем на анимацию приветствия текущей стадии (если не грустный)
            if (_currentEmotionGif.EndsWith("_sad.gif"))
            {
                ApplyAnimation(_currentEmotionGif, force: true);
            }
            else
            {
                ApplyAnimation($"{_currentStagePrefix}_wave.gif", force: true);
            }

            // 5. Пружинистый подскок персонажа
            await PetContainer.ScaleToAsync(_baseScale * 1.05, 120, Easing.CubicOut);
            await PetContainer.ScaleToAsync(_baseScale, 120, Easing.CubicIn);

            // 6. Даем помахать 1.8 секунды, затем возвращаем базовую эмоцию
            await Task.Delay(1800, ct);

            if (!ct.IsCancellationRequested)
            {
                ApplyAnimation(_currentEmotionGif, force: true);
                _isReacting = false;
            }
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FinnyPetView] Error in OnPetTapped: {ex.Message}");
            _isReacting = false;
        }
    }
}
