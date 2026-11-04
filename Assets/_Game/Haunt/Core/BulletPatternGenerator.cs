using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    /// <summary>One world-space formation offset and its common flight velocity.</summary>
    public readonly struct PatternShot
    {
        public readonly float OffsetX, OffsetY, VelocityX, VelocityY;
        public PatternShot(float offsetX, float offsetY, float velocityX, float velocityY)
        {
            OffsetX = offsetX; OffsetY = offsetY;
            VelocityX = velocityX; VelocityY = velocityY;
        }
    }

    /// <summary>
    /// Engine-independent geometry. Angles use degrees, counterclockwise from +X,
    /// normalized to [-180, 180). Invalid patterns are empty and allocation is bounded.
    /// </summary>
    public static class BulletPatternGenerator
    {
        public const int MaximumPatternShots = 256;
        const double DegreesPerRadian = 180 / Math.PI;

        public static float[] Fan(int count, float centerAngleDegrees, float spreadDegrees)
        {
            if (count <= 0 || !Finite(centerAngleDegrees) || !Finite(spreadDegrees) || spreadDegrees < 0)
                return Array.Empty<float>();
            count = Math.Min(count, MaximumPatternShots);
            if (spreadDegrees >= 360) return Ring(count, Normalize((double)centerAngleDegrees - 180));
            var result = new float[count];
            if (count == 1) { result[0] = Normalize(centerAngleDegrees); return result; }
            double start = Normalize(centerAngleDegrees) - spreadDegrees * .5;
            for (int i = 0; i < count; i++)
                result[i] = Normalize(start + (double)spreadDegrees * i / (count - 1));
            return result;
        }

        public static float[] Ring(int count, float phaseDegrees)
        {
            if (count <= 0 || !Finite(phaseDegrees)) return Array.Empty<float>();
            count = Math.Min(count, MaximumPatternShots);
            var result = new float[count];
            double phase = Normalize(phaseDegrees);
            for (int i = 0; i < count; i++) result[i] = Normalize(phase + 360.0 * i / count);
            return result;
        }

        /// <summary>Remove rays on or inside the gap; zero width leaves a complete ring.</summary>
        public static float[] GapRing(int count, float phaseDegrees, float gapCenterDegrees, float gapWidthDegrees)
        {
            if (!Finite(gapCenterDegrees) || !Finite(gapWidthDegrees) || gapWidthDegrees < 0)
                return Array.Empty<float>();
            float[] ring = Ring(count, phaseDegrees);
            if (gapWidthDegrees == 0 || ring.Length == 0) return ring;
            if (gapWidthDegrees >= 360) return Array.Empty<float>();
            var result = new List<float>(ring.Length);
            double center = Normalize(gapCenterDegrees);
            foreach (float angle in ring)
                if (Math.Abs(Normalize(angle - center)) > gapWidthDegrees * .5 + .00001)
                    result.Add(angle);
            return result.ToArray();
        }

        /// <summary>One emission of evenly spaced spiral arms; advance phase between emissions.</summary>
        public static float[] Spiral(int arms, float phaseDegrees) => Ring(arms, phaseDegrees);

        /// <summary>A center plus four perpendicular arms; the complete cross travels together.</summary>
        public static PatternShot[] CrossFormation(int armLength, float spacing, float travelAngleDegrees, float speed)
        {
            if (armLength < 0 || !Finite(spacing) || spacing <= 0 || !Finite(travelAngleDegrees)
                || !Finite(speed) || speed < 0) return Array.Empty<PatternShot>();
            armLength = Math.Min(armLength, (MaximumPatternShots - 1) / 4);
            double radians = Normalize(travelAngleDegrees) / DegreesPerRadian;
            float vx = (float)(Math.Cos(radians) * speed), vy = (float)(Math.Sin(radians) * speed);
            var result = new PatternShot[1 + armLength * 4];
            result[0] = new PatternShot(0, 0, vx, vy);
            for (int i = 1; i <= armLength; i++)
            {
                float offset = (float)Math.Min(float.MaxValue, (double)spacing * i);
                int index = 1 + (i - 1) * 4;
                result[index] = new PatternShot(offset, 0, vx, vy);
                result[index + 1] = new PatternShot(-offset, 0, vx, vy);
                result[index + 2] = new PatternShot(0, offset, vx, vy);
                result[index + 3] = new PatternShot(0, -offset, vx, vy);
            }
            return result;
        }

        /// <summary>
        /// Aim at the earliest constant-velocity interception, limited by maxLeadSeconds.
        /// An unreachable target or invalid lead parameters falls back to direct aim.
        /// </summary>
        public static float PredictiveAim(float originX, float originY, float targetX, float targetY,
            float velocityX, float velocityY, float projectileSpeed, float maxLeadSeconds)
        {
            if (!Finite(originX) || !Finite(originY) || !Finite(targetX) || !Finite(targetY)) return 0;
            double px = (double)targetX - originX, py = (double)targetY - originY;
            float direct = Angle(px, py);
            if (!Finite(velocityX) || !Finite(velocityY) || !Finite(projectileSpeed) || projectileSpeed <= 0
                || !Finite(maxLeadSeconds) || maxLeadSeconds <= 0) return direct;
            double vx = velocityX, vy = velocityY, speed = projectileSpeed;
            double vv = vx * vx + vy * vy, ss = speed * speed;
            double a = vv - ss, b = 2 * (px * vx + py * vy), c = px * px + py * py;
            if (c == 0) return direct;
            double time = double.PositiveInfinity;
            if (Math.Abs(a) <= (vv + ss) * 1e-12)
            {
                if (b != 0) SelectPositiveTime(-c / b, ref time);
            }
            else
            {
                double discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    // The stable quadratic form avoids cancellation for very close
                    // target and projectile speeds and for a target moving away.
                    double squareRoot = Math.Sqrt(discriminant);
                    double q = -.5 * (b + (b >= 0 ? squareRoot : -squareRoot));
                    SelectPositiveTime(q / a, ref time);
                    if (q != 0) SelectPositiveTime(c / q, ref time);
                }
            }
            if (double.IsInfinity(time) || double.IsNaN(time)) return direct;
            time = Math.Min(time, maxLeadSeconds);
            return Angle(px + vx * time, py + vy * time);
        }

        public static float SpeedAt(float age, float initialSpeed, float minSpeed, float deceleration)
        {
            float start = Finite(initialSpeed) ? Math.Max(0, initialSpeed) : 0;
            float floor = Finite(minSpeed) ? Math.Min(start, Math.Max(0, minSpeed)) : 0;
            double elapsed = Finite(age) ? Math.Max(0, age) : 0;
            double decay = Finite(deceleration) ? Math.Max(0, deceleration) : 0;
            return (float)Math.Max(floor, start - elapsed * decay);
        }

        public static float TurnTowardsDegrees(float current, float target, float maxTurnDegrees)
        {
            float start = Finite(current) ? Normalize(current) : 0;
            if (!Finite(target) || !Finite(maxTurnDegrees) || maxTurnDegrees <= 0) return start;
            float delta = Normalize((double)Normalize(target) - start);
            return Normalize(start + Math.Sign(delta) * Math.Min(Math.Abs(delta), maxTurnDegrees));
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void SelectPositiveTime(double candidate, ref double earliest)
        {
            if (candidate >= 0 && candidate < earliest) earliest = candidate;
        }
        static float Angle(double x, double y) => Normalize(Math.Atan2(y, x) * DegreesPerRadian);
        static float Normalize(double angle)
        {
            angle %= 360;
            if (angle >= 180) angle -= 360;
            if (angle < -180) angle += 360;
            float result = (float)angle;
            return result >= 180 ? -180 : result;
        }
    }
}
