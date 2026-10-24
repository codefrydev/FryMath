using System;
using System.Collections.Generic;
using FryMath.Core.LinearAlgebra;

namespace FryMath.Core.Dynamics;

public struct FlowParticle
{
    public double X;
    public double Y;
    public double PrevX;
    public double PrevY;
    public double Age;
    public double MaxAge;
    public double SpeedFactor;
}

/// <summary>
/// Simulates hundreds of fluid particles advecting through a 2D vector field at 60 FPS.
/// </summary>
public sealed class ParticleAdvectionEngine
{
    private readonly Random _random = new(42);
    private readonly FlowParticle[] _particles;

    public int ParticleCount => _particles.Length;
    public IReadOnlyList<FlowParticle> Particles => _particles;

    public ParticleAdvectionEngine(int count = 280)
    {
        _particles = new FlowParticle[count];
        Reset(-5.0, 5.0, -5.0, 5.0);
    }

    public void Reset(double minX, double maxX, double minY, double maxY)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            RespawnParticle(ref _particles[i], minX, maxX, minY, maxY);
            // Stagger initial ages so they don't all respawn simultaneously
            _particles[i].Age = _random.NextDouble() * _particles[i].MaxAge;
        }
    }

    public void Step(VectorField2D field, double dt, double minX, double maxX, double minY, double maxY, double speedMultiplier = 1.0)
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            p.PrevX = p.X;
            p.PrevY = p.Y;

            var v = field.Evaluate(p.X, p.Y);
            double speed = v.Length;

            // Clamp maximum step to prevent tunneling across singularities
            double stepLen = Math.Min(speed * p.SpeedFactor * speedMultiplier * dt, 1.2);
            if (speed > 1e-6)
            {
                p.X += (v.X / speed) * stepLen;
                p.Y += (v.Y / speed) * stepLen;
            }

            p.Age += dt;

            // Respawn if out of viewport or aged out
            if (p.X < minX || p.X > maxX || p.Y < minY || p.Y > maxY || p.Age >= p.MaxAge)
            {
                RespawnParticle(ref p, minX, maxX, minY, maxY);
            }
        }
    }

    private void RespawnParticle(ref FlowParticle p, double minX, double maxX, double minY, double maxY)
    {
        p.X = minX + _random.NextDouble() * (maxX - minX);
        p.Y = minY + _random.NextDouble() * (maxY - minY);
        p.PrevX = p.X;
        p.PrevY = p.Y;
        p.Age = 0.0;
        p.MaxAge = 2.0 + _random.NextDouble() * 3.5; // Lives 2 to 5.5 seconds
        p.SpeedFactor = 0.85 + _random.NextDouble() * 0.35;
    }
}
