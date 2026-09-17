using System.Collections.Generic;
using FinAPP.Models;

namespace FinAPP.Services;

public static class ContentRepository
{
    // Не менее 8 позиций двух типов: Обязательные и Необязательные (п. 2.6 ТЗ)
    public static List<ShopItem> GetShopItems() => new()
    {
        // 1. Обязательные расходы (еда, здоровье, регулярный уход)
        new ShopItem
        {
            Id = "food_lunch",
            Name = "Питательный обед для Финни",
            Category = ExpenseCategory.Obligatory,
            Price = 50,
            HungerBoost = 35,
            MoodBoost = 10,
            IconEmoji = "🍗",
            Description = "Полноценный обед с белками и витаминами. Необходим для поддержания сил питомца."
        },
        new ShopItem
        {
            Id = "food_vitamins",
            Name = "Витаминный комплекс Омега-3",
            Category = ExpenseCategory.Obligatory,
            Price = 70,
            HungerBoost = 25,
            MoodBoost = 20,
            IconEmoji = "💊",
            Description = "Обязательный уход за шерсткой и иммунитетом Финни."
        },
        new ShopItem
        {
            Id = "care_hygiene",
            Name = "Гигиенический набор и щетка",
            Category = ExpenseCategory.Obligatory,
            Price = 40,
            HungerBoost = 15,
            MoodBoost = 25,
            IconEmoji = "🧼",
            Description = "Регулярный уход: чистота и здоровье котика каждый период."
        },
        new ShopItem
        {
            Id = "care_vet_check",
            Name = "Профилактический осмотр у ветеринара",
            Category = ExpenseCategory.Obligatory,
            Price = 90,
            HungerBoost = 20,
            MoodBoost = 35,
            IconEmoji = "🩺",
            Description = "Обязательная забота: предотвращает усталость и плохое самочувствие."
        },

        // 2. Необязательные расходы (желания, игры, развлечения)
        new ShopItem
        {
            Id = "toy_ball",
            Name = "Мячик-пищалка с колокольчиком",
            Category = ExpenseCategory.Discretionary,
            Price = 45,
            HungerBoost = 0,
            MoodBoost = 30,
            IconEmoji = "🎾",
            Description = "Забавная игрушка для активных игр. Финни обожает гонять мячик!"
        },
        new ShopItem
        {
            Id = "toy_laser",
            Name = "Лазерная указка-фонарик",
            Category = ExpenseCategory.Discretionary,
            Price = 85,
            HungerBoost = 0,
            MoodBoost = 55,
            IconEmoji = "🔦",
            Description = "Крутое развлечение! Дарит море радости, но покупку можно отложить при нехватке средств."
        },
        new ShopItem
        {
            Id = "toy_bed",
            Name = "Мягкая лежанка-облачко",
            Category = ExpenseCategory.Discretionary,
            Price = 140,
            HungerBoost = 0,
            MoodBoost = 75,
            IconEmoji = "🛋️",
            Description = "Уютное место для сна. Приятная покупка из категории «Желания»."
        },
        new ShopItem
        {
            Id = "toy_drone",
            Name = "Игрушечный мини-коптер",
            Category = ExpenseCategory.Discretionary,
            Price = 220,
            HungerBoost = 0,
            MoodBoost = 100,
            IconEmoji = "🚁",
            Description = "Супер-гаджет для Финни. Покупать стоит только при избытке карманных денег!"
        }
    };

