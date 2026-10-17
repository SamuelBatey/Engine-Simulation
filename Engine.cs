using EngineSim.Constants;

namespace EngineSim;

public class Engine
{
    // Tuning constants
    private const double RevUpRate = 3.0;                // how quickly RPM climbs toward the target
    private const double RevDownRate = 1.5;              // how quickly RPM falls when you let off
    private const double PistonSpeedLimitMs = 20;        // max mean piston speed, sets the redline
    private const double IdleVariation = 0.02;           // idle wobbles by ± this fraction of TargetIdleRpm
    private const double ClosedThrottleThreshold = 0.005; // below this the throttle counts as closed
    private const double BmepBar = 12.5;                 // brake mean effective pressure, used for torque

    public double BoreMm { get; }  // Diameter of the cylinder
    public double StrokeLengthMm { get; } // distance the piston moves up/down
    public int CylinderCount { get; }
    public double CylinderVolumeLiters { get; }
    public double DisplacementLiters { get; }
    public double MaxRpm { get; }
    public double TargetIdleRpm { get; } = 700;
    public double EngineSpeed { get; private set; }
    public double TorqueNm { get; set; } // BMEP(Pa) * dsiplacement(m^3)/(4pi)

    // 0.0 = closed, 1.0 = wide open. Values outside that range get clamped.
    private double _throttlePosition;
    public double ThrottlePosition
    {
        get => _throttlePosition;
        set => _throttlePosition = Math.Clamp(value, 0.0, 1.0);
    }

    public Engine(double bore, double stroke, int cylinders, AmbientConditions ambientConditions)
    {
        BoreMm = bore;
        StrokeLengthMm = stroke;
        CylinderCount = cylinders;
        CylinderVolumeLiters = CalculateCylinderVolumeLiters();
        DisplacementLiters = TotalDisplacement();
        MaxRpm = CalculateRedline();
        EngineSpeed = CalculateTargetRpm();
        TorqueNm = CalculateTorqueNm();
    }

    // Advance the engine by dt seconds
    public void Update(double dt)
    {
        double targetRpm = CalculateTargetRpm();

        double rate = targetRpm > EngineSpeed ? RevUpRate : RevDownRate;
        EngineSpeed += (targetRpm - EngineSpeed) * rate * dt;
        EngineSpeed = Math.Clamp(EngineSpeed, 0, MaxRpm);
    }

    // Volume swept by one piston: π/4 × bore² × stroke, converted from mm³ to liters
    private double CalculateCylinderVolumeLiters()
    {
        return Math.PI / 4 * Math.Pow(BoreMm, 2) * StrokeLengthMm / 1_000_000;
    }

    private double TotalDisplacement()
    {
        return CalculateCylinderVolumeLiters() * CylinderCount;
    }

    private double CalculateRedline()
    {
        double maxRpm = 30 * PistonSpeedLimitMs / StrokeLengthMm * 1000; // multiply by 1000 to get from mm on stroke length to meters on the piston speed

        return Math.Floor(maxRpm / 100) * 100; // round down to the closest 100 IE 8769 -> 8700
    }

    // The RPM the engine is heading toward; Update eases EngineSpeed toward it
    private double CalculateTargetRpm()
    {
        // Treat a near-zero throttle as closed, since repeated +/- 0.01 steps can leave tiny rounding leftovers
        if (ThrottlePosition < ClosedThrottleThreshold)
        {
            int idleRange = (int)(TargetIdleRpm * IdleVariation);
            return TargetIdleRpm + Random.Shared.Next(-idleRange, idleRange + 1);
        }

        // Throttle maps linearly from idle (closed) to redline (wide open)
        return TargetIdleRpm + ThrottlePosition * (MaxRpm - TargetIdleRpm);
    }

    // Open (+) or close (-) the throttle by the given amount; the setter keeps it between 0 and 1
    public void ThrottleControl(double amount)
    {
        ThrottlePosition += amount;
    }

    public double CalculateTorqueNm()
    {
        // BMEP(Pa) * dsiplacement(m^3)/(4pi)
        double pressureInPa = BmepBar * 100000;

        double DisplacementInMCubed = DisplacementLiters / 1000;

        return pressureInPa * DisplacementInMCubed / (4 * Math.PI);
    }
}
