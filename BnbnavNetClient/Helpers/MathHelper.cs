namespace BnbnavNetClient.Helpers;

public class MathHelper
{
    [Obsolete("Use double.RadiansToDegrees instead.")]
    public static double ToDeg(double rad) => double.RadiansToDegrees(rad);

    [Obsolete("Use double.DegreesToRadians instead.")]
    public static double ToRad(double deg) => double.DegreesToRadians(deg);
}