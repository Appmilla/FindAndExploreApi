using GeoJSON.Net.Geometry;

namespace FindAndExploreApi.Client
{
    public class PointOfInterest
    {       
        public string Name { get; set; }

        public string Category { get; set; }

        public Point Location { get; set; }

        public string Id { get; set; }
    }
}
