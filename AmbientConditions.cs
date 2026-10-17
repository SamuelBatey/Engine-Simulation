namespace EngineSim.Constants;

// The environment the engine is running in. Defaults are a standard day at sea level.
public class AmbientConditions
{
    private const double SpecificGasConstantAir = 287.05; // J/(kg·K), used in the ideal gas law
    private const double StandardPressureKpa = 101.325;   // sea level
    private const double StandardTempC = 15;
    private const double StandardDensityKgM3 = 1.225;     // air density at sea level, 15°C

    public double AirPressureKpa { get; set; } = StandardPressureKpa;
    public double AirTempC { get; set; } = StandardTempC;

    public double AirTempK => AirTempC + 273.15;

    // Ideal gas law: density = pressure / (R × temperature), with pressure converted from kPa to Pa
    public double AirDensityKgM3 => AirPressureKpa * 1000 / (SpecificGasConstantAir * AirTempK);

    // 1.0 on a standard day; below 1.0 in hot or thin air, where the engine makes less power
    public double DensityRatio => AirDensityKgM3 / StandardDensityKgM3;
}