    // Не менее 6 интерактивных заданий по 3 темам (п. 2.5.8 и 2.6 ТЗ)
    // Составлены строго по Единой рамке компетенций Минфина РФ и Банка России
    public static List<FinancialTask> GetFinancialTasks() => new()
    {
        // ТЕМА 1: Планирование личного бюджета (Компетенции 1, 2, 3)
        new FinancialTask
        {
            Id = "task_budget_1",
            Topic = TaskTopic.BudgetPlanning,
            Title = "Золотое правило 50 / 30 / 20",
            CompetencyReference = "Единая рамка компетенций, п. 6.1: Назначение личного бюджета и соотношение доходов и расходов",
            ScenarioDescription = "Тебе выдали 100 монет карманных денег на неделю. Как грамотнее всего распределить эти средства согласно финансовому правилу?",
            Options = new()
            {
                new TaskOption
                {
                    Text = "50 монет — обязательные нужды (еда/проезд), 30 — желания, 20 — в копилку",
                    IsCorrect = true,
                    Explanation = "Отлично! Это классическое правило бюджета: сначала покрываем обязательные нужды, оставляем немного на радости и обязательно откладываем в сбережения.",
                    RewardCoins = 100
                },
                new TaskOption
                {
                    Text = "Сразу потратить все 100 монет на сладости и аттракционы",
                    IsCorrect = false,
                    Explanation = "Если потратить всё на минутные желания, у Финни не останется денег на обед и не получится накопить на большую цель."
                },
                new TaskOption
                {
                    Text = "Спрятать все 100 монет и ничего не есть целую неделю",
                    IsCorrect = false,
                    Explanation = "Обязательные потребности нельзя игнорировать — питомец останется голодным и загрустит. Бюджет должен быть сбалансированным!"
                }
            }
        },
        new FinancialTask
        {
            Id = "task_budget_2",
            Topic = TaskTopic.BudgetPlanning,
            Title = "Ловушка у кассы супермаркета",
            CompetencyReference = "Единая рамка компетенций, п. 6.2: Различение необходимого и желаемого",
            ScenarioDescription = "Ты пришел в магазин за кормом для Финни (нужно 50 монет). Возле кассы лежат яркие наклейки за 40 монет. Твой баланс — 60 монет. Что сделать?",
            Options = new()
            {
                new TaskOption
                {
                    Text = "Купить обязательный корм для Финни, а наклейки внести в план желаний на будущее",
                    IsCorrect = true,
                    Explanation = "Мудрое решение! Обязательные расходы всегда в приоритете перед импульсивными спонтанными желаниями.",
                    RewardCoins = 110
                },
                new TaskOption
                {
                    Text = "Купить наклейки, а питомца оставить голодным",
                    IsCorrect = false,
                    Explanation = "Импульсивная покупка желаемого в ущерб обязательному приводит к падению настроения и сил питомца."
                }
            }
        },

        // ТЕМА 2: Сбережения и подушка безопасности (Компетенции 4, 5)
        new FinancialTask
        {
            Id = "task_savings_1",
            Topic = TaskTopic.SavingsAndReserve,
            Title = "Что такое подушка безопасности?",
            CompetencyReference = "Единая рамка компетенций, п. 6.4: Формирование сбережений и резерва на непредвиденный случай",
            ScenarioDescription = "Зачем финансовые эксперты советуют каждой семье и каждому человеку иметь «подушку безопасности»?",
            Options = new()
            {
                new TaskOption
                {
                    Text = "Это неприкосновенный резерв денег на случай непредвиденных обстоятельств или поломок",
                    IsCorrect = true,
                    Explanation = "Именно так! Резерв на 3–6 месяцев позволяет спокойно пережить любые временные трудности без долгов.",
                    RewardCoins = 120
                },
                new TaskOption
                {
                    Text = "Это мягкая подушка, в которую зашивают купюры",
                    IsCorrect = false,
                    Explanation = "«Подушка безопасности» — это образное выражение, означающее неприкосновенный денежный запас."
                },
                new TaskOption
                {
                    Text = "Это деньги, которые нужно срочно потратить на вечеринку",
                    IsCorrect = false,
                    Explanation = "Сбережения из подушки безопасности нельзя тратить на развлечения — они защищают от кризисов."
                }
            }
        },
        new FinancialTask
        {
            Id = "task_savings_2",
            Topic = TaskTopic.SavingsAndReserve,
            Title = "Магия сложного процента",
            CompetencyReference = "Единая рамка компетенций, п. 6.4: Долгосрочные сбережения и работа капитала",
            ScenarioDescription = "Если регулярно пополнять копилку или банковский вклад, почему сумма со временем начинает расти всё быстрее и быстрее?",
            Options = new()
            {
                new TaskOption
                {
                    Text = "Работает сложный процент: доход начисляется не только на начальную сумму, но и на уже заработанные проценты",
                    IsCorrect = true,
                    Explanation = "Браво! Альберт Эйнштейн называл сложный процент восьмым чудом света. Время — лучший друг сбережений.",
                    RewardCoins = 130
                },
                new TaskOption
                {
                    Text = "Деньги в банке размножаются сами собой каждую ночь",
                    IsCorrect = false,
                    Explanation = "Деньги растут благодаря процентам, которые банк платит за использование ваших сбережений."
                }
            }
        },

        // ТЕМА 3: Платежи, покупки и кибербезопасность (Компетенции 5, 6)
        new FinancialTask
        {
            Id = "task_security_1",
            Topic = TaskTopic.PaymentsAndSecurity,
            Title = "Звонок от «Службы безопасности банка»",
            CompetencyReference = "Единая рамка компетенций, п. 6.5: Защита от финансового мошенничества и кибергигиена",
            ScenarioDescription = "Незнакомец звонит и тревожным голосом говорит: «Ваш счет заблокирован! Срочно продиктуйте код из СМС, чтобы спасти деньги!». Ваши действия?",
            Options = new()
            {
                new TaskOption
                {
                    Text = "Немедленно положить трубку и рассказать взрослым. Никому не сообщать коды из СМС!",
                    IsCorrect = true,
                    Explanation = "Отличная кибербдительность! Настоящие сотрудники банков никогда не спрашивают секретные коды из СМС.",
                    RewardCoins = 140
                },
                new TaskOption
                {
                    Text = "Быстро назвать код, чтобы не потерять деньги",
                    IsCorrect = false,
                    Explanation = "Опасно! Сообщив код из СМС, вы отдадите мошенникам доступ к деньгам. Никогда так не делайте!"
                }
            }
        },
        new FinancialTask
        {
            Id = "task_security_2",
            Topic = TaskTopic.PaymentsAndSecurity,
            Title = "Бесплатные монеты в интернете",
            CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность в цифровой среде",
            ScenarioDescription = "На сайте предлагают ввести пароль от аккаунта и номер карты родителей, чтобы получить «10 000 бесплатных монет в игре». Что делать?",
            Options = new()
            {
                new TaskOption
                {
                    Text = "Закрыть сайт! Это фишинг — мошенническая ловушка для кражи данных",
                    IsCorrect = true,
                    Explanation = "Абсолютно верно! Бесплатный сыр бывает только в мышеловке. Финансовые данные родителей — строгая тайна.",
                    RewardCoins = 150
                },
                new TaskOption
                {
                    Text = "Ввести все данные и ждать подарков",
                    IsCorrect = false,
                    Explanation = "Это приведет к краже аккаунта или списанию денег с карты родителей. Мошенники обманывают обещаниями легких бонусов."
                }
            }
        }
    };

