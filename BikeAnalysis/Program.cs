namespace BikeAnalysis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        try
        {
            string directoryPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var csvFiles = Directory.GetFiles(directoryPath, "*.csv", SearchOption.AllDirectories);
            
            Console.WriteLine($"Znaleziono {csvFiles.Length} plików CSV");
            
            if (csvFiles.Length == 0)
            {
                Console.WriteLine("Nie znaleziono plików CSV.");
                return;
            }

            var data = LoadAllData(csvFiles);
            Console.WriteLine($"Wczytano {data.BikeRides.Count} przejazdów, {data.Stations.Count} stacji");

            AnalyzeSeasonalImpact(data);
            AnalyzeStationDeficit(data);
            AnalyzeUserSpeedDifference(data);
            FindLongestRoutes(data);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Wystąpił błąd: {ex.Message}");
        }
    }

    static BikeData LoadAllData(string[] csvFiles)
    {
        var bikeRides = new List<BikeRide>();
        var stations = new Dictionary<string, Station>();
        var seasonalStats = new Dictionary<string, SeasonalStats>();
        
        foreach (var filePath in csvFiles)
        {
            using (var reader = new StreamReader(filePath))
            {
                string line;
                bool isFirstLine = true;
                
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (isFirstLine) { isFirstLine = false; continue; }

                    var parts = line.Split(',');
                    if (parts.Length < 13) continue;

                    try
                    {
                        var ride = ParseBikeRide(parts);
                        if (ride == null) continue;

                        bikeRides.Add(ride);

                        UpdateStationData(stations, ride);
                        
                        UpdateSeasonalData(seasonalStats, ride);
                    }
                    catch { continue; }
                }
            }
        }
        
        return new BikeData
        {
            BikeRides = bikeRides,
            Stations = stations.Values.ToList(),
            SeasonalStats = seasonalStats.Values.ToList()
        };
    }

    static void UpdateStationData(Dictionary<string, Station> stations, BikeRide ride)
    {
        if (!string.IsNullOrEmpty(ride.StartStationId) && !string.IsNullOrEmpty(ride.StartStationName))
        {
            var startStationKey = $"{ride.StartStationId}_{ride.StartStationName}";
            if (!stations.ContainsKey(startStationKey))
            {
                stations[startStationKey] = new Station
                {
                    StationId = ride.StartStationId,
                    StationName = ride.StartStationName,
                    Latitude = ride.StartLat,
                    Longitude = ride.StartLng
                };
            }
            stations[startStationKey].Departures++;
        }

        if (!string.IsNullOrEmpty(ride.EndStationId) && !string.IsNullOrEmpty(ride.EndStationName))
        {
            var endStationKey = $"{ride.EndStationId}_{ride.EndStationName}";
            if (!stations.ContainsKey(endStationKey))
            {
                stations[endStationKey] = new Station
                {
                    StationId = ride.EndStationId,
                    StationName = ride.EndStationName,
                    Latitude = ride.EndLat,
                    Longitude = ride.EndLng
                };
            }
            stations[endStationKey].Arrivals++;
        }
    }

    static void UpdateSeasonalData(Dictionary<string, SeasonalStats> seasonalStats, BikeRide ride)
    {
        var season = GetSeason(ride.StartedAt.Month);
        
        if (!seasonalStats.ContainsKey(season))
        {
            seasonalStats[season] = new SeasonalStats { Season = season };
        }
        
        seasonalStats[season].RideCount++;
        seasonalStats[season].TotalDurationMinutes += ride.Duration.TotalMinutes;
        seasonalStats[season].TotalDistance += ride.Distance;
    }

    static void AnalyzeSeasonalImpact(BikeData data)
    {
        Console.WriteLine("\n=== WPŁYW PORY ROKU NA WYPOŻYCZENIA ===");
        
        var seasonalAnalysis = data.SeasonalStats
            .OrderBy(s => s.Season);


        foreach (var season in seasonalAnalysis)
        {
            if (season.RideCount > 0)
            {
                Console.WriteLine($"{season.Season}:");
                Console.WriteLine($"  Liczba przejazdów: {season.RideCount}");
                Console.WriteLine($"  Średni czas: {season.TotalDurationMinutes / season.RideCount:F2} min");
                Console.WriteLine($"  Średnia odległość: {season.TotalDistance / season.RideCount:F2} km");
                Console.WriteLine();
            }
        }
    }

    static void AnalyzeStationDeficit(BikeData data)
    {
        Console.WriteLine("\n=== DEFICYT ROWERÓW NA STACJACH ===");
        
        var topDeficitStations = data.Stations
            .Where(s => (s.Departures + s.Arrivals) >= 10)
            .OrderBy(s => s.Balance)
            .Take(10);

        Console.WriteLine("Stacje z największym deficytem rowerów:");
        foreach (var station in topDeficitStations)
        {
            Console.WriteLine($"{station.StationName} (ID: {station.StationId}):");
            Console.WriteLine($"  Wyjazdy: {station.Departures}, Przyjazdy: {station.Arrivals}, Bilans: {station.Balance}");
        }
    }

    static void AnalyzeUserSpeedDifference(BikeData data)
    {
        Console.WriteLine("\n=== RÓŻNICE PRĘDKOŚCI UŻYTKOWNIKÓW ===");
        
        var userSpeedAnalysis = data.BikeRides
            .GroupBy(ride => ride.MemberCasual)
            .Select(g => new UserSpeedStats
            {
                UserType = g.Key,
                TotalSpeed = g.Sum(ride => ride.SpeedKmh),
                RideCount = g.Count(),
                Speeds = g.Select(ride => ride.SpeedKmh).ToList()
            })
            .Where(x => x.RideCount >= 10);

        foreach (var userStats in userSpeedAnalysis)
        {
            Console.WriteLine($"{userStats.UserType}:");
            Console.WriteLine($"  Średnia prędkość: {userStats.TotalSpeed / userStats.RideCount:F2} km/h");
            Console.WriteLine($"  Liczba przejazdów: {userStats.RideCount}");
        }
    }

    static void FindLongestRoutes(BikeData data)
    {
        Console.WriteLine("\n=== NAJDLUŻSZE TRASY ===");
        
        var longestByDuration = data.BikeRides
            .OrderByDescending(ride => ride.Duration)
            .Take(5)
            .ToList();

        var longestByDistance = data.BikeRides
            .OrderByDescending(ride => ride.Distance)
            .Take(5)
            .ToList();

        Console.WriteLine("Najdłuższe trasy (wg czasu):");
        foreach (var ride in longestByDuration)
        {
            Console.WriteLine($"{ride.Duration:dd\\:hh\\:mm\\:ss} - {ride.StartStationName} → {ride.EndStationName}");
            Console.WriteLine($"  Data rozpoczecia: {ride.StartedAt:yyyy-MM-dd}, Data zakonczenia: {ride.EndedAt:yyyy-MM-dd}");
            Console.WriteLine($"Dystans: {ride.Distance:F2} km, Prędkość: {ride.SpeedKmh:F2} km/h");
        }
        
        Console.WriteLine("\nNajdłuższe trasy (wg odległości):");
        foreach (var ride in longestByDistance)
        {
            Console.WriteLine($"{ride.Distance:F2} km - {ride.StartStationName} → {ride.EndStationName}");
            Console.WriteLine($"  Czas: {ride.Duration:hh\\:mm\\:ss}, Data: {ride.StartedAt:yyyy-MM-dd}, Prędkość: {ride.SpeedKmh:F2} km/h");
        }
    }

    static BikeRide ParseBikeRide(string[] parts)
    {
        try
        {
            var ride = new BikeRide
            {
                RideId = parts[0],
                RideableType = parts[1],
                StartedAt = DateTime.ParseExact(parts[2], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                EndedAt = DateTime.ParseExact(parts[3], "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                StartStationName = parts[4],
                StartStationId = parts[5],
                EndStationName = parts[6],
                EndStationId = parts[7],
                MemberCasual = parts[12]
            };

            if (decimal.TryParse(parts[8], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal startLat))
                ride.StartLat = startLat;
            if (decimal.TryParse(parts[9], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal startLng))
                ride.StartLng = startLng;
            if (decimal.TryParse(parts[10], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal endLat))
                ride.EndLat = endLat;
            if (decimal.TryParse(parts[11], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal endLng))
                ride.EndLng = endLng;

            return ride;
        }
        catch
        {
            return null;
        }
    }

    static string GetSeason(int month)
    {
        return month switch
        {
            12 or 1 or 2 => "Zima",
            3 or 4 or 5 => "Wiosna",
            6 or 7 or 8 => "Lato",
            9 or 10 or 11 => "Jesień",
            _ => "Nieznana"
        };
    }
}

public class BikeData
{
    public List<BikeRide> BikeRides { get; set; } = new List<BikeRide>();
    public List<Station> Stations { get; set; } = new List<Station>();
    public List<SeasonalStats> SeasonalStats { get; set; } = new List<SeasonalStats>();
}

public class Station
{
    public string StationId { get; set; }
    public string StationName { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public int Departures { get; set; }
    public int Arrivals { get; set; }
    public int Balance => Arrivals - Departures;
}
