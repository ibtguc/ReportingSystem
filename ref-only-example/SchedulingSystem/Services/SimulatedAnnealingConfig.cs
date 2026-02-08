namespace SchedulingSystem.Services;

/// <summary>
/// Configuration parameters for Simulated Annealing algorithm
/// </summary>
public class SimulatedAnnealingConfig
{
    /// <summary>
    /// Initial temperature - higher values allow more exploration
    /// </summary>
    public double InitialTemperature { get; set; } = 100.0;

    /// <summary>
    /// Final temperature - algorithm stops when temperature reaches this
    /// </summary>
    public double FinalTemperature { get; set; } = 0.1;

    /// <summary>
    /// Cooling rate - how quickly temperature decreases (0 < alpha < 1)
    /// Typical values: 0.95 - 0.99
    /// </summary>
    public double CoolingRate { get; set; } = 0.95;

    /// <summary>
    /// Number of iterations at each temperature level
    /// </summary>
    public int IterationsPerTemperature { get; set; } = 100;

    /// <summary>
    /// Maximum number of iterations without improvement before stopping
    /// </summary>
    public int MaxIterationsWithoutImprovement { get; set; } = 1000;

    /// <summary>
    /// Soft constraint weights for energy calculation
    /// </summary>
    public SoftConstraintWeights Weights { get; set; } = SoftConstraintWeights.Default;

    /// <summary>
    /// Random seed for reproducibility (null = random seed)
    /// </summary>
    public int? RandomSeed { get; set; } = null;

    // Predefined configurations

    /// <summary>
    /// Fast configuration - fewer iterations, quick results
    /// </summary>
    public static SimulatedAnnealingConfig Fast => new SimulatedAnnealingConfig
    {
        InitialTemperature = 50.0,
        FinalTemperature = 0.5,
        CoolingRate = 0.90,
        IterationsPerTemperature = 50,
        MaxIterationsWithoutImprovement = 500,
        Weights = SoftConstraintWeights.Relaxed
    };

    /// <summary>
    /// Balanced configuration - good quality with reasonable time
    /// </summary>
    public static SimulatedAnnealingConfig Balanced => new SimulatedAnnealingConfig
    {
        InitialTemperature = 100.0,
        FinalTemperature = 0.1,
        CoolingRate = 0.95,
        IterationsPerTemperature = 100,
        MaxIterationsWithoutImprovement = 1000,
        Weights = SoftConstraintWeights.Default
    };

    /// <summary>
    /// Thorough configuration - best quality, longer runtime
    /// </summary>
    public static SimulatedAnnealingConfig Thorough => new SimulatedAnnealingConfig
    {
        InitialTemperature = 150.0,
        FinalTemperature = 0.05,
        CoolingRate = 0.98,
        IterationsPerTemperature = 200,
        MaxIterationsWithoutImprovement = 2000,
        Weights = SoftConstraintWeights.Aggressive
    };
}
