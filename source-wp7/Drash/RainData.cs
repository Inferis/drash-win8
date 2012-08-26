using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Drash
{
    public class RainData
    {
        public RainData()
            : this(new List<RainPoint>())
        {

        }

        private RainData(List<RainPoint> points)
        {
            Points = points;
            var weight = 1.0;
            var totalIntensity = 0;
            var accounted = 0;
            var total = -1;
            var totalPrecipitation = 0.0;
            foreach (var point in Points) {
                var useWeight = point.Intensity == 100 ? weight : weight / 2.0;

                totalIntensity += point.AdjustedValue;
                accounted++;
                if (weight > 0) {
                    total = Math.Max(total, 0) + (int)(point.Intensity * useWeight);
                    weight = weight - useWeight;
                }
                totalPrecipitation += point.Precipitation/60.0*5.0;
            }

            Chance = Math.Min(total, 99);
            Intensity = totalIntensity > 0 ? Math.Min((int)((double)totalIntensity / accounted), 100) : 0;
            Precipitation = totalPrecipitation;
        }

        public IList<RainPoint> Points { get; set; }
        public int Chance { get; set; }
        public int Intensity { get; set; }
        public double Precipitation { get; set; }

        public static bool TryParse(string source, out RainData data)
        {
            data = null;

            var lines = source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return false;

            var points = lines.Select(RainPoint.Parse);
            points = points.SkipWhile(p => p.Stamp.AddMinutes(5) < DateTime.Now).Take(7);

            data = new RainData(points.ToList());
            return true;
        }
    }
}