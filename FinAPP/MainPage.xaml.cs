using System;
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
            // Постепенное естественное снижение сытости и настроения
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
            > 60 => Color.FromArgb("#10B981"), // зеленый
            > 30 => Color.FromArgb("#F59E0B"), // желтый
            _ => Color.FromArgb("#EF4444")      // красный
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

        // 5. Кнопка анимации
        BtnAnimToggle.Text = p.AnimationsEnabled ? "🎬 Вкл" : "⏸️ Выкл";
    }

    // --- 1. ПЕРЕКЛЮЧЕНИЕ АНИМАЦИИ (ТЗ п. 3.6 Доступность) ---
    private async void OnAnimToggleClicked(object? sender, EventArgs e)
    {
        _engine.Profile.AnimationsEnabled = !_engine.Profile.AnimationsEnabled;
        RefreshUI();
        await _engine.SaveAsync();
        string status = _engine.Profile.AnimationsEnabled ? "включены" : "отключены (режим энергосбережения)";
        await DisplayAlertAsync("Доступность", $"Анимации {status}.", "ОК");
    }

    // --- 2. КАСТОМИЗАЦИЯ ПИТОМЦА (ТЗ п. 2.5.2, 2.6: 9+ комбинаций) ---
    private async void OnCustomizerClicked(object? sender, EventArgs e)
    {
        string? section = await DisplayActionSheetAsync("🎨 Настройка внешнего вида Финни", "Закрыть", null,
            "🧥 Выбрать цвет куртки",
            "🎩 Выбрать головной убор / аксессуар",
            "✏️ Изменить имя котика или ребенка");

        if (section == "🧥 Выбрать цвет куртки")
        {
            string? jacket = await DisplayActionSheetAsync("Цвет куртки", "Отмена", null,
                "1. Классическая изумрудная с ₽",
                "2. Королевская синяя",
                "3. Рубиновая чемпионская");

            if (jacket?.StartsWith("1") == true) _engine.Profile.Outfit = OutfitType.ClassicGreen;
            else if (jacket?.StartsWith("2") == true) _engine.Profile.Outfit = OutfitType.RoyalBlue;
            else if (jacket?.StartsWith("3") == true) _engine.Profile.Outfit = OutfitType.RubyRed;

            RefreshUI();
            await _engine.SaveAsync();
        }
        else if (section == "🎩 Выбрать головной убор / аксессуар")
        {
            string? acc = await DisplayActionSheetAsync("Аксессуар", "Отмена", null,
                "1. Без аксессуаров",
                "2. Очки «Крутой инвестор»",
                "3. Шапочка «Юный экономист»",
                "4. Корона сбережений");

            if (acc?.StartsWith("1") == true) _engine.Profile.Accessory = AccessoryType.None;
            else if (acc?.StartsWith("2") == true) _engine.Profile.Accessory = AccessoryType.Sunglasses;
            else if (acc?.StartsWith("3") == true) _engine.Profile.Accessory = AccessoryType.AcademicCap;
            else if (acc?.StartsWith("4") == true) _engine.Profile.Accessory = AccessoryType.Crown;

            RefreshUI();
            await _engine.SaveAsync();
        }
        else if (section == "✏️ Изменить имя котика или ребенка")
        {
            string? newPet = await DisplayPromptAsync("Имя питомца", "Как назовем котика?", initialValue: _engine.Profile.PetName);
            if (!string.IsNullOrWhiteSpace(newPet))
            {
                _engine.Profile.PetName = newPet.Trim();
                RefreshUI();
                await _engine.SaveAsync();
            }
        }
    }

    // --- 3. ПЛАНИРОВАНИЕ БЮДЖЕТА (ТЗ п. 2.5.5) ---
    private async void OnBudgetClicked(object? sender, EventArgs e)
    {
        var p = _engine.Profile;
        string planStatus = p.IsPlanConfirmed ? "Утвержден ✅" : "Черновик (требует подтверждения) ⚠️";

        string? action = await DisplayActionSheetAsync($"📊 Бюджет периода #{p.CurrentPeriod} ({planStatus})", "Закрыть", null,
            "1. Составить / Изменить план (3 направления)",
            "2. Сравнить План и Факт расходов");

        if (action?.StartsWith("1") == true)
        {
            string? obStr = await DisplayPromptAsync("Шаг 1: Обязательные расходы", 
                $"Сколько отложить на еду и уход? (Баланс: {p.Balance} ₽):", 
                initialValue: p.PlannedObligatory.ToString(), keyboard: Keyboard.Numeric);
            if (string.IsNullOrEmpty(obStr) || !int.TryParse(obStr, out int ob) || ob < 0) return;

            string? discStr = await DisplayPromptAsync("Шаг 2: Желания и развлечения", 
                $"Сколько выделить на игрушки и радости?:", 
                initialValue: p.PlannedDiscretionary.ToString(), keyboard: Keyboard.Numeric);
            if (string.IsNullOrEmpty(discStr) || !int.TryParse(discStr, out int disc) || disc < 0) return;

            string? savStr = await DisplayPromptAsync("Шаг 3: Накопления", 
                $"Сколько направить в копилку на цель?:", 
                initialValue: p.PlannedSavings.ToString(), keyboard: Keyboard.Numeric);
            if (string.IsNullOrEmpty(savStr) || !int.TryParse(savStr, out int sav) || sav < 0) return;

            var result = _engine.ConfirmBudgetPlan(ob, disc, sav);
            await DisplayAlertAsync(result.Success ? "Успех" : "Внимание", result.Message, "ОК");
        }
        else if (action?.StartsWith("2") == true)
        {
            string planFact = $"📊 Сравнение Плана и Факта (Период #{p.CurrentPeriod}):\n\n" +
                              $"🍗 Обязательные траты:\n" +
                              $"  • План: {p.PlannedObligatory} ₽ | Факт: {p.ActualObligatory} ₽\n\n" +
                              $"🎮 Желания и развлечения:\n" +
                              $"  • План: {p.PlannedDiscretionary} ₽ | Факт: {p.ActualDiscretionary} ₽\n\n" +
                              $"🏦 Накопления в копилку:\n" +
                              $"  • План: {p.PlannedSavings} ₽ | Факт: {p.ActualSavings} ₽\n\n" +
                              $"💡 Совет Финни: Если факт превышает план, скорректируйте траты на желания в следующем периоде!";

            await DisplayAlertAsync("Исполнение бюджета", planFact, "Понятно");
        }
    }

    // --- 4. ФИНАНСОВЫЕ ЗАДАНИЯ И КЕЙСЫ (ТЗ п. 2.5.8) ---
    private async void OnTasksClicked(object? sender, EventArgs e)
    {
        var tasks = ContentRepository.GetFinancialTasks();
        var taskNames = tasks.Select((t, i) => $"{i + 1}. [{t.TopicDisplayName.Split(' ')[0]}] {t.Title}").ToArray();

        string? chosen = await DisplayActionSheetAsync("📚 Академия финансовой грамотности", "Закрыть", null, taskNames);
        if (string.IsNullOrEmpty(chosen) || chosen == "Закрыть") return;

        int index = int.Parse(chosen.Substring(0, chosen.IndexOf('.'))) - 1;
        var task = tasks[index];

        // Показ ситуации
        string optionsSheet = await DisplayActionSheetAsync($"💡 {task.Title}", "Отмена", null,
            task.Options.Select(o => o.Text).ToArray());

        if (string.IsNullOrEmpty(optionsSheet) || optionsSheet == "Отмена") return;

        var selectedOption = task.Options.FirstOrDefault(o => o.Text == optionsSheet);
        if (selectedOption != null)
        {
            var res = _engine.CompleteTask(task, selectedOption);
            await DisplayAlertAsync(res.Success ? "Верно! 🎉" : "Обучающий момент 💡", res.Message, "ОК");
        }
    }

    // --- 5. МАГАЗИН ТОВАРОВ И ЗАБОТЫ (ТЗ п. 2.5.6) ---
    private async void OnShopClicked(object? sender, EventArgs e)
    {
        string? categoryChoice = await DisplayActionSheetAsync("🛒 Магазин: выберите категорию", "Закрыть", null,
            "1. 🍗 Обязательные расходы (Еда, здоровье, гигиена)",
            "2. 🎮 Желания и радости (Игрушки, гаджеты)");

        if (string.IsNullOrEmpty(categoryChoice) || categoryChoice == "Закрыть") return;

        ExpenseCategory selectedCategory = categoryChoice.StartsWith("1") 
            ? ExpenseCategory.Obligatory 
            : ExpenseCategory.Discretionary;

        var items = ContentRepository.GetShopItems()
            .Where(i => i.Category == selectedCategory)
            .ToList();

        string[] itemOptions = items.Select(i => $"{i.IconEmoji} {i.Name} — {i.Price} ₽").ToArray();

        string? selectedItemText = await DisplayActionSheetAsync(
            selectedCategory == ExpenseCategory.Obligatory ? "Обязательные товары" : "Желания", 
            "Назад", null, itemOptions);

        if (string.IsNullOrEmpty(selectedItemText) || selectedItemText == "Назад") return;

        var chosenItem = items.FirstOrDefault(i => selectedItemText.Contains(i.Name));
        if (chosenItem == null) return;

        // Подтверждение покупки с показом цены и влияния на питомца (ТЗ п. 2.5.6)
        bool confirm = await DisplayAlertAsync($"Подтверждение покупки",
            $"Товар: {chosenItem.Name}\n" +
            $"Категория: {chosenItem.CategoryName}\n" +
            $"Цена: {chosenItem.Price} ₽\n" +
            $"Влияние: {chosenItem.EffectDescription}\n\n" +
            $"Ваш текущий баланс: {_engine.Profile.Balance} ₽. Купить?",
            "Купить", "Отмена");

        if (confirm)
        {
            var res = _engine.PurchaseItem(chosenItem);
            await DisplayAlertAsync(res.Success ? "Успешная покупка 🎁" : "Нехватка средств ❌", res.Message, "ОК");
        }
    }

    // --- 6. КОПИЛКА И ЦЕЛИ НАКОПЛЕНИЙ (ТЗ п. 2.5.7) ---
    private async void OnGoalsClicked(object? sender, EventArgs e)
    {
        var p = _engine.Profile;
        var goals = ContentRepository.GetPresetGoals();
        var currentGoal = goals.FirstOrDefault(g => g.Id == p.SelectedGoalId) ?? goals.First();

        int percent = currentGoal.GetProgressPercent(p.Savings);
        int remPeriods = currentGoal.EstimateRemainingPeriods(p.Savings, 50);

        string? action = await DisplayActionSheetAsync(
            $"🏦 Копилка (Цель: {currentGoal.Title})", "Закрыть", null,
            "1. 📥 Пополнить копилку (+50 ₽)",
            "2. 📥 Пополнить копилку (другая сумма)",
            "3. 📤 Снять деньги из копилки (с предупреждением)",
            "4. 🎯 Сменить цель накоплений");

        if (action?.StartsWith("1") == true)
        {
            var res = _engine.DepositToSavings(50, currentGoal);
            await DisplayAlertAsync("Копилка", res.Message, "ОК");
        }
        else if (action?.StartsWith("2") == true)
        {
            string? strAmount = await DisplayPromptAsync("Пополнение", $"Сколько монет отложить? (Баланс: {p.Balance} ₽):", keyboard: Keyboard.Numeric);
            if (int.TryParse(strAmount, out int amt) && amt > 0)
            {
                var res = _engine.DepositToSavings(amt, currentGoal);
                await DisplayAlertAsync("Копилка", res.Message, "ОК");
            }
        }
        else if (action?.StartsWith("3") == true)
        {
            string? strWithdraw = await DisplayPromptAsync("Снятие из копилки", $"Сколько снять? (В копилке: {p.Savings} ₽):", keyboard: Keyboard.Numeric);
            if (int.TryParse(strWithdraw, out int wAmt) && wAmt > 0)
            {
                // Обязательное предупреждение по п. 2.5.7 ТЗ
                int newRem = currentGoal.EstimateRemainingPeriods(Math.Max(0, p.Savings - wAmt), 50);
                bool proceed = await DisplayAlertAsync("⚠️ Предупреждение о цели",
                    $"Снятие {wAmt} ₽ отдалит покупку «{currentGoal.Title}»!\n" +
                    $"Срок накопления увеличится до ~{newRem} периодов. Вы точно хотите снять средства?",
                    "Да, снять", "Отмена");

                if (proceed)
                {
                    var res = _engine.WithdrawFromSavings(wAmt, currentGoal);
                    await DisplayAlertAsync("Результат", res.Message, "ОК");
                }
            }
        }
        else if (action?.StartsWith("4") == true)
        {
            string[] goalOptions = goals.Select(g => $"{g.IconEmoji} {g.Title} ({g.TargetAmount} ₽)").ToArray();
            string? chosenGoal = await DisplayActionSheetAsync("Выберите цель накоплений", "Отмена", null, goalOptions);
            
            var selected = goals.FirstOrDefault(g => chosenGoal != null && chosenGoal.Contains(g.Title));
            if (selected != null)
            {
                p.SelectedGoalId = selected.Id;
                RefreshUI();
                await _engine.SaveAsync();
                await DisplayAlertAsync("Новая цель", $"Установлена цель: «{selected.Title}» на сумму {selected.TargetAmount} ₽!", "Ура!");
            }
        }
    }

    // --- 7. СЛОВАРЬ И ИСТОРИЯ РОСТА (ТЗ п. 2.5.10, 2.5.11) ---
    private async void OnGlossaryClicked(object? sender, EventArgs e)
    {
        string? option = await DisplayActionSheetAsync("📖 Обучающие материалы и прогресс", "Закрыть", null,
            "1. 📚 Финансовый словарь (понятия и примеры)",
            "2. 🌟 Стадии развития питомца",
            "3. 📜 История прошлых игровых периодов");

        if (option?.StartsWith("1") == true)
        {
            var terms = ContentRepository.GetGlossaryTerms();
            string[] termNames = terms.Select(t => $"{t.IconEmoji} {t.Term}").ToArray();

            string? chosenTerm = await DisplayActionSheetAsync("Финансовый словарь", "Назад", null, termNames);
            var item = terms.FirstOrDefault(t => chosenTerm != null && chosenTerm.Contains(t.Term));
            if (item != null)
            {
                await DisplayAlertAsync($"{item.IconEmoji} {item.Term}",
                    $"📖 Определение:\n{item.Definition}\n\n💡 Пример для жизни:\n{item.KidFriendlyExample}", "Понятно");
            }
        }
        else if (option?.StartsWith("2") == true)
        {
            await DisplayAlertAsync("🌟 Стадии развития Финни",
                "1. 🐾 Малыш (Периоды 1-2): Финни только учится обращаться с монетками.\n\n" +
                "2. 🚀 Подросток (Периоды 3-4): Финни уверенно составляет бюджет и копит на цели.\n\n" +
                "3. 🌟 Финни-Мастер (Периоды 5+): Котик в совершенстве владеет финансовой грамотностью и имеет золотое свечение!", "Круто!");
        }
        else if (option?.StartsWith("3") == true)
        {
            var history = _engine.Profile.History;
            if (history.Count == 0)
            {
                await DisplayAlertAsync("История", "Вы находитесь в первом периоде. Завершите период кнопкой ДЕМО, чтобы увидеть отчет!", "ОК");
                return;
            }

            string histText = string.Join("\n\n", history.Select(h => 
                $"📅 Период #{h.PeriodNumber}: {(h.IsBudgetSuccess ? "✅ Успех" : "⚠️ Перерасход")}\n" +
                $"  • Обязательные: план {h.PlannedObligatory} / факт {h.ActualObligatory} ₽\n" +
                $"  • Желания: план {h.PlannedDiscretionary} / факт {h.ActualDiscretionary} ₽\n" +
                $"  • В копилку: факт {h.ActualSavings} ₽"));

            await DisplayAlertAsync("📜 Итоги завершенных периодов", histText, "Закрыть");
        }
    }

    // --- 8. КАБИНЕТ РОДИТЕЛЯ (ТЗ п. 2.5.12, 2.5.13) ---
    private async void OnParentClicked(object? sender, EventArgs e)
    {
        // Простой барьер для взрослого (арифметический пример по п. 2.5.12 ТЗ)
        string? answer = await DisplayPromptAsync("🔒 Раздел для взрослого", "Защитный вопрос: Сколько будет 7 × 8?", keyboard: Keyboard.Numeric);
        if (answer != "56")
        {
            await DisplayAlertAsync("Доступ закрыт", "Неверный ответ на защитный вопрос.", "ОК");
            return;
        }

        var p = _engine.Profile;
        string? adminChoice = await DisplayActionSheetAsync("👨‍👩‍👧‍👦 Кабинет взрослого / Родительский контроль", "Закрыть", null,
            $"📊 Пройдено тестов ребенком: {p.TestsPassedCount}",
            $"💰 Баланс: {p.Balance} ₽ | Копилка: {p.Savings} ₽",
            "🎁 Начислить ребенку карманный бонус (+100 ₽)",
            "🔄 СБРОСИТЬ тестовый профиль к исходному состоянию (Эксперт)");

        if (adminChoice?.StartsWith("🎁") == true)
        {
            _engine.AddIncome(100, "поощрение родителя");
            await DisplayAlertAsync("Бонус начислен", "Ребенку начислено +100 ₽ за реальные успехи или помощь!", "Отлично");
        }
        else if (adminChoice?.StartsWith("🔄") == true)
        {
            bool confirmReset = await DisplayAlertAsync("Сброс профиля", 
                "Вы уверены, что хотите сбросить тестовый профиль к исходному состоянию?\nЭто необходимо для повторного прохождения сценария жюри.",
                "Да, сбросить", "Отмена");

            if (confirmReset)
            {
                _engine.ResetDemoProfile();
                await DisplayAlertAsync("Сброшено", "Профиль сброшен к исходному тестовому состоянию.", "ОК");
            }
        }
    }

    // --- 9. ДЕМО: ПЕРЕХОД К СЛЕДУЮЩЕМУ ПЕРИОДУ (ТЗ п. 2.6, Шаг 10) ---
    private async void OnNextPeriodClicked(object? sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync("Завершение периода",
            $"Завершить Период #{_engine.Profile.CurrentPeriod} и перейти к следующему?\nБудут подведены итоги бюджета и начислены карманные деньги.",
            "Да, вперед!", "Отмена");

        if (confirm)
        {
            string summaryMsg = _engine.AdvanceToNextPeriod();
            await DisplayAlertAsync("Итоги периода", summaryMsg, "Отлично!");
        }
    }
}