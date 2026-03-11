using System.Collections.Generic;

namespace ChangeOptimizer;

static class ChangeCalculator
{
    static readonly int[] Denominations = [5000, 2000, 1000, 500, 100, 50, 25, 10, 5, 1];

    public static List<int> GetOptimalChange(int amountCents)
    {
        var result = new List<int>();

        foreach (int denom in Denominations)
        {
            while (amountCents >= denom)
            {
                amountCents -= denom;
                result.Add(denom);
            }
        }
        
        return result;
    }
}
