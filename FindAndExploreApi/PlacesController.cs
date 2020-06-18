using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

using System.Collections.Generic;
using System.Security.Authentication;
using System.Globalization;
using System.Linq;
using FindAndExploreApi.Client;
using GeoJSON.Net.Geometry;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FindAndExploreApi
{
    public static class PlacesController
    {              
        private static readonly Lazy<MongoClient> lazyMongoClient = new Lazy<MongoClient>(InitializeMongoClient);
        private static MongoClient mongoClient => lazyMongoClient.Value;

        private static readonly Lazy<IMongoDatabase> lazyMongoDatabase = new Lazy<IMongoDatabase>(InitializeMongoDatabase);
        private static IMongoDatabase mongoDatabase => lazyMongoDatabase.Value;

        private static readonly Lazy<IMongoCollection<MongoPointOfInterest>> lazyPointOfInterestCollection = new Lazy<IMongoCollection<MongoPointOfInterest>>(InitializePointsOfInterest);
        private static IMongoCollection<MongoPointOfInterest> pointOfInterestCollection => lazyPointOfInterestCollection.Value;

        private static readonly Lazy<IMongoCollection<MongoFence>> lazySupportedArea = new Lazy<IMongoCollection<MongoFence>>(InitializeSupportedAreas);
        private static IMongoCollection<MongoFence> supportedAreaCollection => lazySupportedArea.Value;

        private static MongoClient InitializeMongoClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");

            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
            settings.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };

            return new MongoClient(settings);            
        }

        private static IMongoDatabase InitializeMongoDatabase()
        {
            return mongoClient.GetDatabase("Places");
        }

        private static IMongoCollection<MongoPointOfInterest> InitializePointsOfInterest()
        {
            return mongoDatabase.GetCollection<MongoPointOfInterest>("PointsOfInterest", new MongoCollectionSettings { ReadPreference = ReadPreference.Nearest });
        }

        private static IMongoCollection<MongoFence> InitializeSupportedAreas()
        {            
            return mongoDatabase.GetCollection<MongoFence>("SupportedAreas", new MongoCollectionSettings { ReadPreference = ReadPreference.Nearest });
        }
        
        [FunctionName("Health")]
        public static async Task<IActionResult> RunHealthCheck(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for Health Check.");

            var supportedAreasOk = await supportedAreaCollection.AsQueryable().AnyAsync();

            var pointsOfInterestOk = await pointOfInterestCollection.AsQueryable().AnyAsync();

            return supportedAreasOk && pointsOfInterestOk
                ? new OkObjectResult(HealthCheckResult.Healthy("A healthy result."))
                : new OkObjectResult(HealthCheckResult.Unhealthy("An unhealthy result."));
        }        

        [FunctionName("CurrentArea")]
        public static async Task<IActionResult> RunAreas(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for CurrentArea.");

            var latQueryParameter = req.Query["lat"].ToString().Trim('f', 'F');

            var lonQueryParameter = req.Query["lon"].ToString().Trim('f', 'F');

            if (double.TryParse(latQueryParameter, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out var lat))
            {
                lat = double.Parse(latQueryParameter, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture);
            }
            else
            {
                return new BadRequestObjectResult("Please pass a lat parameter");
            }

            if (double.TryParse(lonQueryParameter,NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out var lon))
            {
                lon = double.Parse(lonQueryParameter, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture);
            }
            else
            {
                return new BadRequestObjectResult("Please pass a lon parameter");
            }
            
            var result = new List<SupportedArea>();

            try
            {                
                var currentLocation = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(lon, lat));

                var areaFilter = Builders<MongoFence>.Filter.GeoIntersects(x => x.Polygon, currentLocation);

                var supportedAreas = await supportedAreaCollection.Find(areaFilter).ToListAsync();
                
                foreach (var area in supportedAreas)
                {
                    var newArea = new SupportedArea
                    {
                        Id = area.Id.ToString(),
                        Name = area.Name,
                        LocationId = area.LocationId                        
                    };

                    var positions = area.Polygon.Coordinates.Exterior.Positions.Select(point => new Position(point.Latitude, point.Longitude)).Cast<IPosition>().ToList();

                    newArea.SetArea(positions);
                    result.Add(newArea);
                }                
            }
            catch (Exception e)
            {               
                return new BadRequestObjectResult("Error: " + e.Message);
            }

            return new OkObjectResult(result);
        }

        [FunctionName("PointsOfInterest")]
        public static async Task<IActionResult> RunPointsOfInterest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for PointsOfInterest.");

            var locationIdQueryParameter = req.Query["locationId"].ToString();

            if (int.TryParse(locationIdQueryParameter, NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out var locationId))
            {
                locationId = int.Parse(locationIdQueryParameter, NumberStyles.AllowParentheses, CultureInfo.InvariantCulture);
            }
            else
            {
                return new BadRequestObjectResult("Please pass a locationId parameter");
            }
           
            var result = new List<PointOfInterest>();

            try
            {
               var areaLocationIdFilter = Builders<MongoFence>.Filter.Where(a => a.LocationId == locationId);

                var supportedAreasByLocationId = await supportedAreaCollection.Find(areaLocationIdFilter).ToListAsync();
                if(!supportedAreasByLocationId.Any())
                    return new BadRequestObjectResult($"Error: No supported area found for locationId {locationId}");

                var currentArea = supportedAreasByLocationId.SingleOrDefault();

                var areaFilter = Builders<MongoPointOfInterest>.Filter.GeoIntersects(x => x.Location, currentArea?.Polygon);

                var pointsWithinCurrentArea = await pointOfInterestCollection.Find(areaFilter).ToListAsync();

                result.AddRange(pointsWithinCurrentArea.Select(point => new PointOfInterest
                {
                    Id = point.Id.ToString(),
                    Name = point.Name,
                    Category = point.Category,
                    Location = new Point(new Position(point.Location.Coordinates.Latitude, point.Location.Coordinates.Longitude))
                }));
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("Error: " + e.Message);
            }

            return new OkObjectResult(result);
        }
    }
}
