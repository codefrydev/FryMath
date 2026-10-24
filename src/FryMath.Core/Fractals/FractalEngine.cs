using System;
using System.Numerics;
using System.Threading.Tasks;

namespace FryMath.Core.Fractals;

public enum FractalType
{
    Mandelbrot,
    Julia,
    NewtonBasin,
    BurningShip
}

public enum FractalPalette
{
    ElectricNeon,
    FireMagma,
    OceanTurquoise,
    PsychedelicRainbow,
    MonochromeGold
}

public sealed class FractalParams
{
    public FractalType Type { get; set; } = FractalType.Mandelbrot;
    public int MaxIterations { get; set; } = 150;
    public FractalPalette Palette { get; set; } = FractalPalette.ElectricNeon;
    public double JuliaCr { get; set; } = -0.7;
    public double JuliaCi { get; set; } = 0.27015;
    public int NewtonDegree { get; set; } = 3;
}

public static class FractalEngine
{
    public static (double Iteration, int RootIndex) EvaluatePoint(double cr, double ci, FractalParams p)
    {
        return p.Type switch
        {
            FractalType.Mandelbrot => (EvaluateMandelbrot(cr, ci, p.MaxIterations), -1),
            FractalType.Julia => (EvaluateJulia(cr, ci, p.JuliaCr, p.JuliaCi, p.MaxIterations), -1),
            FractalType.BurningShip => (EvaluateBurningShip(cr, ci, p.MaxIterations), -1),
            FractalType.NewtonBasin => EvaluateNewton(cr, ci, p.NewtonDegree, p.MaxIterations),
            _ => (0, -1)
        };
    }

    public static double EvaluateMandelbrot(double cr, double ci, int maxIter)
    {
        // Fast cardioid and period-2 bulb checks
        double q = (cr - 0.25) * (cr - 0.25) + ci * ci;
        if (q * (q + (cr - 0.25)) <= 0.25 * ci * ci)
            return maxIter; // Inside main cardioid

        if ((cr + 1.0) * (cr + 1.0) + ci * ci <= 0.0625)
            return maxIter; // Inside period-2 bulb

        double zr = 0.0;
        double zi = 0.0;
        double zr2 = 0.0;
        double zi2 = 0.0;
        int iter = 0;

        while (zr2 + zi2 <= 4.0 && iter < maxIter)
        {
            zi = 2.0 * zr * zi + ci;
            zr = zr2 - zi2 + cr;
            zr2 = zr * zr;
            zi2 = zi * zi;
            iter++;
        }

        if (iter >= maxIter) return maxIter;

        // Smooth continuous fractional coloring (Normalized Iteration Count)
        double modulusSq = zr2 + zi2;
        double nu = iter + 1.0 - Math.Log(0.5 * Math.Log(modulusSq)) / Math.Log(2.0);
        return Math.Max(0.0, nu);
    }

    public static double EvaluateJulia(double zr, double zi, double cr, double ci, int maxIter)
    {
        double zr2 = zr * zr;
        double zi2 = zi * zi;
        int iter = 0;

        while (zr2 + zi2 <= 4.0 && iter < maxIter)
        {
            zi = 2.0 * zr * zi + ci;
            zr = zr2 - zi2 + cr;
            zr2 = zr * zr;
            zi2 = zi * zi;
            iter++;
        }

        if (iter >= maxIter) return maxIter;

        double modulusSq = zr2 + zi2;
        double nu = iter + 1.0 - Math.Log(0.5 * Math.Log(modulusSq)) / Math.Log(2.0);
        return Math.Max(0.0, nu);
    }

    public static double EvaluateBurningShip(double cr, double ci, int maxIter)
    {
        double zr = 0.0;
        double zi = 0.0;
        double zr2 = 0.0;
        double zi2 = 0.0;
        int iter = 0;

        while (zr2 + zi2 <= 4.0 && iter < maxIter)
        {
            zi = Math.Abs(2.0 * zr * zi) + ci;
            zr = zr2 - zi2 + cr;
            zr2 = zr * zr;
            zi2 = zi * zi;
            iter++;
        }

        if (iter >= maxIter) return maxIter;

        double modulusSq = zr2 + zi2;
        double nu = iter + 1.0 - Math.Log(0.5 * Math.Log(modulusSq)) / Math.Log(2.0);
        return Math.Max(0.0, nu);
    }

    public static (double Iteration, int RootIndex) EvaluateNewton(double x, double y, int degree, int maxIter)
    {
        if (degree < 3) degree = 3;
        if (degree > 7) degree = 7;

        Complex z = new(x, y);
        int iter = 0;
        const double tol = 1e-4;

        while (iter < maxIter)
        {
            Complex zPow = Complex.Pow(z, degree - 1);
            Complex f = z * zPow - 1.0;
            Complex df = (double)degree * zPow;

            if (df.Magnitude < 1e-12) break;

            Complex zNext = z - f / df;
            if ((zNext - z).Magnitude < tol)
            {
                z = zNext;
                break;
            }

            z = zNext;
            iter++;
        }

        int closestRoot = 0;
        double minDistance = double.MaxValue;
        for (int k = 0; k < degree; k++)
        {
            double angle = 2.0 * Math.PI * k / degree;
            Complex root = new(Math.Cos(angle), Math.Sin(angle));
            double d = (z - root).Magnitude;
            if (d < minDistance)
            {
                minDistance = d;
                closestRoot = k;
            }
        }

        return (iter, closestRoot);
    }

