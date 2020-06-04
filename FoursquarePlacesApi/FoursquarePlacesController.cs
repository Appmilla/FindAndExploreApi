using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FourSquare.SharpSquare.Core;

namespace FoursquarePlacesApi
{
    public static class FoursquarePlacesController
    {
        private static Lazy<SharpSquare> lazySharpSquare = new Lazy<SharpSquare>(InitializeSharpSquare);
        private static SharpSquare sharpSquare => lazySharpSquare.Value;

        private static SharpSquare InitializeSharpSquare()
        {
            var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");

            return new SharpSquare(clientId, clientSecret);
        }

        [FunctionName("GetVenuesAtLocation")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string latQueryParameter = req.Query["lat"].ToString().Trim('f', 'F', '{', '}');            
            if(string.IsNullOrEmpty(latQueryParameter)) 
                return new BadRequestObjectResult("Please pass a lat parameter");

            string lonQueryParameter = req.Query["lon"].ToString().Trim('f', 'F', '{', '}');
            if (string.IsNullOrEmpty(lonQueryParameter))
                return new BadRequestObjectResult("Please pass a lon parameter");

            /*
            var venues = sharpSquare.SearchVenues(new Dictionary<string, string>
            {
                { "ll", $"{latQueryParameter}, {lonQueryParameter}"}
                //{ "near","new york" },
                //{ "query","peter luger steak house" }
            });
           */

            var venues = sharpSquare.SearchVenues(new Dictionary<string, string>
            {
                { "ll", "51.079287, -3.018348"} //Whitestocks lat long
                //{ "near","new york" },
                //{ "query","peter luger steak house" }
            });

            return new OkObjectResult(venues);
        }
    }
}
