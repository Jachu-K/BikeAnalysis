namespace BikeAnalysis;

public class UserSpeedStats
{
    public string UserType { get; set; }
    public double TotalSpeed { get; set; }
    public int RideCount { get; set; }
    public List<double> Speeds { get; set; } = new List<double>();
}