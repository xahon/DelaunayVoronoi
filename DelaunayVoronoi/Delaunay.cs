using System;
using System.Collections.Generic;
using System.Linq;
using Point = System.Drawing.PointF;

namespace DelaunayVoronoi
{
    public class DelaunayVoronoi
    {
        private float MaxX { get; set; }
        private float MaxY { get; set; }
        private IEnumerable<Triangle> _border;
        private Dictionary<Point, HashSet<Triangle>> _adjacentTrianglesInfo;

        public IEnumerable<Point> GeneratePoints(int amount, float maxX, float maxY)
        {
            MaxX = maxX;
            MaxY = maxY;

            _adjacentTrianglesInfo = new Dictionary<Point, HashSet<Triangle>>();

            // TODO make more beautiful
            var point0 = new Point(0, 0);
            var point1 = new Point(0, MaxY);
            var point2 = new Point(MaxX, MaxY);
            var point3 = new Point(MaxX, 0);
            var points = new List<Point>
            {
                point0,
                point1,
                point2,
                point3
            };
            var tri1 = CreateTriangle(point0, point1, point2);
            var tri2 = CreateTriangle(point0, point2, point3);
            _border = new List<Triangle>
            {
                tri1,
                tri2
            };

            var random = new Random();
            for (int i = 0; i < amount - 4; i++)
            {
                var pointX = (float) random.NextDouble() * MaxX;
                var pointY = (float) random.NextDouble() * MaxY;
                points.Add(new Point(pointX, pointY));
            }

            return points;
        }

        public IEnumerable<Triangle> BowyerWatsonTriangulation(IEnumerable<Point> points)
        {
            //var supraTriangle = GenerateSupraTriangle();
            var triangulation = new HashSet<Triangle>(_border);

            foreach (var point in points)
            {
                var badTriangles = FindBadTriangles(point, triangulation);
                var polygon = FindHoleBoundaries(badTriangles);

                foreach (var triangle in badTriangles)
                {
                    foreach (var vertex in triangle.Vertices)
                    {
                        _adjacentTrianglesInfo[vertex].Remove(triangle);
                    }
                }

                triangulation.RemoveWhere(o => badTriangles.Contains(o));

                foreach (var edge in polygon)
                {
                    var triangle = CreateTriangle(point, edge.Point1, edge.Point2);
                    triangulation.Add(triangle);
                }
            }

            //triangulation.RemoveWhere(o => o.Vertices.Any(v => supraTriangle.Vertices.Contains(v)));
            return triangulation;
        }

        public IEnumerable<Edge> GenerateVoronoiEdges(IEnumerable<Triangle> triangulation)
        {
            var voronoiEdges = new HashSet<Edge>();
            foreach (var triangle in triangulation)
            {
                var trianglesWithSharedEdges = triangle.Vertices.SelectMany(v =>
                    _adjacentTrianglesInfo[v].Where(t => t != triangle && triangle.SharesEdgeWith(t))
                );

                foreach (var neighbor in trianglesWithSharedEdges)
                {
                    var edge = new Edge(triangle.Circumcenter, neighbor.Circumcenter);
                    voronoiEdges.Add(edge);
                }
            }

            return voronoiEdges;
        }

        public Triangle CreateTriangle(Point a, Point b, Point c)
        {
            var triangle = new Triangle(a, b, c);

            if (!_adjacentTrianglesInfo.TryGetValue(a, out _))
                _adjacentTrianglesInfo.Add(a, new HashSet<Triangle>());
            if (!_adjacentTrianglesInfo.TryGetValue(b, out _))
                _adjacentTrianglesInfo.Add(b, new HashSet<Triangle>());
            if (!_adjacentTrianglesInfo.TryGetValue(c, out _))
                _adjacentTrianglesInfo.Add(c, new HashSet<Triangle>());

            _adjacentTrianglesInfo[a].Add(triangle);
            _adjacentTrianglesInfo[b].Add(triangle);
            _adjacentTrianglesInfo[c].Add(triangle);
            return triangle;
        }

        private IEnumerable<Edge> FindHoleBoundaries(IEnumerable<Triangle> badTriangles)
        {
            var edges = new List<Edge>();
            foreach (var triangle in badTriangles)
            {
                edges.Add(new Edge(triangle.Vertices[0], triangle.Vertices[1]));
                edges.Add(new Edge(triangle.Vertices[1], triangle.Vertices[2]));
                edges.Add(new Edge(triangle.Vertices[2], triangle.Vertices[0]));
            }

            var boundaryEdges = edges.GroupBy(o => o).Where(o => o.Count() == 1).Select(o => o.First());
            return boundaryEdges.ToList();
        }

        private Triangle GenerateSupraTriangle()
        {
            //   1  -> maxX
            //  / \
            // 2---3
            // |
            // v maxY
            const int margin = 500;
            var point1 = new Point(0.5f * MaxX, -2f * MaxX - margin);
            var point2 = new Point(-2f * MaxY - margin, 2f * MaxY + margin);
            var point3 = new Point(2f * MaxX + MaxY + margin, 2f * MaxY + margin);
            return CreateTriangle(point1, point2, point3);
        }

        private ISet<Triangle> FindBadTriangles(Point point, IEnumerable<Triangle> triangles)
        {
            var badTriangles = triangles.Where(o => o.IsPointInsideCircumcircle(point));
            return new HashSet<Triangle>(badTriangles);
        }
    }
}
