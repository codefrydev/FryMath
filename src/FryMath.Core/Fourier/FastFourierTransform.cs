using System;
using System.Numerics;

namespace FryMath.Core.Fourier;

public static class FastFourierTransform
{
    /// <summary>
    /// In-place radix-2 Cooley-Tukey Fast Fourier Transform (FFT).
    /// </summary>
    public static void FFT(Complex[] buffer, bool invert = false)
    {
        int n = buffer.Length;
        if (n <= 1) return;

        // Bit-reversal permutation
        int j = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (i < j)
            {
                (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
            }
            int k = n >> 1;
            while (k <= j)
            {
                j -= k;
                k >>= 1;
            }
            j += k;
        }

        // Danielson-Lanczos section
        for (int len = 2; len <= n; len <<= 1)
        {
            double angle = 2.0 * Math.PI / len * (invert ? -1.0 : 1.0);
            Complex wlen = new(Math.Cos(angle), Math.Sin(angle));

            for (int i = 0; i < n; i += len)
            {
                Complex w = Complex.One;
                for (int k = 0; k < len / 2; k++)
                {
                    Complex u = buffer[i + k];
                    Complex v = buffer[i + k + len / 2] * w;
                    buffer[i + k] = u + v;
                    buffer[i + k + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }

        if (invert)
        {
            for (int i = 0; i < n; i++)
            {
                buffer[i] /= n;
            }
        }
    }

    public static double[] ComputeMagnitudeSpectrum(double[] timeDomainSignal)
    {
        int n = 1;
        while (n < timeDomainSignal.Length) n <<= 1;

        var complexSignal = new Complex[n];
        for (int i = 0; i < timeDomainSignal.Length; i++)
        {
            complexSignal[i] = new Complex(timeDomainSignal[i], 0);
        }

        FFT(complexSignal);

        int half = n / 2;
        var magnitudes = new double[half];
        for (int i = 0; i < half; i++)
        {
            magnitudes[i] = complexSignal[i].Magnitude / (n / 2.0);
        }
        return magnitudes;
    }
}
