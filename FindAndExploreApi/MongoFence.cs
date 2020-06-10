using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace FindAndExploreApi
{
    internal class MongoFence
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
}
