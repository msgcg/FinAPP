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
            IconEmoji = "",
            IconImage = "ic_shop_lunch.png",
            ThanksText = "Муррр! Спасибо за питательный обед! Теперь я сыт и полон сил!",
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
            IconEmoji = "",
            IconImage = "ic_shop_vitamins.png",
            ThanksText = "Муррр! Спасибо за полезные витаминки! Моя шерстка сияет!",
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
            IconEmoji = "",
            IconImage = "ic_shop_hygiene.png",
            ThanksText = "Муррр! Спасибо за гигиенический набор и щётку! Я такой чистый!",
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
            IconEmoji = "",
            IconImage = "ic_shop_vet.png",
            ThanksText = "Муррр! Спасибо за заботу и визит к ветеринару! Чувствую себя отлично!",
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
            IconEmoji = "",
            IconImage = "ic_shop_ball.png",
            ThanksText = "Муррр! Спасибо за весёлый мячик! Побежали играть!",
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
            IconEmoji = "",
            IconImage = "ic_shop_laser.png",
            ThanksText = "Муррр! Спасибо за лазерную указку! Я поймаю этот огонёк!",
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
            IconEmoji = "",
            IconImage = "ic_shop_bed.png",
            ThanksText = "Муррр! Спасибо за мягкую лежанку! Буду сладко мурлыкать!",
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
            IconEmoji = "",
            IconImage = "ic_shop_drone.png",
            ThanksText = "Муррр! Спасибо за крутой мини-коптер! Финни теперь пилот!",
            Description = "Супер-гаджет для Финни. Покупать стоит только при избытке карманных денег!"
        }
    };

    // 12 интерактивных заданий: по 6 на каждую возрастную группу (п. 2.5.8 и 2.6 ТЗ)
    // Составлены строго по Единой рамке компетенций Минфина РФ и Банка России
    public static List<FinancialTask> GetFinancialTasks(AgeGroup? age = null)
    {
        var allTasks = new List<FinancialTask>
        {
            // ==========================================
            // МЛАДШАЯ ГРУППА: 7–8 ЛЕТ (1–2 КЛАСС)
            // Простые числа (до 50), короткие формулировки,
            // наглядные ситуации: хочу/надо, копилка, сдача
            // ==========================================
            new FinancialTask
            {
                Id = "junior_task_1",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Хочу или Надо?",
                CompetencyReference = "Единая рамка компетенций, п. 6.2: Различение необходимого и желаемого",
                ScenarioDescription = "Финни проголодался, его полезный обед стоит 15 монет. А рядом продается блестящая наклейка за 15 монет. У тебя в кармане всего 20 монет. Что нужно купить?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Купить сытный обед для Финни (Надо!), а наклейку отложить на потом",
                        IsCorrect = true,
                        Explanation = "Умница! Обязательные потребности (еда и здоровье) всегда важнее минутных капризов. Финни сыт и доволен!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "Купить наклейку, а котика оставить голодным на весь день",
                        IsCorrect = false,
                        Explanation = "Если потратить монетки на наклейку, питомец останется голодным и загрустит. Сначала всегда покупаем то, что необходимо!"
                    }
                }
            },
            new FinancialTask
            {
                Id = "junior_task_2",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Три волшебных конверта ️",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Назначение личного бюджета и распределение средств",
                ScenarioDescription = "Мама дала тебе 30 карманных монет. Как лучше всего распределить их по конвертам?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "15 монет — на обед (надо), 10 монет — на мячик (хочу), 5 монет — в копилку (мечта)",
                        IsCorrect = true,
                        Explanation = "Отлично! Это золотое правило: накормить питомца, оставить немного на игру и обязательно отложить монетку в копилку!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "Купить сразу 6 леденцов и всё съесть за пять минут",
                        IsCorrect = false,
                        Explanation = "Если потратить всё на сладости, разболится живот, а у Финни не останется монеток на еду и копилку."
                    }
                }
            },
            new FinancialTask
            {
                Id = "junior_task_3",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Копилка на мечту",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Формирование сбережений и достижение цели",
                ScenarioDescription = "В витрине сидит заводной мышонок за 40 монет. У тебя есть 10 монет каждый день. Как быстрее всего купить игрушку?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Каждый день класть в копилку по 10 монет — и через 4 дня мышонок наш!",
                        IsCorrect = true,
                        Explanation = "Браво! Регулярные маленькие накопления превращаются в большую радость. Терпение помогает достичь цели!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "Тратить 10 монет каждый день на жвачку и надеяться, что мышонок сам прибежит",
                        IsCorrect = false,
                        Explanation = "Если тратить все карманные деньги на мелочи, накопить на большую игрушку никогда не получится."
                    }
                }
            },
            new FinancialTask
            {
                Id = "junior_task_4",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Монетка про запас",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Резерв на непредвиденный случай",
                ScenarioDescription = "Зачем Финни оставляет 5 монеток на дне копилки и не трогает их?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "На случай, если порвется ошейник или понадобится витаминка, когда закончатся карманные деньги",
                        IsCorrect = true,
                        Explanation = "Верно! Даже у маленького котика должен быть запас на непредвиденный случай — это финансовая подушка безопасности!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "Просто чтобы монеткам было темно и скучно в банке",
                        IsCorrect = false,
                        Explanation = "Монетки хранят не просто так, а как спасательный круг на случай неожиданных трат."
                    }
                }
            },
            new FinancialTask
            {
                Id = "junior_task_5",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Считаем сдачу в буфете",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Простые покупки и наличные расчеты",
                ScenarioDescription = "Ты купил яблоко для Финни за 15 монет и дал продавцу монетку в 20 монет. Сколько сдачи продавец должен тебе вернуть?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "5 монет (потому что 20 минус 15 равно 5)",
                        IsCorrect = true,
                        Explanation = "Идеально! 20 - 15 = 5. Всегда проверяй сдачу прямо у кассы, это привычка грамотного финансиста!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "Нисколько, сдачу забирать не нужно",
                        IsCorrect = false,
                        Explanation = "Сдача — это твои законные деньги. Их нужно забирать и бережно складывать в кошелек!"
                    },
                    new TaskOption
                    {
                        Text = "10 монет",
                        IsCorrect = false,
                        Explanation = "Давай посчитаем вместе: 20 - 15 = 5 монет, а не 10."
                    }
                }
            },
            new FinancialTask
            {
                Id = "junior_task_6",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Секрет твоего кошелька",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность и защита карманных денег",
                ScenarioDescription = "Во дворе незнакомец говорит: «Покажи, сколько у тебя монеток в кошельке, я фокус покажу и удвою их!». Что ты сделаешь?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Не доставать кошелек, громко сказать «Нет!» и сразу подойти к родителям или друзьям",
                        IsCorrect = true,
                        Explanation = "Молодец! Никогда не показывай деньги чужим людям на улице. Деньги любят тишину и безопасность!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "Отдать кошелек незнакомцу и закрыть глаза",
                        IsCorrect = false,
                        Explanation = "Очень опасно! Незнакомец может убежать с твоими деньгами. Чужим людям деньги доверять нельзя!"
                    }
                }
            },

            // ==========================================
            // СТАРШАЯ ГРУППА: 9–11 ЛЕТ (3–5 КЛАСС)
            // Расчет выгоды, скидки 1+1, подушка безопасности,
            // фишинг, цена за грамм, бюджет 50/30/20
            // ==========================================
            new FinancialTask
            {
                Id = "senior_task_1",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Золотое правило 50 / 30 / 20",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Назначение личного бюджета и соотношение доходов и расходов",
                ScenarioDescription = "Тебе выделили 100 монет карманных денег на неделю. Как грамотнее всего распределить эти средства согласно финансовому правилу?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "50 монет — обязательные нужды (еда/проезд), 30 — желания, 20 — в накопления",
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
                Id = "senior_task_2",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Ловушка акции «Второй за полцены» ️",
                CompetencyReference = "Единая рамка компетенций, п. 6.2: Различение необходимого и желаемого, маркетинговые уловки",
                ScenarioDescription = "Пачка корма стоит 60 монет. По акции вторую пачку предлагают за 30 монет. Но срок годности истекает завтра, а Финни успеет съесть только одну. Выгодно ли брать вторую?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Нет! Вторая пачка испортится и отправится в мусорку. Это лишняя трата 30 монет",
                        IsCorrect = true,
                        Explanation = "Блестящий финансовый анализ! Скидка на ненужный или портящийся товар — это не экономия, а лишний расход.",
                        RewardCoins = 110
                    },
                    new TaskOption
                    {
                        Text = "Да, ведь скидка 50%! Надо всегда покупать то, на что есть скидка",
                        IsCorrect = false,
                        Explanation = "Маркетологи часто подталкивают купить то, что испортится. Если товар не будет использован, деньги выброшены на ветер."
                    }
                }
            },
            new FinancialTask
            {
                Id = "senior_task_3",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Подушка безопасности для семьи и питомца ️",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Формирование сбережений и резерва на непредвиденный случай",
                ScenarioDescription = "Зачем финансовые эксперты советуют каждому человеку и семье иметь «подушку безопасности»?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Это неприкосновенный резерв на случай непредвиденных обстоятельств (поломка, лечение), чтобы не брать в долг",
                        IsCorrect = true,
                        Explanation = "Именно так! Резервный фонд позволяет спокойно пережить любые непредвиденные трудности без долгов и кредитов.",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Это мягкая диванная подушка, в которую зашивают монеты",
                        IsCorrect = false,
                        Explanation = "«Подушка безопасности» — финансовый термин, означающий неприкосновенный денежный запас."
                    }
                }
            },
            new FinancialTask
            {
                Id = "senior_task_4",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Сравнение цены за грамм ️",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Принятие решений в условиях ограниченного бюджета",
                ScenarioDescription = "В магазине две пачки корма: маленькая 200г за 60 монет (30 монет/100г) и большая 500г за 120 монет (24 монеты/100г). Какая покупка выгоднее при регулярном кормлении?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Большая пачка (500г): в пересчете за 100 грамм она на 20% экономичнее!",
                        IsCorrect = true,
                        Explanation = "Отличный расчет! Сравнение удельной стоимости (цены за 100г или за 1 кг) — главный навык экономного покупателя.",
                        RewardCoins = 130
                    },
                    new TaskOption
                    {
                        Text = "Маленькая пачка, потому что 60 монет число меньше, чем 120",
                        IsCorrect = false,
                        Explanation = "Хотя общий чек меньше, грамм корма в маленькой пачке обойдется дороже. При регулярных покупках большая пачка выгоднее!"
                    }
                }
            },
            new FinancialTask
            {
                Id = "senior_task_5",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Звонок от «Службы безопасности»",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Защита от финансового мошенничества и кибергигиена",
                ScenarioDescription = "Незнакомец звонит и тревожно говорит: «Ваш счет атакован! Срочно продиктуйте код из СМС, чтобы спасти баланс!». Что делать?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Немедленно повесить трубку и рассказать родителям. Никому и никогда не сообщать коды из СМС!",
                        IsCorrect = true,
                        Explanation = "Отличная кибербдительность! Настоящие сотрудники банков никогда не просят назвать секретный код из СМС.",
                        RewardCoins = 140
                    },
                    new TaskOption
                    {
                        Text = "Быстро продиктовать код, чтобы деньги не пропали",
                        IsCorrect = false,
                        Explanation = "Опасно! Сообщив код из СМС, вы отдадите мошенникам доступ к списанию денег. Никогда так не делайте!"
                    }
                }
            },
            new FinancialTask
            {
                Id = "senior_task_6",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Фишинг: бесплатные монеты в интернете",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность в цифровой среде и защита от мошенников",
                ScenarioDescription = "На сайте предлагают ввести логин, пароль и номер карты родителей, чтобы получить «10 000 бесплатных монет в игре». Что делать?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Закрыть сайт! Это фишинг — мошенническая ловушка для кражи паролей и денег",
                        IsCorrect = true,
                        Explanation = "Абсолютно верно! Бесплатный сыр бывает только в мышеловке. Финансовые данные родителей — строгая тайна.",
                        RewardCoins = 150
                    },
                    new TaskOption
                    {
                        Text = "Ввести все данные и ждать подарков",
                        IsCorrect = false,
                        Explanation = "Это приведет к краже аккаунта или списанию денег с карты родителей. Мошенники заманивают обещаниями легких бонусов."
                    }
                }
            }
        };

        if (age.HasValue)
        {
            return allTasks.FindAll(t => t.TargetAge == age.Value);
        }
        return allTasks;
    }

    // Не менее 3 целей накопления (п. 2.6 ТЗ)
    public static List<FinancialGoal> GetPresetGoals() => new()
    {
        new FinancialGoal
        {
            Id = "goal_scooter",
            Title = "Спортивный трюковой самокат",
            TargetAmount = 350,
            IconEmoji = "",
            IconImage = "ic_goal_scooter.png",
            Description = "Отличная краткосрочная цель для прогулок в парке."
        },
        new FinancialGoal
        {
            Id = "goal_game",
            Title = "Настольная экономическая игра",
            TargetAmount = 650,
            IconEmoji = "",
            IconImage = "ic_goal_game.png",
            Description = "Увлекательная игра для всей семьи, учит инвестициям и торговле."
        },
        new FinancialGoal
        {
            Id = "goal_gadget",
            Title = "Умные детские часы с GPS",
            TargetAmount = 1000,
            IconEmoji = "",
            IconImage = "ic_goal_gadget.png",
            Description = "Полезный гаджет для связи с родителями и шагомера."
        }
    };

    // Интерактивный словарь терминов (п. 2.5.11 ТЗ) с учетом возраста
    public static List<GlossaryTerm> GetGlossaryTerms(AgeGroup? age = null)
    {
        var list = new List<GlossaryTerm>
        {
            new GlossaryTerm
            {
                Term = "Монеты и деньги",
                Definition = "Условные единицы для покупки еды, одежды и полезных вещей.",
                KidFriendlyExample = "Когда мы помогаем по дому или решаем задачки, мы получаем монетки для Финни.",
                IconEmoji = "",
                TargetAge = null
            },
            new GlossaryTerm
            {
                Term = "Обязательное и Желания",
                Definition = "Обязательное — то, без чего нельзя обойтись (еда, здоровье). Желания — то, что приносит радость, но может подождать (игрушки).",
                KidFriendlyExample = "Обед для котика — это «Надо!», а лазерная указка — это «Хочу!».",
                IconEmoji = "",
                TargetAge = null
            },
            new GlossaryTerm
            {
                Term = "Копилка и накопления",
                Definition = "Часть монет, которую мы откладываем прямо сейчас, чтобы потом купить большую мечту.",
                KidFriendlyExample = "Откладывая по 5–10 монет каждый период, ты накопишь на самокат!",
                IconEmoji = "",
                TargetAge = null
            },
            new GlossaryTerm
            {
                Term = "Сдача",
                Definition = "Монеты, которые продавец возвращает тебе, если ты дал больше стоимости товара.",
                KidFriendlyExample = "Товар стоит 15 монет, ты дал 20 — продавец возвращает 5 монет сдачи.",
                IconEmoji = "",
                TargetAge = AgeGroup.Junior7_8
            },
            new GlossaryTerm
            {
                Term = "Личный бюджет",
                Definition = "План доходов и расходов на период времени (неделю или месяц).",
                KidFriendlyExample = "Как карта путешествия: помогает заранее знать, на что хватит средств и сколько удастся отложить.",
                IconEmoji = "",
                TargetAge = AgeGroup.Senior9_11
            },
            new GlossaryTerm
            {
                Term = "Подушка безопасности",
                Definition = "Запас денег на случай непредвиденных ситуаций (поломка, лечение, срочный ремонт).",
                KidFriendlyExample = "Резервный фонд, который защищает тебя от неприятных сюрпризов.",
                IconEmoji = "",
                TargetAge = AgeGroup.Senior9_11
            },
            new GlossaryTerm
            {
                Term = "Инфляция",
                Definition = "Постепенный рост цен со временем, из-за которого на ту же сумму покупается меньше товаров.",
                KidFriendlyExample = "Если корм стоил 50 монет, а через год стал стоить 60 — это действие инфляции.",
                IconEmoji = "",
                TargetAge = AgeGroup.Senior9_11
            },
            new GlossaryTerm
            {
                Term = "Фишинг и кибергигиена",
                Definition = "Попытки мошенников выманить пароли или коды подтверждения из СМС.",
                KidFriendlyExample = "Никому не сообщай коды из СМС и не вводи данные родителей на подозрительных сайтах!",
                IconEmoji = "",
                TargetAge = AgeGroup.Senior9_11
            }
        };

        if (age.HasValue)
        {
            return list.FindAll(t => t.TargetAge == null || t.TargetAge == age.Value);
        }
        return list;
    }
}
