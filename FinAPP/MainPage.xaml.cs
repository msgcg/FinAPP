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

    // Активная задача
    private int _currentTaskIndex = 0;
    private List<FinancialTask> _tasks = new();

    // Категория магазина (true = Obligatory, false = Discretionary)
    private bool _isShopObligCategory = true;

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
        PetView.NextQuote();
        PetView.PlayAction("wave");
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
        await view.ScaleToAsync(0.93, 60, Easing.CubicOut);
        await view.ScaleToAsync(1.0, 60, Easing.CubicIn);
    }

    private async Task ShowModal(string title, VisualElement activePanel)
    {
        LblModalHeader.Text = title;

        PanelBudget.IsVisible = false;
        PanelTasks.IsVisible = false;
        PanelShop.IsVisible = false;
        PanelGoals.IsVisible = false;
        PanelGlossary.IsVisible = false;
        PanelParent.IsVisible = false;
        PanelCustomizer.IsVisible = false;
        PanelTransfer.IsVisible = false;

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
    }

    private async void OnCloseModalClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await CloseModal();
    }

    // =========================================================================
    // 2. МОДАЛКА: КАСТОМИЗАЦИЯ ПЛАТФОРМ И ИМЯ
    // =========================================================================
    private async void OnCustomizerClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        EntryPetName.Text = _engine.Profile.PetName;
        UpdateCustomizerPlatformBadges(_engine.Profile.Platform);
        await ShowModal("Платформа и имя Финни", PanelCustomizer);
    }

    private void UpdateCustomizerPlatformBadges(PetPlatformType platform)
    {
        BadgePlatformFlowers.IsVisible = platform == PetPlatformType.Flowers;
        BadgePlatformStars.IsVisible = platform == PetPlatformType.Stars;
        BadgePlatformEmerald.IsVisible = platform == PetPlatformType.Emerald;
        BadgePlatformCosmic.IsVisible = platform == PetPlatformType.Cosmic;
        BadgePlatformCloud.IsVisible = platform == PetPlatformType.Cloud;
    }

    private async Task SelectPlatformAsync(PetPlatformType platform, string speech)
    {
        _engine.Profile.Platform = platform;
        UpdateCustomizerPlatformBadges(platform);
        PetView.UpdatePlatform(platform);
        PetView.PlayAction("proud");
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText(speech);
    }

    private async void OnPlatformFlowersClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Flowers, "Цветочная полянка! Лапкам тепло и пахнет весенней свежестью!");
    }

    private async void OnPlatformStarsClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Stars, "Звёздная дорожка! Настоящий золотой пьедестал финансового успеха!");
    }

    private async void OnPlatformEmeraldClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        await SelectPlatformAsync(PetPlatformType.Emerald, "Изумрудный кристалл! Неоновая энергия накоплений заряжает копилку!");
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
        UpdateBudgetModalLabels();
        await ShowModal($"Бюджет периода #{p.CurrentPeriod}", PanelBudget);
    }

    private void UpdateBudgetModalLabels()
    {
        LblBudgetIncome.Text = $"Доход: {PeriodIncome} монет";
        LblBudgetObligVal.Text = $"{_tempOblig} монет";
        LblBudgetDiscVal.Text = $"{_tempDisc} монет";
        LblBudgetSavVal.Text = $"{_tempSav} монет";

        int sum = _tempOblig + _tempDisc + _tempSav;
        int diff = PeriodIncome - sum;
        if (diff == 0)
        {
            LblBudgetRemaining.Text = "Распределено 100%";
            LblBudgetRemaining.TextColor = Color.FromArgb("#10B981");
        }
        else if (diff > 0)
        {
            LblBudgetRemaining.Text = $"Осталось: {diff} монет";
            LblBudgetRemaining.TextColor = Color.FromArgb("#FF0053");
        }
        else
        {
            LblBudgetRemaining.Text = $"Перерасход: {Math.Abs(diff)} монет";
            LblBudgetRemaining.TextColor = Color.FromArgb("#EF4444");
        }
    }

    private void OnBudgetObligPlus(object? sender, EventArgs e)
    {
        if (_tempOblig + 10 <= PeriodIncome) { _tempOblig += 10; UpdateBudgetModalLabels(); }
    }
    private void OnBudgetObligMinus(object? sender, EventArgs e)
    {
        if (_tempOblig - 10 >= 0) { _tempOblig -= 10; UpdateBudgetModalLabels(); }
    }

    private void OnBudgetDiscPlus(object? sender, EventArgs e)
    {
        if (_tempDisc + 10 <= PeriodIncome) { _tempDisc += 10; UpdateBudgetModalLabels(); }
    }
    private void OnBudgetDiscMinus(object? sender, EventArgs e)
    {
        if (_tempDisc - 10 >= 0) { _tempDisc -= 10; UpdateBudgetModalLabels(); }
    }

    private void OnBudgetSavPlus(object? sender, EventArgs e)
    {
        if (_tempSav + 10 <= PeriodIncome) { _tempSav += 10; UpdateBudgetModalLabels(); }
    }
    private void OnBudgetSavMinus(object? sender, EventArgs e)
    {
        if (_tempSav - 10 >= 0) { _tempSav -= 10; UpdateBudgetModalLabels(); }
    }

    private async void OnSaveBudgetPlanClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        int total = _tempOblig + _tempDisc + _tempSav;
        if (total > PeriodIncome)
        {
            PetView.SetSpeechText("Мяу! План превышает доход! Уменьши одну из категорий.");
            return;
        }

        var p = _engine.Profile;
        p.PlannedObligatory = _tempOblig;
        p.PlannedDiscretionary = _tempDisc;
        p.PlannedSavings = _tempSav;
        p.IsPlanConfirmed = true;

        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Отличный план! Теперь совершай покупки согласно конвертам.");
        await CloseModal();
    }

    private void OnShowPlanFactClicked(object? sender, EventArgs e)
    {
        var p = _engine.Profile;
        string report = $"Сравнение План vs Факт:\n\n" +
            $"Обязательные: План {_tempOblig} монет | Факт {p.SpentObligatory} монет\n" +
            $"Желания: План {_tempDisc} монет | Факт {p.SpentDiscretionary} монет\n" +
            $"В копилку: План {_tempSav} монет | Накоплено {p.Savings} монет";
        PetView.SetSpeechText(report);
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
            BorderTaskResultExplanation.BackgroundColor = Color.FromArgb("#F0FDF4");
            BorderTaskResultExplanation.Stroke = Color.FromArgb("#86EFAC");

            BtnTaskResultTryAgain.IsVisible = false;
            Grid.SetColumn(BtnTaskResultNext, 0);
            Grid.SetColumnSpan(BtnTaskResultNext, 2);
            LblTaskResultNext.Text = "Следующее задание ➜";
            BtnTaskResultNext.BackgroundColor = Color.FromArgb("#10B981");

            // Анимация гордого Финни
            string gif = $"{stagePrefix}_proud.gif";
            string html = await FinnyPetView.GetOrLoadHtmlAsync(gif);
            var stampedHtml = html.Replace("</html>", $"<!-- {DateTime.UtcNow.Ticks} --></html>");
            WvTaskResultFinny.Source = new HtmlWebViewSource { Html = stampedHtml };
        }
        else
        {
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
            BorderTaskResultExplanation.BackgroundColor = Color.FromArgb("#FFF1F2");
            BorderTaskResultExplanation.Stroke = Color.FromArgb("#FECDD3");

            BtnTaskResultTryAgain.IsVisible = true;
            Grid.SetColumn(BtnTaskResultNext, 1);
            Grid.SetColumnSpan(BtnTaskResultNext, 1);
            LblTaskResultNext.Text = "Дальше ➜";
            BtnTaskResultNext.BackgroundColor = Color.FromArgb("#6B7280");

            // Анимация расстроенного Финни (подлинный плачущий котик со слезой)
            string gif = $"{stagePrefix}_sad.gif";
            string html = await FinnyPetView.GetOrLoadHtmlAsync(gif);
            var stampedHtml = html.Replace("</html>", $"<!-- {DateTime.UtcNow.Ticks} --></html>");
            WvTaskResultFinny.Source = new HtmlWebViewSource { Html = stampedHtml };
        }

        ModalTaskResult.Opacity = 0;
        ModalTaskResult.IsVisible = true;
        TaskResultCard.Scale = 0.88;

        var f = ModalTaskResult.FadeToAsync(1.0, 160, Easing.CubicOut);
        var s = TaskResultCard.ScaleToAsync(1.0, 160, Easing.CubicOut);
        await Task.WhenAll(f, s);
    }

    private async void OnTaskResultTryAgainClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        var f = ModalTaskResult.FadeToAsync(0.0, 120, Easing.CubicIn);
        var s = TaskResultCard.ScaleToAsync(0.88, 120, Easing.CubicIn);
        await Task.WhenAll(f, s);
        ModalTaskResult.IsVisible = false;
    }

    private async void OnTaskResultNextClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        var f = ModalTaskResult.FadeToAsync(0.0, 120, Easing.CubicIn);
        var s = TaskResultCard.ScaleToAsync(0.88, 120, Easing.CubicIn);
        await Task.WhenAll(f, s);
        ModalTaskResult.IsVisible = false;

        _currentTaskIndex = (_currentTaskIndex + 1) % _tasks.Count;
        RenderCurrentTask();
    }

    private void OnNextTaskClicked(object? sender, EventArgs e)
    {
        _currentTaskIndex = (_currentTaskIndex + 1) % _tasks.Count;
        RenderCurrentTask();
    }

    // =========================================================================
    // 5. МОДАЛКА: МАГАЗИН ЗАБОТЫ (ПОКУПКИ ЕДЫ И ИГРУШЕК)
    // =========================================================================
    private async void OnShopClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _isShopObligCategory = true;
        RenderShopCategoryUI();
        await ShowModal("Магазин заботы о Финни", PanelShop);
    }

    private void OnShopTabObligClicked(object? sender, EventArgs e)
    {
        _isShopObligCategory = true;
        RenderShopCategoryUI();
    }

    private void OnShopTabDiscClicked(object? sender, EventArgs e)
    {
        _isShopObligCategory = false;
        RenderShopCategoryUI();
    }

    private void RenderShopCategoryUI()
    {
        LblShopBalance.Text = $"Доступно монет: {_engine.Profile.Balance} монет";

        if (_isShopObligCategory)
        {
            BtnShopTabOblig.BackgroundColor = Color.FromArgb("#520978");
            LblShopTabOblig.TextColor = Colors.White;
            BtnShopTabDisc.BackgroundColor = Color.FromArgb("#E5E7EB");
            LblShopTabDisc.TextColor = Color.FromArgb("#4B5563");
        }
        else
        {
            BtnShopTabDisc.BackgroundColor = Color.FromArgb("#520978");
            LblShopTabDisc.TextColor = Colors.White;
            BtnShopTabOblig.BackgroundColor = Color.FromArgb("#E5E7EB");
            LblShopTabOblig.TextColor = Color.FromArgb("#4B5563");
        }

        ShopItemsContainer.Children.Clear();
        var category = _isShopObligCategory ? ExpenseCategory.Obligatory : ExpenseCategory.Discretionary;
        var items = ContentRepository.GetShopItems().Where(i => i.Category == category).ToList();

        foreach (var item in items)
        {
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
                ColumnSpacing = 10
            };

            // Иконка товара из презентации (никаких эмоджи!)
            var imgIcon = new Image
            {
                Source = item.IconImage,
                WidthRequest = 36,
                HeightRequest = 36,
                VerticalOptions = LayoutOptions.Center
            };
            grid.Children.Add(imgIcon);

            // Описание
            var vText = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(vText, 1);
            vText.Children.Add(new Label { Text = item.Name, FontFamily = "MontserratBold", FontSize = 13, TextColor = Color.FromArgb("#1F2937") });
            string effectStr = item.HungerBoost > 0 ? $"+{item.HungerBoost}% Сытость" : $"+{item.MoodBoost}% Настроение";
            vText.Children.Add(new Label { Text = effectStr, FontFamily = "MontserratMedium", FontSize = 11, TextColor = Color.FromArgb("#059669") });
            grid.Children.Add(vText);

            // Кнопка покупки
            var buyBtn = new Border
            {
                BackgroundColor = Color.FromArgb("#10B981"),
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
                Padding = new Thickness(12, 6),
                InputTransparent = false,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(buyBtn, 2);
            buyBtn.Content = new Label { Text = $"{item.Price} монет", TextColor = Colors.White, FontFamily = "MontserratBold", FontSize = 12, HorizontalOptions = LayoutOptions.Center };

            var tap = new TapGestureRecognizer();
            tap.Tapped += async (s, e) =>
            {
                await AnimateTap(buyBtn);
                await BuyShopItem(item);
            };
            buyBtn.GestureRecognizers.Add(tap);
            grid.Children.Add(buyBtn);

            card.Content = grid;
            ShopItemsContainer.Children.Add(card);
        }
    }

    private async Task BuyShopItem(ShopItem item)
    {
        var p = _engine.Profile;
        if (p.Balance < item.Price)
        {
            PetView.SetSpeechText("Недостаточно монет! Выполни задание или спланируй бюджет.");
            return;
        }

        p.Balance -= item.Price;
        if (item.Category == ExpenseCategory.Obligatory) p.SpentObligatory += item.Price;
        else p.SpentDiscretionary += item.Price;

        p.Hunger = Math.Min(100, p.Hunger + item.HungerBoost);
        p.Mood = Math.Min(100, p.Mood + item.MoodBoost);

        RefreshUI();
        await _engine.SaveAsync();
        RenderShopCategoryUI();
        // Грамотная благодарность на русском языке в винительном падеже
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

            if (isCurrent)
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
                Grid.SetColumn(currentBadge, 2);
                grid.Children.Add(currentBadge);
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
                Grid.SetColumn(selectBtn, 2);
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
                grid.Children.Add(selectBtn);
            }

            goalCard.Content = grid;
            GoalsListContainer.Children.Add(goalCard);
        }
    }

    private async void OnAddCustomGoalClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        string name = await DisplayPromptAsync("Новая цель", "На что ты хочешь накопить?", "Далее", "Отмена", "Например: Роликовые коньки", maxLength: 35);
        if (string.IsNullOrWhiteSpace(name)) return;

        string amountStr = await DisplayPromptAsync("Стоимость цели", $"Сколько монет стоит «{name.Trim()}»?", "Создать", "Отмена", "Например: 500", keyboard: Keyboard.Numeric);
        if (int.TryParse(amountStr, out int targetAmount) && targetAmount > 0)
        {
            var newGoal = new FinancialGoal
            {
                Id = $"goal_custom_{Guid.NewGuid():N}",
                Title = name.Trim(),
                TargetAmount = targetAmount,
                IconImage = "ic_goal_custom.png",
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

    private async Task DepositToSavings(int amount)
    {
        var p = _engine.Profile;
        if (p.Balance < amount)
        {
            PetView.SetSpeechText($"Не хватает {amount} монет на балансе для пополнения копилки!");
            return;
        }

        p.Balance -= amount;
        p.Savings += amount;
        RefreshUI();
        await _engine.SaveAsync();
        RenderGoalsUI();
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
            PetView.SetSpeechText($"В копилке только {p.Savings} монет, нельзя снять {amount}!");
            return;
        }

        p.Savings -= amount;
        p.Balance += amount;
        RefreshUI();
        await _engine.SaveAsync();
        RenderGoalsUI();
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

    private void OnTransferModeSavingsClicked(object? sender, EventArgs e)
    {
        _isTransferToSavings = true;
        UpdateTransferUI();
    }

    private void OnTransferModeWalletClicked(object? sender, EventArgs e)
    {
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

            BtnTransferModeWallet.BackgroundColor = Colors.Transparent;
            LblTransferModeWallet.TextColor = Color.FromArgb("#6B7280");
            LblTransferModeWallet.FontFamily = "MontserratMedium";

            LblTransferHint.Text = "Пополнение копилки приближает цель и радует Финни!";
            LblTransferHint.TextColor = Color.FromArgb("#520978");

            LblTransferBtn1.Text = "+20 монет";
            LblTransferBtn2.Text = "+50 монет";
            LblTransferBtn3.Text = "+100 монет";
        }
        else
        {
            BtnTransferModeWallet.BackgroundColor = Color.FromArgb("#520978");
            LblTransferModeWallet.TextColor = Colors.White;
            LblTransferModeWallet.FontFamily = "MontserratBold";

            BtnTransferModeSavings.BackgroundColor = Colors.Transparent;
            LblTransferModeSavings.TextColor = Color.FromArgb("#6B7280");
            LblTransferModeSavings.FontFamily = "MontserratMedium";

            LblTransferHint.Text = "Снятие из копилки в кошелёк для неотложных трат.";
            LblTransferHint.TextColor = Color.FromArgb("#B91C1C");

            LblTransferBtn1.Text = "-20 монет";
            LblTransferBtn2.Text = "-50 монет";
            LblTransferBtn3.Text = "-100 монет";
        }
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
                BorderTransferAlert.IsVisible = true;
                LblTransferAlert.Text = $"Недостаточно средств в кошельке! Доступно: {p.Balance} монет.";
                return;
            }
            p.Balance -= amount;
            p.Savings += amount;
            BorderTransferAlert.IsVisible = false;
            PetView.SetSpeechText($"Звон монеток! +{amount} монет отправлены в копилку!");
            PetView.PlayAction("proud");
        }
        else
        {
            if (p.Savings < amount)
            {
                BorderTransferAlert.IsVisible = true;
                LblTransferAlert.Text = $"В копилке недостаточно средств! Накоплено: {p.Savings} монет.";
                return;
            }
            p.Savings -= amount;
            p.Balance += amount;
            BorderTransferAlert.IsVisible = false;
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
    // 8. МОДАЛКА: КАБИНЕТ РОДИТЕЛЕЙ
    // =========================================================================
    private async void OnParentClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        // Генерируем случайный пример
        var rnd = new Random();
        _parentMathA = rnd.Next(4, 9);
        _parentMathB = rnd.Next(3, 9);
        LblParentMathQuestion.Text = $"{_parentMathA} × {_parentMathB} = ?";
        EntryParentMathAnswer.Text = "";
        ParentPinGate.IsVisible = true;
        ParentContent.IsVisible = false;

        await ShowModal("Кабинет родителей", PanelParent);
    }

    private async void OnParentUnlockClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        if (int.TryParse(EntryParentMathAnswer.Text, out int ans) && ans == _parentMathA * _parentMathB)
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
        else
        {
            PetView.SetSpeechText("Неверный ответ! Вход только для родителей.");
        }
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
        PetView.SetSpeechText("Родители выдали карманные деньги: +100 монет!");
        await CloseModal();
    }

    private async void OnParentResetDataClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.ResetData();
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Данные сброшены! Начинаем финансовый путь заново!");
        await CloseModal();
    }

    // =========================================================================
    // 9. ДЕМО: ЗАВЕРШЕНИЕ ПЕРИОДА И ПЕРЕХОД К СЛЕДУЮЩЕМУ (Шаг 10 ТЗ)
    // =========================================================================
    private async void OnNextPeriodClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);

        var p = _engine.Profile;
        string comparison = _engine.GetPlanVsFactAnalysis();

        p.CurrentPeriod++;
        p.Balance += PeriodIncome;
        p.SpentObligatory = 0;
        p.SpentDiscretionary = 0;
        p.IsPlanConfirmed = false;

        if (p.CurrentPeriod >= 5) p.Stage = GrowthStage.Master;
        else if (p.CurrentPeriod >= 3) p.Stage = GrowthStage.Teen;

        RefreshUI();
        await _engine.SaveAsync();

        PetView.SetSpeechText($"Период #{p.CurrentPeriod} начался! Начислен доход +{PeriodIncome} монет!\n{comparison}");
    }
}
