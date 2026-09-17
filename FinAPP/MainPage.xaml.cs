using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinAPP.Models;
using FinAPP.Services;
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

    public MainPage()
    {
        InitializeComponent();

        _storageService = new StorageService();
        _engine = new GameEngine(_storageService);
        _engine.OnStateChanged += () => MainThread.BeginInvokeOnMainThread(RefreshUI);

        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, EventArgs e)
    {
        await _engine.InitializeAsync();
        _tasks = ContentRepository.GetFinancialTasks();
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

    private void RefreshUI()
    {
        var p = _engine.Profile;
        var currentGoal = ContentRepository.GetPresetGoals()
            .FirstOrDefault(g => g.Id == p.SelectedGoalId) 
            ?? ContentRepository.GetPresetGoals().First();

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
        LblBalance.Text = $"{p.Balance} ₽";
        LblSavings.Text = $"{p.Savings} ₽";

        // 4. Прогресс цели
        int percent = currentGoal.GetProgressPercent(p.Savings);
        LblGoalTitle.Text = $"🎯 {currentGoal.Title}";
        LblGoalProgressText.Text = $"{p.Savings} / {currentGoal.TargetAmount} ₽ ({percent}%)";
        BarGoal.Progress = percent / 100.0;

        int remainingPeriods = currentGoal.EstimateRemainingPeriods(p.Savings, 50);
        LblGoalEstimatedTime.Text = p.Savings >= currentGoal.TargetAmount
            ? "🎉 Цель достигнута! Можно покупать!"
            : $"⏱️ До цели осталось: ~{remainingPeriods} периодов (при сбережениях 50 ₽/период)";

        // 5. Иконка доступности
        LblAnimIcon.Text = p.AnimationsEnabled ? "🎬" : "⏸️";
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
    // 1. ПЕРЕКЛЮЧАТЕЛЬ АНИМАЦИИ (ДОСТУПНОСТЬ)
    // =========================================================================
    private async void OnAnimToggleClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.AnimationsEnabled = !_engine.Profile.AnimationsEnabled;
        RefreshUI();
        await _engine.SaveAsync();
        string status = _engine.Profile.AnimationsEnabled ? "включены 🎬" : "отключены ⏸️";
        PetView.SetSpeechText($"Анимации {status}!");
    }

    // =========================================================================
    // 2. МОДАЛКА: КАСТОМИЗАЦИЯ ВНЕШНЕГО ВИДА
    // =========================================================================
    private async void OnCustomizerClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        EntryPetName.Text = _engine.Profile.PetName;
        await ShowModal("🎨 Гардероб и имя Финни", PanelCustomizer);
    }

    private async void OnOutfitGreenClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Outfit = OutfitType.ClassicGreen;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Изумрудная куртка с ₽ — мой классический стиль! 🟢");
    }

    private async void OnOutfitBlueClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Outfit = OutfitType.RoyalBlue;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Королевский синий цвет — выбор уверенного инвестора! 🔵");
    }

    private async void OnOutfitRubyClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Outfit = OutfitType.RubyRed;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Рубиновый чемпионский цвет заряжает энергией! 🔴");
    }

    private async void OnAccNoneClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Accessory = AccessoryType.None;
        RefreshUI();
        await _engine.SaveAsync();
    }

    private async void OnAccSunglassesClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Accessory = AccessoryType.Sunglasses;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Очки надел — к большим доходам готов! 😎");
    }

    private async void OnAccAcademicClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Accessory = AccessoryType.AcademicCap;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Шапочка юного экономиста! Теперь я профессор финансов! 🎓");
    }

    private async void OnAccCrownClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Accessory = AccessoryType.Crown;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Корона сбережений! Мы накопили королевский запас! 👑");
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
            PetView.SetSpeechText($"Ура! Теперь меня зовут {newName}! 🐾");
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
        await ShowModal($"📊 Бюджет периода #{p.CurrentPeriod}", PanelBudget);
    }

    private void UpdateBudgetModalLabels()
    {
        LblBudgetIncome.Text = $"💰 Доход: {PeriodIncome} ₽";
        LblBudgetObligVal.Text = $"{_tempOblig} ₽";
        LblBudgetDiscVal.Text = $"{_tempDisc} ₽";
        LblBudgetSavVal.Text = $"{_tempSav} ₽";

        int sum = _tempOblig + _tempDisc + _tempSav;
        int diff = PeriodIncome - sum;
        if (diff == 0)
        {
            LblBudgetRemaining.Text = "Распределено 100% ✅";
            LblBudgetRemaining.TextColor = Color.FromArgb("#10B981");
        }
        else if (diff > 0)
        {
            LblBudgetRemaining.Text = $"Осталось: {diff} ₽";
            LblBudgetRemaining.TextColor = Color.FromArgb("#FF0053");
        }
        else
        {
            LblBudgetRemaining.Text = $"Перерасход: {Math.Abs(diff)} ₽ ⚠️";
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
            PetView.SetSpeechText("Мяу! План превышает доход! Уменьши одну из категорий 💡");
            return;
        }

        var p = _engine.Profile;
        p.PlannedObligatory = _tempOblig;
        p.PlannedDiscretionary = _tempDisc;
        p.PlannedSavings = _tempSav;
        p.IsPlanConfirmed = true;

        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Отличный план! Теперь совершай покупки согласно конвертам! 📋");
        await CloseModal();
    }

    private void OnShowPlanFactClicked(object? sender, EventArgs e)
    {
        var p = _engine.Profile;
        string report = $"📊 Сравнение План vs Факт:\n\n" +
            $"🍗 Обязательные: План {_tempOblig} ₽ | Факт {p.SpentObligatory} ₽\n" +
            $"🎮 Желания: План {_tempDisc} ₽ | Факт {p.SpentDiscretionary} ₽\n" +
            $"🏦 В копилку: План {_tempSav} ₽ | Накоплено {p.Savings} ₽";
        PetView.SetSpeechText(report);
    }

    // =========================================================================
    // 4. МОДАЛКА: ОБРАЗОВАТЕЛЬНЫЕ ЗАДАНИЯ И КВИЗ
    // =========================================================================
    private async void OnTasksClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        RenderCurrentTask();
        await ShowModal("📚 Финансовые задачи", PanelTasks);
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
            LblTaskFeedback.Text = $"🎉 {msg}";
            LblTaskFeedback.TextColor = Color.FromArgb("#166534");

            RefreshUI();
            PetView.SetSpeechText($"Ура! Ты блестяще решил задачу и заработал +{option.RewardCoins} монет! 🌟");
        }
        else
        {
            selectedBorder.BackgroundColor = Color.FromArgb("#FEF3C7");
            selectedBorder.Stroke = Color.FromArgb("#F59E0B");
            TaskFeedbackBorder.BackgroundColor = Color.FromArgb("#FFFBEB");
            TaskFeedbackBorder.Stroke = Color.FromArgb("#FCD34D");
            LblTaskFeedback.Text = msg;
            LblTaskFeedback.TextColor = Color.FromArgb("#92400E");
            PetView.SetSpeechText("Ошибаться полезно — так мы учимся быть финансово грамотными! 🐾");
        }
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
        await ShowModal("🛒 Магазин заботы о Финни", PanelShop);
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
        LblShopBalance.Text = $"💰 Доступно монет: {_engine.Profile.Balance} ₽";

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

            // Иконка
            var lblIcon = new Label { Text = item.Icon, FontSize = 24, VerticalOptions = LayoutOptions.Center };
            grid.Children.Add(lblIcon);

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
            buyBtn.Content = new Label { Text = $"{item.Price} ₽", TextColor = Colors.White, FontFamily = "MontserratBold", FontSize = 12, HorizontalOptions = LayoutOptions.Center };

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
            PetView.SetSpeechText("Недостаточно монет! Выполни задание или спланируй бюджет! 💡");
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
        PetView.SetSpeechText($"Муррр! Спасибо за {item.Name}! Теперь я доволен! 🐾");
    }

    // =========================================================================
    // 6. МОДАЛКА: КОПИЛКА И ФИНАНСОВЫЕ ЦЕЛИ
    // =========================================================================
    private async void OnGoalsClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        RenderGoalsUI();
        await ShowModal("🎯 Копилка и цели", PanelGoals);
    }

    private void RenderGoalsUI()
    {
        var p = _engine.Profile;
        var goals = ContentRepository.GetPresetGoals();
        var currentGoal = goals.FirstOrDefault(g => g.Id == p.SelectedGoalId) ?? goals.First();

        int percent = currentGoal.GetProgressPercent(p.Savings);
        LblModalGoalTitle.Text = $"{currentGoal.Icon} {currentGoal.Title}";
        LblModalGoalProgress.Text = $"Накоплено: {p.Savings} / {currentGoal.TargetAmount} ₽ ({percent}%)";
        BarModalGoal.Progress = percent / 100.0;

        GoalsListContainer.Children.Clear();
        foreach (var goal in goals)
        {
            bool isCurrent = goal.Id == currentGoal.Id;
            var goalCard = new Border
            {
                BackgroundColor = isCurrent ? Color.FromArgb("#F3E8FF") : Color.FromArgb("#F9FAFB"),
                Stroke = isCurrent ? Color.FromArgb("#8A83D1") : Color.FromArgb("#E5E7EB"),
                StrokeThickness = 1.5,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
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

            grid.Children.Add(new Label { Text = goal.Icon, FontSize = 20, VerticalOptions = LayoutOptions.Center });
            var info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
            Grid.SetColumn(info, 1);
            info.Children.Add(new Label { Text = goal.Title, FontFamily = "MontserratBold", FontSize = 12, TextColor = Color.FromArgb("#1F2937") });
            info.Children.Add(new Label { Text = $"Цель: {goal.TargetAmount} ₽", FontFamily = "MontserratMedium", FontSize = 11, TextColor = Color.FromArgb("#6B7280") });
            grid.Children.Add(info);

            if (isCurrent)
            {
                var lblSel = new Label { Text = "Активна", FontFamily = "MontserratBold", FontSize = 11, TextColor = Color.FromArgb("#520978"), VerticalOptions = LayoutOptions.Center };
                Grid.SetColumn(lblSel, 2);
                grid.Children.Add(lblSel);
            }
            else
            {
                var selectBtn = new Border
                {
                    BackgroundColor = Color.FromArgb("#520978"),
                    StrokeThickness = 0,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(8, 4),
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
                };
                selectBtn.GestureRecognizers.Add(tap);
                grid.Children.Add(selectBtn);
            }

            goalCard.Content = grid;
            GoalsListContainer.Children.Add(goalCard);
        }
    }

    private async Task DepositToSavings(int amount)
    {
        var p = _engine.Profile;
        if (p.Balance < amount)
        {
            PetView.SetSpeechText($"Не хватает {amount} ₽ на балансе для пополнения копилки!");
            return;
        }

        p.Balance -= amount;
        p.Savings += amount;
        RefreshUI();
        await _engine.SaveAsync();
        RenderGoalsUI();
        PetView.SetSpeechText($"Звон монетки! +{amount} ₽ отправились в копилку! 🏦");
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

    // =========================================================================
    // 7. МОДАЛКА: СЛОВАРЬ И РОСТ
    // =========================================================================
    private async void OnGlossaryClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        RenderGlossaryUI();
        await ShowModal("📖 Словарь юного финансиста", PanelGlossary);
    }

    private void RenderGlossaryUI()
    {
        GlossaryTermsContainer.Children.Clear();
        var terms = ContentRepository.GetGlossaryTerms();

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

        await ShowModal("🔒 Кабинет родителей", PanelParent);
    }

    private async void OnParentUnlockClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        if (int.TryParse(EntryParentMathAnswer.Text, out int ans) && ans == _parentMathA * _parentMathB)
        {
            ParentPinGate.IsVisible = false;
            ParentContent.IsVisible = true;

            var p = _engine.Profile;
            LblParentStats.Text = $"Ребенок: {p.KidName}\n" +
                $"Периодов сыграно: {p.CurrentPeriod}\n" +
                $"Накоплено в копилке: {p.Savings} ₽\n" +
                $"Заданий выполнено: {p.CompletedTasksCount}\n" +
                $"Текущий баланс: {p.Balance} ₽";
        }
        else
        {
            PetView.SetSpeechText("Неверный ответ! Вход только для родителей 🔒");
        }
    }

    private async void OnParentGiveBonusClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.Profile.Balance += 100;
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Родители выдали карманные деньги: +100 ₽! 🎉");
        await CloseModal();
    }

    private async void OnParentResetDataClicked(object? sender, EventArgs e)
    {
        await AnimateTap(sender as VisualElement);
        _engine.ResetData();
        RefreshUI();
        await _engine.SaveAsync();
        PetView.SetSpeechText("Данные сброшены! Начинаем финансовый путь заново! 🚀");
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

        PetView.SetSpeechText($"Период #{p.CurrentPeriod} начался! Начислен доход +{PeriodIncome} ₽! 🚀\n{comparison}");
    }
}
