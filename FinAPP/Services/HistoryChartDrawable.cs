using System;
using System.Collections.Generic;
using System.Linq;
using FinAPP.Models;
using Microsoft.Maui.Graphics;

namespace FinAPP.Services;

public class HistoryChartDrawable : IDrawable
{
    public List<PeriodSummary> History { get; set; } = new();

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        float paddingLeft = 36f;
        float paddingRight = 16f;
        float paddingTop = 24f;
        float paddingBottom = 30f;

        float chartW = dirtyRect.Width - paddingLeft - paddingRight;
        float chartH = dirtyRect.Height - paddingTop - paddingBottom;

        if (chartW <= 0 || chartH <= 0)
        {
            canvas.RestoreState();
            return;
        }

        // Если истории еще нет, рисуем заглушку
        if (History == null || History.Count == 0)
        {
            canvas.FontColor = Color.FromArgb("#9CA3AF");
            canvas.FontSize = 13f;
            canvas.DrawString("История появится после завершения 1-го периода",
                dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height,
                HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.RestoreState();
            return;
        }

        // Отображаем до 8 последних периодов для предотвращения наложения меток
        var list = History.Count > 8 ? History.TakeLast(8).ToList() : History;
        int count = list.Count;

        float maxVal = 200f;
        foreach (var s in list)
        {
            float sav = s.EndPeriodSavings > 0 ? s.EndPeriodSavings : s.ActualSavings;
            float periodMax = Math.Max(s.ActualObligatory + s.ActualDiscretionary, sav);
            periodMax = Math.Max(periodMax, Math.Max(s.PlannedObligatory + s.PlannedDiscretionary, s.PlannedSavings));
            if (periodMax > maxVal) maxVal = periodMax;
        }
        maxVal = (float)Math.Ceiling(maxVal / 50.0) * 50f;

        // Координатная сетка (мягкий контрастный серый для светлого фона #F8F7FD)
        canvas.StrokeColor = Color.FromArgb("#E5E7EB");
        canvas.StrokeSize = 1f;

        int gridLines = 4;
        for (int i = 0; i <= gridLines; i++)
        {
            float val = maxVal * (i / (float)gridLines);
            float y = paddingTop + chartH - (i / (float)gridLines) * chartH;

            // Горизонтальная линия
            canvas.DrawLine(paddingLeft, y, paddingLeft + chartW, y);

            // Значение на оси Y
            canvas.FontColor = Color.FromArgb("#6B7280");
            canvas.FontSize = 10f;
            canvas.DrawString($"{(int)val}", 2, y - 7, paddingLeft - 6, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        float stepX = chartW / Math.Max(1, count);
        var savingsPoints = new List<PointF>();

        // Отрисовка столбиков и цветных маркеров расходов для каждого периода
        for (int i = 0; i < count; i++)
        {
            var p = list[i];
            float colCenterX = paddingLeft + (i + 0.5f) * stepX;
            float barWidth = Math.Min(18f, stepX * 0.32f);

            // Столбик обязательных расходов (Изумрудный) + маркер
            float oblH = (p.ActualObligatory / maxVal) * chartH;
            float oblY = paddingTop + chartH - oblH;
            canvas.FillColor = Color.FromArgb("#10B981");
            canvas.FillRoundedRectangle(colCenterX - barWidth - 2, oblY, barWidth, oblH, 3);
            if (p.ActualObligatory > 0)
            {
                canvas.FillColor = Color.FromArgb("#10B981");
                canvas.FillCircle(colCenterX - barWidth * 0.5f - 2, oblY, 4f);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(colCenterX - barWidth * 0.5f - 2, oblY, 2f);
            }

            // Столбик свободных расходов (Фиолетовый) + маркер
            float discH = (p.ActualDiscretionary / maxVal) * chartH;
            float discY = paddingTop + chartH - discH;
            canvas.FillColor = Color.FromArgb("#8B5CF6");
            canvas.FillRoundedRectangle(colCenterX + 2, discY, barWidth, discH, 3);
            if (p.ActualDiscretionary > 0)
            {
                canvas.FillColor = Color.FromArgb("#8B5CF6");
                canvas.FillCircle(colCenterX + barWidth * 0.5f + 2, discY, 4f);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(colCenterX + barWidth * 0.5f + 2, discY, 2f);
            }

            // Точка сбережений (Золотой)
            float savVal = p.EndPeriodSavings > 0 ? p.EndPeriodSavings : p.ActualSavings;
            float savH = (savVal / maxVal) * chartH;
            float savY = paddingTop + chartH - savH;
            savingsPoints.Add(new PointF(colCenterX, savY));

            // Подпись периода на оси X (высококонтрастный фиолетовый)
            canvas.FontColor = Color.FromArgb("#520978");
            canvas.FontSize = 11f;
            canvas.DrawString($"#{p.PeriodNumber}", colCenterX - 18, paddingTop + chartH + 8, 36, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Соединяющая линия динамики сбережений (Золотая)
        if (savingsPoints.Count > 0)
        {
            canvas.StrokeColor = Color.FromArgb("#F59E0B");
            canvas.StrokeSize = 3f;
            canvas.StrokeLineCap = LineCap.Round;

            for (int i = 0; i < savingsPoints.Count - 1; i++)
            {
                canvas.DrawLine(savingsPoints[i], savingsPoints[i + 1]);
            }

            // Маркеры точек сбережений (Золотой кружок с белым центром)
            foreach (var pt in savingsPoints)
            {
                canvas.FillColor = Color.FromArgb("#F59E0B");
                canvas.FillCircle(pt.X, pt.Y, 5.5f);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(pt.X, pt.Y, 2.5f);
            }
        }

        canvas.RestoreState();
    }
}
