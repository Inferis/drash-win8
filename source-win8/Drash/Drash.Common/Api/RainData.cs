using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Drash.Common.Api
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
        }

        public int ChanceForEntries(int entries)
        {
            var weight = 1.0;
            var total = -1;
            foreach (var point in Points.Take(entries)) {
                var useWeight = point.Intensity == 100 ? weight : point.Intensity < 2 ? weight * 0.1 : weight / 2.0;

                if (weight > 0) {
                    total = Math.Max(total, 0) + (int)(point.Intensity * useWeight);
                    weight = weight - useWeight;
                }
                else if (point.Intensity > 80) {
                    var factor = 1.0 / Math.Max(1.0, 100.0 - point.Intensity);
                    total = (int)(total * (1.0 - factor) + point.Intensity * factor);
                }
            }

            total = Math.Min(total, 99);

            return total;
        }

        public int IntensityForEntries(int entries)
        {
            var totalIntensity = 0;
            var accounted = 0;
            foreach (var point in Points.Take(entries)) {
                accounted++;
                totalIntensity += point.AdjustedValue;
            }

            totalIntensity = totalIntensity > 0 ? Math.Min((int)((double)totalIntensity / accounted), 100) : 0;
            if (ChanceForEntries(entries) > 0)
                totalIntensity = Math.Max(1, totalIntensity);

            return totalIntensity;
        }

        public double PrecipitationForEntries(int entries)
        {
            var totalPrecipitation = Points.Take(entries).Sum(point => point.Precipitation / 60.0 * 5.0);

            if (ChanceForEntries(entries) > 0)
                totalPrecipitation = Math.Max(0.001, totalPrecipitation);

            return totalPrecipitation;
        }

        public IList<RainPoint> Points { get; set; }

        public static Task<RainData> TryParseAsync(string source)
        {
            return Task.Run(() => {
                var lines = source.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0)
                    return null;

                var points = lines.Select(RainPoint.Parse).SkipWhile(p => p.Stamp.AddMinutes(5) < DateTime.Now).ToList();

                var last = points.LastOrDefault() ?? new RainPoint(DateTime.Now, 0);
                points = points.Concat(Enumerable.Range(0, Math.Max(25 - points.Count, 0)).Select(x => last)).ToList();

                return new RainData(points.ToList());
            });
        }
    }
}