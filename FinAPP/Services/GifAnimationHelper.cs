using System;
using System.Threading;

namespace FinAPP.Services;

/// <summary>
/// Помощник для работы с GIF-анимациями Финни.
/// Обеспечивает воспроизведение ровно 1 такта анимации за счёт динамической
/// модификации расширения комментария GIF89a (Comment Extension) перед трейлером 0x3B.
/// Это заставляет браузерный движок Chromium считать изображение уникальным ресурсом
/// и проигрывать его с нулевого кадра до завершающего кадра без зацикливания.
/// </summary>
public static class GifAnimationHelper
{
    private static long s_uniqueCounter = 0;

    /// <summary>
    /// Вставляет блок расширения комментария GIF89a (0x21, 0xFE, 0x08, [8 байт счётчика], 0x00)
    /// непосредственно перед завершающим байтом трейлера GIF (0x3B).
    /// </summary>
    public static byte[] CreateUniqueGifBytes(byte[] originalGif)
    {
        if (originalGif == null || originalGif.Length < 10)
            return originalGif ?? Array.Empty<byte>();

        // Находим завершающий байт трейлера GIF (0x3B) с конца файла
        int trailerIndex = originalGif.Length - 1;
        while (trailerIndex >= 0 && originalGif[trailerIndex] != 0x3B)
        {
            trailerIndex--;
        }

        if (trailerIndex < 0)
        {
            return originalGif;
        }

        long id = Interlocked.Increment(ref s_uniqueCounter);
        byte[] idBytes = BitConverter.GetBytes(id);

        int trailingRemaining = originalGif.Length - 1 - trailerIndex;
        // 13 байт нового блока вместо 1 байта 0x3B = увеличение на 12 байт
        byte[] patched = new byte[originalGif.Length + 12];

        // Копируем всё содержимое до байта трейлера
        Buffer.BlockCopy(originalGif, 0, patched, 0, trailerIndex);

        // Вставляем блок комментария стандарта GIF89a:
        patched[trailerIndex] = 0x21;     // Extension Introducer
        patched[trailerIndex + 1] = 0xFE; // Comment Extension Label
        patched[trailerIndex + 2] = 0x08; // Размер суб-блока: 8 байт
        Buffer.BlockCopy(idBytes, 0, patched, trailerIndex + 3, 8);
        patched[trailerIndex + 11] = 0x00; // Block Terminator (пустой суб-блок)
        patched[trailerIndex + 12] = 0x3B; // GIF Trailer

        if (trailingRemaining > 0)
        {
            Buffer.BlockCopy(originalGif, trailerIndex + 1, patched, trailerIndex + 13, trailingRemaining);
        }

        return patched;
    }

    /// <summary>
    /// Генерирует прозрачный HTML с выравниванием персонажа по левому краю
    /// и указанным Base64 источником данных GIF.
    /// </summary>
    public static string GenerateFinnyHtml(string base64)
    {
        if (string.IsNullOrEmpty(base64)) return string.Empty;

        return $@"<!DOCTYPE html>
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
    }
}
