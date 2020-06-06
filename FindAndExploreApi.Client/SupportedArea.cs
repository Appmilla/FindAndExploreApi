using System.Collections.Generic;

using MongoDB.Bson;
using MongoDB.Driver.GeoJsonObjectModel;
using MongoDB.Bson.Serialization.Attributes;

namespace FindAndExploreApi.Client
{
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
}
