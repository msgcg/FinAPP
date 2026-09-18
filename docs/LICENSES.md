# ВЕДОМОСТЬ ЛИЦЕНЗИЙ И СТОРОННИХ КОМПОНЕНТОВ (ГОСТ 7.32-2017 / ГОСТ Р 54593-2011)

**Программный продукт:** Мобильное игровое обучающее приложение «FinAPP» (Питомец Финни)  
**Назначение:** Обучение школьников 7–11 лет финансовой грамотности в игровой форме  
**Статус соответствия:** 100% лицензионная чистота, отсутствие нарушений исключительных прав третьих лиц, соблюдение условий открытых лицензий (Open Source / Royalty-Free).

---

## 1. Реестр сторонних компонентов и прав использования

| № | Компонент / Ресурс | Правообладатель / Источник | Тип лицензии | Назначение в FinAPP |
|---|--------------------|----------------------------|--------------|---------------------|
| 1 | **Звуковые эффекты интерфейса (SFX)**:<br>• `sfx_money.mp3`<br>• `sfx_success.mp3`<br>• `sfx_error.mp3`<br>• `sfx_meow.mp3`<br>• `sfx_purr.mp3` | Портал звуков «ЗвукиПро» ([zvukipro.com](https://zvukipro.com/user-interface/1961-jelektronnye-zvuki-pokupki-oplaty.html)) | Royalty-Free Free License with Attribution (Бесплатная лицензия с атрибуцией) | Звуковое сопровождение покупок в магазине, пополнения копилки, правильных/неправильных ответов в викторине, реакций маскота Финни на поглаживание и кормление |
| 2 | **Фоновая музыка (BGM)**:<br>• `bgm_idle.mp3` | Портал звуков «ЗвукиПро» ([zvukipro.com](https://zvukipro.com)) | Royalty-Free Free License with Attribution (Бесплатная лицензия с атрибуцией) | Атмосферный игровой саундтрек для создания дружелюбной и вовлекающей обстановки |
| 3 | **Шрифт Montserrat** | Julieta Ulanovsky, Sol Matas, Juan Pablo del Peral, Jacques Le Bailly ([Google Fonts](https://fonts.google.com/specimen/Montserrat)) | SIL Open Font License, Version 1.1 (OFL-1.1) | Основной акцентный и заголовочный шрифт приложения (начертания Bold, SemiBold, Medium) |
| 4 | **Шрифт Open Sans** | Steve Matteson ([Google Fonts](https://fonts.google.com/specimen/Open+Sans)) | Apache License, Version 2.0 | Текстовый шрифт для длинных пояснений, финансового словарика и описания заданий |
| 5 | **Маскот Финни и подиумы** | Авторский эскиз художницы команды + Google Generative AI / Imagen API | Google Terms of Service & Generative AI Additional Terms of Service | Интерактивный персонаж-маскот, анимированный в 3 возрастных стадиях (Малыш, Юниор, Мастер) и 4 эмоциональных состояниях |
| 6 | **Microsoft .NET MAUI / .NET 10** | .NET Foundation and Contributors (Microsoft Corporation) | MIT License | Мультиплатформенный UI-фреймворк и среда выполнения приложения |
| 7 | **AndroidX Core & AppCompat** | The Android Open Source Project (Google LLC) | Apache License, Version 2.0 | Системные компоненты интеграции с Android OS, Splash screen, Activity lifecycle |
| 8 | **xUnit.net Testing Framework** | Brad Wilson, James Newkirk | Apache License, Version 2.0 | Модульное автоматизированное тестирование бизнес-логики и сценариев ТЗ (34/34 тестов) |

---

## 2. Условия использования аудиоресурсов (ЗвукиПро)

Аудиофайлы, задействованные в звуковой подсистеме приложения FinAPP, получены из открытого каталога звуков [ЗвукиПро (zvukipro.com)](https://zvukipro.com):
- **Ссылка на первоисточник:** https://zvukipro.com/
- **Правила лицензирования портала:** материалы предоставляются на безвозмездной основе для свободного использования в некоммерческих и коммерческих мультимедийных проектах, играх и приложениях при условии указания источника (атрибуции).
- **Произведённая адаптация:** исходные аудиодорожки были обработаны с помощью утилиты FFmpeg (нормализация уровней громкости по стандарту EBU R128, точная покадровая обрезка пауз, добавление микро-затуханий fade-in/fade-out для исключения щелчков, сжатие с битрейтом 128 kbps для минимизации размера APK).

---

## 3. Лицензия SIL Open Font License 1.1 (Montserrat)

```text
Copyright 2011 The Montserrat Project Authors (https://github.com/JulietaUla/Montserrat)

This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
http://scripts.sil.org/OFL

Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font Software,
subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.
```

---

## 4. Лицензия Apache License 2.0 (Open Sans, AndroidX, xUnit)

```text
Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
```

---

## 5. Лицензия MIT (Microsoft .NET MAUI)

```text
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors
All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## 6. Декларация об отсутствии нелицензионных элементов и эмодзи

1. В графическом интерфейсе приложения **полностью исключены сторонние системные эмодзи** (Unicode Emoji), которые могли бы вызвать визуальный диссонанс или нарушение корпоративного стиля. Все пиктограммы являются оригинальными векторными фигурами палитры бренда (цвета `#520978`, `#310F53`, `#FF0053`, `#8A83D1`).
2. Вся бизнес-логика (расчет бюджета, симуляция периодов, начисление сложных процентов, валидация PIN-кода, генерация образовательных заданий) разработана авторами с нуля без использования закрытых платных SDK.
3. Доступ к информации о лицензиях обеспечен как в репозитории проекта, так и непосредственно в пользовательском интерфейсе работающего мобильного приложения через раздел «Настройки и управление -> Лицензии сторонних ресурсов».
