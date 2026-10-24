using System;
using System.Collections.Generic;

namespace FryMath.Core.Lagrangian;

public enum ClassicalMechanicsPreset
{
    DoublePendulumChaos,
    FoucaultPendulum,
    KapitzaInverted,
    SpringPendulum,
    PoincareKamTorus
}

public sealed record PendulumState(
    double Theta1,
    double Theta2,
    double Omega1,
    double Omega2
);

public sealed record TrailPoint(
    double X,
    double Y,
    double Alpha,
    bool IsPerturbed = false
);

public sealed record PoincarePoint(
    double Theta,
    double Omega
);

public class LagrangianMechanicsEngine
{
    public ClassicalMechanicsPreset Preset { get; private set; } = ClassicalMechanicsPreset.DoublePendulumChaos;
    public string PresetName { get; private set; } = "Double Pendulum Chaos";

    // Double Pendulum Physical Parameters
    public double L1 { get; set; } = 1.0;
    public double L2 { get; set; } = 0.9;
    public double M1 { get; set; } = 1.0;
    public double M2 { get; set; } = 0.8;
    public double Gravity { get; set; } = 9.81;
    public double Damping { get; set; } = 0.0; // 0 for conservative Hamiltonian chaos

    // Primary State: (Theta1, Theta2, Omega1, Omega2)
    public PendulumState State { get; private set; } = new(Math.PI * 0.65, Math.PI * 0.75, 0.0, 0.0);

    // Perturbed Shadow State (to demonstrate Butterfly Effect / Lyapunov exponent)
    public PendulumState ShadowState { get; private set; } = new(Math.PI * 0.65 + 1e-4, Math.PI * 0.75, 0.0, 0.0);

    // Spring Pendulum Parameters (for Fermi Resonance)
    public double SpringK { get; set; } = 40.0;
    public double SpringRestLength { get; set; } = 0.8;
    public double SpringR { get; set; } = 0.8;
    public double SpringDr { get; set; } = 0.0;
    public double SpringTheta { get; set; } = 0.4;
    public double SpringOmega { get; set; } = 0.0;

    // Kapitza Inverted Pendulum Parameters
    public double KapitzaAmp { get; set; } = 0.15;
    public double KapitzaFreq { get; set; } = 45.0; // Rapid base oscillation
    public double Time { get; private set; } = 0.0;

    // Diagnostics
    public double TotalEnergy { get; private set; }
    public double KineticEnergy { get; private set; }
    public double PotentialEnergy { get; private set; }
    public double LagrangianL { get; private set; }
    public double InitialEnergy { get; private set; }
    public double EnergyDrift { get; private set; }
    public double LyapunovSeparation { get; private set; }

    // Trajectory Trails
    public List<TrailPoint> Trail { get; } = new();
    public List<TrailPoint> ShadowTrail { get; } = new();
    public List<PoincarePoint> PoincarePoints { get; } = new();

    public int MaxTrailPoints { get; set; } = 450;
    public bool ShowShadowTrajectory { get; set; } = true;
    public bool ShowPhasePortrait { get; set; } = true;

    public LagrangianMechanicsEngine()
    {
        ResetToState(Math.PI * 0.65, Math.PI * 0.75, 0.0, 0.0);
        PrewarmTrail();
    }

    public void ResetToState(double th1, double th2, double w1, double w2)
    {
        State = new PendulumState(th1, th2, w1, w2);
        ShadowState = new PendulumState(th1 + 1e-4, th2, w1, w2);
        Time = 0.0;
        Trail.Clear();
        ShadowTrail.Clear();
        PoincarePoints.Clear();

        ComputeEnergy(State, out double t, out double v);
        InitialEnergy = t + v;
        TotalEnergy = InitialEnergy;
        KineticEnergy = t;
        PotentialEnergy = v;
        LagrangianL = t - v;
        EnergyDrift = 0.0;
        LyapunovSeparation = 1e-4;
    }

    private void PrewarmTrail()
    {
        for (int i = 0; i < 180; i++)
        {
            Step(0.012);
        }
    }

    public void Step(double dt)
    {
        Time += dt;

        switch (Preset)
        {
            case ClassicalMechanicsPreset.DoublePendulumChaos:
            case ClassicalMechanicsPreset.PoincareKamTorus:
                StepDoublePendulum(dt);
                break;
            case ClassicalMechanicsPreset.KapitzaInverted:
                StepKapitza(dt);
                break;
            case ClassicalMechanicsPreset.SpringPendulum:
                StepSpringPendulum(dt);
                break;
            case ClassicalMechanicsPreset.FoucaultPendulum:
                StepFoucault(dt);
                break;
        }
    }

