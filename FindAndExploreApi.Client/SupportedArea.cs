using System.Collections.Generic;
using GeoJSON.Net.Geometry;

namespace FindAndExploreApi.Client
{
    public class SupportedArea
    {        
        public string Id { get; set; }
        
        public int LocationId { get; set; }

        public string Name { get; set; }

         public Polygon Polygon { get; set; }


        public void SetArea(List<IPosition> coordinatesList)
                                                        => SetPolygon(coordinatesList);

        private void SetPolygon(List<IPosition> coordinatesList)
        {
            Polygon = new Polygon(new List<LineString>
                {
                    new LineString(coordinatesList)
                });
        }
    }
}
