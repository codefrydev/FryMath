using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FryMath.Core.Audio;

public sealed record HarmonicVoice(int HarmonicIndex, double Amplitude, double Phase);

public static class AudioToneGenerator
{
    private static Process? _activeAudioProcess;
    private static string? _currentTempWav;
    private static readonly object _lock = new();

    /// <summary>
    /// Generates a standard 16-bit mono 44.1 kHz PCM WAV byte buffer for given harmonics and duration.
    /// </summary>
    public static byte[] GenerateWavBuffer(
        double fundamentalFreq = 220.0,
        double durationSeconds = 1.5,
        double masterVolume = 0.35,
        IReadOnlyList<HarmonicVoice>? harmonics = null)
    {
        const int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * durationSeconds);
        short[] samples = new short[sampleCount];

        harmonics ??= new List<HarmonicVoice> { new(1, 1.0, 0.0) };

        double dt = 1.0 / sampleRate;

        for (int i = 0; i < sampleCount; i++)
        {
            double t = i * dt;
            double sum = 0.0;

            foreach (var h in harmonics)
            {
                if (h.Amplitude <= 1e-4) continue;
                double freq = fundamentalFreq * h.HarmonicIndex;
                if (freq > sampleRate / 2.0) continue; // Nyquist limit
                sum += h.Amplitude * Math.Sin(2.0 * Math.PI * freq * t + h.Phase);
            }

            // Envelope to avoid click at start and end
            double envelope = 1.0;
            double attackTime = 0.03;
            double releaseTime = 0.05;
            if (t < attackTime) envelope = t / attackTime;
            else if (t > durationSeconds - releaseTime) envelope = Math.Max(0.0, (durationSeconds - t) / releaseTime);

            double sampleVal = sum * masterVolume * envelope;
            sampleVal = Math.Clamp(sampleVal, -1.0, 1.0);
            samples[i] = (short)(sampleVal * 32767);
        }

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // RIFF Header
        bw.Write(Encoding.ASCII.GetBytes("RIFF"));
        bw.Write(36 + sampleCount * 2); // File size - 8
        bw.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        bw.Write(Encoding.ASCII.GetBytes("fmt "));
        bw.Write(16); // Subchunk1Size (16 for PCM)
        bw.Write((short)1); // AudioFormat (1 for PCM)
        bw.Write((short)1); // NumChannels (1 = Mono)
        bw.Write(sampleRate); // SampleRate
        bw.Write(sampleRate * 2); // ByteRate (SampleRate * NumChannels * BitsPerSample/8)
        bw.Write((short)2); // BlockAlign (NumChannels * BitsPerSample/8)
        bw.Write((short)16); // BitsPerSample

        // data chunk
        bw.Write(Encoding.ASCII.GetBytes("data"));
        bw.Write(sampleCount * 2);
        for (int i = 0; i < sampleCount; i++)
        {
            bw.Write(samples[i]);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Plays synthesized audio asynchronously using the OS platform audio command.
    /// </summary>
    public static void PlayHarmonicWave(
        double fundamentalFreq = 220.0,
        double durationSeconds = 1.2,
        double masterVolume = 0.35,
        IReadOnlyList<HarmonicVoice>? harmonics = null)
    {
        Task.Run(() =>
        {
            lock (_lock)
            {
                Stop();

                try
                {
                    byte[] wavBytes = GenerateWavBuffer(fundamentalFreq, durationSeconds, masterVolume, harmonics);
                    string tempPath = Path.Combine(Path.GetTempPath(), $"frymath_tone_{Guid.NewGuid():N}.wav");
                    File.WriteAllBytes(tempPath, wavBytes);
                    _currentTempWav = tempPath;

                    if (OperatingSystem.IsMacOS())
                    {
                        _activeAudioProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = "afplay",
                            Arguments = $"\"{tempPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        _activeAudioProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = "aplay",
                            Arguments = $"-q \"{tempPath}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        });
                    }
                }
                catch
                {
                    // Audio failure should not crash math studio
                }
            }
        });
    }

    /// <summary>
    /// Plays a single pure sine tone or rich harmonic spectrum at specified frequency.
    /// </summary>
    public static void PlayTone(double frequency = 440.0, double durationSeconds = 0.8, double masterVolume = 0.35)
    {
        PlayHarmonicWave(frequency, durationSeconds, masterVolume, new List<HarmonicVoice>
        {
            new(1, 1.0, 0.0),
            new(2, 0.25, 0.0),
            new(3, 0.10, 0.0)
        });
    }

    /// <summary>
    /// Plays the sonified musical frequency of a non-trivial Riemann zero.
    /// Fundamental baseline: gamma_1 ≈ 14.1347 -> 220.0 Hz (A3).
    /// </summary>
    public static void PlayRiemannZeroTone(double zeroHeight, double durationSeconds = 1.0, double masterVolume = 0.35)
    {
        double freq = Math.Clamp(220.0 * (zeroHeight / 14.134725), 110.0, 3500.0);
        PlayHarmonicWave(freq, durationSeconds, masterVolume, new List<HarmonicVoice>
        {
            new(1, 1.0, 0.0),
            new(2, 0.35, 0.1),
            new(3, 0.15, 0.2)
        });
    }

    public static void Stop()
    {
        lock (_lock)
        {
            if (_activeAudioProcess != null && !_activeAudioProcess.HasExited)
            {
                try
                {
                    _activeAudioProcess.Kill();
                }
                catch { /* Ignore */ }
                _activeAudioProcess.Dispose();
                _activeAudioProcess = null;
            }

            if (_currentTempWav != null && File.Exists(_currentTempWav))
            {
                try
                {
                    File.Delete(_currentTempWav);
                }
                catch { /* Ignore */ }
                _currentTempWav = null;
            }
        }
    }
}
