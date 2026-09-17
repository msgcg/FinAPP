using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinAPP.Models;
using FinAPP.Services;
using FinAPP.Views.Components;
using Microsoft.Maui.Controls;

namespace FinAPP;

public partial class MainPage : ContentPage
{
    private readonly StorageService _storageService;
    private readonly GameEngine _engine;
    private IDispatcherTimer? _gameLoopTimer;

    // Состояние планирования в модалке бюджета
    private int _tempOblig = 250;
    private int _tempDisc = 150;
    private int _tempSav = 100;
    private const int PeriodIncome = 500;

    // Отрисовщики кастомных графиков
    private readonly BudgetDonutChartDrawable _budgetDonut = new();
    private readonly HistoryChartDrawable _historyChart = new();

    // Интерактивный онбординг (4 шага)
    private int _onboardingStep = 0;
    private readonly (string Title, string Desc, string Icon, string IconBg)[] _onboardingSlides = new[]
    {
        ("Привет! Я твой кот Финни!", 
         "Добро пожаловать в FinAPP — твой персональный тренажёр финансовой грамотности! Вместе мы научимся планировать бюджет, копить на мечту и принимать умные решения!",
         "ic_gift.png", "#7C3AED"),
        ("Правило трёх конвертов",
         "Каждый период карманные деньги распределяются по трём конвертам:\n• Обязательные расходы (уход и здоровье Финни)\n• Желания и радости (игрушки и сладости)\n• Копилка (накопления на твою главную мечту!)",
         "ic_stat_balance.png", "#059669"),
        ("Зарабатывай и получай %!",
         "Решай финансовые задачки, получай монетные награды и откладывай в копилку. А в конце каждого периода банк начисляет +5% сложного процента на все твои сбережения!",
         "ic_stat_savings.png", "#D97706"),
        ("Стань Финни-Мастером!",
         "Заботься обо мне, выбирай подиумы и рабочие столы в гардеробе, следи за бюджетом и пройди путь эволюции от Малыша до Финни-Мастера 3-й стадии!",
         "ic_shield.png", "#520978")
    };

    // Ввод PIN-кода родителя
    private string _currentPinInput = "";

    // Активная задача
    private int _currentTaskIndex = 0;
    private List<FinancialTask> _tasks = new();

    // Выбранная вкладка магазина (0 = Obligatory, 1 = Discretionary, 2 = Interior)
    private int _shopSelectedCategoryTab = 0;

    // Пин для родителей
    private int _parentMathA = 7;
    private int _parentMathB = 8;

    // Режим быстрого трансфера (true = в копилку, false = в кошелек)
    private bool _isTransferToSavings = true;

