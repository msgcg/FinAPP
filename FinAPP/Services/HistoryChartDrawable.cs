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

        int count = History.Count;
        float maxVal = 200f;
        foreach (var s in History)
        {
            float periodMax = Math.Max(s.ActualObligatory + s.ActualDiscretionary, s.ActualSavings);
            periodMax = Math.Max(periodMax, Math.Max(s.PlannedObligatory + s.PlannedDiscretionary, s.PlannedSavings));
            if (periodMax > maxVal) maxVal = periodMax;
        }
        maxVal = (float)Math.Ceiling(maxVal / 50.0) * 50f;

        // Координатная сетка
        canvas.StrokeColor = Color.FromArgb("#3B1B5D");
        canvas.StrokeSize = 1f;

        int gridLines = 4;
        for (int i = 0; i <= gridLines; i++)
        {
            float val = maxVal * (i / (float)gridLines);
            float y = paddingTop + chartH - (i / (float)gridLines) * chartH;

            // Горизонтальная линия
            canvas.DrawLine(paddingLeft, y, paddingLeft + chartW, y);

            // Значение на оси Y
            canvas.FontColor = Color.FromArgb("#9CA3AF");
            canvas.FontSize = 10f;
            canvas.DrawString($"{(int)val}", 2, y - 7, paddingLeft - 6, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        float stepX = chartW / Math.Max(1, count);
        var savingsPoints = new List<PointF>();

        // Отрисовка столбиков расходов для каждого периода
        for (int i = 0; i < count; i++)
        {
            var p = History[i];
            float colCenterX = paddingLeft + (i + 0.5f) * stepX;
            float barWidth = Math.Min(22f, stepX * 0.35f);

            // Столбик обязательных расходов (Изумрудный)
            float oblH = (p.ActualObligatory / maxVal) * chartH;
            float oblY = paddingTop + chartH - oblH;
            canvas.FillColor = Color.FromArgb("#10B981");
            canvas.FillRoundedRectangle(colCenterX - barWidth - 1, oblY, barWidth, oblH, 3);

            // Столбик свободных расходов (Фиолетовый)
            float discH = (p.ActualDiscretionary / maxVal) * chartH;
            float discY = paddingTop + chartH - discH;
            canvas.FillColor = Color.FromArgb("#8B5CF6");
            canvas.FillRoundedRectangle(colCenterX + 1, discY, barWidth, discH, 3);

            // Точка сбережений (Золотой)
            float savH = (p.ActualSavings / maxVal) * chartH;
            float savY = paddingTop + chartH - savH;
            savingsPoints.Add(new PointF(colCenterX, savY));

            // Подпись периода на оси X
            canvas.FontColor = Color.FromArgb("#E5E7EB");
            canvas.FontSize = 11f;
            canvas.DrawString($"#{p.PeriodNumber}", colCenterX - 20, paddingTop + chartH + 8, 40, 16, HorizontalAlignment.Center, VerticalAlignment.Center);
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

            // Маркеры точек сбережений
            foreach (var pt in savingsPoints)
            {
                canvas.FillColor = Color.FromArgb("#F59E0B");
                canvas.FillCircle(pt.X, pt.Y, 5f);
                canvas.FillColor = Colors.White;
                canvas.FillCircle(pt.X, pt.Y, 2.5f);
            }
        }

        canvas.RestoreState();
    }
}
