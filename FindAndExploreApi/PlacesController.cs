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

namespace FindAndExploreApi
{
    public static class PlacesController
    {              
        private static Lazy<MongoClient> lazyMongoClient = new Lazy<MongoClient>(InitializeMongoClient);
        private static MongoClient mongoClient => lazyMongoClient.Value;

        private static Lazy<IMongoDatabase> lazyMongoDatabase = new Lazy<IMongoDatabase>(InitializeMongoDatabase);
        private static IMongoDatabase mongoDatabase => lazyMongoDatabase.Value;

        private static Lazy<IMongoCollection<PointOfInterest>> lazyPointOfInterestCollection = new Lazy<IMongoCollection<PointOfInterest>>(InitializePointsOfInterest);
        private static IMongoCollection<PointOfInterest> pointOfInterestCollection => lazyPointOfInterestCollection.Value;

        private static Lazy<IMongoCollection<SupportedArea>> lazySupportedArea = new Lazy<IMongoCollection<SupportedArea>>(InitializeSupportedAreas);
        private static IMongoCollection<SupportedArea> supportedAreaCollection => lazySupportedArea.Value;

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

        private static IMongoCollection<PointOfInterest> InitializePointsOfInterest()
        {
            return mongoDatabase.GetCollection<PointOfInterest>("PointsOfInterest", new MongoCollectionSettings { ReadPreference = ReadPreference.Nearest });
        }

        private static IMongoCollection<SupportedArea> InitializeSupportedAreas()
        {            
            return mongoDatabase.GetCollection<SupportedArea>("SupportedAreas", new MongoCollectionSettings { ReadPreference = ReadPreference.Nearest });
        }        

        [FunctionName("GetCurrentArea")]
        public static async Task<IActionResult> RunAreas(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for GetCurrentArea.");

            double lat = 0.0F;
            double lon = 0.0F;

            string latQueryParameter = req.Query["lat"].ToString().Trim('f', 'F');

            string lonQueryParameter = req.Query["lon"].ToString().Trim('f', 'F');

            if (double.TryParse(latQueryParameter, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out lat))
            {
                lat = double.Parse(latQueryParameter, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture);
            }
            else
            {
                return new BadRequestObjectResult("Please pass a lat parameter");
            }

            if (double.TryParse(lonQueryParameter,NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out lon))
            {
                lon = double.Parse(lonQueryParameter, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowParentheses, CultureInfo.InvariantCulture);
            }
            else
            {
                return new BadRequestObjectResult("Please pass a lon parameter");
            }

            List<SupportedArea> supportedAreas;

            try 
            {
                var currentLocation = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(lat, lon));

                var areaFilter = Builders<SupportedArea>.Filter.GeoIntersects<GeoJson2DGeographicCoordinates>(x => x.Polygon, currentLocation);

                supportedAreas = await supportedAreaCollection.Find(areaFilter).ToListAsync();
            }
            catch (Exception e)
            {               
                return new BadRequestObjectResult("Error: " + e.Message);
            }

            return (ActionResult)new OkObjectResult(supportedAreas);
        }

        [FunctionName("GetAreasPointsOfInterest")]
        public static async Task<IActionResult> RunPointsOfInterest(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request for GetAreasPointsOfInterest.");

            string locationIdQueryParameter = req.Query["locationId"].ToString();

            int locationId = 0;
            
            if (int.TryParse(locationIdQueryParameter, NumberStyles.AllowParentheses, CultureInfo.InvariantCulture, out locationId))
            {
                locationId = int.Parse(locationIdQueryParameter, NumberStyles.AllowParentheses, CultureInfo.InvariantCulture);
            }
            else
            {
                return new BadRequestObjectResult("Please pass a locationId parameter");
            }

            List<PointOfInterest> pointsWithinCurrentArea;

            try
            {
               var areaLocationIdFilter = Builders<SupportedArea>.Filter.Where(a => a.LocationId == locationId);

                var supportedAreasByLocationId = await supportedAreaCollection.Find(areaLocationIdFilter).ToListAsync();
                if(!supportedAreasByLocationId.Any())
                    return new BadRequestObjectResult($"Error: No supported area found for locationId {locationId}");

                var currentArea = supportedAreasByLocationId.SingleOrDefault();

                var areaFilter = Builders<PointOfInterest>.Filter.GeoIntersects(x => x.Location, currentArea?.Polygon);

                pointsWithinCurrentArea = await pointOfInterestCollection.Find(areaFilter).ToListAsync();
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("Error: " + e.Message);
            }

            return (ActionResult)new OkObjectResult(pointsWithinCurrentArea);
        }
    }
}
