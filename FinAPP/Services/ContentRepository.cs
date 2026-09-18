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
        },

        // 3. Мебель и обустройство комнаты (Интерьер - платные столы и подиумы)
        new ShopItem
        {
            Id = "desk_modern",
            Name = "Стол IT-финансиста",
            Category = ExpenseCategory.Interior,
            Price = 200,
            HungerBoost = 0,
            MoodBoost = 40,
            IconEmoji = "",
            IconImage = "desk_modern.png",
            LinkedDesk = PetDeskType.Modern,
            ThanksText = "Муррр! Какой крутой IT-стол! Теперь Финни настоящий программист и инвестор!",
            Description = "Ноутбук, настольная лампа и мониторы для юного финансового гения."
        },
        new ShopItem
        {
            Id = "desk_artisan",
            Name = "Творческий стол",
            Category = ExpenseCategory.Interior,
            Price = 220,
            HungerBoost = 0,
            MoodBoost = 45,
            IconEmoji = "",
            IconImage = "desk_artisan.png",
            LinkedDesk = PetDeskType.Artisan,
            ThanksText = "Муррр! Творческая мастерская открыта! Будем рисовать графики накоплений!",
            Description = "Палитра, краски и холст для создания творческих шедевров."
        },
        new ShopItem
        {
            Id = "desk_market",
            Name = "Лавка предпринимателя",
            Category = ExpenseCategory.Interior,
            Price = 250,
            HungerBoost = 0,
            MoodBoost = 50,
            IconEmoji = "",
            IconImage = "desk_market.png",
            LinkedDesk = PetDeskType.Market,
            ThanksText = "Муррр! Наш первый стартап начинает работу! Добро пожаловать в лавку Финни!",
            Description = "Витрина, монеты и касса своего первого настоящего бизнеса."
        },
        new ShopItem
        {
            Id = "desk_maker",
            Name = "Верстак инженера",
            Category = ExpenseCategory.Interior,
            Price = 280,
            HungerBoost = 0,
            MoodBoost = 55,
            IconEmoji = "",
            IconImage = "desk_maker.png",
            LinkedDesk = PetDeskType.Maker,
            ThanksText = "Муррр! Шестерёнки крутятся, механизм работает! Отличный верстак!",
            Description = "Шестерни, чертежи и инструменты юного изобретателя."
        },
        new ShopItem
        {
            Id = "desk_reading",
            Name = "Кабинет профессора",
            Category = ExpenseCategory.Interior,
            Price = 300,
            HungerBoost = 0,
            MoodBoost = 60,
            IconEmoji = "",
            IconImage = "desk_reading.png",
            LinkedDesk = PetDeskType.Reading,
            ThanksText = "Муррр! Какая библиотека! Читаю умные книги по экономике и финансам!",
            Description = "Книги, старинный глобус и свитки финансовой мудрости."
        },
        new ShopItem
        {
            Id = "desk_botanical",
            Name = "Эко-стол биолога",
            Category = ExpenseCategory.Interior,
            Price = 240,
            HungerBoost = 0,
            MoodBoost = 45,
            IconEmoji = "",
            IconImage = "desk_botanical.png",
            LinkedDesk = PetDeskType.Botanical,
            ThanksText = "Муррр! Растения радуют зеленью, как растущие сбережения в копилке!",
            Description = "Комнатные растения, пробирки и лейка."
        },
        new ShopItem
        {
            Id = "platform_stars",
            Name = "Подиум «Звёздная дорожка»",
            Category = ExpenseCategory.Interior,
            Price = 0,
            HungerBoost = 0,
            MoodBoost = 35,
            IconEmoji = "",
            IconImage = "ic_stat_mood.png",
            LinkedPlatform = PetPlatformType.Stars,
            ThanksText = "Муррр! Золотой пьедестал сияет звёздами! Финни — звезда финансов!",
            Description = "Сияющий золотой кристалл со мерцающими звёздами."
        },
        new ShopItem
        {
            Id = "platform_cloud",
            Name = "Подиум «Облако накоплений»",
            Category = ExpenseCategory.Interior,
            Price = 0,
            HungerBoost = 0,
            MoodBoost = 40,
            IconEmoji = "",
            IconImage = "ic_shop_bed.png",
            LinkedPlatform = PetPlatformType.Cloud,
            ThanksText = "Муррр! Мягко, словно на настоящем небесном облачке сбережений!",
            Description = "Небесно-голубой подиум, дарящий ощущение лёгкости."
        },
        new ShopItem
        {
            Id = "platform_emerald",
            Name = "Подиум «Изумрудный кристалл»",
            Category = ExpenseCategory.Interior,
            Price = 0,
            HungerBoost = 0,
            MoodBoost = 45,
            IconEmoji = "",
            IconImage = "ic_stat_hunger.png",
            LinkedPlatform = PetPlatformType.Emerald,
            ThanksText = "Муррр! Изумрудный кристалл заряжает копилку неоновой энергией!",
            Description = "Неоновый изумрудно-мятный свет для стильной комнаты Финни."
        },
        new ShopItem
        {
            Id = "platform_cosmic",
            Name = "Подиум «Космический неон»",
            Category = ExpenseCategory.Interior,
            Price = 0,
            HungerBoost = 0,
            MoodBoost = 50,
            IconEmoji = "",
            IconImage = "ic_shop_drone.png",
            LinkedPlatform = PetPlatformType.Cosmic,
            ThanksText = "Муррр! Кибер-подиум из будущего! Летим к звёздным целям!",
            Description = "Футуристический кибер-подиум с пульсирующей подсветкой."
        }
    };

    // 36 интерактивных заданий: по 18 на каждую возрастную группу (п. 2.5.8 и 2.6 ТЗ)
    // Составлены строго по Единой рамке компетенций Минфина РФ и Банка России
    public static List<FinancialTask> GetFinancialTasks(AgeGroup? age = null)
    {
        var allTasks = new List<FinancialTask>
        {
            // ==========================================
            // МЛАДШАЯ ГРУППА: 7–8 ЛЕТ (1–2 КЛАСС, 18 ЗАДАЧ)
            // ==========================================
            new FinancialTask
            {
                Id = "junior_task_1",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Хочу или Надо: поход в буфет 🥪",
                CompetencyReference = "Единая рамка компетенций, п. 6.2: Различение необходимого и желаемого",
                ScenarioDescription = "На перемене ты проголодался. Горячий сытный пирожок стоит 25 монет, а рядом лежит брелок с котиком за 25 монет. В кармане ровно 30 монет. Что правильнее купить в первую очередь?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Купить пирожок (Надо!), чтобы подкрепиться, а покупку брелока отложить",
                        IsCorrect = true,
                        Explanation = "Отлично! Еда и здоровье — это первоочередные обязательные потребности. Без брелока можно обойтись, а силы для учёбы нужны прямо сейчас!",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Купить брелок (Хочу!), а на уроке сидеть голодным",
                        IsCorrect = false,
                        Explanation = "Если потратить деньги на игрушку вместо еды, заболит живот и будет трудно учиться. Сначала удовлетворяем обязательные потребности!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_2",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Три конверта для карманных денег ✉️",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Личный бюджет и метод конвертов",
                ScenarioDescription = "Родители выдали тебе 30 монет на неделю. Как распределить их по правилу трёх конвертов?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "15 монет — на обеды (Обязательное), 10 монет — на сладости (Желания), 5 монет — в копилку (Сбережения)",
                        IsCorrect = true,
                        Explanation = "Блестяще! Это классическое распределение личного бюджета: хватает и на нужды, и на маленькие радости, и на будущую мечту!",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Потратить все 30 монет в первый же день на аттракционы",
                        IsCorrect = false,
                        Explanation = "Тогда в остальные дни недели у тебя не останется монет даже на школьный обед. Деньги нужно распределять на весь период!",
                        RewardCoins = 0
                    },
                    new TaskOption
                    {
                        Text = "Спрятать все 30 монет под подушку и ничего не покупать",
                        IsCorrect = false,
                        Explanation = "Сбережения важны, но если совсем не покупать еду, питомец и ты останетесь без сил. Бюджет должен быть сбалансированным!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_3",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Копилка на мечту: шаг за шагом 🐷",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Формирование регулярных сбережений",
                ScenarioDescription = "Ты мечтаешь о новом конструкторе за 100 монет. Каждую неделю у тебя остается 20 монет. Как быстрее и надежнее достичь цели?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Регулярно откладывать по 20 монет в копилку каждую неделю и не брать оттуда на мелочи",
                        IsCorrect = true,
                        Explanation = "Супер! Регулярность — главный секрет сбережений. Ровно через 5 недель конструктор будет твоим!",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Каждый раз покупать мороженое, надеясь, что 100 монет накопятся сами",
                        IsCorrect = false,
                        Explanation = "Если тратить остаток на сиюминутные желания, копилка останется пустой. Мечта требует дисциплины!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_4",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "В супермаркет со списком покупок 📝",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Принятие решений в условиях ограниченного бюджета",
                ScenarioDescription = "Мама отправила тебя в магазин за хлебом и молоком, дав список и 50 монет. Возле кассы стоят яркие леденцы. Как поступить?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Строго купить хлеб и молоко по списку, а сдачу вернуть маме",
                        IsCorrect = true,
                        Explanation = "Умница! Список покупок защищает от импульсивных трат и помогает сохранить семейный бюджет.",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Вместо молока купить пять леденцов, потому что они такие яркие",
                        IsCorrect = false,
                        Explanation = "Семья останется без молока для каши, а мама расстроится. Нельзя нарушать поручение и список покупок!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_5",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Считаем сдачу у кассы 🧮",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Расчеты наличными деньгами и проверка сдачи",
                ScenarioDescription = "Тетрадь стоит 15 монет. Ты протянул кассиру монету достоинством 20 монет. Сколько сдачи тебе обязаны вернуть?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Ровно 5 монет",
                        IsCorrect = true,
                        Explanation = "Точно! 20 - 15 = 5 монет сдачи. Всегда полезно пересчитывать сдачу прямо у кассы!",
                        RewardCoins = 50
                    },
                    new TaskOption
                    {
                        Text = "10 монет",
                        IsCorrect = false,
                        Explanation = "Давай посчитаем вместе: 20 минус 15 получается 5 монет, а не 10.",
                        RewardCoins = 0
                    },
                    new TaskOption
                    {
                        Text = "Ничего, ведь покупка сделана",
                        IsCorrect = false,
                        Explanation = "Кассир обязан вернуть разницу между внесенной суммой и стоимостью товара. Сдача принадлежит покупателю!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_6",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Секрет твоего кошелька во дворе 🛡️",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность и защита карманных денег",
                ScenarioDescription = "Во дворе незнакомый подросток говорит: «Покажи, сколько у тебя монеток в кошельке, я фокус покажу и удвою их!». Что ты сделаешь?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Не доставать кошелёк, твёрдо сказать «Нет!» и сразу подойти к родителям или друзьям",
                        IsCorrect = true,
                        Explanation = "Молодец! Никогда не показывай деньги посторонним людям на улице. Деньги любят тишину и безопасность!",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Отдать кошелёк незнакомцу и ждать чуда",
                        IsCorrect = false,
                        Explanation = "Очень опасно! Незнакомец может убежать с твоими деньгами. Доверять свои деньги чужим людям нельзя!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_7",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Чужая банковская карта на площадке 💳",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Финансовая безопасность и правовые нормы",
                ScenarioDescription = "На детской площадке у качелей ты увидел лежащую банковскую карту. Твой знакомый предлагает попробовать купить по ней чипсы. Как поступить правильно?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Ни в коем случае не тратить чужие деньги! Отдать карту родителям или отнести администратору парка",
                        IsCorrect = true,
                        Explanation = "Абсолютно верно! Тратить деньги с чужой карты — это серьезное правонарушение. Карту нужно передать взрослым, чтобы вернуть владельцу.",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Пойти в магазин и попытаться приложить чужую карту к терминалу",
                        IsCorrect = false,
                        Explanation = "Чужая карта — это чужая собственность! Использование чужой карты незаконно и может повлечь серьезные проблемы с полицией.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_8",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Береги школьные вещи 🎒",
                CompetencyReference = "Единая рамка компетенций, п. 6.2: Бережное отношение к вещам как экономия бюджета",
                ScenarioDescription = "Почему аккуратное отношение к школьному рюкзаку, куртке и учебникам помогает твоей семье экономить деньги?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Потому что родителям не придется покупать новые вещи взамен испорченных, а сэкономленные деньги пойдут на отдых и мечту",
                        IsCorrect = true,
                        Explanation = "Замечательно! Бережливость — это тоже форма заработка. Сохраненная вещь бережет семейные финансы!",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Вещи не стоят денег, их можно ломать каждый день",
                        IsCorrect = false,
                        Explanation = "Каждая вещь покупается на заработанные родителями деньги. Небрежность приводит к лишним семейным тратам.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_9",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Секретный ПИН-код детской карты 🤫",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Конфиденциальность платежных реквизитов",
                ScenarioDescription = "Родители оформили тебе детскую карту для школьных обедов. Где правильнее всего хранить 4-значный ПИН-код?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Запомнить наизусть в голове и никому не называть, даже друзьям",
                        IsCorrect = true,
                        Explanation = "Верно! ПИН-код — это главный ключ к твоим деньгам. Его нельзя никому говорить и нельзя писать прямо на карте!",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Написать маркером прямо на лицевой стороне карты",
                        IsCorrect = false,
                        Explanation = "Если карту украдут или ты ее потеряешь, нашедший сможет сразу снять все деньги. Писать ПИН на карте категорически нельзя!",
                        RewardCoins = 0
                    },
                    new TaskOption
                    {
                        Text = "Громко диктовать ПИН-код на весь класс",
                        IsCorrect = false,
                        Explanation = "ПИН-код — строгая тайна. Назвав его вслух, ты рискуешь потерять свои сбережения.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_10",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Игрушка-ловушка у кассы 🍭",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Маркетинговые уловки и спонтанные покупки",
                ScenarioDescription = "Почему в магазинах самые яркие конфеты и маленькие игрушки кладут прямо перед кассой на уровне детских глаз?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Это уловка магазина: пока стоишь в очереди, хочется схватить мелочь без раздумий",
                        IsCorrect = true,
                        Explanation = "Отличная наблюдательность! Это метод импульсивных продаж. Зная об этом, ты легко откажешься от ненужной траты.",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Их туда кладут случайно, потому что на других полках кончилось место",
                        IsCorrect = false,
                        Explanation = "Расположение товаров в магазине тщательно планируют маркетологи, чтобы спровоцировать спонтанную покупку.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_11",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Откуда берутся деньги в семье? 💼",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Источники доходов и ценность труда",
                ScenarioDescription = "Откуда в семейном кошельке и на банковских картах родителей появляются деньги?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Родители получают заработную плату за свой ежедневный полезный труд и знания",
                        IsCorrect = true,
                        Explanation = "Именно так! Деньги — это вознаграждение за выполненную работу, а банкомат лишь выдает то, что было заработано.",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Банкомат печатает сколько угодно денег бесплатно по нажатию кнопки",
                        IsCorrect = false,
                        Explanation = "Банкомат не производит деньги. Он выдает только те средства, которые родители заработали и положили на счет.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_12",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Сравнение цен на цветные карандаши ✏️",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Сравнение предложений и выбор выгодной цены",
                ScenarioDescription = "Тебе нужен набор из 12 карандашей для рисования. В киоске у школы он стоит 40 монет, а в канцелярском магазине за углом — 25 монет. Где лучше купить?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Дойти до магазина за углом и купить за 25 монет: так ты сохранишь 15 монет в кармане!",
                        IsCorrect = true,
                        Explanation = "Умный выбор! Сравнение цен в разных местах позволяет экономить существенные суммы без потери качества.",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Купить в киоске за 40 монет, ведь лень делать лишние 30 шагов",
                        IsCorrect = false,
                        Explanation = "Переплачивать 15 монет за лень неразумно. На эти сэкономленные монетки можно накормить питомца!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_13",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Срочный случай: потерялся проездной 🚌",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Непредвиденные расходы и личный резерв",
                ScenarioDescription = "После школы ты обнаружил, что забыл дома проездной билет на автобус. Какая полезная привычка выручит тебя в такой ситуации?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Всегда иметь при себе неприкосновенный запас на 1 поездку (20 монет) в потайном кармашке",
                        IsCorrect = true,
                        Explanation = "Молодец! Это твой личный детский резерв безопасности. Он спасает в непредвиденных ситуациях и позволяет спокойно добраться домой.",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Тратить все карманные деньги до последней копейки сразу в буфете",
                        IsCorrect = false,
                        Explanation = "Если потратить всё до нуля, при любой неожиданности ты останешься без возможности оплатить проезд.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_14",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Игра на планшете просит монеты 🎮",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность встроенных покупок в цифровых играх",
                ScenarioDescription = "В мобильной игре появилось яркое окно: «Нажми зеленую кнопку, чтобы получить супер-меч за 99 рублей!». Карта привязана к аккаунту мамы. Что делать?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Не нажимать кнопку покупки! Сначала подойти к маме и спросить разрешения",
                        IsCorrect = true,
                        Explanation = "Правильно! Встроенные покупки списывают настоящие деньги родителей с карты. Покупать что-то в играх можно только с их согласия.",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Быстро нажимать кнопку много раз, пока игра не залагала",
                        IsCorrect = false,
                        Explanation = "Каждое нажатие спишет реальные деньги со счета родителей, что приведет к неприятностям и потере семейного бюджета!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_15",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Угощение для друга: щедрость и баланс 🍏",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Планирование личных трат и социальное поведение",
                ScenarioDescription = "Друг забыл дома яблоко и просит тебя купить ему в столовой обед на все твои карманные деньги (50 монет). Как поступить мудро?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Купить сытный обед себе, а друга угостить булочкой или соком из свободных монет на радости",
                        IsCorrect = true,
                        Explanation = "Мудрое и доброе решение! Ты проявил дружбу и заботу, но не оставил себя голодным и сохранил бюджет.",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Отдать другу все деньги, а самому остаться голодным на весь учебный день",
                        IsCorrect = false,
                        Explanation = "Жертвовать своими базовыми потребностями не нужно. Дружбу можно проявить разумным угощением в рамках бюджета.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_16",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Кассир ошибся в твою пользу 🪙",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Честность в расчетах и финансовая этика",
                ScenarioDescription = "В магазине продавец по ошибке дал тебе сдачу 50 монет вместо 20 монет. Заметил это только ты. Как поступить честно?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Сразу сказать кассиру об ошибке и вернуть лишние 30 монет",
                        IsCorrect = true,
                        Explanation = "Браво! Честность — фундамент финансовой культуры. Кассиру пришлось бы вкладывать свои личные деньги в кассу при недостаче.",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Быстро убежать с лишними деньгами и радоваться",
                        IsCorrect = false,
                        Explanation = "Чужие деньги счастья не принесут. При вечернем отчете у продавца обнаружат недостачу и накажут за ошибку.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_17",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Свет и вода дома — это тоже деньги 💡",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Коммунальные платежи и бережное потребление",
                ScenarioDescription = "Почему полезно выключать свет в пустой комнате и закрывать кран, когда чистишь зубы?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Семья платит по счетчикам за каждый киловатт и литр, экономия снижает расходы на коммуналку",
                        IsCorrect = true,
                        Explanation = "Прекрасно! Бережное отношение к воде и электричеству снижает счета за квартиру, оставляя больше средств на семейный отдых.",
                        RewardCoins = 60
                    },
                    new TaskOption
                    {
                        Text = "Вода и электричество в квартире абсолютно бесплатны и безлимитны",
                        IsCorrect = false,
                        Explanation = "Коммунальные услуги — это одна из крупнейших статей обязательных расходов семьи, которая оплачивается по счетчикам.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "junior_task_18",
                TargetAge = AgeGroup.Junior7_8,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Разбитая копилка или терпение? ⏳",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Достижение долгосрочных целей и самоконтроль",
                ScenarioDescription = "Ты копишь на классный самокат за 150 монет, накопил уже 80 монет. В магазине увидел светящийся спиннер за 80 монет. Что посоветует грамотный финансист?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Вспомнить о своей большой мечте (самокат!), пройти мимо спиннера и продолжить копить",
                        IsCorrect = true,
                        Explanation = "Настоящая финансовая стойкость! Умение отложить минутное желание ради большой цели отличает успешного человека.",
                        RewardCoins = 70
                    },
                    new TaskOption
                    {
                        Text = "Спустить все накопленные 80 монет на спиннер и начать копить на самокат с нуля",
                        IsCorrect = false,
                        Explanation = "Спиннер надоест через пару дней, а мечта о самокате отодвинется на многие месяцы назад!",
                        RewardCoins = 0
                    },
                }
            },

            // ==========================================
            // СТАРШАЯ ГРУППА: 9–11 ЛЕТ (3–5 КЛАСС, 18 ЗАДАЧ)
            // ==========================================
            new FinancialTask
            {
                Id = "senior_task_1",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Золотое правило 50 / 30 / 20 📊",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Назначение личного бюджета и баланс расходов",
                ScenarioDescription = "Тебе выделили 100 монет карманных денег на месяц. Как грамотнее всего распределить их по популярной системе личных финансов?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "50 монет — обязательные нужды, 30 — желания и досуг, 20 — в неприкосновенные сбережения",
                        IsCorrect = true,
                        Explanation = "Идеально! Это классическая формула 50/30/20: базовые потребности защищены, радости присутствуют, а капитал стабильно растет.",
                        RewardCoins = 100
                    },
                    new TaskOption
                    {
                        Text = "Сразу потратить все 100 монет в первые выходные на фастфуд и игры",
                        IsCorrect = false,
                        Explanation = "Такой подход приведет к финансовому голоду до конца месяца. Без бюджета деньги исчезают мгновенно!",
                        RewardCoins = 0
                    },
                    new TaskOption
                    {
                        Text = "Запереть все 100 монет и отказывать себе даже в базовых нуждах",
                        IsCorrect = false,
                        Explanation = "Бюджет должен служить комфортной жизни. Чрезмерная скупость ведет к срывам и ухудшению качества жизни.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_2",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Ловушка акции «Второй за полцены» 🏷️",
                CompetencyReference = "Единая рамка компетенций, п. 6.2: Различение необходимого и желаемого, маркетинговые уловки",
                ScenarioDescription = "Пачка витаминов стоит 60 монет. По акции вторую пачку отдают за 30 монет. Но срок годности истекает через 3 дня, а питомцу хватит одной пачки на месяц. Выгодно ли брать вторую?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Нет! Вторая пачка испортится и отправится в мусор. Это не экономия, а лишний расход 30 монет",
                        IsCorrect = true,
                        Explanation = "Блестящий экономический анализ! Скидка на ненужный или портящийся товар — это чистый убыток для бюджета.",
                        RewardCoins = 110
                    },
                    new TaskOption
                    {
                        Text = "Да, ведь скидка целых 50%! Нужно всегда покупать всё, на что объявлена скидка",
                        IsCorrect = false,
                        Explanation = "Маркетологи часто используют скидки, чтобы сбыть товар с истекающим сроком. Деньги будут выброшены на ветер.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_3",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Подушка безопасности для бюджета 🛡️",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Формирование сбережений и резерва на непредвиденный случай",
                ScenarioDescription = "Финансовые консультанты рекомендуют иметь «подушку безопасности» в размере 3–6 месяцев обязательных расходов семьи. Зачем она нужна?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Это неприкосновенный резерв на случай форс-мажора (болезнь, ремонт, поиск работы), спасающий от долгов и кредитов",
                        IsCorrect = true,
                        Explanation = "В точку! Подушка безопасности обеспечивает психологическое спокойствие и независимость в любых кризисных ситуациях.",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Это деньги, которые надо потратить на самый дорогой новогодний фейерверк",
                        IsCorrect = false,
                        Explanation = "Резервный фонд нельзя тратить на развлечения. Его цель — защита от непредвиденных бедствий.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_4",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Сравнение цены за грамм товара ⚖️",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Принятие решений в условиях ограниченного бюджета",
                ScenarioDescription = "В зоомагазине две пачки корма: 200г за 60 монет (30 монет за 100г) и большая 500г за 120 монет (24 монеты за 100г). Какая покупка выгоднее при регулярном питании котика?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Большая пачка (500г): в пересчете на единицу веса она на 20% экономичнее!",
                        IsCorrect = true,
                        Explanation = "Отличный расчет! Сравнение удельной цены (за 100г или 1кг) — ключевой навык грамотного покупателя.",
                        RewardCoins = 110
                    },
                    new TaskOption
                    {
                        Text = "Маленькая пачка, потому что ценник 60 монет меньше, чем 120",
                        IsCorrect = false,
                        Explanation = "Хотя чек меньше прямо сейчас, каждый грамм корма обходится дороже. При регулярном потреблении это ведет к переплате.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_5",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Звонок от «Службы безопасности» 📞",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Защита от финансового мошенничества и социальная инженерия",
                ScenarioDescription = "Незнакомец звонит с неизвестного номера и тревожно говорит: «Ваш счет заблокирован подозрительным переводом! Срочно продиктуйте код из СМС, чтобы спасти средства!». Твои действия?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Немедленно сбросить звонок! Рассказать родителям. Никому и никогда не называть коды подтверждения из СМС",
                        IsCorrect = true,
                        Explanation = "Превосходная кибербдительность! Настоящие сотрудники банков никогда не спрашивают секретные коды подтверждения из СМС.",
                        RewardCoins = 130
                    },
                    new TaskOption
                    {
                        Text = "Быстро назвать код из СМС, испугавшись блокировки",
                        IsCorrect = false,
                        Explanation = "Это фатальная ошибка! Код из СМС дает мошенникам доступ к списанию денег со счета.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_6",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Фишинг: бесплатные монеты в онлайн-игре 🎣",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность в цифровой среде и защита персональных данных",
                ScenarioDescription = "В чате популярной онлайн-игры прислали ссылку: «Раздача бесплатных робуксов и монет! Перейди по ссылке, введи логин, пароль и телефон мамы». Что делать?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Закрыть сообщение и пожаловаться модераторам! Это фишинг для кражи игрового аккаунта и денег",
                        IsCorrect = true,
                        Explanation = "Абсолютно верно! Бесплатный сыр бывает только в мышеловке мошенников. Не переходи по подозрительным ссылкам!",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Перейти по ссылке и ввести все данные, чтобы получить приз",
                        IsCorrect = false,
                        Explanation = "Ты потеряешь доступ к своему аккаунту, а с привязанного телефона родителей мошенники спишут деньги.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_7",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Сложный процент: денежный снежный ком ❄️",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Банковский вклад, капитализация и сложный процент",
                ScenarioDescription = "Ты положил 100 монет на вклад под 10% годовых с ежегодной капитализацией (сложным процентом). В конце 1 года стало 110 монет. Сколько начислит банк в конце 2 года?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "11 монет (10% от 110 монет), и на счете станет 121 монета: процент начисляется и на ранее заработанные проценты!",
                        IsCorrect = true,
                        Explanation = "Гениально! В этом и состоит магия сложного процента (капитализации): проценты начисляются на всю сумму вместе с предыдущей прибылью!",
                        RewardCoins = 130
                    },
                    new TaskOption
                    {
                        Text = "Снова ровно 10 монет, сумма процентов никогда не меняется",
                        IsCorrect = false,
                        Explanation = "При капитализации процент начисляется на увеличившуюся сумму, поэтому с каждым годом доход растет быстрее!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_8",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Инфляция: почему деньги худеют? 📉",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Покупательная способность денег и влияние инфляции",
                ScenarioDescription = "Пять лет назад на 100 рублей можно было купить 5 шоколадок, а сегодня только 2 такие же шоколадки. Какое экономическое явление это объясняет?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Инфляция: общий рост цен снижает покупательную способность денег со временем",
                        IsCorrect = true,
                        Explanation = "Совершенно верно! Инфляция обесценивает наличные под подушкой. Именно поэтому сбережения важно держать на доходных вкладах.",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Шоколад стал в 2 раза тяжелее и крупнее",
                        IsCorrect = false,
                        Explanation = "Шоколадки остались прежними, но из-за инфляции ценность денежной единицы снизилась.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_9",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Первый стартап: выручка и чистая прибыль 🍋",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Основы предпринимательства, доходы и себестоимость",
                ScenarioDescription = "Ты открыл лимонадную стойку. На лимоны, сахар и стаканчики потратил 40 монет. За день продал лимонада на 100 монет. Какова твоя чистая прибыль?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "60 монет (Чистая прибыль = Выручка 100 - Себестоимость 40)",
                        IsCorrect = true,
                        Explanation = "Блестяще! Юный предприниматель должен четко отличать выручку (все полученные деньги) от чистой прибыли (то, что осталось после оплаты всех затрат)!",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Все 100 монет являются чистой прибылью",
                        IsCorrect = false,
                        Explanation = "Нельзя забывать о расходах! 40 монет ушло на покупку ингредиентов, поэтому чистый заработок составил 60 монет.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_10",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Оплата по QR-коду и СБП в кафе 📲",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Цифровые платежи и безналичные расчеты",
                ScenarioDescription = "В кафе ты оплачиваешь перекус через камеру смартфона по QR-коду Системы быстрых платежей (СБП). Что обязательно нужно сделать перед подтверждением оплаты?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Внимательно сверить название заведения и итоговую сумму на экране смартфона с чеком",
                        IsCorrect = true,
                        Explanation = "Очень грамотно! Всегда проверяй сумму и наименование получателя перед отправкой платежа, чтобы избежать ошибочного списания.",
                        RewardCoins = 110
                    },
                    new TaskOption
                    {
                        Text = "Не глядя нажать кнопку «Оплатить», не проверяя сумму",
                        IsCorrect = false,
                        Explanation = "В QR-коде может быть зашита неверная сумма или чужой счет. Проверка экрана — обязательное правило безопасности.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_11",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Общественный Wi-Fi в торговом центре 📶",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Кибергигиена в общественных сетях",
                ScenarioDescription = "Ты сидишь в фудкорте и подключился к открытой сети Wi-Fi без пароля. Безопасно ли заходить в мобильный банк или совершать онлайн-покупки через такую сеть?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Нет! Открытые общественные сети уязвимы для перехвата паролей и платежных данных. Лучше использовать мобильный интернет",
                        IsCorrect = true,
                        Explanation = "Превосходное понимание кибербезопасности! Открытые Wi-Fi сети часто служат ловушкой для перехвата учетных записей.",
                        RewardCoins = 130
                    },
                    new TaskOption
                    {
                        Text = "Да, открытый Wi-Fi полностью защищен от любых хакеров",
                        IsCorrect = false,
                        Explanation = "Открытые сети передают данные в незашифрованном виде, что позволяет злоумышленникам перехватывать информацию.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_12",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Покупка с рук в интернете: предоплата 📦",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность онлайн-шопинга и защита от обмана",
                ScenarioDescription = "На сайте объявлений продавец предлагает редкую приставку в 3 раза дешевле рынка, но требует: «Переведи мне 100% денег на карту прямо сейчас, а посылку я отправлю завтра». Как поступить?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Отказаться от сделки или использовать только безопасную сделку сервиса с оплатой при получении",
                        IsCorrect = true,
                        Explanation = "Именно так! Требование прямой предоплаты на карту от анонимного продавца — верный признак мошенничества.",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Срочно перевести деньги на карту, чтобы не упустить огромную скидку",
                        IsCorrect = false,
                        Explanation = "После перевода мошенник удалит аккаунт и выключит телефон, а ты останешься и без денег, и без приставки.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_13",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Кассовый чек и гарантия на товар 🧾",
                CompetencyReference = "Единая рамка компетенций, п. 6.3: Защита прав потребителей и подтверждение покупки",
                ScenarioDescription = "Ты купил наушники, но дома обнаружил, что один динамик не работает. Зачем при обращении в магазин тебе потребуется чек или электронная квитанция?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Чек доказывает факт и дату покупки именно в этом магазине, давая право на обмен или возврат денег по закону",
                        IsCorrect = true,
                        Explanation = "Верно! Закон о защите прав потребителей гарантирует замену бракованного товара при наличии доказательства покупки.",
                        RewardCoins = 100
                    },
                    new TaskOption
                    {
                        Text = "Чек нужен просто чтобы сложить из него бумажный самолетик",
                        IsCorrect = false,
                        Explanation = "Чек — официальный платежный документ, защищающий твои права как покупателя.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_14",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Опасность кредитов и «Купи сейчас — плати потом» 💳",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Кредитование, проценты за пользование чужими деньгами",
                ScenarioDescription = "Почему брать кредит или заем на покупку развлечений и модных гаджетов часто приводит к финансовым проблемам?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Потому что отдавать придется намного больше из-за процентов, а долг ограничивает будущий доход семьи",
                        IsCorrect = true,
                        Explanation = "Взрослый и зрелый вывод! Заемные деньги — это чужие деньги, которые забирают кусок твоего будущего дохода.",
                        RewardCoins = 120
                    },
                    new TaskOption
                    {
                        Text = "Кредитные деньги не нужно возвращать банку",
                        IsCorrect = false,
                        Explanation = "Банк начисляет штрафы и пени за каждый день просрочки. Невозврат кредита портит кредитную историю на всю жизнь.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_15",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.PaymentsAndSecurity,
                Title = "Секретный код CVC/CVV на обороте карты 🔒",
                CompetencyReference = "Единая рамка компетенций, п. 6.5: Безопасность пластиковых карт и реквизиты",
                ScenarioDescription = "Друг в чате просит: «Сфотографируй свою карту с двух сторон, я хочу посмотреть дизайн». Можно ли отправлять фото обратной стороны карты?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Категорически нет! 3-значный код безопасности на обороте позволяет совершать покупки в интернете без твоего согласия",
                        IsCorrect = true,
                        Explanation = "Блестящая бдительность! Код CVC/CVV и срок действия карты нельзя показывать никому, даже лучшим друзьям.",
                        RewardCoins = 130
                    },
                    new TaskOption
                    {
                        Text = "Конечно можно, на обороте карты нет ничего секретного",
                        IsCorrect = false,
                        Explanation = "Зная номер, срок действия и CVC-код, любой человек может потратить все деньги с карты в онлайн-магазинах.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_16",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Незаметные микротраты: подписки и сервисы 📱",
                CompetencyReference = "Единая рамка компетенций, п. 6.1: Учет мелких регулярных списаний в бюджете",
                ScenarioDescription = "Ты оформил бесплатный пробный период на 5 онлайн-сервисов (музыка, кино, игры), забыв их отключить. Через месяц со счета списалось 1500 рублей. Как защитить себя от таких утечек бюджета?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Вести список подписок, ставить напоминание в календаре за день до окончания промо-периода и отключать ненужные",
                        IsCorrect = true,
                        Explanation = "Прекрасный организационный навык! Регулярный аудит цифровых подписок бережет тысячи рублей в год.",
                        RewardCoins = 110
                    },
                    new TaskOption
                    {
                        Text = "Просто не смотреть историю списаний в банковском приложении",
                        IsCorrect = false,
                        Explanation = "Игнорирование проблемы ведет к постоянной утечке денег на сервисы, которыми ты даже не пользуешься.",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_17",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.BudgetPlanning,
                Title = "Донаты любимому стримеру или блогеру 🎥",
                CompetencyReference = "Единая рамка компетенций, п. 6.2: Осознанные расходы в цифровой среде",
                ScenarioDescription = "Во время прямой трансляции стример призывает зрителей присылать платные донаты, чтобы попасть на экран. Как к этому относиться с точки зрения личных финансов?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "Это необязательное развлечение (категория Желания): отправлять можно только заранее выделенную небольшую сумму, не трогая копилку",
                        IsCorrect = true,
                        Explanation = "Отличный баланс! Поддерживать любимых авторов приятно, но это должно строго укладываться в лимит расходов на досуг.",
                        RewardCoins = 110
                    },
                    new TaskOption
                    {
                        Text = "Отправить стримеру все накопления из копилки, чтобы услышать свое имя в эфире на 3 секунды",
                        IsCorrect = false,
                        Explanation = "Ради минутного упоминания ты лишишься всех денег, которые копил месяцами на свою реальную мечту!",
                        RewardCoins = 0
                    },
                }
            },
            new FinancialTask
            {
                Id = "senior_task_18",
                TargetAge = AgeGroup.Senior9_11,
                Topic = TaskTopic.SavingsAndReserve,
                Title = "Финансовая цель по правилу SMART 🎯",
                CompetencyReference = "Единая рамка компетенций, п. 6.4: Целеполагание и планирование сбережений",
                ScenarioDescription = "Какая из этих формулировок является грамотно поставленной финансовой целью?",
                Options = new()
                {
                    new TaskOption
                    {
                        Text = "«Накопить 300 монет за 3 месяца, откладывая по 25 монет в неделю из карманных денег, для покупки микроскопа»",
                        IsCorrect = true,
                        Explanation = "Браво! Цель конкретна, измерима, достижима и ограничена по времени (критерии SMART). Именно такие цели достигаются на 100%!",
                        RewardCoins = 140
                    },
                    new TaskOption
                    {
                        Text = "«Хочу когда-нибудь стать очень богатым и купить всё на свете»",
                        IsCorrect = false,
                        Explanation = "Это абстрактная мечта без сроков, цифр и конкретного плана действий. Она не приводит к реальному результату.",
                        RewardCoins = 0
                    },
                }
            },
        };

        if (age.HasValue)
        {
            return allTasks.FindAll(t => t.TargetAge == age.Value);
        }
        return allTasks;
    }

    // Цели накопления (п. 2.6 ТЗ): мебель, подиумы и желанные игрушки
    public static List<FinancialGoal> GetPresetGoals() => new()
    {
        new FinancialGoal
        {
            Id = "goal_desk_modern",
            Title = "Стол IT-финансиста",
            TargetAmount = 200,
            IconEmoji = "",
            IconImage = "desk_modern.png",
            LinkedDesk = PetDeskType.Modern,
            Description = "Ноутбук, лампа и мониторы для цифрового банкинга и учета финансов."
        },
        new FinancialGoal
        {
            Id = "goal_desk_artisan",
            Title = "Творческий стол художника",
            TargetAmount = 220,
            IconEmoji = "",
            IconImage = "desk_artisan.png",
            LinkedDesk = PetDeskType.Artisan,
            Description = "Мольберт, краски и палитра для творческих финансовых проектов."
        },
        new FinancialGoal
        {
            Id = "goal_desk_market",
            Title = "Лавка предпринимателя",
            TargetAmount = 250,
            IconEmoji = "",
            IconImage = "desk_market.png",
            LinkedDesk = PetDeskType.Market,
            Description = "Своя собственная торговая лавка: витрина, сладости и первая касса!"
        },
        new FinancialGoal
        {
            Id = "goal_toy_laser",
            Title = "Лазерная указка-дразнилка",
            TargetAmount = 140,
            IconEmoji = "",
            IconImage = "ic_shop_laser.png",
            LinkedShopItemId = "toy_laser",
            Description = "Яркий луч света для весёлых прыжков и тренировки реакции Финни."
        },
        new FinancialGoal
        {
            Id = "goal_desk_reading",
            Title = "Кабинет профессора",
            TargetAmount = 300,
            IconEmoji = "",
            IconImage = "desk_reading.png",
            LinkedDesk = PetDeskType.Reading,
            Description = "Книги, старинный глобус и свитки для изучения финансовой науки."
        },
        new FinancialGoal
        {
            Id = "goal_toy_drone",
            Title = "Игрушечный мини-коптер",
            TargetAmount = 220,
            IconEmoji = "",
            IconImage = "ic_shop_drone.png",
            LinkedShopItemId = "toy_drone",
            Description = "Крутой летающий дрон с камерой для активных игр с Финни."
        },
        new FinancialGoal
        {
            Id = "goal_toy_bed",
            Title = "Мягкая лежанка-облачко",
            TargetAmount = 140,
            IconEmoji = "",
            IconImage = "ic_shop_bed.png",
            LinkedShopItemId = "toy_bed",
            Description = "Уютное спальное место для сладких снов Финни."
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