    public static void RenderToBuffer(uint[] buffer, int width, int height, double centerX, double centerY, double zoom, FractalParams p)
    {
        if (buffer.Length < width * height) return;

        double aspect = (double)width / height;
        double viewHeight = 3.0 / zoom;
        double viewWidth = viewHeight * aspect;

        double xMin = centerX - viewWidth * 0.5;
        double yMax = centerY + viewHeight * 0.5;

        double dx = viewWidth / width;
        double dy = viewHeight / height;

        Parallel.For(0, height, py =>
        {
            double y = yMax - py * dy;
            int rowOffset = py * width;

            for (int px = 0; px < width; px++)
            {
                double x = xMin + px * dx;
                var (iter, root) = EvaluatePoint(x, y, p);
                buffer[rowOffset + px] = MapToColor(iter, root, p);
            }
        });
    }

    public static uint MapToColor(double iter, int rootIndex, FractalParams p)
    {
        if (iter >= p.MaxIterations)
        {
            return 0xFF080B14;
        }

        if (p.Type == FractalType.NewtonBasin && rootIndex >= 0)
        {
            return MapNewtonColor(iter, rootIndex, p.NewtonDegree, p.MaxIterations);
        }

        return p.Palette switch
        {
            FractalPalette.ElectricNeon => PaletteElectricNeon(iter),
            FractalPalette.FireMagma => PaletteFireMagma(iter),
            FractalPalette.OceanTurquoise => PaletteOceanTurquoise(iter),
            FractalPalette.PsychedelicRainbow => PaletteRainbow(iter),
            FractalPalette.MonochromeGold => PaletteGold(iter),
            _ => PaletteElectricNeon(iter)
        };
    }

    private static uint MapNewtonColor(double iter, int root, int degree, int maxIter)
    {
        double baseHue = (double)root / degree;
        double factor = Math.Clamp(1.0 - (iter / (double)maxIter) * 1.5, 0.15, 1.0);
        return HsvToArgb(baseHue, 0.85, factor);
    }

    private static uint PaletteElectricNeon(double iter)
    {
        double t = (iter * 0.05) % 1.0;
        if (t < 0.25)
        {
            double u = t / 0.25;
            return LerpColor(0xFF00E5FF, 0xFF6C5CE7, u);
        }
        else if (t < 0.5)
        {
            double u = (t - 0.25) / 0.25;
            return LerpColor(0xFF6C5CE7, 0xFFFF007A, u);
        }
        else if (t < 0.75)
        {
            double u = (t - 0.5) / 0.25;
            return LerpColor(0xFFFF007A, 0xFFFFB300, u);
        }
        else
        {
            double u = (t - 0.75) / 0.25;
            return LerpColor(0xFFFFB300, 0xFF00E5FF, u);
        }
    }

    private static uint PaletteFireMagma(double iter)
    {
        double t = (iter * 0.04) % 1.0;
        if (t < 0.3)
            return LerpColor(0xFF1A0505, 0xFFD32F2F, t / 0.3);
        if (t < 0.6)
            return LerpColor(0xFFD32F2F, 0xFFFF9800, (t - 0.3) / 0.3);
        if (t < 0.85)
            return LerpColor(0xFFFF9800, 0xFFFFEB3B, (t - 0.6) / 0.25);
        return LerpColor(0xFFFFEB3B, 0xFFFFFFFF, (t - 0.85) / 0.15);
    }

    private static uint PaletteOceanTurquoise(double iter)
    {
        double t = (iter * 0.04) % 1.0;
        if (t < 0.35)
            return LerpColor(0xFF021B2B, 0xFF00897B, t / 0.35);
        if (t < 0.7)
            return LerpColor(0xFF00897B, 0xFF26A69A, (t - 0.35) / 0.35);
        return LerpColor(0xFF26A69A, 0xFFE0F2F1, (t - 0.7) / 0.3);
    }

    private static uint PaletteRainbow(double iter)
    {
        double hue = (iter * 0.035) % 1.0;
        return HsvToArgb(hue, 0.9, 0.95);
    }

    private static uint PaletteGold(double iter)
    {
        double t = (iter * 0.03) % 1.0;
        return LerpColor(0xFF0D0A05, 0xFFFFD700, t < 0.5 ? t * 2.0 : (1.0 - t) * 2.0);
    }

    private static uint LerpColor(uint c1, uint c2, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte r1 = (byte)((c1 >> 16) & 0xFF);
        byte g1 = (byte)((c1 >> 8) & 0xFF);
        byte b1 = (byte)(c1 & 0xFF);

        byte r2 = (byte)((c2 >> 16) & 0xFF);
        byte g2 = (byte)((c2 >> 8) & 0xFF);
        byte b2 = (byte)(c2 & 0xFF);

        byte r = (byte)(r1 + (r2 - r1) * t);
        byte g = (byte)(g1 + (g2 - g1) * t);
        byte b = (byte)(b1 + (b2 - b1) * t);

        return 0xFF000000 | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    private static uint HsvToArgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1.0 - Math.Abs((h * 6.0) % 2.0 - 1.0));
        double m = v - c;

        double r = 0, g = 0, b = 0;
        int sextant = (int)(h * 6.0) % 6;
        switch (sextant)
        {
            case 0: r = c; g = x; b = 0; break;
            case 1: r = x; g = c; b = 0; break;
            case 2: r = 0; g = c; b = x; break;
            case 3: r = 0; g = x; b = c; break;
            case 4: r = x; g = 0; b = c; break;
            case 5: r = c; g = 0; b = x; break;
        }

        byte br = (byte)((r + m) * 255.0);
        byte bg = (byte)((g + m) * 255.0);
        byte bb = (byte)((b + m) * 255.0);

        return 0xFF000000 | ((uint)br << 16) | ((uint)bg << 8) | bb;
    }
}
