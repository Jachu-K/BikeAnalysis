namespace BikeAnalysis;

public class BikeRide
{
    public string RideId { get; set; }
    public string RideableType { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public string StartStationName { get; set; }
    public string StartStationId { get; set; }
    public string EndStationName { get; set; }
    public string EndStationId { get; set; }
    public decimal StartLat { get; set; }
    public decimal StartLng { get; set; }
    public decimal EndLat { get; set; }
    public decimal EndLng { get; set; }
    public string MemberCasual { get; set; }
    public TimeSpan Duration => EndedAt - StartedAt;
    public double Distance => CalculateDistance();
    public double SpeedKmh => CalculateSpeed();
    
    private bool? _hasBeenDebugged = null; // Śledzi czy ta instancja była już debugowana

    private double CalculateDistance()
    {
        
        
        if (StartLat == 0 && StartLng == 0)
        {
            return 0;
        }
        if (EndLat == 0 && EndLng == 0)
        {
            return 0;
        }
        
        if (Math.Abs((double)StartLat) > 90 || Math.Abs((double)StartLng) > 180 ||
            Math.Abs((double)EndLat) > 90 || Math.Abs((double)EndLng) > 180)
        {
            return 0;
        }
        
        //https://www.movable-type.co.uk/scripts/latlong.html

        const double R = 6371e3; // metres
        
        var φ1 = (double)StartLat * Math.PI/180;
        var φ2 = (double)EndLat * Math.PI/180;
        var Δφ = (double)(EndLat-StartLat) * Math.PI/180;
        var Δλ = (double)(EndLng-StartLng) * Math.PI/180;


        var a = Math.Sin(Δφ/2) * Math.Sin(Δφ/2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ/2) * Math.Sin(Δλ/2);

        
        if (a < 0 || a > 1)
        {
            return 0;
        }
                
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
        var d = R * c; // in metres
        
        var distanceKm = d / 1000;

        return distanceKm;
    }
    
    private double CalculateSpeed()
    {

        if (Duration.TotalHours <= 0)
        {
            return 0;
        }
        
        var speed = (Distance / Duration.TotalSeconds) * 3600;
        
        return speed;
    }
}