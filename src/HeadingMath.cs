using System;
namespace SignalisVrTracking
{
    public static class HeadingMath
    {
        public static double Yaw(double x, double y, double z, double w)
        {
            double forwardX = 2 * (x * z + w * y);
            double forwardZ = 1 - 2 * (x * x + y * y);
            // At vertical gaze, use the horizontal right axis instead.
            if (forwardX * forwardX + forwardZ * forwardZ < 0.000001)
                return Math.Atan2(2 * (w * y - x * z), 1 - 2 * (y * y + z * z));
            return Math.Atan2(forwardX, forwardZ);
        }
    }
}
