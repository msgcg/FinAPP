using System;
using Microsoft.Maui.Graphics;

namespace FinAPP.Services;

public class BudgetDonutChartDrawable : IDrawable
{
    public float ObligatoryAmount { get; set; } = 150;
    public float DiscretionaryAmount { get; set; } = 100;
    public float SavingsAmount { get; set; } = 100;

    public float PlannedObligatory { get => ObligatoryAmount; set => ObligatoryAmount = value; }
    public float PlannedDiscretionary { get => DiscretionaryAmount; set => DiscretionaryAmount = value; }
    public float PlannedSavings { get => SavingsAmount; set => SavingsAmount = value; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();

        float total = ObligatoryAmount + DiscretionaryAmount + SavingsAmount;
        if (total <= 0) total = 1;

        float centerX = dirtyRect.Center.X;
        float centerY = dirtyRect.Center.Y;
        float radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 14f;
        float strokeWidth = 22f;

        // Фон кольца (подложка)
        canvas.StrokeColor = Color.FromArgb("#2D124D");
        canvas.StrokeSize = strokeWidth;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawCircle(centerX, centerY, radius);

        float oblAngle = (ObligatoryAmount / total) * 360f;
        float discAngle = (DiscretionaryAmount / total) * 360f;
        float savAngle = (SavingsAmount / total) * 360f;

        float currentAngle = -90f; // Начинаем сверху (12 часов)

        // 1. Обязательные расходы (Изумрудный)
        if (ObligatoryAmount > 0)
        {
            canvas.StrokeColor = Color.FromArgb("#10B981");
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Butt;
            DrawArc(canvas, centerX, centerY, radius, currentAngle, currentAngle + oblAngle);
            currentAngle += oblAngle;
        }

        // 2. Свободные желания (Фиолетовый)
        if (DiscretionaryAmount > 0)
        {
            canvas.StrokeColor = Color.FromArgb("#8B5CF6");
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Butt;
            DrawArc(canvas, centerX, centerY, radius, currentAngle, currentAngle + discAngle);
            currentAngle += discAngle;
        }

        // 3. Сбережения в копилку (Золотой)
        if (SavingsAmount > 0)
        {
            canvas.StrokeColor = Color.FromArgb("#F59E0B");
            canvas.StrokeSize = strokeWidth;
            canvas.StrokeLineCap = LineCap.Butt;
            DrawArc(canvas, centerX, centerY, radius, currentAngle, currentAngle + savAngle);
        }

        // Текст в центре: общая сумма и подпись
        canvas.FontColor = Colors.White;
        canvas.FontSize = 20f;
        canvas.DrawString($"{(int)total}", centerX - 50, centerY - 14, 100, 24, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.FontColor = Color.FromArgb("#D1D5DB");
        canvas.FontSize = 10f;
        canvas.DrawString("МОНЕТ", centerX - 50, centerY + 10, 100, 16, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.RestoreState();
    }

    private static void DrawArc(ICanvas canvas, float cx, float cy, float r, float startAngleDeg, float endAngleDeg)
    {
        if (Math.Abs(endAngleDeg - startAngleDeg) < 0.5f) return;

        var path = new PathF();
        float startRad = (float)(startAngleDeg * Math.PI / 180.0);
        float endRad = (float)(endAngleDeg * Math.PI / 180.0);

        float x1 = cx + r * (float)Math.Cos(startRad);
        float y1 = cy + r * (float)Math.Sin(startRad);

        path.MoveTo(x1, y1);

        int steps = Math.Max(2, (int)(Math.Abs(endAngleDeg - startAngleDeg) / 4f));
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float curRad = startRad + (endRad - startRad) * t;
            float px = cx + r * (float)Math.Cos(curRad);
            float py = cy + r * (float)Math.Sin(curRad);
            path.LineTo(px, py);
        }

        canvas.DrawPath(path);
    }
}
