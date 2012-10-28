using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Drash
{
    public class RainPoint
    {
        static Random rnd = new Random();

        public RainPoint()
        {
            
        }

        public RainPoint(DateTime stamp, int value)
        {
            Stamp = stamp;
            Value = (int)(value * 100.0 / 255.0);
            Precipitation = value == 0 ? 0 : Math.Pow(10, (value - 109.0) / 32.0);
            AdjustedValue = (int)(Math.Min(Value, 70) / 70.0 * 100.0);
            var logisticIntensity = (Value - 14) / 40.0 * 12.0;
            Intensity = (int)Math.Round(1 / (1 + Math.Pow(Math.E, -logisticIntensity)) * 100);
        }

        public DateTime Stamp { get; set; }
        public int Value { get; set; }
        public int AdjustedValue { get; set; }
        public int Intensity { get; set; }
        public double Precipitation { get; set; }

        public static RainPoint Parse(string source)
        {
            var parts = source.Trim().Split(new[] { '|' });

            var stamp = DateTime.ParseExact(parts[1], "HH:mm", CultureInfo.InvariantCulture);
            if (stamp.Subtract(DateTime.Now).TotalHours < -2)
                stamp = stamp.AddDays(1);

            return new RainPoint(stamp, int.Parse(parts[0]));
        }
    }
}