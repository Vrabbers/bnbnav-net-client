using System;

namespace BnbnavNetClient.Helpers;

public class MathHelper
{
    public static double ToDeg(double rad) => rad * (180 / double.Pi);

    public static double ToRad(double deg) => deg * (double.Pi / 180);
}