    private void StepDoublePendulum(double dt)
    {
        double prevTh2 = State.Theta2;

        // Advance primary state via RK4
        State = IntegrateRk4(State, dt);

        // Advance perturbed shadow state via RK4
        ShadowState = IntegrateRk4(ShadowState, dt);

        // Record Tip Positions
        (double x1, double y1, double x2, double y2) = GetDoublePendulumCoordinates(State);
        Trail.Add(new TrailPoint(x2, y2, 1.0, false));
        if (Trail.Count > MaxTrailPoints) Trail.RemoveAt(0);

        (double sx1, double sy1, double sx2, double sy2) = GetDoublePendulumCoordinates(ShadowState);
        ShadowTrail.Add(new TrailPoint(sx2, sy2, 1.0, true));
        if (ShadowTrail.Count > MaxTrailPoints) ShadowTrail.RemoveAt(0);

        // Check for Poincaré section crossing: Theta2 crosses 0 with Omega2 > 0
        double normPrev = NormalizeAngle(prevTh2);
        double normCur = NormalizeAngle(State.Theta2);
        if (normPrev < 0 && normCur >= 0 && State.Omega2 > 0)
        {
            PoincarePoints.Add(new PoincarePoint(NormalizeAngle(State.Theta1), State.Omega1));
            if (PoincarePoints.Count > 350) PoincarePoints.RemoveAt(0);
        }

        // Diagnostics
        ComputeEnergy(State, out double t, out double v);
        KineticEnergy = t;
        PotentialEnergy = v;
        TotalEnergy = t + v;
        LagrangianL = t - v;
        EnergyDrift = Math.Abs(InitialEnergy) > 1e-6 ? Math.Abs(TotalEnergy - InitialEnergy) / Math.Abs(InitialEnergy) : 0.0;

        // Lyapunov divergence: distance between tip of primary and tip of shadow
        double dx = x2 - sx2;
        double dy = y2 - sy2;
        LyapunovSeparation = Math.Sqrt(dx * dx + dy * dy);
    }

    private PendulumState IntegrateRk4(PendulumState s, double dt)
    {
        var k1 = Derivatives(s);
        var s2 = new PendulumState(
            s.Theta1 + 0.5 * dt * k1.dTh1,
            s.Theta2 + 0.5 * dt * k1.dTh2,
            s.Omega1 + 0.5 * dt * k1.dOm1,
            s.Omega2 + 0.5 * dt * k1.dOm2
        );

        var k2 = Derivatives(s2);
        var s3 = new PendulumState(
            s.Theta1 + 0.5 * dt * k2.dTh1,
            s.Theta2 + 0.5 * dt * k2.dTh2,
            s.Omega1 + 0.5 * dt * k2.dOm1,
            s.Omega2 + 0.5 * dt * k2.dOm2
        );

        var k3 = Derivatives(s3);
        var s4 = new PendulumState(
            s.Theta1 + dt * k3.dTh1,
            s.Theta2 + dt * k3.dTh2,
            s.Omega1 + dt * k3.dOm1,
            s.Omega2 + dt * k3.dOm2
        );

        var k4 = Derivatives(s4);

        return new PendulumState(
            s.Theta1 + (dt / 6.0) * (k1.dTh1 + 2 * k2.dTh1 + 2 * k3.dTh1 + k4.dTh1),
            s.Theta2 + (dt / 6.0) * (k1.dTh2 + 2 * k2.dTh2 + 2 * k3.dTh2 + k4.dTh2),
            s.Omega1 + (dt / 6.0) * (k1.dOm1 + 2 * k2.dOm1 + 2 * k3.dOm1 + k4.dOm1),
            s.Omega2 + (dt / 6.0) * (k1.dOm2 + 2 * k2.dOm2 + 2 * k3.dOm2 + k4.dOm2)
        );
    }

