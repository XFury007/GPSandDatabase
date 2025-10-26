using Microsoft.Data.Sqlite;

namespace GPSm2u.Data;

public static class SchoolDatabase
{
    public static string DbPath { get; } = Path.Combine(FileSystem.AppDataDirectory, "schools.db");

    public static async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);

        await using var connection = new SqliteConnection($"Data Source={DbPath};");
        await connection.OpenAsync(cancellationToken);

        // Create table if needed
        const string createSql = """
        CREATE TABLE IF NOT EXISTS Schools (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            Latitude REAL NOT NULL,
            Longitude REAL NOT NULL,
            City TEXT NULL,
            State TEXT NULL
        );
        """;
        await using (var createCmd = connection.CreateCommand())
        {
            createCmd.CommandText = createSql;
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        // Check if we already have data
        await using (var countCmd = connection.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(1) FROM Schools;";
            var count = (long)(await countCmd.ExecuteScalarAsync(cancellationToken) ?? 0L);
            if (count > 0) return;
        }

        // Seed 25 school locations (sample coordinates)
        var seed = new (string Name, double Lat, double Lng, string City, string State)[]
        {
            ("Lincoln High School", 34.052235, -118.243683, "Los Angeles", "CA"),
            ("Roosevelt High School", 40.712776, -74.005974, "New York", "NY"),
            ("Washington High School", 41.878113, -87.629799, "Chicago", "IL"),
            ("Jefferson High School", 29.760427, -95.369804, "Houston", "TX"),
            ("Franklin High School", 33.448376, -112.074036, "Phoenix", "AZ"),
            ("Madison High School", 39.739236, -104.990251, "Denver", "CO"),
            ("Hamilton High School", 47.606209, -122.332069, "Seattle", "WA"),
            ("Adams High School", 32.776665, -96.796989, "Dallas", "TX"),
            ("Kennedy High School", 37.774929, -122.419418, "San Francisco", "CA"),
            ("Grant High School", 45.512230, -122.658722, "Portland", "OR"),
            ("Central High School", 39.952583, -75.165222, "Philadelphia", "PA"),
            ("Northview High School", 33.749001, -84.387978, "Atlanta", "GA"),
            ("Westview High School", 32.715736, -117.161087, "San Diego", "CA"),
            ("Eastview High School", 25.761681, -80.191788, "Miami", "FL"),
            ("Southridge High School", 38.627003, -90.199402, "St. Louis", "MO"),
            ("Riverside High School", 42.360081, -71.058884, "Boston", "MA"),
            ("Maple Grove High School", 44.977753, -93.265015, "Minneapolis", "MN"),
            ("Oak Ridge High School", 36.162663, -86.781601, "Nashville", "TN"),
            ("Pinecrest High School", 35.227085, -80.843124, "Charlotte", "NC"),
            ("Cedar Valley High School", 39.768402, -86.158066, "Indianapolis", "IN"),
            ("Hillside High School", 29.424122, -98.493629, "San Antonio", "TX"),
            ("Lakeside High School", 39.103119, -84.512016, "Cincinnati", "OH"),
            ("Valley View High School", 36.169941, -115.139832, "Las Vegas", "NV"),
            ("Summit High School", 45.815010, -122.678452, "Vancouver", "WA"),
            ("Brookside High School", 43.653225, -79.383186, "Toronto", "ON")
        };

        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        await using (var insertCmd = connection.CreateCommand())
        {
            insertCmd.CommandText = """
            INSERT INTO Schools (Name, Latitude, Longitude, City, State)
            VALUES ($name, $lat, $lng, $city, $state);
            """;
            var pName = insertCmd.CreateParameter(); pName.ParameterName = "$name"; insertCmd.Parameters.Add(pName);
            var pLat  = insertCmd.CreateParameter(); pLat.ParameterName  = "$lat";  insertCmd.Parameters.Add(pLat);
            var pLng  = insertCmd.CreateParameter(); pLng.ParameterName  = "$lng";  insertCmd.Parameters.Add(pLng);
            var pCity = insertCmd.CreateParameter(); pCity.ParameterName = "$city"; insertCmd.Parameters.Add(pCity);
            var pSt   = insertCmd.CreateParameter(); pSt.ParameterName   = "$state";insertCmd.Parameters.Add(pSt);

            foreach (var s in seed)
            {
                pName.Value = s.Name;
                pLat.Value  = s.Lat;
                pLng.Value  = s.Lng;
                pCity.Value = s.City;
                pSt.Value   = s.State;
                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        await tx.CommitAsync(cancellationToken);
    }

    public static async Task<IReadOnlyList<SchoolLocation>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = new List<SchoolLocation>();
        await using var connection = new SqliteConnection($"Data Source={DbPath};");
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Latitude, Longitude, City, State FROM Schools ORDER BY Name;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new SchoolLocation
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Latitude = reader.GetDouble(2),
                Longitude = reader.GetDouble(3),
                City = reader.IsDBNull(4) ? null : reader.GetString(4),
                State = reader.IsDBNull(5) ? null : reader.GetString(5),
            });
        }
        return list;
    }
}