using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace PhysicsEngine.Numerics;

using static Unsafe;

public static partial class MathG
{
    private static class Primes
    {
        public const int X = 501125321;
        public const int Y = 1136930381;
        public const int Z = 1720413743;
        public const int W = 1066037191;
    }

    private const int Prime = 0x27d4eb2d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static F Simplex<F, I>(F x, F y, I seed)
        where F : IFloatingPoint<F>
        where I : IBinaryInteger<I>
    {
        const double SQRT3 = 1.7320508075688772935274463415059;
        const double F2 = 0.5 * (SQRT3 - 1.0);
        const double G2 = (3.0 - SQRT3) / 6.0;

        F f = Broad<F>(F2) * (x + y);
        F x0 = F.Floor(x + f);
        F y0 = F.Floor(y + f);

        I primeX = I.CreateChecked(Primes.X);
        I primeY = I.CreateChecked(Primes.Y);

        I i = F.ConvertToInteger<I>(x0) * primeX;
        I j = F.ConvertToInteger<I>(y0) * primeY;

        F g = Broad<F>(G2) * (x0 + y0);
        x0 = x - (x0 - g);
        y0 = y - (y0 - g);

        bool i1 = x0 > y0;
        bool j1 = !i1;

        F x1 = Select(i1, x0 - F.One, x0) + Broad<F>(G2);
        F y1 = Select(j1, y0 - F.One, y0) + Broad<F>(G2);

        F x2 = x0 + Broad<F>(G2 * 2 - 1);
        F y2 = y0 + Broad<F>(G2 * 2 - 1);

        F fc0d5 = Broad<F>(0.5);
        F t0 = F.MultiplyAddEstimate(x0, -x0, F.MultiplyAddEstimate(y0, -y0, fc0d5));
        F t1 = F.MultiplyAddEstimate(x1, -x1, F.MultiplyAddEstimate(y1, -y1, fc0d5));
        F t2 = F.MultiplyAddEstimate(x2, -x2, F.MultiplyAddEstimate(y2, -y2, fc0d5));

        t0 = F.Max(t0, F.Zero);
        t1 = F.Max(t1, F.Zero);
        t2 = F.Max(t2, F.Zero);

        t0 *= t0;
        t0 *= t0;
        t1 *= t1;
        t1 *= t1;
        t2 *= t2;
        t2 *= t2;

        F n0 = GetGradientDot(
            HashPrimes(seed, i, j),
            x0, y0);

        F n1 = GetGradientDot(
            HashPrimes(
                seed,
                Select(i1, i + primeX, i),
                Select(j1, j + primeY, j)),
            x1, y1);

        F n2 = GetGradientDot(
            HashPrimes(
                seed,
                i + primeX,
                j + primeY),
            x2, y2);

        F last = F.MultiplyAddEstimate(n0, t0, F.MultiplyAddEstimate(n1, t1, n2 * t2));
        return Broad<F>(38.283687591552734375) * last;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static I HashPrimes<I>(I seed, I x, I y)
        where I : IBinaryInteger<I>
    {
        I hash = seed ^ (x ^ y);
        hash *= I.CreateChecked(Prime);
        return (hash >> 15) ^ hash;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static F Simplex<F, I>(F x, F y, F z, I seed)
        where F : IFloatingPoint<F>
        where I : IBinaryInteger<I>
    {
        const double F3 = 1.0 / 3.0;
        const double G3 = 1.0 / 2.0;

        F s = Broad<F>(F3) * (x + y + z);
        x = x + s;
        y = y + s;
        z = z + s;

        F x0 = F.Floor(x);
        F y0 = F.Floor(y);
        F z0 = F.Floor(z);
        F xi = x - x0;
        F yi = y - y0;
        F zi = z - z0;

        I primeX = I.CreateChecked(Primes.X);
        I primeY = I.CreateChecked(Primes.Y);
        I primeZ = I.CreateChecked(Primes.Z);

        I i = F.ConvertToInteger<I>(x0) * primeX;
        I j = F.ConvertToInteger<I>(y0) * primeY;
        I k = F.ConvertToInteger<I>(z0) * primeZ;

        bool x_ge_y = xi >= yi;
        bool y_ge_z = yi >= zi;
        bool x_ge_z = xi >= zi;

        F fcG3 = Broad<F>(G3);
        F g = fcG3 * (xi + yi + zi);
        x0 = xi - g;
        y0 = yi - g;
        z0 = zi - g;

        bool i1 = x_ge_y & x_ge_z;
        bool j1 = y_ge_z & (!x_ge_y);
        bool k1 = (!x_ge_z) & (!y_ge_z);

        bool i2 = x_ge_y | x_ge_z;
        bool j2 = (!x_ge_y) | y_ge_z;
        bool k2 = !(x_ge_z & y_ge_z);

        F x1 = Select(i1, x0 - F.One, x0) + fcG3;
        F y1 = Select(j1, y0 - F.One, y0) + fcG3;
        F z1 = Select(k1, z0 - F.One, z0) + fcG3;

        F fc2G3 = Broad<F>(G3 * 2);
        F x2 = Select(i2, x0 - F.One, x0) + fc2G3;
        F y2 = Select(j2, y0 - F.One, y0) + fc2G3;
        F z2 = Select(k2, z0 - F.One, z0) + fc2G3;

        F fc3G3 = Broad<F>(G3 * 3 - 1);
        F x3 = x0 + fc3G3;
        F y3 = y0 + fc3G3;
        F z3 = z0 + fc3G3;

        F fc0d6 = Broad<F>(0.6);
        F t0 = F.MultiplyAddEstimate(
            x0, -x0, F.MultiplyAddEstimate(y0, -y0, F.MultiplyAddEstimate(z0, -z0, fc0d6)));
        F t1 = F.MultiplyAddEstimate(
            x1, -x1, F.MultiplyAddEstimate(y1, -y1, F.MultiplyAddEstimate(z1, -z1, fc0d6)));
        F t2 = F.MultiplyAddEstimate(
            x2, -x2, F.MultiplyAddEstimate(y2, -y2, F.MultiplyAddEstimate(z2, -z2, fc0d6)));
        F t3 = F.MultiplyAddEstimate(
            x3, -x3, F.MultiplyAddEstimate(y3, -y3, F.MultiplyAddEstimate(z3, -z3, fc0d6)));

        t0 = F.Max(t0, F.Zero);
        t1 = F.Max(t1, F.Zero);
        t2 = F.Max(t2, F.Zero);
        t3 = F.Max(t3, F.Zero);

        t0 *= t0;
        t0 *= t0;
        t1 *= t1;
        t1 *= t1;
        t2 *= t2;
        t2 *= t2;
        t3 *= t3;
        t3 *= t3;

        F n0 = GetGradientDot(
            HashPrimes(seed, i, j, k), x0, y0, z0);

        F n1 = GetGradientDot(
            HashPrimes(
                seed,
                Select(i1, i + primeX, i),
                Select(j1, j + primeY, j),
                Select(k1, k + primeZ, k)),
            x1, y1, z1);

        F n2 = GetGradientDot(
            HashPrimes(
                seed,
                Select(i2, i + primeX, i),
                Select(j2, j + primeY, j),
                Select(k2, k + primeZ, k)),
            x2, y2, z2);

        F n3 = GetGradientDot(
            HashPrimes(
                seed,
                i + primeX,
                j + primeY,
                k + primeZ),
            x3, y3, z3);

        F last = F.MultiplyAddEstimate(
            n0, t0, F.MultiplyAddEstimate(n1, t1, F.MultiplyAddEstimate(n2, t2, n3 * t3)));

        return Broad<F>(32.69428253173828125) * last;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static I HashPrimes<I>(I seed, I x, I y, I z)
        where I : IBinaryInteger<I>
    {
        I hash = seed ^ (x ^ (y ^ z));
        hash *= I.CreateChecked(Prime);
        return (hash >> 15) ^ hash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static F GetGradientDot<F, I>(I hash, F fX, F fY)
        where F : IFloatingPoint<F>
        where I : IBinaryInteger<I>
    {
        // ( 1+R2, 1 ) ( -1-R2, 1 ) ( 1+R2, -1 ) ( -1-R2, -1 )
        // ( 1, 1+R2 ) ( 1, -1-R2 ) ( -1, 1+R2 ) ( -1, -1-R2 )

        bool bit1 = (hash & I.One) != I.Zero;
        bool bit2 = (hash & (I.One << 1)) != I.Zero;
        bool bit4 = (hash & (I.One << 2)) != I.Zero;

        fX = FlipSign(fX, bit1);
        fY = FlipSign(fY, bit2);

        F a = Select(bit4, fY, fX);
        F b = Select(bit4, fX, fY);

        const double ROOT2 = 1.4142135623730950488;
        return F.MultiplyAddEstimate(Broad<F>(1.0 + ROOT2), a, b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static F GetGradientDot<F, I>(I hash, F fX, F fY, F fZ)
        where F : IFloatingPoint<F>
        where I : IBinaryInteger<I>
    {
        I hasha13 = hash & I.CreateChecked(13);

        //if h < 8 then x, else y
        F u = Select(hasha13 < I.CreateChecked(8), fX, fY);

        //if h < 4 then y else if h is 12 or 14 then x else z
        F v = Select(hasha13 == I.CreateChecked(12), fX, fZ);
        v = Select(hasha13 < I.CreateChecked(2), fY, v);

        //if h1 then -u else u
        //if h2 then -v else v
        bool h1 = (hash & I.One) != I.Zero;
        bool h2 = (hash & (I.One << 1)) != I.Zero;

        return FlipSign(u, h1) + FlipSign(v, h2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Select<T>(bool condition, T left, T right)
    {
        if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
        {
            var c = Vector128.Create((condition ? 1 : 0) - 1).As<int, T>();
            var a = Vector128.CreateScalarUnsafe(left);
            var b = Vector128.CreateScalarUnsafe(right);
            return Vector128.ConditionalSelect(c, b, a).ToScalar();
        }
        return condition ? left : right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static F FlipSign<F>(F value, bool bit)
        where F : ISignedNumber<F>
    {
        if (Vector128<F>.IsSupported)
        {
            Vector128<F> sign = Vector128.CreateScalarUnsafe(Select(bit, -F.Zero, F.Zero));
            return (Vector128.CreateScalarUnsafe(value) ^ sign).ToScalar();
        }
        return value * Select(bit, F.NegativeOne, F.One);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Broad<T>(double value)
        where T : INumber<T>
    {
        if (SizeOf<T>() <= sizeof(float))
            return T.CreateChecked((float) value);
        return T.CreateChecked(value);
    }
}