    // Не менее 3 целей накопления (п. 2.6 ТЗ)
    public static List<FinancialGoal> GetPresetGoals() => new()
    {
        new FinancialGoal
        {
            Id = "goal_scooter",
            Title = "Спортивный трюковой самокат",
            TargetAmount = 350,
            IconEmoji = "🛴",
            Description = "Отличная краткосрочная цель для прогулок в парке."
        },
        new FinancialGoal
        {
            Id = "goal_game",
            Title = "Настольная экономическая игра",
            TargetAmount = 650,
            IconEmoji = "🎲",
            Description = "Увлекательная игра для всей семьи, учит инвестициям и торговле."
        },
        new FinancialGoal
        {
            Id = "goal_gadget",
            Title = "Умные детские часы с GPS",
            TargetAmount = 1000,
            IconEmoji = "⌚",
            Description = "Полезный гаджет для связи с родителями и шагомера."
        }
    };

    // Интерактивный словарь терминов (п. 2.5.11 ТЗ)
    public static List<GlossaryTerm> GetGlossaryTerms() => new()
    {
        new GlossaryTerm
        {
            Term = "Бюджет",
            Definition = "План доходов и расходов на определенный период времени (неделю или месяц).",
            KidFriendlyExample = "Как карта путешествия: бюджет помогает заранее знать, на что хватит денег и сколько удастся отложить.",
            IconEmoji = "📋"
        },
        new GlossaryTerm
        {
            Term = "Доход",
            Definition = "Все деньги, которые ты получаешь: карманные деньги, подарки на день рождения или награды за выполненные задания.",
            KidFriendlyExample = "Когда бабушка подарила 100 монет или ты получил бонус за тест — это твой доход.",
            IconEmoji = "💰"
        },
        new GlossaryTerm
        {
            Term = "Обязательные расходы",
            Definition = "Траты на то, без чего нельзя обойтись: еда, жилье, одежда по сезону, лекарства и проезд.",
            KidFriendlyExample = "Обед для Финни — обязательный расход, ведь без еды котик заболеет и потеряет силы.",
            IconEmoji = "🍞"
        },
        new GlossaryTerm
        {
            Term = "Необязательные расходы (Желания)",
            Definition = "Траты на вещи, которые приносят радость, но без которых можно прожить: игрушки, сладости, развлечения.",
            KidFriendlyExample = "Мячик или лазерная указка — это желания. Если денег мало, их покупку можно перенести.",
            IconEmoji = "🎮"
        },
        new GlossaryTerm
        {
            Term = "Накопления (Копилка)",
            Definition = "Часть денег, которую ты не тратишь сейчас, а сохраняешь для покупки большой мечты в будущем.",
            KidFriendlyExample = "Откладывая по 50 монет каждую неделю, через пару месяцев ты сможешь купить крутой самокат!",
            IconEmoji = "🏦"
        },
        new GlossaryTerm
        {
            Term = "Подушка безопасности",
            Definition = "Запас денег на случай непредвиденных ситуаций (поломка телефона, болезнь, срочный ремонт).",
            KidFriendlyExample = "Резервный фонд, который защищает тебя от неприятных сюрпризов.",
            IconEmoji = "🛡️"
        },
        new GlossaryTerm
        {
            Term = "Инфляция",
            Definition = "Постепенный рост цен со временем, из-за которого на одну и ту же сумму денег можно купить меньше товаров.",
            KidFriendlyExample = "Если мороженое стоило 50 монет, а через год стало стоить 60 монет — это действие инфляции.",
            IconEmoji = "📈"
        },
        new GlossaryTerm
        {
            Term = "Сложный процент",
            Definition = "Начисление процентов на всю сумму вклада вместе с уже накопленными ранее процентами («проценты на проценты»).",
            KidFriendlyExample = "Снежный ком, который катится с горы и с каждым оборотом становится всё больше и больше!",
            IconEmoji = "❄️"
        }
    };
}