    private (double dTh1, double dTh2, double dOm1, double dOm2) Derivatives(PendulumState s)
    {
        double delta = s.Theta1 - s.Theta2;
        double sinDelta = Math.Sin(delta);
        double cosDelta = Math.Cos(delta);

        double mu = 1.0 + (M1 / M2);
        double denom1 = L1 * (mu - cosDelta * cosDelta);
        double denom2 = L2 * (mu - cosDelta * cosDelta);

        double num1 = Gravity * (Math.Sin(s.Theta2) * cosDelta - mu * Math.Sin(s.Theta1)) -
                      (L2 * s.Omega2 * s.Omega2 + L1 * s.Omega1 * s.Omega1 * cosDelta) * sinDelta;

        double num2 = Gravity * mu * (Math.Sin(s.Theta1) * cosDelta - Math.Sin(s.Theta2)) +
                      (mu * L1 * s.Omega1 * s.Omega1 + L2 * s.Omega2 * s.Omega2 * cosDelta) * sinDelta;

        double alpha1 = (num1 / denom1) - Damping * s.Omega1;
        double alpha2 = (num2 / denom2) - Damping * s.Omega2;

        return (s.Omega1, s.Omega2, alpha1, alpha2);
    }

    private void ComputeEnergy(PendulumState s, out double kinetic, out double potential)
    {
        double delta = s.Theta1 - s.Theta2;
        kinetic = 0.5 * (M1 + M2) * L1 * L1 * s.Omega1 * s.Omega1 +
                  0.5 * M2 * L2 * L2 * s.Omega2 * s.Omega2 +
                  M2 * L1 * L2 * s.Omega1 * s.Omega2 * Math.Cos(delta);

        potential = -(M1 + M2) * Gravity * L1 * Math.Cos(s.Theta1) -
                    M2 * Gravity * L2 * Math.Cos(s.Theta2);
    }

    public (double x1, double y1, double x2, double y2) GetDoublePendulumCoordinates(PendulumState s)
    {
        double x1 = L1 * Math.Sin(s.Theta1);
        double y1 = -L1 * Math.Cos(s.Theta1);
        double x2 = x1 + L2 * Math.Sin(s.Theta2);
        double y2 = y1 - L2 * Math.Cos(s.Theta2);
        return (x1, y1, x2, y2);
    }

    private static double NormalizeAngle(double a)
    {
        while (a > Math.PI) a -= 2 * Math.PI;
        while (a < -Math.PI) a += 2 * Math.PI;
        return a;
    }

    #region Kapitza Inverted Pendulum

    private void StepKapitza(double dt)
    {
        // Kapitza base vertical oscillation: y_base = A * cos(omega * t)
        // d²theta/dt² = - (g/L) * sin(theta) - (A * omega² / L) * cos(omega * t) * sin(theta)
        double baseAccel = -KapitzaAmp * KapitzaFreq * KapitzaFreq * Math.Cos(KapitzaFreq * Time);
        double effectiveG = Gravity + baseAccel;

        double alpha = -(effectiveG / L1) * Math.Sin(State.Theta1) - 0.05 * State.Omega1;
        double newOm = State.Omega1 + alpha * dt;
        double newTh = State.Theta1 + newOm * dt;

        State = new PendulumState(newTh, 0, newOm, 0);

        double x = L1 * Math.Sin(newTh);
        double y = -L1 * Math.Cos(newTh) + KapitzaAmp * Math.Cos(KapitzaFreq * Time);
        Trail.Add(new TrailPoint(x, y, 1.0, false));
        if (Trail.Count > MaxTrailPoints) Trail.RemoveAt(0);

        KineticEnergy = 0.5 * M1 * L1 * L1 * newOm * newOm;
        PotentialEnergy = M1 * Gravity * y;
        TotalEnergy = KineticEnergy + PotentialEnergy;
        LagrangianL = KineticEnergy - PotentialEnergy;
    }

    #endregion

    #region Spring Pendulum (Fermi Resonance)