    public MainPage()
    {
        InitializeComponent();

        _storageService = new StorageService();
        _engine = new GameEngine(_storageService);
        _engine.OnStateChanged += () => MainThread.BeginInvokeOnMainThread(RefreshUI);

        GvBudgetDonut.Drawable = _budgetDonut;
        GvHistoryChart.Drawable = _historyChart;

        PetView.SpeechTextChanged = (text) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LblSpeech.Text = text;
            });
        };

        WvTaskResultFinny.HandlerChanged += (s, e) => FinnyPetView.ConfigurePlatformWebView(WvTaskResultFinny);

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await _engine.InitializeAsync();
        _tasks = _engine.GetTasksForCurrentAge();
        StartPetLifeTimer();
        RefreshUI();
        SyncAudioSwitches();

        if (!_engine.Profile.IsOnboardingCompleted)
        {
            ShowOnboarding();
        }
    }

    private void StartPetLifeTimer()
    {
        _gameLoopTimer?.Stop();
        _gameLoopTimer = Dispatcher.CreateTimer();
        _gameLoopTimer.Interval = TimeSpan.FromSeconds(35);
        _gameLoopTimer.Tick += (s, e) =>
        {
            var p = _engine.Profile;
            p.Hunger = Math.Max(15, p.Hunger - 2);
            p.Mood = Math.Max(20, p.Mood - 2);
            RefreshUI();
        };
        _gameLoopTimer.Start();
    }

    private List<FinancialGoal> GetAllGoals()
    {
        var list = new List<FinancialGoal>(ContentRepository.GetPresetGoals());
        if (_engine.Profile.CustomGoals != null && _engine.Profile.CustomGoals.Count > 0)
        {
            list.AddRange(_engine.Profile.CustomGoals);
        }
        return list;
    }

    private void RefreshUI()
    {
        var p = _engine.Profile;
        var allGoals = GetAllGoals();
        var currentGoal = allGoals.FirstOrDefault(g => g.Id == p.SelectedGoalId) 
            ?? allGoals.First();

        // 0. Демо-режим (по ТЗ управляется из Кабинета родителей)
        BadgeDemo.IsVisible = p.IsDemoMode;
        CardDemoNextPeriod.IsVisible = p.IsDemoMode;

        // 1. Питомец и эмоция
        string emotion = _engine.CurrentEmotion;
        PetView.UpdatePet(p, emotion);
        LblPetHeader.Text = $"{p.PetName} ({p.KidName})";
        LblPeriodHeader.Text = $"Период #{p.CurrentPeriod}";
        LblStatusExplanation.Text = _engine.EmotionStatusExplanation;

        // 2. Показатели сытости и настроения
        LblHungerVal.Text = $"{p.Hunger}%";
        BarHunger.Progress = p.Hunger / 100.0;
        BarHunger.ProgressColor = p.Hunger switch
        {
            > 60 => Color.FromArgb("#10B981"),
            > 30 => Color.FromArgb("#F59E0B"),
            _ => Color.FromArgb("#EF4444")
        };

        LblMoodVal.Text = $"{p.Mood}%";
        BarMood.Progress = p.Mood / 100.0;
        BarMood.ProgressColor = p.Mood switch
        {
            > 60 => Color.FromArgb("#FC3777"),
            > 30 => Color.FromArgb("#F59E0B"),
            _ => Color.FromArgb("#EF4444")
        };

        // 3. Финансовый дашборд
        LblBalance.Text = $"{p.Balance} монет";
        LblSavings.Text = $"{p.Savings} монет";

        // 4. Прогресс цели
        int percent = currentGoal.GetProgressPercent(p.Savings);
        LblGoalTitle.Text = $"Цель: {currentGoal.Title}";
        ImgGoalCard.Source = currentGoal.IconImage;
        LblGoalProgressText.Text = $"{p.Savings} / {currentGoal.TargetAmount} монет ({percent}%)";
        BarGoal.Progress = percent / 100.0;

        int remainingPeriods = currentGoal.EstimateRemainingPeriods(p.Savings, 50);
        LblGoalEstimatedTime.Text = p.Savings >= currentGoal.TargetAmount
            ? "Цель достигнута! Можно покупать!"
            : $"До цели осталось ~{remainingPeriods} периодов (при +50 монет/период)";

        // 5. Тумблер возраста (7–8 лет / 9–11 лет)
        bool isJunior = p.AgeGroup == AgeGroup.Junior7_8;
        BtnAgeJunior.BackgroundColor = isJunior ? Color.FromArgb("#520978") : Colors.Transparent;
        LblAgeJunior.TextColor = isJunior ? Colors.White : Color.FromArgb("#6B7280");
        LblAgeJunior.FontFamily = isJunior ? "MontserratBold" : "MontserratMedium";

        BtnAgeSenior.BackgroundColor = !isJunior ? Color.FromArgb("#520978") : Colors.Transparent;
        LblAgeSenior.TextColor = !isJunior ? Colors.White : Color.FromArgb("#6B7280");
        LblAgeSenior.FontFamily = !isJunior ? "MontserratBold" : "MontserratMedium";

        if (BtnParentAgeJunior != null)
        {
            BtnParentAgeJunior.BackgroundColor = isJunior ? Color.FromArgb("#520978") : Color.FromArgb("#EBE9F8");
            LblParentAgeJunior.TextColor = isJunior ? Colors.White : Color.FromArgb("#520978");
            BtnParentAgeSenior.BackgroundColor = !isJunior ? Color.FromArgb("#520978") : Color.FromArgb("#EBE9F8");
            LblParentAgeSenior.TextColor = !isJunior ? Colors.White : Color.FromArgb("#520978");
        }

        UpdateParentStageButtons();
    }

    private async void OnSpeechBubbleTapped(object? sender, EventArgs e)
    {
        await AnimateTap(SpeechBubble);
        if (_engine.CurrentEmotion == "sad")
        {
            PetView.SetSpeechText(_engine.EmotionStatusExplanation);
            PetView.PlayAction("sad");
        }
        else
        {
            PetView.NextQuote();
            PetView.PlayAction("wave");
        }
    }

    private async void OnAgeJuniorClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _engine.SetAgeGroup(AgeGroup.Junior7_8);
        _tasks = _engine.GetTasksForCurrentAge();
        _currentTaskIndex = 0;
        PetView.SetSpeechText("Установлена программа для 1–2 классов (7–8 лет)!");
        RefreshUI();
    }

    private async void OnAgeSeniorClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _engine.SetAgeGroup(AgeGroup.Senior9_11);
        _tasks = _engine.GetTasksForCurrentAge();
        _currentTaskIndex = 0;
        PetView.SetSpeechText("Установлена программа для 3–5 классов (9–11 лет)!");
        RefreshUI();
    }

    // =========================================================================
    // АНИМАЦИИ НАЖАТИЙ И УПРАВЛЕНИЕ ВНУТРИСТРАНИЧНЫМИ МОДАЛКАМИ
    // =========================================================================

    private async Task AnimateTap(VisualElement? view)
    {
        if (view == null) return;
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        // Автопроигрывание 1 такта анимации Финни при любом нажатии на кнопку интерфейса
        if (view != PetView)
        {
            PetView?.ReplayCurrent();
        }

        await view.ScaleToAsync(0.93, 60, Easing.CubicOut);
        await view.ScaleToAsync(1.0, 60, Easing.CubicIn);
    }

    private void HideAllPanels()
    {
        PanelBudget.IsVisible = false;
        PanelTasks.IsVisible = false;
        PanelShop.IsVisible = false;
        PanelGoals.IsVisible = false;
        PanelGlossary.IsVisible = false;
        PanelParent.IsVisible = false;
        PanelCustomizer.IsVisible = false;
        PanelTransfer.IsVisible = false;
        PanelAnalytics.IsVisible = false;
        PanelAudioSettings.IsVisible = false;
    }

    private async Task ShowModal(string title, VisualElement activePanel)
    {
        LblModalHeader.Text = title;
        HideAllPanels();

        activePanel.IsVisible = true;
        ModalOverlay.Opacity = 0;
        ModalOverlay.IsVisible = true;
        ModalCard.Scale = 0.92;

        var f = ModalOverlay.FadeToAsync(1.0, 160, Easing.CubicOut);
        var s = ModalCard.ScaleToAsync(1.0, 160, Easing.CubicOut);
        await Task.WhenAll(f, s);
    }

    private async Task CloseModal()
    {
        var f = ModalOverlay.FadeToAsync(0.0, 130, Easing.CubicIn);
        var s = ModalCard.ScaleToAsync(0.92, 130, Easing.CubicIn);
        await Task.WhenAll(f, s);
        ModalOverlay.IsVisible = false;
        HideAllPanels();
    }

    private async void OnCloseModalClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await CloseModal();
    }

    // =========================================================================
    // 1.5. СТИЛИЗОВАННЫЕ ВНУТРИИГРОВЫЕ ДИАЛОГИ (ALERT, CONFIRM, PROMPT, ACTIONSHEET)
    // =========================================================================
    private TaskCompletionSource<bool>? _dialogAlertTcs;
    private TaskCompletionSource<string?>? _dialogPromptTcs;
    private TaskCompletionSource<string?>? _dialogActionSheetTcs;

    private async Task ShowStyledAlertAsync(string title, string message, string icon = "ic_stat_balance.png", string buttonText = "Понятно")
    {
        _dialogAlertTcs?.TrySetCanceled();
        _dialogAlertTcs = new TaskCompletionSource<bool>();

        StylizedDialogTitle.Text = title;
        StylizedDialogMessage.Text = message;
        StylizedDialogIcon.Source = icon;

        StylizedDialogInputContainer.IsVisible = false;
        StylizedDialogOptionsScroll.IsVisible = false;

        StylizedDialogButtonsGrid.IsVisible = true;
        StylizedDialogBtnCancel.IsVisible = false;

        Grid.SetColumn(StylizedDialogBtnConfirm, 0);
        Grid.SetColumnSpan(StylizedDialogBtnConfirm, 2);
        StylizedDialogBtnConfirm.IsVisible = true;
        StylizedDialogBtnConfirm.BackgroundColor = Color.FromArgb("#520978");
        StylizedDialogLblConfirm.Text = buttonText;

        await AnimateShowStyledDialog();
        try { await _dialogAlertTcs.Task; } catch { }
        await AnimateHideStyledDialog();
    }

    private async Task<bool> ShowStyledConfirmAsync(string title, string message, string icon = "ic_stat_balance.png", string confirmText = "Да", string cancelText = "Отмена", bool isDestructive = false)
    {
        _dialogAlertTcs?.TrySetCanceled();
        _dialogAlertTcs = new TaskCompletionSource<bool>();

        StylizedDialogTitle.Text = title;
        StylizedDialogMessage.Text = message;
        StylizedDialogIcon.Source = icon;

        StylizedDialogInputContainer.IsVisible = false;
        StylizedDialogOptionsScroll.IsVisible = false;

        StylizedDialogButtonsGrid.IsVisible = true;
        StylizedDialogBtnCancel.IsVisible = true;
        Grid.SetColumn(StylizedDialogBtnCancel, 0);
        Grid.SetColumnSpan(StylizedDialogBtnCancel, 1);
        StylizedDialogLblCancel.Text = cancelText;

        Grid.SetColumn(StylizedDialogBtnConfirm, 1);
        Grid.SetColumnSpan(StylizedDialogBtnConfirm, 1);
        StylizedDialogBtnConfirm.IsVisible = true;
        StylizedDialogBtnConfirm.BackgroundColor = isDestructive ? Color.FromArgb("#DC2626") : Color.FromArgb("#520978");
        StylizedDialogLblConfirm.Text = confirmText;

        await AnimateShowStyledDialog();
        bool result = false;
        try { result = await _dialogAlertTcs.Task; } catch { }
        await AnimateHideStyledDialog();
        return result;
    }

    private async Task<string?> ShowStyledPromptAsync(string title, string message, string icon = "ic_stat_balance.png", string acceptText = "ОК", string cancelText = "Отмена", string placeholder = "", Keyboard? keyboard = null, int maxLength = 40)
    {
        _dialogPromptTcs?.TrySetCanceled();
        _dialogPromptTcs = new TaskCompletionSource<string?>();

        StylizedDialogTitle.Text = title;
        StylizedDialogMessage.Text = message;
        StylizedDialogIcon.Source = icon;

        StylizedDialogEntry.Text = string.Empty;
        StylizedDialogEntry.Placeholder = placeholder;
        StylizedDialogEntry.Keyboard = keyboard ?? Keyboard.Default;
        StylizedDialogEntry.MaxLength = maxLength;
        StylizedDialogInputContainer.IsVisible = true;
        StylizedDialogOptionsScroll.IsVisible = false;

        StylizedDialogButtonsGrid.IsVisible = true;
        StylizedDialogBtnCancel.IsVisible = true;
        Grid.SetColumn(StylizedDialogBtnCancel, 0);
        Grid.SetColumnSpan(StylizedDialogBtnCancel, 1);
        StylizedDialogLblCancel.Text = cancelText;

        Grid.SetColumn(StylizedDialogBtnConfirm, 1);
        Grid.SetColumnSpan(StylizedDialogBtnConfirm, 1);
        StylizedDialogBtnConfirm.IsVisible = true;
        StylizedDialogBtnConfirm.BackgroundColor = Color.FromArgb("#520978");
        StylizedDialogLblConfirm.Text = acceptText;

        await AnimateShowStyledDialog();
        StylizedDialogEntry.Focus();
        string? result = null;
        try { result = await _dialogPromptTcs.Task; } catch { }
        await AnimateHideStyledDialog();
        return result;
    }

    private async Task<string?> ShowStyledActionSheetAsync(string title, string message, string icon = "ic_stat_balance.png", string cancelText = "Отмена", params string[] options)
    {
        _dialogActionSheetTcs?.TrySetCanceled();
        _dialogActionSheetTcs = new TaskCompletionSource<string?>();

        StylizedDialogTitle.Text = title;
        StylizedDialogMessage.Text = message;
        StylizedDialogIcon.Source = icon;

        StylizedDialogInputContainer.IsVisible = false;
        StylizedDialogOptionsContainer.Children.Clear();

        foreach (var opt in options)
        {
            var optCard = new Border
            {
                BackgroundColor = Color.FromArgb("#F8F7FD"),
                Stroke = Color.FromArgb("#8A83D1"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(14, 10),
                InputTransparent = false
            };
            var optLabel = new Label
            {
                Text = opt,
                FontFamily = "MontserratBold",
                FontSize = 13,
                TextColor = Color.FromArgb("#520978"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            optCard.Content = optLabel;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                await AnimateTap(optCard);
                _dialogActionSheetTcs?.TrySetResult(opt);
            };
            optCard.GestureRecognizers.Add(tap);
            StylizedDialogOptionsContainer.Children.Add(optCard);
        }

        StylizedDialogOptionsScroll.IsVisible = true;

        StylizedDialogButtonsGrid.IsVisible = true;
        StylizedDialogBtnCancel.IsVisible = true;
        Grid.SetColumn(StylizedDialogBtnCancel, 0);
        Grid.SetColumnSpan(StylizedDialogBtnCancel, 2);
        StylizedDialogLblCancel.Text = cancelText;
        StylizedDialogBtnConfirm.IsVisible = false;

        await AnimateShowStyledDialog();
        string? result = null;
        try { result = await _dialogActionSheetTcs.Task; } catch { }
        await AnimateHideStyledDialog();
        return result;
    }

    private async Task AnimateShowStyledDialog()
    {
        StylizedDialogOverlay.Opacity = 0;
        StylizedDialogOverlay.IsVisible = true;
        StylizedDialogCard.Scale = 0.9;
        var f = StylizedDialogOverlay.FadeToAsync(1.0, 150, Easing.CubicOut);
        var s = StylizedDialogCard.ScaleToAsync(1.0, 150, Easing.CubicOut);
        await Task.WhenAll(f, s);
    }

    private async Task AnimateHideStyledDialog()
    {
        var f = StylizedDialogOverlay.FadeToAsync(0.0, 120, Easing.CubicIn);
        var s = StylizedDialogCard.ScaleToAsync(0.9, 120, Easing.CubicIn);
        await Task.WhenAll(f, s);
        StylizedDialogOverlay.IsVisible = false;
    }

    private async void OnStylizedDialogCancelClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _dialogAlertTcs?.TrySetResult(false);
        _dialogPromptTcs?.TrySetResult(null);
        _dialogActionSheetTcs?.TrySetResult(StylizedDialogLblCancel.Text);
    }

    private async void OnStylizedDialogConfirmClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _dialogAlertTcs?.TrySetResult(true);
        _dialogPromptTcs?.TrySetResult(StylizedDialogEntry.Text?.Trim() ?? string.Empty);
    }

    // =========================================================================
    // 2. МОДАЛКА: КАСТОМИЗАЦИЯ ПОДИУМОВ, РАБОЧИХ СТОЛОВ И ИМЕНИ
    // =========================================================================
    private async void OnCustomizerClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        EntryPetName.Text = _engine.Profile.PetName;
        UpdateCustomizerPlatformBadges(_engine.Profile.Platform);
        UpdateCustomizerDeskBadges(_engine.Profile.Desk);
        SetCustomizerTab(true);
        await ShowModal("Внешний вид и имя Финни", PanelCustomizer);
    }

    private void SetCustomizerTab(bool isPlatform)
    {
        if (isPlatform)
        {
            BtnCustomizerTabPlatform.BackgroundColor = Color.FromArgb("#520978");
            LblCustomizerTabPlatform.TextColor = Colors.White;
            BtnCustomizerTabDesk.BackgroundColor = Color.FromArgb("#EBE9F8");
            LblCustomizerTabDesk.TextColor = Color.FromArgb("#520978");
            CustomizerPlatformsView.IsVisible = true;
            CustomizerDesksView.IsVisible = false;
        }
        else
        {
            BtnCustomizerTabDesk.BackgroundColor = Color.FromArgb("#520978");
            LblCustomizerTabDesk.TextColor = Colors.White;
            BtnCustomizerTabPlatform.BackgroundColor = Color.FromArgb("#EBE9F8");
            LblCustomizerTabPlatform.TextColor = Color.FromArgb("#520978");
            CustomizerPlatformsView.IsVisible = false;
            CustomizerDesksView.IsVisible = true;
        }
    }

    private async void OnCustomizerTabPlatformClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        SetCustomizerTab(true);
    }

    private async void OnCustomizerTabDeskClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        SetCustomizerTab(false);
    }

    private static int GetPlatformPrice(PetPlatformType platform) => platform switch
    {
        PetPlatformType.Stars => 180,
        PetPlatformType.Emerald => 220,
        PetPlatformType.Cosmic => 260,
        PetPlatformType.Cloud => 200,
        _ => 0
    };

    private static string GetPlatformName(PetPlatformType platform) => platform switch
    {
        PetPlatformType.Flowers => "Цветочная полянка",
        PetPlatformType.Stars => "Звёздная дорожка",
        PetPlatformType.Emerald => "Изумрудный кристалл",
        PetPlatformType.Cosmic => "Космический неон",
        PetPlatformType.Cloud => "Облако накоплений",
        _ => "Подиум"
    };

    private static int GetDeskPrice(PetDeskType desk) => desk switch
    {
        PetDeskType.Modern => 200,
        PetDeskType.Artisan => 220,
        PetDeskType.Market => 250,
        PetDeskType.Maker => 280,
        PetDeskType.Reading => 300,
        PetDeskType.Botanical => 240,
        _ => 0
    };

    private static string GetDeskName(PetDeskType desk) => desk switch
    {
        PetDeskType.None => "Без стола",
        PetDeskType.Modern => "Стол IT-финансиста",
        PetDeskType.Artisan => "Творческий стол",
        PetDeskType.Market => "Лавка предпринимателя",
        PetDeskType.Maker => "Верстак инженера",
        PetDeskType.Reading => "Кабинет профессора",
        PetDeskType.Botanical => "Эко-стол биолога",
        _ => "Рабочий стол"
    };

    private static string GetDeskIcon(PetDeskType desk) => desk switch
    {
        PetDeskType.Modern => "desk_modern.png",
        PetDeskType.Artisan => "desk_artisan.png",
        PetDeskType.Market => "desk_market.png",
        PetDeskType.Maker => "desk_maker.png",
        PetDeskType.Reading => "desk_reading.png",
        PetDeskType.Botanical => "desk_botanical.png",
        _ => "ic_customizer.png"
    };

    private void UpdateBadgeState(Border badge, bool isUnlocked, bool isActive, int price)
    {
        if (badge.Content is Label lbl)
        {
            badge.IsVisible = true;
            if (isActive)
            {
                lbl.Text = "Активно ✓";
                lbl.TextColor = Colors.White;
                badge.BackgroundColor = Color.FromArgb("#10B981");
                badge.StrokeThickness = 0;
            }
            else if (isUnlocked)
            {
                lbl.Text = "Выбрать";
                lbl.TextColor = Colors.White;
                badge.BackgroundColor = Color.FromArgb("#10B981");
                badge.StrokeThickness = 0;
            }
            else
            {
                bool canAfford = _engine.Profile.Balance >= price;
                lbl.Text = canAfford ? $"🔓 {price} м." : $"🔒 {price} м.";
                lbl.TextColor = Colors.White;
                badge.BackgroundColor = canAfford ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
                badge.StrokeThickness = 0;
            }
        }
    }

    private void UpdateCustomizerPlatformBadges(PetPlatformType platform)
    {
        var p = _engine.Profile;
        UpdateBadgeState(BadgePlatformFlowers, true, platform == PetPlatformType.Flowers, 0);
        UpdateBadgeState(BadgePlatformStars, p.IsPlatformUnlocked(PetPlatformType.Stars), platform == PetPlatformType.Stars, GetPlatformPrice(PetPlatformType.Stars));
        UpdateBadgeState(BadgePlatformEmerald, p.IsPlatformUnlocked(PetPlatformType.Emerald), platform == PetPlatformType.Emerald, GetPlatformPrice(PetPlatformType.Emerald));
        UpdateBadgeState(BadgePlatformCosmic, p.IsPlatformUnlocked(PetPlatformType.Cosmic), platform == PetPlatformType.Cosmic, GetPlatformPrice(PetPlatformType.Cosmic));
        UpdateBadgeState(BadgePlatformCloud, p.IsPlatformUnlocked(PetPlatformType.Cloud), platform == PetPlatformType.Cloud, GetPlatformPrice(PetPlatformType.Cloud));
    }

    private void UpdateCustomizerDeskBadges(PetDeskType desk)
    {
        var p = _engine.Profile;
        UpdateBadgeState(BadgeDeskNone, true, desk == PetDeskType.None, 0);
        UpdateBadgeState(BadgeDeskModern, p.IsDeskUnlocked(PetDeskType.Modern), desk == PetDeskType.Modern, GetDeskPrice(PetDeskType.Modern));
        UpdateBadgeState(BadgeDeskArtisan, p.IsDeskUnlocked(PetDeskType.Artisan), desk == PetDeskType.Artisan, GetDeskPrice(PetDeskType.Artisan));
        UpdateBadgeState(BadgeDeskMarket, p.IsDeskUnlocked(PetDeskType.Market), desk == PetDeskType.Market, GetDeskPrice(PetDeskType.Market));
        UpdateBadgeState(BadgeDeskMaker, p.IsDeskUnlocked(PetDeskType.Maker), desk == PetDeskType.Maker, GetDeskPrice(PetDeskType.Maker));
        UpdateBadgeState(BadgeDeskReading, p.IsDeskUnlocked(PetDeskType.Reading), desk == PetDeskType.Reading, GetDeskPrice(PetDeskType.Reading));
        UpdateBadgeState(BadgeDeskBotanical, p.IsDeskUnlocked(PetDeskType.Botanical), desk == PetDeskType.Botanical, GetDeskPrice(PetDeskType.Botanical));
    }

    private async Task SelectPlatformAsync(PetPlatformType platform, string speech)
    {
        var p = _engine.Profile;
        if (!p.IsPlatformUnlocked(platform))
        {
            int price = GetPlatformPrice(platform);
            string? choice = await ShowStyledActionSheetAsync(
                $"Подиум «{GetPlatformName(platform)}»",
                $"Этот подиум закрыт. Стоимость: {price} монет.",
                "ic_customizer.png",
                "Отмена",
                $"Купить за {price} монет", "Поставить целью накопления 🎯");
            if (choice == $"Купить за {price} монет")
            {
                if (p.Balance < price)
                {
                    AudioService.Instance.PlaySfx("sfx_error");
                    await ShowStyledAlertAsync(
                        "Не хватает монет",
                        $"У тебя {p.Balance} монет, а требуется {price} монет.\nПополни баланс за счёт заданий или сними часть из копилки.",
                        "ic_stat_balance.png",
                        "Понятно");
                    return;
                }
                p.Balance -= price;
                p.SpentDiscretionary += price;
                p.UnlockPlatform(platform);
                p.Platform = platform;
                PlayPurchaseParticleBurst("ic_stat_mood.png");
                AudioService.Instance.PlaySfx("sfx_money");
                AudioService.Instance.PlaySfx("sfx_purr");
                PetView.UpdatePlatform(platform);
                RefreshUI();
                await _engine.SaveAsync();
                UpdateCustomizerPlatformBadges(platform);
                PetView.SetSpeechText($"Ура! Подиум «{GetPlatformName(platform)}» куплен и установлен!");
                PetView.PlayAction("proud");
            }
            else if (choice == "Поставить целью накопления 🎯")
            {
                SetPlatformAsGoal(platform);
            }
            return;
        }

        p.Platform = platform;
        AudioService.Instance.PlaySfx("sfx_tap");
        PetView.UpdatePlatform(platform);
        RefreshUI();
        await _engine.SaveAsync();
        UpdateCustomizerPlatformBadges(platform);
        PetView.SetSpeechText(speech);
        PetView.PlayAction("wave");
    }

    private void SetPlatformAsGoal(PetPlatformType platform)
    {
        int price = GetPlatformPrice(platform);
        string name = GetPlatformName(platform);
        var allGoals = GetAllGoals();
        var existing = allGoals.FirstOrDefault(g => g.LinkedPlatform == platform);
        if (existing == null)
        {
            existing = new FinancialGoal
            {
                Id = $"goal_plat_{platform}",
                Title = $"Подиум «{name}»",
                TargetAmount = price,
                IconImage = "ic_stat_mood.png",
                LinkedPlatform = platform,
                Description = "Стильный подиум для комнаты Финни.",
                IsCustom = true
            };
            _engine.Profile.CustomGoals.Add(existing);
        }

        _engine.Profile.SelectedGoalId = existing.Id;
        RefreshUI();
        _ = _engine.SaveAsync();
        RenderGoalsUI();
        AudioService.Instance.PlaySfx("sfx_button");
        PetView.SetSpeechText($"Подиум «{name}» выбран новой целью! Копим {price} монет!");
    }

    private void SetDeskAsGoal(PetDeskType desk)
    {
        int price = GetDeskPrice(desk);
        string name = GetDeskName(desk);
        var allGoals = GetAllGoals();
        var existing = allGoals.FirstOrDefault(g => g.LinkedDesk == desk);
        if (existing == null)
        {
            existing = new FinancialGoal
            {
                Id = $"goal_desk_{desk}",
                Title = name,
                TargetAmount = price,
                IconImage = GetDeskIcon(desk),
                LinkedDesk = desk,
                Description = "Рабочий стол для финансового кабинета Финни.",
                IsCustom = true
            };
            _engine.Profile.CustomGoals.Add(existing);
        }

        _engine.Profile.SelectedGoalId = existing.Id;
        RefreshUI();
        _ = _engine.SaveAsync();
        RenderGoalsUI();
        AudioService.Instance.PlaySfx("sfx_button");
        PetView.SetSpeechText($"Рабочий стол «{name}» выбран новой целью! Копим {price} монет!");
    }

    private async void OnPlatformFlowersClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Flowers, "Зелёная полянка с ромашками! Свежий воздух вдохновляет расти!");
    }

    private async void OnPlatformStarsClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Stars, "Золотой звёздный подиум! Сверкает за каждую сохранённую монетку!");
    }

    private async void OnPlatformEmeraldClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Emerald, "Изумрудный кристалл! Символ финансовой стабильности и процветания!");
    }

    private async void OnPlatformCosmicClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Cosmic, "Космический неон! Кибер-платформа для полёта к звёздным целям!");
    }

    private async void OnPlatformCloudClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Cloud, "Облако накоплений! Мягкий небесный подиум для лёгких сбережений!");
    }

    private async Task SelectDeskAsync(PetDeskType desk, string speech)
    {
        var p = _engine.Profile;
        if (!p.IsDeskUnlocked(desk))
        {
            int price = GetDeskPrice(desk);
            string? choice = await ShowStyledActionSheetAsync(
                $"Стол «{GetDeskName(desk)}»",
                $"Этот рабочий стол закрыт. Стоимость: {price} монет.",
                "ic_customizer.png",
                "Отмена",
                $"Купить за {price} монет", "Поставить целью накопления 🎯");
            if (choice == $"Купить за {price} монет")
            {
                if (p.Balance < price)
                {
                    AudioService.Instance.PlaySfx("sfx_error");
                    await ShowStyledAlertAsync(
                        "Не хватает монет",
                        $"У тебя {p.Balance} монет, а требуется {price} монет.\nПополни баланс за счёт заданий или сними часть из копилки.",
                        "ic_stat_balance.png",
                        "Понятно");
                    return;
                }
                p.Balance -= price;
                p.SpentDiscretionary += price;
                p.UnlockDesk(desk);
                p.Desk = desk;
                PlayPurchaseParticleBurst(GetDeskIcon(desk));
                AudioService.Instance.PlaySfx("sfx_money");
                AudioService.Instance.PlaySfx("sfx_purr");
                PetView.UpdateDesk(desk);
                RefreshUI();
                await _engine.SaveAsync();
                UpdateCustomizerDeskBadges(desk);
                PetView.SetSpeechText($"Ура! Рабочий стол «{GetDeskName(desk)}» куплен!");
                PetView.PlayAction("proud");
                return;
            }
            else if (choice == "Поставить целью накопления 🎯")
            {
                SetDeskAsGoal(desk);
                return;
            }
            return;
        }

        p.Desk = desk;
        UpdateCustomizerDeskBadges(desk);
        PetView.UpdateDesk(desk);
        PetView.PlayAction("proud");
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText(speech);
    }

    private async void OnDeskNoneClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.None, "Стол убран. Финни свободно гуляет по подиуму!");
    }

    private async void OnDeskModernClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.Modern, "Современная дизайн-студия! Ноутбук готов к учету цифровых финансов!");
    }

    private async void OnDeskArtisanClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.Artisan, "Мастерская ремесленника! Инструменты помогают создавать ценные вещи своими руками!");
    }

    private async void OnDeskMarketClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.Market, "Торговая лавка! Учимся продавать, договариваться и понимать основы торговли!");
    }

    private async void OnDeskMakerClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.Maker, "Лаборатория инженера! Чертежи, лампы и точные расчеты бюджета!");
    }

    private async void OnDeskReadingClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.Reading, "Кабинет профессора! Книги мудрости, глобус и финансовая наука!");
    }

    private async void OnDeskBotanicalClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await SelectDeskAsync(PetDeskType.Botanical, "Эко-стол биолога! Заботимся о растениях, как о растущих инвестициях!");
    }

    private async void OnSavePetNameClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        string newName = EntryPetName.Text?.Trim() ?? "";
        if (!string.IsNullOrWhiteSpace(newName))
        {
            _engine.Profile.PetName = newName;
            RefreshUI();
            await _engine.SaveAsync();
            PetView.SetSpeechText($"Ура! Теперь меня зовут {newName}!");
            await CloseModal();
        }
    }

    // =========================================================================
    // 3. МОДАЛКА: ПЛАНИРОВАНИЕ БЮДЖЕТА (3 КОНВЕРТА)
    // =========================================================================
    private async void OnBudgetClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        var p = _engine.Profile;
        _tempOblig = p.PlannedObligatory > 0 ? p.PlannedObligatory : 250;
        _tempDisc = p.PlannedDiscretionary > 0 ? p.PlannedDiscretionary : 150;
        _tempSav = p.PlannedSavings > 0 ? p.PlannedSavings : 100;

        BorderPlanFactCard.IsVisible = false;
        BorderBudgetAlert.IsVisible = false;
        LblComparePlanFactButton.Text = "Сравнить План и Факт";

        if (p.IsPlanConfirmed)
        {
            LblSaveBudgetPlan.Text = "Обновить план бюджета";
            BtnSaveBudgetPlan.BackgroundColor = Color.FromArgb("#520978");
        }
        else
        {
            LblSaveBudgetPlan.Text = "Утвердить план бюджета";
            BtnSaveBudgetPlan.BackgroundColor = Color.FromArgb("#520978");
        }

        UpdateBudgetModalLabels();
        await ShowModal($"Бюджет периода #{p.CurrentPeriod}", PanelBudget);
    }

    private void UpdateBudgetModalLabels()
    {
        LblBudgetIncome.Text = $"{PeriodIncome} монет";
        LblBudgetObligVal.Text = $"{_tempOblig} монет";
        LblBudgetDiscVal.Text = $"{_tempDisc} монет";
        LblBudgetSavVal.Text = $"{_tempSav} монет";

        int sum = _tempOblig + _tempDisc + _tempSav;
        int diff = PeriodIncome - sum;
        if (diff == 0)
        {
            LblBudgetRemainingTitle.Text = "БАЛАНС";
            LblBudgetRemaining.Text = "100% сошлось ✓";
            LblBudgetRemaining.TextColor = Color.FromArgb("#059669");
            BorderBudgetRemaining.BackgroundColor = Color.FromArgb("#DCFCE7");
        }
        else if (diff > 0)
        {
            LblBudgetRemainingTitle.Text = "ОСТАЛОСЬ";
            LblBudgetRemaining.Text = $"{diff} монет";
            LblBudgetRemaining.TextColor = Color.FromArgb("#520978");
            BorderBudgetRemaining.BackgroundColor = Color.FromArgb("#EBE9F8");
        }
        else
        {
            LblBudgetRemainingTitle.Text = "ПЕРЕРАСХОД";
            LblBudgetRemaining.Text = $"-{Math.Abs(diff)} монет";
            LblBudgetRemaining.TextColor = Color.FromArgb("#DC2626");
            BorderBudgetRemaining.BackgroundColor = Color.FromArgb("#FEE2E2");
        }

        // Обновляем визуальную круговую диаграмму (ТЗ п. 2.5.5) и легенду
        _budgetDonut.PlannedObligatory = _tempOblig;
        _budgetDonut.PlannedDiscretionary = _tempDisc;
        _budgetDonut.PlannedSavings = _tempSav;
        GvBudgetDonut.Invalidate();

        LblLegendOblig.Text = $"{_tempOblig} м.";
        LblLegendDisc.Text = $"{_tempDisc} м.";
        LblLegendSav.Text = $"{_tempSav} м.";
    }

    private async void OnBudgetObligPlus(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_tempOblig + 10 <= PeriodIncome) { _tempOblig += 10; UpdateBudgetModalLabels(); }
    }
    private async void OnBudgetObligMinus(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_tempOblig - 10 >= 0) { _tempOblig -= 10; UpdateBudgetModalLabels(); }
    }

    private async void OnBudgetDiscPlus(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_tempDisc + 10 <= PeriodIncome) { _tempDisc += 10; UpdateBudgetModalLabels(); }
    }
    private async void OnBudgetDiscMinus(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_tempDisc - 10 >= 0) { _tempDisc -= 10; UpdateBudgetModalLabels(); }
    }

    private async void OnBudgetSavPlus(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_tempSav + 10 <= PeriodIncome) { _tempSav += 10; UpdateBudgetModalLabels(); }
    }
    private async void OnBudgetSavMinus(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_tempSav - 10 >= 0) { _tempSav -= 10; UpdateBudgetModalLabels(); }
    }

    private async void OnSaveBudgetPlanClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        int total = _tempOblig + _tempDisc + _tempSav;
        if (total > PeriodIncome)
        {
            AudioService.Instance.PlaySfx("sfx_error");
            BorderBudgetAlert.BackgroundColor = Color.FromArgb("#FEE2E2");
            BorderBudgetAlert.Stroke = Color.FromArgb("#F87171");
            ImgBudgetAlert.Source = "ic_stat_mood.png";
            LblBudgetAlertText.Text = $"Мяу! План ({total} м.) превышает доход ({PeriodIncome} м.)! Уменьши конверты.";
            LblBudgetAlertText.TextColor = Color.FromArgb("#991B1B");
            BorderBudgetAlert.IsVisible = true;
            PetView.SetSpeechText("Мяу! План превышает доход! Уменьши одну из категорий.");
            return;
        }

        var p = _engine.Profile;
        p.PlannedObligatory = _tempOblig;
        p.PlannedDiscretionary = _tempDisc;
        p.PlannedSavings = _tempSav;
        p.IsPlanConfirmed = true;

        AudioService.Instance.PlaySfx("sfx_success");
        BorderBudgetAlert.BackgroundColor = Color.FromArgb("#DCFCE7");
        BorderBudgetAlert.Stroke = Color.FromArgb("#4ADE80");
        ImgBudgetAlert.Source = "ic_shield.png";
        LblBudgetAlertText.Text = "План бюджета успешно утверждён! Следуй распределению конвертов.";
        LblBudgetAlertText.TextColor = Color.FromArgb("#166534");
        BorderBudgetAlert.IsVisible = true;

        LblSaveBudgetPlan.Text = "План утверждён ✓";
        BtnSaveBudgetPlan.BackgroundColor = Color.FromArgb("#059669");

        PetView.PlayAction("proud");
        PetView.SetSpeechText("Отличный план! Теперь совершай покупки согласно конвертам.");
        RefreshUI();
        await _engine.SaveAsync();

        // Задержка 800мс, чтобы ребенок увидел подтверждение в окне, затем плавное закрытие
        await Task.Delay(800);
        await CloseModal();
    }

    private async void OnShowPlanFactClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        AudioService.Instance.PlaySfx("sfx_money");

        BorderPlanFactCard.IsVisible = !BorderPlanFactCard.IsVisible;
        if (!BorderPlanFactCard.IsVisible)
        {
            LblComparePlanFactButton.Text = "Сравнить План и Факт";
            return;
        }

        LblComparePlanFactButton.Text = "Скрыть сравнение";

        var p = _engine.Profile;
        int obligDiff = p.SpentObligatory - _tempOblig;
        int discDiff = p.SpentDiscretionary - _tempDisc;

        LblPlanFactOblig.Text = $"План {_tempOblig} м. | Факт {p.SpentObligatory} м.";
        LblPlanFactOblig.TextColor = obligDiff > 0 ? Color.FromArgb("#DC2626") : Color.FromArgb("#166534");

        LblPlanFactDisc.Text = $"План {_tempDisc} м. | Факт {p.SpentDiscretionary} м.";
        LblPlanFactDisc.TextColor = discDiff > 0 ? Color.FromArgb("#DC2626") : Color.FromArgb("#9D174D");

        LblPlanFactSav.Text = $"План {_tempSav} м. | Факт {p.Savings} м.";
        LblPlanFactSav.TextColor = Color.FromArgb("#1E40AF");

        if (obligDiff > 0 || discDiff > 0)
        {
            LblPlanFactStatusBadge.Text = "Есть перерасход!";
            LblPlanFactStatusBadge.TextColor = Color.FromArgb("#DC2626");
            LblPlanFactAdvice.Text = "Внимание: по одной из категорий факт превысил план. Старайся не превышать конверт!";
        }
        else
        {
            LblPlanFactStatusBadge.Text = "В рамках плана ✓";
            LblPlanFactStatusBadge.TextColor = Color.FromArgb("#166534");
            LblPlanFactAdvice.Text = "Отличная дисциплина! Твои расходы строго в рамках запланированного бюджета.";
        }

        PetView.SetSpeechText("Вот как соотносятся твои планы и реальные расходы!");
    }

    // =========================================================================
    // 4. МОДАЛКА: ОБРАЗОВАТЕЛЬНЫЕ ЗАДАНИЯ И КВИЗ
    // =========================================================================
    private async void OnTasksClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        RenderCurrentTask();
        await ShowModal("Финансовые задачи", PanelTasks);
    }

    private void RenderCurrentTask()
    {
        if (_tasks.Count == 0) return;
        var task = _tasks[_currentTaskIndex % _tasks.Count];

        LblTaskTopic.Text = $"{task.TopicDisplayName}";
        LblTaskSituation.Text = $"{task.Title}\n\n{task.ScenarioDescription}";
        TaskFeedbackBorder.IsVisible = false;

        TaskOptionsContainer.Children.Clear();
        for (int i = 0; i < task.Options.Count; i++)
        {
            var option = task.Options[i];

            var optionBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#F9FAFB"),
                Stroke = Color.FromArgb("#E5E7EB"),
                StrokeThickness = 1.5,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12, 10),
                InputTransparent = false
            };

            var label = new Label
            {
                Text = $"{i + 1}. {option.Text}",
                FontFamily = "MontserratMedium",
                FontSize = 12,
                TextColor = Color.FromArgb("#1F2937"),
                LineBreakMode = LineBreakMode.WordWrap
            };
            optionBorder.Content = label;

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                await AnimateTap(optionBorder);
                await SelectTaskAnswer(task, option, optionBorder);
            };
            optionBorder.GestureRecognizers.Add(tap);

            TaskOptionsContainer.Children.Add(optionBorder);
        }
    }

    private async Task SelectTaskAnswer(FinancialTask task, TaskOption option, Border selectedBorder)
    {
        var (success, msg) = _engine.CompleteTask(task, option);
        TaskFeedbackBorder.IsVisible = true;

        if (success)
        {
            selectedBorder.BackgroundColor = Color.FromArgb("#DCFCE7");
            selectedBorder.Stroke = Color.FromArgb("#10B981");
            TaskFeedbackBorder.BackgroundColor = Color.FromArgb("#F0FDF4");
            TaskFeedbackBorder.Stroke = Color.FromArgb("#86EFAC");
            LblTaskFeedback.Text = msg;
            LblTaskFeedback.TextColor = Color.FromArgb("#166534");

            RefreshUI();
            PetView.SetSpeechText($"Ура! Ты отлично решил задачу и заработал +{option.RewardCoins} монет!");
        }
        else
        {
            selectedBorder.BackgroundColor = Color.FromArgb("#FEF3C7");
            selectedBorder.Stroke = Color.FromArgb("#F59E0B");
            TaskFeedbackBorder.BackgroundColor = Color.FromArgb("#FFFBEB");
            TaskFeedbackBorder.Stroke = Color.FromArgb("#FCD34D");
            LblTaskFeedback.Text = msg;
            LblTaskFeedback.TextColor = Color.FromArgb("#92400E");
            PetView.SetSpeechText("Ошибаться полезно — так мы учимся быть финансово грамотными!");
        }

        // Показываем праздничную / поучительную модалку результата с анимированным Финни
        await ShowTaskResultModal(success, task, option);
    }

    private async Task ShowTaskResultModal(bool success, FinancialTask task, TaskOption option)
    {
        var p = _engine.Profile;
        string stagePrefix = p.Stage switch
        {
            GrowthStage.Baby => "finny_baby",
            GrowthStage.Teen => "finny_teen",
            _ => "finny_master"
        };

        if (success)
        {
            AudioService.Instance.PlaySfx("sfx_success");
            // Праздничный стиль (изумрудно-зеленый шейдер-градиент)
            HeaderTaskResult.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#059669"), 0.0f),
                    new GradientStop(Color.FromArgb("#10B981"), 1.0f)
                }
            };
            TaskResultCard.Stroke = Color.FromArgb("#10B981");
            ShadowTaskResult.Brush = Color.FromArgb("#10B981");

            ImgTaskResultIcon.Source = "ic_gift.png";
            LblTaskResultTitle.Text = "УРА! ПРАВИЛЬНЫЙ ОТВЕТ!";
            BadgeTaskResultReward.IsVisible = true;
            LblTaskResultReward.Text = $"+{option.RewardCoins} монет на баланс!";

            LblTaskResultExplanationHeader.Text = "Мудрость Финни:";
            LblTaskResultExplanationHeader.TextColor = Color.FromArgb("#059669");
            LblTaskResultExplanation.Text = option.Explanation;
            LblTaskResultExplanation.TextColor = Color.FromArgb("#065F46");
            BorderTaskResultExplanation.BackgroundColor = Color.FromArgb("#F0FDF4");
            BorderTaskResultExplanation.Stroke = Color.FromArgb("#86EFAC");

            BtnTaskResultTryAgain.IsVisible = false;
            Grid.SetColumn(BtnTaskResultNext, 0);
            Grid.SetColumnSpan(BtnTaskResultNext, 2);
            LblTaskResultNext.Text = "Следующее задание ➜";
            BtnTaskResultNext.BackgroundColor = Color.FromArgb("#10B981");
        }
        else
        {
            AudioService.Instance.PlaySfx("sfx_error");
            // Поучительный/поддерживающий стиль (розово-коралловый шейдер-градиент)
            HeaderTaskResult.Background = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromArgb("#BE123C"), 0.0f),
                    new GradientStop(Color.FromArgb("#E11D48"), 1.0f)
                }
            };
            TaskResultCard.Stroke = Color.FromArgb("#E11D48");
            ShadowTaskResult.Brush = Color.FromArgb("#E11D48");

            ImgTaskResultIcon.Source = "ic_shield.png";
            LblTaskResultTitle.Text = "ЕСТЬ НАД ЧЕМ ПОДУМАТЬ!";
            BadgeTaskResultReward.IsVisible = false;

            LblTaskResultExplanationHeader.Text = "Совет от Финни:";
            LblTaskResultExplanationHeader.TextColor = Color.FromArgb("#BE123C");
            LblTaskResultExplanation.Text = option.Explanation;
            LblTaskResultExplanation.TextColor = Color.FromArgb("#881337");
            BorderTaskResultExplanation.BackgroundColor = Color.FromArgb("#FFF1F2");
            BorderTaskResultExplanation.Stroke = Color.FromArgb("#FECDD3");

            BtnTaskResultTryAgain.IsVisible = true;
            Grid.SetColumn(BtnTaskResultNext, 1);
            Grid.SetColumnSpan(BtnTaskResultNext, 1);
            LblTaskResultNext.Text = "Дальше ➜";
            BtnTaskResultNext.BackgroundColor = Color.FromArgb("#6B7280");
        }

        // 1. Делаем контейнер видимым, чтобы нативный Android WebView инициализировался
        ModalTaskResult.Opacity = 0;
        ModalTaskResult.IsVisible = true;
        TaskResultCard.Scale = 0.88;
        WvTaskResultFinny.Scale = 0.85;

        // 2. Получаем СВЕЖИЙ HTML с уникальным Comment Extension для гарантированного автопроигрывания 1 такта
        string gif = success ? $"{stagePrefix}_proud.gif" : $"{stagePrefix}_sad.gif";
        string html = await FinnyPetView.GetFreshHtmlAsync(gif);
        if (!string.IsNullOrEmpty(html))
        {
            WvTaskResultFinny.Source = new HtmlWebViewSource { Html = html };
            FinnyPetView.ConfigurePlatformWebView(WvTaskResultFinny);
        }

        // 3. Плавное появление с подскоком персонажа
        var f = ModalTaskResult.FadeToAsync(1.0, 160, Easing.CubicOut);
        var s = TaskResultCard.ScaleToAsync(1.0, 160, Easing.CubicOut);
        var w = WvTaskResultFinny.ScaleToAsync(1.0, 180, Easing.CubicOut);
        await Task.WhenAll(f, s, w);
    }

    private async void OnTaskResultTryAgainClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        var f = ModalTaskResult.FadeToAsync(0.0, 120, Easing.CubicIn);
        var s = TaskResultCard.ScaleToAsync(0.88, 120, Easing.CubicIn);
        await Task.WhenAll(f, s);
        ModalTaskResult.IsVisible = false;
        WvTaskResultFinny.Source = null;
    }

    private async void OnTaskResultNextClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        var f = ModalTaskResult.FadeToAsync(0.0, 120, Easing.CubicIn);
        var s = TaskResultCard.ScaleToAsync(0.88, 120, Easing.CubicIn);
        await Task.WhenAll(f, s);
        ModalTaskResult.IsVisible = false;
        WvTaskResultFinny.Source = null;

        _currentTaskIndex = (_currentTaskIndex + 1) % _tasks.Count;
        RenderCurrentTask();
    }

    private async void OnNextTaskClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _currentTaskIndex = (_currentTaskIndex + 1) % _tasks.Count;
        RenderCurrentTask();
    }

    // =========================================================================
    // 5. МОДАЛКА: МАГАЗИН ЗАБОТЫ (ПОКУПКИ ЕДЫ, ИГРУШЕК И МЕБЕЛИ)
    // =========================================================================
    private async void OnShopClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _shopSelectedCategoryTab = 0;
        RenderShopCategoryUI();
        await ShowModal("Магазин заботы о Финни", PanelShop);
    }

    private async void OnShopTabObligClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _shopSelectedCategoryTab = 0;
        RenderShopCategoryUI();
    }

    private async void OnShopTabDiscClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _shopSelectedCategoryTab = 1;
        RenderShopCategoryUI();
    }

    private async void OnShopTabDecorClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _shopSelectedCategoryTab = 2;
        RenderShopCategoryUI();
    }

    private void RenderShopCategoryUI()
    {
        LblShopBalance.Text = $"Доступно монет: {_engine.Profile.Balance} монет";

        BtnShopTabOblig.BackgroundColor = _shopSelectedCategoryTab == 0 ? Color.FromArgb("#520978") : Color.FromArgb("#E5E7EB");
        LblShopTabOblig.TextColor = _shopSelectedCategoryTab == 0 ? Colors.White : Color.FromArgb("#4B5563");

        BtnShopTabDisc.BackgroundColor = _shopSelectedCategoryTab == 1 ? Color.FromArgb("#520978") : Color.FromArgb("#E5E7EB");
        LblShopTabDisc.TextColor = _shopSelectedCategoryTab == 1 ? Colors.White : Color.FromArgb("#4B5563");

        BtnShopTabDecor.BackgroundColor = _shopSelectedCategoryTab == 2 ? Color.FromArgb("#520978") : Color.FromArgb("#E5E7EB");
        LblShopTabDecor.TextColor = _shopSelectedCategoryTab == 2 ? Colors.White : Color.FromArgb("#4B5563");

        ShopItemsContainer.Children.Clear();
        ExpenseCategory category = _shopSelectedCategoryTab switch
        {
            0 => ExpenseCategory.Obligatory,
            1 => ExpenseCategory.Discretionary,
            _ => ExpenseCategory.Interior
        };
        var items = ContentRepository.GetShopItems().Where(i => i.Category == category).ToList();
        var p = _engine.Profile;

        foreach (var item in items)
        {
            bool isUnlocked = (item.LinkedDesk.HasValue && p.IsDeskUnlocked(item.LinkedDesk.Value)) ||
                              (item.LinkedPlatform.HasValue && p.IsPlatformUnlocked(item.LinkedPlatform.Value));
            bool isActive = (item.LinkedDesk.HasValue && p.Desk == item.LinkedDesk.Value) ||
                            (item.LinkedPlatform.HasValue && p.Platform == item.LinkedPlatform.Value);

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#F9FAFB"),
                Stroke = Color.FromArgb("#E5E7EB"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(12, 8),
                InputTransparent = false
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 8
            };

            // Иконка товара (в контрастном контейнере)
            var imgIcon = new Image
            {
                Source = item.IconImage,
                WidthRequest = 32,
                HeightRequest = 32,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            var iconBadge = new Border
            {
                BackgroundColor = item.Category == ExpenseCategory.Obligatory 
                    ? Color.FromArgb("#DCFCE7") 
                    : (item.Category == ExpenseCategory.Interior ? Color.FromArgb("#FEF3C7") : Color.FromArgb("#EDE9FE")),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                WidthRequest = 42,
                HeightRequest = 42,
                VerticalOptions = LayoutOptions.Center,
                Content = imgIcon
            };
            grid.Children.Add(iconBadge);

            // Описание
            var vText = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(vText, 1);
            vText.Children.Add(new Label { Text = item.Name, FontFamily = "MontserratBold", FontSize = 13, TextColor = Color.FromArgb("#1F2937") });
            string effectStr = item.Category == ExpenseCategory.Interior
                ? "Мебель / Интерьер"
                : (item.HungerBoost > 0 ? $"+{item.HungerBoost}% Сытость" : $"+{item.MoodBoost}% Настроение");
            vText.Children.Add(new Label { Text = effectStr, FontFamily = "MontserratMedium", FontSize = 11, TextColor = Color.FromArgb("#059669") });
            grid.Children.Add(vText);

            // Действия
            var actionLayout = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(actionLayout, 2);

            if (isUnlocked)
            {
                var activeBadge = new Border
                {
                    BackgroundColor = isActive ? Color.FromArgb("#10B981") : Color.FromArgb("#6B7280"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(10, 6),
                    VerticalOptions = LayoutOptions.Center,
                    InputTransparent = false
                };
                activeBadge.Content = new Label
                {
                    Text = isActive ? "Активно ✓" : "Выбрать",
                    TextColor = Colors.White,
                    FontFamily = "MontserratBold",
                    FontSize = 11,
                    HorizontalOptions = LayoutOptions.Center
                };
                var tapActive = new TapGestureRecognizer();
                tapActive.Tapped += async (s, e) =>
                {
                    await AnimateTap(activeBadge);
                    if (item.LinkedDesk.HasValue)
                    {
                        p.Desk = item.LinkedDesk.Value;
                        PetView.UpdateDesk(p.Desk);
                    }
                    if (item.LinkedPlatform.HasValue)
                    {
                        p.Platform = item.LinkedPlatform.Value;
                        PetView.UpdatePlatform(p.Platform);
                    }
                    RefreshUI();
                    await _engine.SaveAsync();
                    RenderShopCategoryUI();
                };
                activeBadge.GestureRecognizers.Add(tapActive);
                actionLayout.Children.Add(activeBadge);
            }
            else
            {
                // Кнопка "В цель 🎯"
                var goalBtn = new Border
                {
                    BackgroundColor = Color.FromArgb("#EBE9F8"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(8, 6),
                    InputTransparent = false,
                    VerticalOptions = LayoutOptions.Center
                };
                goalBtn.Content = new Label { Text = "🎯", FontFamily = "MontserratBold", FontSize = 12, HorizontalOptions = LayoutOptions.Center };
                var tapGoal = new TapGestureRecognizer();
                tapGoal.Tapped += async (s, e) =>
                {
                    await AnimateTap(goalBtn);
                    SetItemAsGoal(item);
                };
                goalBtn.GestureRecognizers.Add(tapGoal);
                actionLayout.Children.Add(goalBtn);

                // Кнопка покупки
                bool canAfford = p.Balance >= item.Price;
                var buyBtn = new Border
                {
                    BackgroundColor = canAfford ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                    Padding = new Thickness(10, 6),
                    InputTransparent = false,
                    VerticalOptions = LayoutOptions.Center
                };
                buyBtn.Content = new Label
                {
                    Text = canAfford ? $"{item.Price} м." : $"🔒 {item.Price} м.",
                    TextColor = Colors.White,
                    FontFamily = "MontserratBold",
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center
                };
                var tapBuy = new TapGestureRecognizer();
                tapBuy.Tapped += async (s, e) =>
                {
                    await AnimateTap(buyBtn);
                    await BuyShopItem(item);
                };
                buyBtn.GestureRecognizers.Add(tapBuy);
                actionLayout.Children.Add(buyBtn);
            }

            grid.Children.Add(actionLayout);
            card.Content = grid;
            ShopItemsContainer.Children.Add(card);
        }
    }

    private void PlayPurchaseParticleBurst(string iconSource)
    {
        try
        {
            if (string.IsNullOrEmpty(iconSource))
                iconSource = "ic_stat_balance.png";

            double screenW = Width > 0 ? Width : 360;
            double screenH = Height > 0 ? Height : 640;

            int particleCount = 16;
            var rnd = new Random();
            var tasks = new List<Task>();
            var particles = new List<Image>();

            for (int i = 0; i < particleCount; i++)
            {
                var particle = new Image
                {
                    Source = iconSource,
                    WidthRequest = rnd.Next(32, 48),
                    HeightRequest = rnd.Next(32, 48),
                    Opacity = 1.0,
                    InputTransparent = true,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    TranslationX = 0,
                    TranslationY = 0,
                    Scale = 0.6
                };

                ParticleOverlayGrid.Children.Add(particle);
                particles.Add(particle);

                double angle = rnd.NextDouble() * 2 * Math.PI;
                double distance = rnd.Next(100, 240);
                double targetX = Math.Cos(angle) * distance;
                double targetY = Math.Sin(angle) * distance + rnd.Next(30, 90);
                double targetRot = rnd.Next(-360, 360);

                var moveTask = Task.WhenAll(
                    particle.TranslateToAsync(targetX, targetY, (uint)rnd.Next(650, 950), Easing.CubicOut),
                    particle.RotateToAsync(targetRot, (uint)rnd.Next(650, 950), Easing.CubicOut),
                    particle.ScaleToAsync(rnd.NextDouble() * 0.5 + 0.7, (uint)rnd.Next(650, 950), Easing.CubicOut),
                    Task.Run(async () =>
                    {
                        await Task.Delay(rnd.Next(350, 500));
                        await particle.FadeToAsync(0.0, 350, Easing.CubicIn);
                    })
                );
                tasks.Add(moveTask);
            }

            _ = Task.WhenAll(tasks).ContinueWith(_ =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var p in particles)
                    {
                        ParticleOverlayGrid.Children.Remove(p);
                    }
                });
            });
        }
        catch
        {
            // Игнорируем исключения при прерывании анимации частиц
        }
    }

    private void SetItemAsGoal(ShopItem item)
    {
        var allGoals = GetAllGoals();
        var existing = allGoals.FirstOrDefault(g => 
            (item.LinkedDesk.HasValue && g.LinkedDesk == item.LinkedDesk) ||
            (item.LinkedPlatform.HasValue && g.LinkedPlatform == item.LinkedPlatform) ||
            (!string.IsNullOrEmpty(item.Id) && g.LinkedShopItemId == item.Id));

        if (existing == null)
        {
            existing = new FinancialGoal
            {
                Id = $"goal_shop_{item.Id}",
                Title = item.Name,
                TargetAmount = item.Price,
                IconImage = item.IconImage,
                LinkedDesk = item.LinkedDesk,
                LinkedPlatform = item.LinkedPlatform,
                LinkedShopItemId = item.Id,
                Description = item.Description,
                IsCustom = true
            };
            _engine.Profile.CustomGoals.Add(existing);
        }

        _engine.Profile.SelectedGoalId = existing.Id;
        RefreshUI();
        _ = _engine.SaveAsync();
        RenderGoalsUI();
        AudioService.Instance.PlaySfx("sfx_button");
        PetView.SetSpeechText($"Товар «{existing.Title}» выбран новой целью! Копим {existing.TargetAmount} монет!");
    }

    private async Task ShowPurchaseToastAsync(ShopItem item)
    {
        try
        {
            string effectText = item.HungerBoost > 0 ? $"+{item.HungerBoost}% сытости!" : $"+{item.MoodBoost}% настроения!";
            if (item.Category == ExpenseCategory.Interior) effectText = "Мебель для комнаты!";
            LblPurchaseToast.Text = $"{effectText} (-{item.Price} монет)";
            BorderPurchaseToast.Opacity = 0;
            BorderPurchaseToast.TranslationY = 15;
            BorderPurchaseToast.Scale = 0.8;
            BorderPurchaseToast.IsVisible = true;

            var fadeIn = BorderPurchaseToast.FadeToAsync(1.0, 160, Easing.CubicOut);
            var moveUp = BorderPurchaseToast.TranslateToAsync(0, 0, 180, Easing.CubicOut);
            var scaleUp = BorderPurchaseToast.ScaleToAsync(1.0, 180, Easing.CubicOut);
            await Task.WhenAll(fadeIn, moveUp, scaleUp);

            await Task.Delay(1800);

            var fadeOut = BorderPurchaseToast.FadeToAsync(0.0, 220, Easing.CubicIn);
            var moveUpMore = BorderPurchaseToast.TranslateToAsync(0, -18, 220, Easing.CubicIn);
            await Task.WhenAll(fadeOut, moveUpMore);
            BorderPurchaseToast.IsVisible = false;
        }
        catch
        {
            // Игнорируем исключения при прерывании анимации
        }
    }

    private async Task BuyShopItem(ShopItem item)
    {
        var p = _engine.Profile;
        if (p.Balance < item.Price)
        {
            AudioService.Instance.PlaySfx("sfx_error");
            await ShowStyledAlertAsync(
                "Не хватает монет",
                $"У тебя {p.Balance} монет, а требуется {item.Price} монет.\nПополни баланс за счёт заданий или забери монетки из копилки.",
                "ic_stat_balance.png",
                "Понятно");
            PetView.SetSpeechText("Недостаточно монет! Выполни задание или спланируй бюджет.");
            return;
        }

        p.Balance -= item.Price;
        if (item.Category == ExpenseCategory.Obligatory) p.SpentObligatory += item.Price;
        else p.SpentDiscretionary += item.Price;

        p.Hunger = Math.Min(100, p.Hunger + item.HungerBoost);
        p.Mood = Math.Min(100, p.Mood + item.MoodBoost);

        if (item.LinkedDesk.HasValue)
        {
            p.UnlockDesk(item.LinkedDesk.Value);
            p.Desk = item.LinkedDesk.Value;
            PetView.UpdateDesk(p.Desk);
        }
        if (item.LinkedPlatform.HasValue)
        {
            p.UnlockPlatform(item.LinkedPlatform.Value);
            p.Platform = item.LinkedPlatform.Value;
            PetView.UpdatePlatform(p.Platform);
        }

        RefreshUI();
        await _engine.SaveAsync();
        RenderShopCategoryUI();

        // Запуск анимации рассыпания частиц купленной вещи!
        PlayPurchaseParticleBurst(item.IconImage);

        // Озвучивание покупки и урчания довольного котика
        AudioService.Instance.PlaySfx("sfx_money");
        if (item.HungerBoost > 0 || item.MoodBoost > 15 || item.Category == ExpenseCategory.Interior)
        {
            AudioService.Instance.PlaySfx("sfx_purr");
        }

        // Визуальный бейдж обратной связи над персонажем (ТЗ п. 2.5.6)
        _ = ShowPurchaseToastAsync(item);

        PetView.SetSpeechText(string.IsNullOrEmpty(item.ThanksText) ? "Муррр! Спасибо за заботу! Теперь я доволен!" : item.ThanksText);
        PetView.PlayAction("proud");
    }

    // =========================================================================
    // 6. МОДАЛКА: КОПИЛКА И ФИНАНСОВЫЕ ЦЕЛИ
    // =========================================================================
    private async void OnGoalsClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        RenderGoalsUI();
        await ShowModal("Копилка и цели", PanelGoals);
    }

    private void RenderGoalsUI()
    {
        var p = _engine.Profile;
        var goals = GetAllGoals();
        var currentGoal = goals.FirstOrDefault(g => g.Id == p.SelectedGoalId) ?? goals.First();

        int percent = currentGoal.GetProgressPercent(p.Savings);
        LblModalGoalTitle.Text = currentGoal.Title;
        ImgModalCurrentGoal.Source = currentGoal.IconImage;
        LblModalGoalProgress.Text = $"Накоплено: {p.Savings} / {currentGoal.TargetAmount} монет ({percent}%)";
        BarModalGoal.Progress = percent / 100.0;
        UpdateGoalDepositWithdrawButtons();

        GoalsListContainer.Children.Clear();
        foreach (var goal in goals)
        {
            bool isCurrent = goal.Id == currentGoal.Id;
            int gPercent = goal.GetProgressPercent(p.Savings);
            var goalCard = new Border
            {
                BackgroundColor = isCurrent ? Color.FromArgb("#F3E8FF") : Color.FromArgb("#F9FAFB"),
                Stroke = isCurrent ? Color.FromArgb("#8A83D1") : Color.FromArgb("#E5E7EB"),
                StrokeThickness = 1.5,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(12, 10),
                InputTransparent = false
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10
            };

            var imgGoal = new Image
            {
                Source = goal.IconImage,
                WidthRequest = 34,
                HeightRequest = 34,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Children.Add(imgGoal);

            var info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
            Grid.SetColumn(info, 1);
            info.Children.Add(new Label { Text = goal.Title, FontFamily = "MontserratBold", FontSize = 13, TextColor = Color.FromArgb("#1F2937") });
            info.Children.Add(new Label { Text = $"Стоимость: {goal.TargetAmount} монет  •  {gPercent}%", FontFamily = "MontserratMedium", FontSize = 11, TextColor = Color.FromArgb("#6B7280") });
            grid.Children.Add(info);

            var actionLayout = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(actionLayout, 2);

            bool isAchieved = p.Savings >= goal.TargetAmount;
            if (isAchieved)
            {
                var buyAchievedBtn = new Border
                {
                    BackgroundColor = Color.FromArgb("#10B981"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(10, 5),
                    InputTransparent = false,
                    VerticalOptions = LayoutOptions.Center
                };
                buyAchievedBtn.Content = new Label { Text = "Купить! 🎉", TextColor = Colors.White, FontFamily = "MontserratBold", FontSize = 11 };
                var tapAchieved = new TapGestureRecognizer();
                tapAchieved.Tapped += async (s, e) =>
                {
                    await AnimateTap(buyAchievedBtn);
                    await ClaimGoalRewardAsync(goal);
                };
                buyAchievedBtn.GestureRecognizers.Add(tapAchieved);
                actionLayout.Children.Add(buyAchievedBtn);
            }
            else if (isCurrent)
            {
                var currentBadge = new Border
                {
                    BackgroundColor = Color.FromArgb("#520978"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(10, 5),
                    VerticalOptions = LayoutOptions.Center,
                    Content = new Label { Text = "Активна", FontFamily = "MontserratBold", FontSize = 11, TextColor = Colors.White }
                };
                actionLayout.Children.Add(currentBadge);
            }
            else
            {
                var selectBtn = new Border
                {
                    BackgroundColor = Color.FromArgb("#FF0053"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(10, 5),
                    InputTransparent = false,
                    VerticalOptions = LayoutOptions.Center
                };
                selectBtn.Content = new Label { Text = "Выбрать", TextColor = Colors.White, FontFamily = "MontserratBold", FontSize = 11 };

                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    await AnimateTap(selectBtn);
                    p.SelectedGoalId = goal.Id;
                    RefreshUI();
                    await _engine.SaveAsync();
                    RenderGoalsUI();
                    PetView.SetSpeechText($"Отличный выбор! Наша новая цель — «{goal.Title}»! Копим монетки!");
                };
                selectBtn.GestureRecognizers.Add(tap);
                actionLayout.Children.Add(selectBtn);
            }

            grid.Children.Add(actionLayout);
            goalCard.Content = grid;
            GoalsListContainer.Children.Add(goalCard);
        }
    }

    private async Task ClaimGoalRewardAsync(FinancialGoal goal)
    {
        var p = _engine.Profile;
        if (p.Savings < goal.TargetAmount)
        {
            AudioService.Instance.PlaySfx("sfx_error");
            PetView.SetSpeechText("Сначала нужно накопить всю сумму цели в копилке!");
            return;
        }

        p.Savings -= goal.TargetAmount;

        if (goal.LinkedDesk.HasValue)
        {
            p.UnlockDesk(goal.LinkedDesk.Value);
            p.Desk = goal.LinkedDesk.Value;
            PetView.UpdateDesk(p.Desk);
        }
        if (goal.LinkedPlatform.HasValue)
        {
            p.UnlockPlatform(goal.LinkedPlatform.Value);
            p.Platform = goal.LinkedPlatform.Value;
            PetView.UpdatePlatform(p.Platform);
        }
        if (!string.IsNullOrEmpty(goal.LinkedShopItemId))
        {
            var shopItem = ContentRepository.GetShopItems().FirstOrDefault(i => i.Id == goal.LinkedShopItemId);
            if (shopItem != null)
            {
                p.Hunger = Math.Min(100, p.Hunger + shopItem.HungerBoost);
                p.Mood = Math.Min(100, p.Mood + shopItem.MoodBoost);
            }
        }

        if (goal.IsCustom && p.CustomGoals.Contains(goal))
        {
            p.CustomGoals.Remove(goal);
        }

        var remainingGoals = GetAllGoals();
        p.SelectedGoalId = remainingGoals.FirstOrDefault()?.Id ?? "goal_desk_modern";

        RefreshUI();
        await _engine.SaveAsync();
        RenderGoalsUI();

        PlayPurchaseParticleBurst(goal.IconImage);

        AudioService.Instance.PlaySfx("sfx_fanfare");
        AudioService.Instance.PlaySfx("sfx_purr");
        PetView.SetSpeechText($"УРААА! МЕЧТА ИСПОЛНИЛАСЬ! Мы накопили и купили «{goal.Title}»! Ты настоящий мастер сбережений!");
        PetView.PlayAction("proud");
    }

    private async Task ShowDeskGoalPickerAsync()
    {
        var desks = new[]
        {
            PetDeskType.Modern,
            PetDeskType.Artisan,
            PetDeskType.Market,
            PetDeskType.Maker,
            PetDeskType.Reading,
            PetDeskType.Botanical
        };
        var options = desks.Select(d => $"{GetDeskName(d)} ({GetDeskPrice(d)} монет)").ToArray();
        string? choice = await ShowStyledActionSheetAsync(
            "Цель: Рабочий стол",
            "Выбери рабочий стол для накопления:",
            "ic_customizer.png",
            "Отмена",
            options);
        if (!string.IsNullOrEmpty(choice) && choice != "Отмена")
        {
            int idx = Array.IndexOf(options, choice);
            if (idx >= 0)
            {
                SetDeskAsGoal(desks[idx]);
            }
        }
    }

    private async Task ShowPlatformGoalPickerAsync()
    {
        var platforms = new[]
        {
            PetPlatformType.Stars,
            PetPlatformType.Cloud,
            PetPlatformType.Emerald,
            PetPlatformType.Cosmic
        };
        var options = platforms.Select(p => $"{GetPlatformName(p)} ({GetPlatformPrice(p)} монет)").ToArray();
        string? choice = await ShowStyledActionSheetAsync(
            "Цель: Подиум",
            "Выбери подиум для накопления:",
            "ic_customizer.png",
            "Отмена",
            options);
        if (!string.IsNullOrEmpty(choice) && choice != "Отмена")
        {
            int idx = Array.IndexOf(options, choice);
            if (idx >= 0)
            {
                SetPlatformAsGoal(platforms[idx]);
            }
        }
    }

    private async Task ShowToyGoalPickerAsync()
    {
        var toys = ContentRepository.GetShopItems().Where(i => i.Category == ExpenseCategory.Discretionary).ToList();
        var options = toys.Select(t => $"{t.Name} ({t.Price} монет)").ToArray();
        string? choice = await ShowStyledActionSheetAsync(
            "Цель: Игрушка",
            "Выбери игрушку для накопления:",
            "ic_stat_mood.png",
            "Отмена",
            options);
        if (!string.IsNullOrEmpty(choice) && choice != "Отмена")
        {
            int idx = Array.IndexOf(options, choice);
            if (idx >= 0)
            {
                SetItemAsGoal(toys[idx]);
            }
        }
    }

    private async void OnAddCustomGoalClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        string? choice = await ShowStyledActionSheetAsync(
            "Новая цель накопления 🎯",
            "Как ты хочешь выбрать цель?",
            "ic_stat_savings.png",
            "Отмена",
            "Ввести свою мечту и сумму вручную",
            "Выбрать рабочий столик Финни",
            "Выбрать подиум под лапки",
            "Выбрать игрушку из магазина");

        if (choice == "Ввести свою мечту и сумму вручную")
        {
            string? name = await ShowStyledPromptAsync(
                "Новая цель",
                "На что ты хочешь накопить?",
                "ic_stat_savings.png",
                "Далее",
                "Отмена",
                placeholder: "Например: Роликовые коньки",
                maxLength: 35);
            if (string.IsNullOrWhiteSpace(name)) return;

            string? amountStr = await ShowStyledPromptAsync(
                "Стоимость цели",
                $"Сколько монет стоит «{name.Trim()}»?",
                "ic_stat_balance.png",
                "Создать",
                "Отмена",
                placeholder: "Например: 500",
                keyboard: Keyboard.Numeric,
                maxLength: 6);
            if (int.TryParse(amountStr, out int targetAmount) && targetAmount > 0)
            {
                var newGoal = new FinancialGoal
                {
                    Id = $"goal_custom_{Guid.NewGuid():N}",
                    Title = name.Trim(),
                    TargetAmount = targetAmount,
                    IconImage = "ic_stat_savings.png",
                    Description = "Твоя личная мечта!",
                    IsCustom = true
                };
                _engine.Profile.CustomGoals.Add(newGoal);
                _engine.Profile.SelectedGoalId = newGoal.Id;
                RefreshUI();
                await _engine.SaveAsync();
                RenderGoalsUI();
                PetView.SetSpeechText($"Ура! Мы поставили новую цель: «{name.Trim()}»! Накопим вместе!");
            }
        }
        else if (choice == "Выбрать рабочий столик Финни")
        {
            await ShowDeskGoalPickerAsync();
        }
        else if (choice == "Выбрать подиум под лапки")
        {
            await ShowPlatformGoalPickerAsync();
        }
        else if (choice == "Выбрать игрушку из магазина")
        {
            await ShowToyGoalPickerAsync();
        }
    }

    private void UpdateGoalDepositWithdrawButtons()
    {
        var p = _engine.Profile;

        // Пополнение (+20, +50, +100) из кошелька
        SetDepositButtonState(BtnGoalDeposit20, LblGoalDeposit20, p.Balance >= 20, "+20 монет");
        SetDepositButtonState(BtnGoalDeposit50, LblGoalDeposit50, p.Balance >= 50, "+50 монет");
        SetDepositButtonState(BtnGoalDeposit100, LblGoalDeposit100, p.Balance >= 100, "+100 монет");

        // Снятие (-20, -50, -100) из сбережений
        SetWithdrawButtonState(BtnGoalWithdraw20, LblGoalWithdraw20, p.Savings >= 20, "-20 монет");
        SetWithdrawButtonState(BtnGoalWithdraw50, LblGoalWithdraw50, p.Savings >= 50, "-50 монет");
        SetWithdrawButtonState(BtnGoalWithdraw100, LblGoalWithdraw100, p.Savings >= 100, "-100 монет");
    }

    private void SetDepositButtonState(Border btn, Label lbl, bool canAfford, string text)
    {
        lbl.Text = text;
        if (canAfford)
        {
            btn.BackgroundColor = Color.FromArgb("#DCFCE7");
            btn.Stroke = Color.FromArgb("#10B981");
            lbl.TextColor = Color.FromArgb("#166534");
        }
        else
        {
            btn.BackgroundColor = Color.FromArgb("#FEE2E2");
            btn.Stroke = Color.FromArgb("#EF4444");
            lbl.TextColor = Color.FromArgb("#991B1B");
        }
    }

    private void SetWithdrawButtonState(Border btn, Label lbl, bool canWithdraw, string text)
    {
        lbl.Text = text;
        if (canWithdraw)
        {
            btn.BackgroundColor = Color.FromArgb("#EBE9F8");
            btn.Stroke = Color.FromArgb("#8A83D1");
            lbl.TextColor = Color.FromArgb("#520978");
        }
        else
        {
            btn.BackgroundColor = Color.FromArgb("#FEE2E2");
            btn.Stroke = Color.FromArgb("#EF4444");
            lbl.TextColor = Color.FromArgb("#991B1B");
        }
    }

    private async Task DepositToSavings(int amount)
    {
        var p = _engine.Profile;
        if (p.Balance < amount)
        {
            AudioService.Instance.PlaySfx("sfx_error");
            await ShowStyledAlertAsync(
                "Не хватает монет",
                $"В кошельке только {p.Balance} монет, а нужно {amount} монет для пополнения копилки!",
                "ic_stat_balance.png",
                "Понятно");
            PetView.SetSpeechText($"Не хватает {amount} монет на балансе для пополнения копилки!");
            return;
        }

        p.Balance -= amount;
        p.Savings += amount;
        RefreshUI();
        await _engine.SaveAsync();
        RenderGoalsUI();
        AudioService.Instance.PlaySfx("sfx_money");
        PetView.SetSpeechText($"Звон монетки! +{amount} монет отправлены в копилку!");
        PetView.PlayAction("proud");
    }

    private async void OnDeposit20Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await DepositToSavings(20);
    }
    private async void OnDeposit50Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await DepositToSavings(50);
    }
    private async void OnDeposit100Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await DepositToSavings(100);
    }

    private async Task WithdrawFromSavings(int amount)
    {
        var p = _engine.Profile;
        if (p.Savings < amount)
        {
            AudioService.Instance.PlaySfx("sfx_error");
            await ShowStyledAlertAsync(
                "Не хватает монет",
                $"В копилке только {p.Savings} монет, нельзя снять {amount} монет!",
                "ic_stat_savings.png",
                "Понятно");
            PetView.SetSpeechText($"В копилке только {p.Savings} монет, нельзя снять {amount}!");
            return;
        }

        p.Savings -= amount;
        p.Balance += amount;
        RefreshUI();
        await _engine.SaveAsync();
        RenderGoalsUI();
        AudioService.Instance.PlaySfx("sfx_money");
        PetView.SetSpeechText($"Взяли из копилки {amount} монет в кошелёк. Не забывай пополнять снова!");
        PetView.PlayAction("wave");
    }

    private async void OnWithdraw20Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await WithdrawFromSavings(20);
    }
    private async void OnWithdraw50Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await WithdrawFromSavings(50);
    }
    private async void OnWithdraw100Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await WithdrawFromSavings(100);
    }

    // =========================================================================
    // 6.1 МОДАЛКА: БЫСТРЫЙ ДВУСТОРОННИЙ ПЕРЕВОД (КОШЕЛЁК <-> КОПИЛКА)
    // =========================================================================
    private async void OnTransferMoneyClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _isTransferToSavings = true;
        UpdateTransferUI();
        await ShowModal("Перевод средств", PanelTransfer);
    }

    private async void OnTransferModeSavingsClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _isTransferToSavings = true;
        UpdateTransferUI();
    }

    private async void OnTransferModeWalletClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _isTransferToSavings = false;
        UpdateTransferUI();
    }

    private void UpdateTransferUI()
    {
        var p = _engine.Profile;
        LblTransferWalletVal.Text = $"{p.Balance} монет";
        LblTransferSavingsVal.Text = $"{p.Savings} монет";
        BorderTransferAlert.IsVisible = false;

        if (_isTransferToSavings)
        {
            BtnTransferModeSavings.BackgroundColor = Color.FromArgb("#520978");
            LblTransferModeSavings.TextColor = Colors.White;
            LblTransferModeSavings.FontFamily = "MontserratBold";

            BtnTransferModeWallet.BackgroundColor = Color.FromArgb("#F3F4F6");
            LblTransferModeWallet.TextColor = Color.FromArgb("#4B5563");
            LblTransferModeWallet.FontFamily = "MontserratMedium";

            LblTransferHint.Text = "Пополнение копилки приближает цель и радует Финни!";
            LblTransferHint.TextColor = Color.FromArgb("#520978");
        }
        else
        {
            BtnTransferModeWallet.BackgroundColor = Color.FromArgb("#520978");
            LblTransferModeWallet.TextColor = Colors.White;
            LblTransferModeWallet.FontFamily = "MontserratBold";

            BtnTransferModeSavings.BackgroundColor = Color.FromArgb("#F3F4F6");
            LblTransferModeSavings.TextColor = Color.FromArgb("#4B5563");
            LblTransferModeSavings.FontFamily = "MontserratMedium";

            LblTransferHint.Text = "Снятие из копилки в кошелёк для неотложных трат.";
            LblTransferHint.TextColor = Color.FromArgb("#B91C1C");
        }

        int sourceAvailable = _isTransferToSavings ? p.Balance : p.Savings;
        SetDepositButtonState(BtnTransfer1, LblTransferBtn1, sourceAvailable >= 20, _isTransferToSavings ? "+20 монет" : "-20 монет");
        SetDepositButtonState(BtnTransfer2, LblTransferBtn2, sourceAvailable >= 50, _isTransferToSavings ? "+50 монет" : "-50 монет");
        SetDepositButtonState(BtnTransfer3, LblTransferBtn3, sourceAvailable >= 100, _isTransferToSavings ? "+100 монет" : "-100 монет");
    }

    private async void OnTransferBtn1Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await ExecuteTransfer(20);
    }

    private async void OnTransferBtn2Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await ExecuteTransfer(50);
    }

    private async void OnTransferBtn3Clicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await ExecuteTransfer(100);
    }

    private async Task ExecuteTransfer(int amount)
    {
        var p = _engine.Profile;
        if (_isTransferToSavings)
        {
            if (p.Balance < amount)
            {
                AudioService.Instance.PlaySfx("sfx_error");
                BorderTransferAlert.IsVisible = true;
                LblTransferAlert.Text = $"Недостаточно средств в кошельке! Доступно: {p.Balance} монет.";
                return;
            }
            p.Balance -= amount;
            p.Savings += amount;
            p.ActualSavings += amount;
            BorderTransferAlert.IsVisible = false;
            AudioService.Instance.PlaySfx("sfx_money");
            PetView.SetSpeechText($"Звон монеток! +{amount} монет отправлены в копилку!");
            PetView.PlayAction("proud");
        }
        else
        {
            if (p.Savings < amount)
            {
                AudioService.Instance.PlaySfx("sfx_error");
                BorderTransferAlert.IsVisible = true;
                LblTransferAlert.Text = $"В копилке недостаточно средств! Накоплено: {p.Savings} монет.";
                return;
            }
            p.Savings -= amount;
            p.Balance += amount;
            p.ActualSavings = Math.Max(0, p.ActualSavings - amount);
            BorderTransferAlert.IsVisible = false;
            AudioService.Instance.PlaySfx("sfx_money");
            PetView.SetSpeechText($"Сняли из копилки {amount} монет в кошелёк.");
            PetView.PlayAction("wave");
        }

        RefreshUI();
        await _engine.SaveAsync();
        UpdateTransferUI();
    }

    // =========================================================================
    // 7. МОДАЛКА: СЛОВАРЬ И РОСТ
    // =========================================================================
    private async void OnGlossaryClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        RenderGlossaryUI();
        await ShowModal("Словарь юного финансиста", PanelGlossary);
    }

    private void RenderGlossaryUI()
    {
        GlossaryTermsContainer.Children.Clear();
        var terms = _engine.GetGlossaryForCurrentAge();

        foreach (var t in terms)
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#F9FAFB"),
                Stroke = Color.FromArgb("#E5E7EB"),
                StrokeThickness = 1,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                Padding = new Thickness(12, 8)
            };

            var v = new VerticalStackLayout { Spacing = 2 };
            v.Children.Add(new Label { Text = t.Term, FontFamily = "MontserratBold", FontSize = 13, TextColor = Color.FromArgb("#310F53") });
            v.Children.Add(new Label { Text = t.Definition, FontFamily = "MontserratMedium", FontSize = 11, TextColor = Color.FromArgb("#4B5563"), LineBreakMode = LineBreakMode.WordWrap });
            card.Content = v;

            GlossaryTermsContainer.Children.Add(card);
        }
    }

    // =========================================================================
    // 8. МОДАЛКА: КАБИНЕТ РОДИТЕЛЕЙ (PIN-КОД И АРИФМЕТИЧЕСКАЯ КАПЧА)
    // =========================================================================
    private async void OnParentClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        // Генерируем случайный контрольный пример для резервного входа
        var rnd = new Random();
        _parentMathA = rnd.Next(4, 9);
        _parentMathB = rnd.Next(3, 9);
        LblParentMathQuestion.Text = $"{_parentMathA} × {_parentMathB} = ?";
        EntryParentMathAnswer.Text = "";

        ParentPinGate.IsVisible = true;
        ParentContent.IsVisible = false;

        bool hasPin = !string.IsNullOrWhiteSpace(_engine.Profile.ParentPin);
        ParentPinContainer.IsVisible = hasPin;
        ParentMathContainer.IsVisible = !hasPin;
        _currentPinInput = "";
        UpdatePinDisplay();

        await ShowModal("Кабинет родителей", PanelParent);
    }

    private void UpdatePinDisplay()
    {
        int len = _currentPinInput.Length;
        string[] dots = new string[4];
        for (int i = 0; i < 4; i++)
        {
            dots[i] = i < len ? "●" : "○";
        }
        LblParentPinDisplay.Text = string.Join("   ", dots);
        LblParentPinDisplay.TextColor = Color.FromArgb("#520978");
    }

    private async void OnPinDigitTapped(object? sender, EventArgs e)
    {
        if (sender is Border b) await AnimateTap(b);
        string digit = "";
        if (e is TappedEventArgs tea && tea.Parameter != null)
        {
            digit = tea.Parameter.ToString() ?? "";
        }
        else if (sender is Border b2 && b2.Content is Label l)
        {
            digit = l.Text.Trim();
        }

        if (!string.IsNullOrEmpty(digit) && _currentPinInput.Length < 4)
        {
            _currentPinInput += digit;
            UpdatePinDisplay();
            if (_currentPinInput.Length == 4)
            {
                await ValidatePinAsync();
            }
        }
    }

    private async void OnPinBackspaceTapped(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_currentPinInput.Length > 0)
        {
            _currentPinInput = _currentPinInput[..^1];
            UpdatePinDisplay();
        }
    }

    private async void OnPinSubmitTapped(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await ValidatePinAsync();
    }

    private async Task ValidatePinAsync()
    {
        if (_currentPinInput == _engine.Profile.ParentPin)
        {
            AudioService.Instance.PlaySfx("sfx_success");
            UnlockParentCabinet();
        }
        else
        {
            AudioService.Instance.PlaySfx("sfx_error");
            LblParentPinDisplay.TextColor = Color.FromArgb("#EF4444");
            await Task.Delay(350);
            _currentPinInput = "";
            UpdatePinDisplay();
            PetView.SetSpeechText("Неверный PIN-код! Попробуйте снова или решите контрольный пример.");
        }
    }

    private async void OnParentForgotPinClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        ParentPinContainer.IsVisible = false;
        ParentMathContainer.IsVisible = true;
    }

    private async void OnParentUsePinClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        ParentMathContainer.IsVisible = false;
        ParentPinContainer.IsVisible = true;
        _currentPinInput = "";
        UpdatePinDisplay();
    }

    private async void OnParentChangePinClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        string? result = await ShowStyledPromptAsync(
            "PIN-код родителей",
            "Задайте 4-значный цифровой PIN для входа (или оставьте пустым для входа по арифметическому примеру):",
            "ic_action_parent.png",
            "Сохранить", "Отмена", placeholder: "4 цифры", maxLength: 4, keyboard: Keyboard.Numeric);

        if (result != null)
        {
            result = result.Trim();
            if (result.Length == 4 && int.TryParse(result, out _))
            {
                _engine.Profile.ParentPin = result;
                await _engine.SaveAsync();
                PetView.SetSpeechText("Новый 4-значный PIN-код родителей успешно установлен!");
            }
            else if (string.IsNullOrEmpty(result))
            {
                _engine.Profile.ParentPin = string.Empty;
                await _engine.SaveAsync();
                PetView.SetSpeechText("PIN-код снят. Доступ теперь через арифметический пример.");
            }
            else
            {
                PetView.SetSpeechText("PIN-код должен состоять ровно из 4 цифр!");
            }
        }
    }

    private async void OnParentUnlockClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        if (int.TryParse(EntryParentMathAnswer.Text, out int ans) && ans == _parentMathA * _parentMathB)
        {
            AudioService.Instance.PlaySfx("sfx_success");
            UnlockParentCabinet();
        }
        else
        {
            AudioService.Instance.PlaySfx("sfx_error");
            PetView.SetSpeechText("Неверный ответ! Вход только для родителей.");
        }
    }

    private void UnlockParentCabinet()
    {
        ParentPinGate.IsVisible = false;
        ParentContent.IsVisible = true;

        var p = _engine.Profile;
        SwitchParentDemoMode.IsToggled = p.IsDemoMode;
        LblParentStats.Text = $"Ребенок: {p.KidName}\n" +
            $"Периодов сыграно: {p.CurrentPeriod}\n" +
            $"Накоплено в копилке: {p.Savings} монет\n" +
            $"Заданий выполнено: {p.CompletedTasksCount}\n" +
            $"Текущий баланс: {p.Balance} монет";

        UpdateParentStageButtons();
    }

    private void UpdateParentStageButtons()
    {
        if (_engine == null) return;
        var stage = _engine.Profile.Stage;
        if (BtnParentStageBaby != null)
        {
            BtnParentStageBaby.BackgroundColor = stage == GrowthStage.Baby ? Color.FromArgb("#520978") : Color.FromArgb("#EBE9F8");
            LblParentStageBaby.TextColor = stage == GrowthStage.Baby ? Colors.White : Color.FromArgb("#520978");
            BtnParentStageTeen.BackgroundColor = stage == GrowthStage.Teen ? Color.FromArgb("#520978") : Color.FromArgb("#EBE9F8");
            LblParentStageTeen.TextColor = stage == GrowthStage.Teen ? Colors.White : Color.FromArgb("#520978");
            BtnParentStageMaster.BackgroundColor = stage == GrowthStage.Master ? Color.FromArgb("#520978") : Color.FromArgb("#EBE9F8");
            LblParentStageMaster.TextColor = stage == GrowthStage.Master ? Colors.White : Color.FromArgb("#520978");
        }
    }

    private async void OnParentStageBabyClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _engine.Profile.Stage = GrowthStage.Baby;
        RefreshUI();
        await _engine.SaveAsync();
        UpdateParentStageButtons();
        PetView.SetSpeechText("Установлена стадия: Финни-Малыш (1–2 период)!");
    }

    private async void OnParentStageTeenClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _engine.Profile.Stage = GrowthStage.Teen;
        RefreshUI();
        await _engine.SaveAsync();
        UpdateParentStageButtons();
        PetView.SetSpeechText("Установлена стадия: Финни-Юниор (3–4 период)!");
    }

    private async void OnParentStageMasterClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _engine.Profile.Stage = GrowthStage.Master;
        RefreshUI();
        await _engine.SaveAsync();
        UpdateParentStageButtons();
        PetView.SetSpeechText("Установлена стадия: Финни-Мастер (5+ периодов)!");
    }

    private async void OnParentDemoModeToggled(object? sender, ToggledEventArgs e)
    {
        if (_engine == null) return;
        _engine.Profile.IsDemoMode = e.Value;
        BadgeDemo.IsVisible = e.Value;
        CardDemoNextPeriod.IsVisible = e.Value;
        await _engine.SaveAsync();
        string status = e.Value ? "включен" : "выключен";
        PetView.SetSpeechText($"Демо-режим {status}!");
    }

    private async void OnParentGiveBonusClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Balance += 100;
        RefreshUI();
        await _engine.SaveAsync();
        AudioService.Instance.PlaySfx("sfx_money");
        PetView.SetSpeechText("Родители выдали карманные деньги: +100 монет!");
        await CloseModal();
    }

    private async void OnParentResetDataClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        bool confirm = await ShowStyledConfirmAsync(
            "Сброс данных приложения",
            "Вы действительно хотите сбросить все данные приложения к начальному состоянию? Весь накопленный прогресс будет удалён.",
            "ic_action_parent.png",
            "Сбросить", "Отмена",
            isDestructive: true);
        if (!confirm) return;
        _engine.ResetData();
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Данные сброшены! Начинаем финансовый путь заново!");
        await CloseModal();
    }

    // =========================================================================
    // 8.5. УПРАВЛЕНИЕ ЗВУКОМ И МУЗЫКОЙ
    // =========================================================================
    private async void OnAudioQuickClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        SyncAudioSwitches();
        await ShowModal("Звук и музыка", PanelAudioSettings);
    }

    private void OnSfxToggled(object? sender, ToggledEventArgs e)
    {
        AudioService.Instance.IsSfxEnabled = e.Value;
        SyncAudioSwitches();
        if (e.Value)
        {
            AudioService.Instance.PlaySfx("sfx_money");
        }
    }

    private void OnMusicToggled(object? sender, ToggledEventArgs e)
    {
        AudioService.Instance.IsMusicEnabled = e.Value;
        SyncAudioSwitches();
    }

    private void SyncAudioSwitches()
    {
        bool sfx = AudioService.Instance.IsSfxEnabled;
        bool music = AudioService.Instance.IsMusicEnabled;

        if (SwitchQuickSfx != null) SwitchQuickSfx.IsToggled = sfx;
        if (SwitchParentSfx != null) SwitchParentSfx.IsToggled = sfx;
        if (SwitchQuickMusic != null) SwitchQuickMusic.IsToggled = music;
        if (SwitchParentMusic != null) SwitchParentMusic.IsToggled = music;

        if (ImgIconSfx != null) ImgIconSfx.Source = sfx ? "ic_sound_on.png" : "ic_sound_off.png";
        if (ImgIconMusic != null) ImgIconMusic.Source = music ? "ic_music_on.png" : "ic_music_off.png";
        if (ImgAudioQuick != null) ImgAudioQuick.Source = (sfx || music) ? "ic_sound_on.png" : "ic_sound_off.png";
    }

    // =========================================================================
    // 9. ИСТОРИЯ И АНАЛИТИКА ПЕРИОДОВ (ТЗ п. 2.5.11)
    // =========================================================================
    private async void OnAnalyticsClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        _historyChart.History = _engine.Profile.History;
        GvHistoryChart.Invalidate();

        StackHistoryList.Children.Clear();
        if (_engine.Profile.History == null || _engine.Profile.History.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "Пока нет завершённых периодов.\nПерейдите к следующему периоду в демо-режиме, чтобы увидеть историю трат и накоплений!",
                FontFamily = "MontserratMedium",
                FontSize = 12,
                TextColor = Color.FromArgb("#6B7280"),
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(10, 14)
            };
            StackHistoryList.Children.Add(emptyLabel);
        }
        else
        {
            foreach (var item in _engine.Profile.History.OrderByDescending(h => h.PeriodNumber))
            {
                var card = new Border
                {
                    BackgroundColor = Color.FromArgb("#F9FAFB"),
                    Stroke = Color.FromArgb("#E5E7EB"),
                    StrokeThickness = 1,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
                    Padding = new Thickness(12, 8)
                };

                var vStack = new VerticalStackLayout { Spacing = 3 };

                var headerGrid = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    }
                };

                headerGrid.Children.Add(new Label
                {
                    Text = $"Период #{item.PeriodNumber}",
                    FontFamily = "MontserratBold",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#520978"),
                    VerticalOptions = LayoutOptions.Center
                });

                var badge = new Border
                {
                    BackgroundColor = item.IsBudgetSuccess ? Color.FromArgb("#DCFCE7") : Color.FromArgb("#FEE2E2"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                    Padding = new Thickness(8, 2),
                    VerticalOptions = LayoutOptions.Center
                };
                Grid.SetColumn(badge, 1);
                badge.Content = new Label
                {
                    Text = item.IsBudgetSuccess ? "Бюджет соблюдён" : "Превышение",
                    FontFamily = "MontserratBold",
                    FontSize = 10,
                    TextColor = item.IsBudgetSuccess ? Color.FromArgb("#166534") : Color.FromArgb("#991B1B")
                };
                headerGrid.Children.Add(badge);
                vStack.Children.Add(headerGrid);

                string details = $"Обязательные: {item.ActualObligatory}/{item.PlannedObligatory} м.  •  Желания: {item.ActualDiscretionary}/{item.PlannedDiscretionary} м.";
                vStack.Children.Add(new Label { Text = details, FontFamily = "MontserratMedium", FontSize = 11, TextColor = Color.FromArgb("#4B5563") });

                int endSav = item.EndPeriodSavings > 0 ? item.EndPeriodSavings : item.ActualSavings;
                string savDetails = $"Копилка: {endSav} м. (+{item.ActualSavings} за период)";
                if (item.InterestEarned > 0) savDetails += $"  •  Сложный процент (+5%): +{item.InterestEarned} м.";
                vStack.Children.Add(new Label { Text = savDetails, FontFamily = "MontserratBold", FontSize = 11, TextColor = Color.FromArgb("#D97706") });

                card.Content = vStack;
                StackHistoryList.Children.Add(card);
            }
        }

        await ShowModal("История и динамика периодов", PanelAnalytics);
    }

    // =========================================================================
    // 10. ИНТЕРАКТИВНЫЙ ОНБОРДИНГ (ТЗ п. 2.5.1)
    // =========================================================================
    private void ShowOnboarding()
    {
        _onboardingStep = 0;
        RenderOnboardingSlide();
        ModalOnboarding.Opacity = 0;
        ModalOnboarding.IsVisible = true;
        _ = ModalOnboarding.FadeToAsync(1.0, 160, Easing.CubicOut);
    }

    private void RenderOnboardingSlide()
    {
        var slide = _onboardingSlides[_onboardingStep];
        LblOnboardingStepBadge.Text = $"{_onboardingStep + 1} из {_onboardingSlides.Length}";
        LblOnboardingSlideTitle.Text = slide.Title;
        LblOnboardingSlideDesc.Text = slide.Desc;
        ImgOnboardingSlide.Source = slide.Icon;
        BorderOnboardingIconBg.BackgroundColor = Color.FromArgb(slide.IconBg);

        Color activeDot = Color.FromArgb("#520978");
        Color inactiveDot = Color.FromArgb("#E5E7EB");
        DotStep1.BackgroundColor = _onboardingStep == 0 ? activeDot : inactiveDot;
        DotStep2.BackgroundColor = _onboardingStep == 1 ? activeDot : inactiveDot;
        DotStep3.BackgroundColor = _onboardingStep == 2 ? activeDot : inactiveDot;
        DotStep4.BackgroundColor = _onboardingStep == 3 ? activeDot : inactiveDot;

        BtnOnboardingBack.IsVisible = _onboardingStep > 0;
        LblOnboardingNext.Text = _onboardingStep == _onboardingSlides.Length - 1 ? "Начать играть ➜" : "Далее ➜";
    }

    private async void OnOnboardingNextClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_onboardingStep < _onboardingSlides.Length - 1)
        {
            _onboardingStep++;
            RenderOnboardingSlide();
        }
        else
        {
            await FinishOnboardingAsync();
        }
    }

    private async void OnOnboardingBackClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        if (_onboardingStep > 0)
        {
            _onboardingStep--;
            RenderOnboardingSlide();
        }
    }

    private async void OnOnboardingSkipClicked(object? sender, EventArgs e)
    {
        await FinishOnboardingAsync();
    }

    private async Task FinishOnboardingAsync()
    {
        _engine.Profile.IsOnboardingCompleted = true;
        await _engine.SaveAsync();
        await ModalOnboarding.FadeToAsync(0.0, 120, Easing.CubicIn);
        ModalOnboarding.IsVisible = false;
        PetView.SetSpeechText("Добро пожаловать в FinAPP! Давай спланируем бюджет или решим задачку!");
        PetView.PlayAction("proud");
    }

    private async void OnOnboardingHeaderClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        ShowOnboarding();
    }

    private async void OnParentReplayOnboardingClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement v) await AnimateTap(v);
        await CloseModal();
        ShowOnboarding();
    }

    // =========================================================================
    // 11. ДЕМО: ЗАВЕРШЕНИЕ ПЕРИОДА И ПЕРЕХОД К СЛЕДУЮЩЕМУ (Шаг 10 ТЗ)
    // =========================================================================
    private async void OnNextPeriodClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);

        string advanceMsg = _engine.AdvanceToNextPeriod();
        RefreshUI();
        await _engine.SaveAsync();

        PetView.SetSpeechText(advanceMsg);
        PetView.PlayAction("proud");
    }
}
