using System;
using System.Collections.Generic;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Probability;

public sealed class GaltonBall
{
    public Vector2D Position;
    public Vector2D Velocity;
    public bool IsSettled;
    public int FinalBin = -1;
    public double Radius = 3.5;

    public GaltonBall(Vector2D startPos)
    {
        Position = startPos;
        Velocity = new Vector2D((Random.Shared.NextDouble() - 0.5) * 2.0, 1.0);
    }
}

public sealed class GaltonBoardSimulation
{
    public int Rows { get; }
    public int BinCount => Rows + 1;
    public double PegSpacingX { get; } = 28.0;
    public double PegSpacingY { get; } = 24.0;
    public double TopY { get; } = 40.0;
    public double PegRadius { get; } = 3.0;

    public List<Vector2D> Pegs { get; } = new();
    public List<GaltonBall> Balls { get; } = new();
    public int[] BinCounts { get; }
    public int TotalSettledCount { get; private set; }

    public double SuccessProbabilityP { get; set; } = 0.5;

    public GaltonBoardSimulation(int rows = 12)
    {
        Rows = Math.Clamp(rows, 4, 20);
        BinCounts = new int[Rows + 1];

        // Generate triangular peg grid
        for (int r = 0; r < Rows; r++)
        {
            double y = TopY + r * PegSpacingY;
            double rowWidth = r * PegSpacingX;
            double startX = -rowWidth * 0.5;

            for (int c = 0; c <= r; c++)
            {
                double x = startX + c * PegSpacingX;
                Pegs.Add(new Vector2D(x, y));
            }
        }
    }

    public void SpawnBall()
    {
        if (Balls.Count > 1500) return;
        double startX = (Random.Shared.NextDouble() - 0.5) * 4.0;
        Balls.Add(new GaltonBall(new Vector2D(startX, 10.0)));
    }

    public void Step(double dt)
    {
        double bottomY = TopY + Rows * PegSpacingY + 30.0;
        double binFloorY = bottomY + 120.0;
        double gravity = 320.0;

        for (int i = 0; i < Balls.Count; i++)
        {
            var b = Balls[i];
            if (b.IsSettled) continue;

            // Gravity
            b.Velocity = new Vector2D(b.Velocity.X * 0.98, b.Velocity.Y + gravity * dt);
            b.Position += b.Velocity * dt;

            // Check collision with pegs
            foreach (var peg in Pegs)
            {
                double dx = b.Position.X - peg.X;
                double dy = b.Position.Y - peg.Y;
                double distSq = dx * dx + dy * dy;
                double minDist = b.Radius + PegRadius;

                if (distSq < minDist * minDist && dy < 4.0)
                {
                    // Deflection with bias P
                    bool goRight = Random.Shared.NextDouble() < SuccessProbabilityP;
                    double deflectX = goRight ? (12.0 + Random.Shared.NextDouble() * 15.0) : -(12.0 + Random.Shared.NextDouble() * 15.0);
                    b.Velocity = new Vector2D(deflectX, Math.Max(b.Velocity.Y * 0.4, 15.0));
                    b.Position = new Vector2D(peg.X + (goRight ? minDist : -minDist), peg.Y + 2.0);
                }
            }

            // Check if reached bottom collection bin
            if (b.Position.Y >= bottomY)
            {
                double totalWidth = Rows * PegSpacingX;
                double normX = (b.Position.X + totalWidth * 0.5) / PegSpacingX;
                int bin = (int)Math.Clamp(Math.Round(normX), 0, Rows);

                b.FinalBin = bin;
                b.IsSettled = true;
                BinCounts[bin]++;
                TotalSettledCount++;

                // Settle in bin column visually
                double stackHeight = BinCounts[bin] * 3.5;
                b.Position = new Vector2D(-totalWidth * 0.5 + bin * PegSpacingX, binFloorY - stackHeight);
                b.Velocity = Vector2D.Zero;
            }
        }
    }

    public void Reset()
    {
        Balls.Clear();
        Array.Clear(BinCounts);
        TotalSettledCount = 0;
    }
}