    private void StepSpringPendulum(double dt)
    {
        // 2D Spring Pendulum equations in polar coordinates (r, theta)
        // r'' = r * theta'^2 - (k/m)*(r - r0) + g * cos(theta)
        // theta'' = -(g/r)*sin(theta) - (2/r)*r'*theta'
        double r = SpringR;
        double dr = SpringDr;
        double th = SpringTheta;
        double om = SpringOmega;

        double ddr = r * om * om - (SpringK / M1) * (r - SpringRestLength) + Gravity * Math.Cos(th);
        double ddom = -(Gravity / r) * Math.Sin(th) - (2.0 / r) * dr * om;

        SpringDr += ddr * dt;
        SpringR += SpringDr * dt;
        SpringOmega += ddom * dt;
        SpringTheta += SpringOmega * dt;

        double x = SpringR * Math.Sin(SpringTheta);
        double y = -SpringR * Math.Cos(SpringTheta);

        Trail.Add(new TrailPoint(x, y, 1.0, false));
        if (Trail.Count > MaxTrailPoints) Trail.RemoveAt(0);

        KineticEnergy = 0.5 * M1 * (SpringDr * SpringDr + SpringR * SpringR * SpringOmega * SpringOmega);
        PotentialEnergy = 0.5 * SpringK * (SpringR - SpringRestLength) * (SpringR - SpringRestLength) - M1 * Gravity * SpringR * Math.Cos(SpringTheta);
        TotalEnergy = KineticEnergy + PotentialEnergy;
        LagrangianL = KineticEnergy - PotentialEnergy;

        State = new PendulumState(SpringTheta, 0, SpringOmega, 0);
    }

    #endregion

    #region Foucault Pendulum

    private void StepFoucault(double dt)
    {
        // Earth rotation precession: dpsi/dt = -Omega_earth * sin(latitude)
        double latitude = Math.PI * 0.27; // ~48.8° (Paris Pantheon)
        double precessionRate = 0.5 * Math.Sin(latitude); // Earth rotation projection

        double omega = Math.Sqrt(Gravity / L1);
        double th = 0.35 * Math.Cos(omega * Time);
        double psi = -precessionRate * Time;

        double x = L1 * Math.Sin(th) * Math.Cos(psi);
        double y = L1 * Math.Sin(th) * Math.Sin(psi);

        Trail.Add(new TrailPoint(x, y, 1.0, false));
        if (Trail.Count > MaxTrailPoints) Trail.RemoveAt(0);

        State = new PendulumState(th, psi, -omega * 0.35 * Math.Sin(omega * Time), -precessionRate);
        KineticEnergy = 0.5 * M1 * (omega * omega * 0.12);
        PotentialEnergy = M1 * Gravity * L1 * (1 - Math.Cos(th));
        TotalEnergy = KineticEnergy + PotentialEnergy;
        LagrangianL = KineticEnergy - PotentialEnergy;
    }

    #endregion

    #region Presets

    public void LoadPreset(string presetName)
    {
        PresetName = presetName;

        switch (presetName)
        {
            case "Double Pendulum Chaos":
                Preset = ClassicalMechanicsPreset.DoublePendulumChaos;
                L1 = 1.0;
                L2 = 0.9;
                M1 = 1.0;
                M2 = 0.8;
                Damping = 0.0;
                ResetToState(Math.PI * 0.65, Math.PI * 0.75, 0.0, 0.0);
                PrewarmTrail();
                break;

            case "Poincaré Section & KAM Torus":
                Preset = ClassicalMechanicsPreset.PoincareKamTorus;
                L1 = 1.0;
                L2 = 1.0;
                M1 = 1.0;
                M2 = 1.0;
                Damping = 0.0;
                ResetToState(0.4, 0.3, 0.0, 0.0);
                for (int i = 0; i < 400; i++) Step(0.012);
                break;

            case "Kapitza's Inverted Pendulum":
                Preset = ClassicalMechanicsPreset.KapitzaInverted;
                L1 = 0.9;
                M1 = 1.0;
                KapitzaAmp = 0.12;
                KapitzaFreq = 50.0;
                ResetToState(Math.PI - 0.08, 0, 0, 0); // Released near upright vertical!
                for (int i = 0; i < 150; i++) Step(0.012);
                break;

            case "Spring Pendulum (Fermi Resonance)":
                Preset = ClassicalMechanicsPreset.SpringPendulum;
                SpringK = 35.0;
                SpringRestLength = 0.75;
                SpringR = 0.95;
                SpringDr = 0.0;
                SpringTheta = 0.45;
                SpringOmega = 0.0;
                ResetToState(0.45, 0, 0, 0);
                for (int i = 0; i < 200; i++) Step(0.012);
                break;

            case "Foucault Pendulum Precession":
                Preset = ClassicalMechanicsPreset.FoucaultPendulum;
                L1 = 1.2;
                ResetToState(0.35, 0, 0, 0);
                for (int i = 0; i < 200; i++) Step(0.012);
                break;
        }
    }

    #endregion
}
