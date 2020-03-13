using System;
using System.Threading;

namespace TX11Frontend.Models
{
    public static class ScalingHelper
    {
        private static double scalingFactor = 1.00;
        public static double ScalingFactor => scalingFactor;

        public static void SetScalingFactor(double value)
        {
            Interlocked.Exchange(ref scalingFactor, value);
        }
    }
}
