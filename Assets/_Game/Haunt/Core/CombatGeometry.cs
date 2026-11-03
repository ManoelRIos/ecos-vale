using System;

namespace Ecos.Haunt
{
    /// <summary>Inclusive swept tests: fast projectiles cannot jump over targets.</summary>
    public static class CombatGeometry
    {
        public static bool SegmentHitsCircle(float ax, float ay, float bx, float by, float cx, float cy, float radius)
        {
            return CircleHitFraction(ax, ay, bx, by, cx, cy, radius, out _);
        }

        public static bool SegmentHitsBox(float ax, float ay, float bx, float by, float minX, float minY, float maxX, float maxY)
        {
            return BoxHitFraction(ax, ay, bx, by, minX, minY, maxX, maxY, out _);
        }

        /// <summary>Earliest contact along [A,B], including tangency and start-inside.</summary>
        public static bool CircleHitFraction(float ax, float ay, float bx, float by, float cx, float cy, float radius, out float fraction)
        {
            fraction = float.PositiveInfinity;
            if (radius < 0) return false;
            double dx = (double)bx - ax, dy = (double)by - ay;
            double ox = (double)ax - cx, oy = (double)ay - cy;
            double c = ox * ox + oy * oy - (double)radius * radius;
            if (c <= 0) { fraction = 0; return true; }
            double a = dx * dx + dy * dy;
            if (a == 0) return false;
            double b = ox * dx + oy * dy;
            double discriminant = b * b - a * c;
            if (discriminant < 0) return false;
            double first = (-b - Math.Sqrt(discriminant)) / a;
            if (!(first >= 0 && first <= 1)) return false;
            fraction = (float)first;
            return true;
        }

        public static bool BoxHitFraction(float ax, float ay, float bx, float by, float minX, float minY, float maxX, float maxY, out float fraction)
        {
            fraction = float.PositiveInfinity;
            if (minX > maxX || minY > maxY) return false;
            double enter = 0, exit = 1;
            if (!Clip(ax, (double)bx - ax, minX, maxX, ref enter, ref exit)
                || !Clip(ay, (double)by - ay, minY, maxY, ref enter, ref exit)) return false;
            fraction = (float)enter;
            return true;
        }

        static bool Clip(double origin, double delta, double minimum, double maximum, ref double enter, ref double exit)
        {
            if (delta == 0) return origin >= minimum && origin <= maximum;
            double first = (minimum - origin) / delta, second = (maximum - origin) / delta;
            if (first > second) { double temporary = first; first = second; second = temporary; }
            enter = Math.Max(enter, first);
            exit = Math.Min(exit, second);
            return enter <= exit;
        }
    }
}
