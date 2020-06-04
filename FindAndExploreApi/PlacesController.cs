using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Bson.Serialization.Attributes;

using System.Collections.Generic;
using System.Security.Authentication;
using System.Globalization;
using System.Linq;

namespace FindAndExploreApi
{
    public class PointOfInterest
    {
        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Category")]
        public string Category { get; set; }

        [BsonElement("Location")]
        public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }

        [BsonId]
        public ObjectId Id { get; set; }
    }

    public class SupportedArea
    {
        [BsonId]
        public ObjectId Id { get; set; }

        [BsonElement("LocationId")]
        public int LocationId { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; }

        [BsonElement("Polygon")]
        public GeoJsonPolygon<GeoJson2DGeographicCoordinates> Polygon { get; private set; }


        public void SetArea(List<GeoJson2DGeographicCoordinates> coordinatesList)
                                                        => SetPolygon(coordinatesList);

        private void SetPolygon(List<GeoJson2DGeographicCoordinates> coordinatesList)
        {
            Polygon = new GeoJsonPolygon<GeoJson2DGeographicCoordinates>(
                      new GeoJsonPolygonCoordinates<GeoJson2DGeographicCoordinates>(
                      new GeoJsonLinearRingCoordinates<GeoJson2DGeographicCoordinates>(
                                                                     coordinatesList)));
        }
    }

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
            // Perform any initialization here
            //v2.5 on connection string
            //var connectionString = "mongodb+srv://richardwoollcott:Incywincy100@geocluster0-pxkjn.azure.mongodb.net/test?ssl=true&retryWrites=true&w=majority";

            //earlier driver connection string was needed for mobile app
            //string connectionString = @"mongodb://richardwoollcott:Incywincy100@geocluster0-shard-00-00-pxkjn.azure.mongodb.net:27017,geocluster0-shard-00-01-pxkjn.azure.mongodb.net:27017,geocluster0-shard-00-02-pxkjn.azure.mongodb.net:27017/test?ssl=true&replicaSet=GeoCluster0-shard-0&authSource=admin&retryWrites=true&w=majority";

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
