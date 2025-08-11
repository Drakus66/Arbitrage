namespace Arbitrage.Utils;

public static class CompareExtension
{
    private const double Epsilon = 0.0001;
    private const float FloatEpsilon = (float)Epsilon;
    private const decimal DecimalEpsilon = (decimal)Epsilon;

    public static bool IsEquals(this double self, double other, double epsilon = Epsilon) => Math.Abs(self - other) < epsilon;

    public static bool IsEquals(this decimal self, decimal other, decimal epsilon = DecimalEpsilon) => Math.Abs(self - other) < epsilon;

    public static bool IsEquals(this float self, float other, float epsilon = FloatEpsilon) => Math.Abs(self - other) < epsilon;

    public static bool IsGreaterThan(this decimal self, decimal other, decimal epsilon = DecimalEpsilon) => self - other > epsilon;

    public static bool IsLessThan(this decimal self, decimal other, decimal epsilon = DecimalEpsilon) => self - other < epsilon;

    public static bool IsGreaterThan(this float self, float other, float epsilon = FloatEpsilon) => self - other > epsilon;

    public static bool IsLessThan(this float self, float other, float epsilon = FloatEpsilon) => self - other < epsilon;

    public static bool IsGreaterThan(this double self, double other, double epsilon = Epsilon) => self - other > epsilon;

    public static bool IsLessThan(this double self, double other, double epsilon = Epsilon) => self - other < epsilon;
}
