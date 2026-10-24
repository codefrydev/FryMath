using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FryMath.Core.Information;

public enum InformationMode
{
    HuffmanAndEntropy,
    ConstellationAndCapacity,
    BinarySymmetricChannel,
    KlDivergence
}

public enum ModulationScheme
{
    Bpsk,
    Qpsk,
    Psk8,
    Qam16
}

public sealed class HuffmanNode : IComparable<HuffmanNode>
{
    public char? Character { get; set; }
    public double Frequency { get; set; }
    public int Count { get; set; }
    public HuffmanNode? Left { get; set; }
    public HuffmanNode? Right { get; set; }
    public string Code { get; set; } = string.Empty;

    // Visual layout coordinates for tree rendering
    public double X { get; set; }
    public double Y { get; set; }
    public int Depth { get; set; }

    public bool IsLeaf => Left == null && Right == null;

    public int CompareTo(HuffmanNode? other)
    {
        if (other == null) return 1;
        int cmp = Frequency.CompareTo(other.Frequency);
        if (cmp != 0) return cmp;
        return (Character ?? '\0').CompareTo(other.Character ?? '\0');
    }
}

public sealed record SymbolInfo(
    char Character,
    int Count,
    double Probability,
    double InformationBits,
    string HuffmanCode,
    int CodeLength
);

public sealed record ConstellationPoint(
    double I,
    double Q,
    string BitPattern,
    int SymbolIndex
);

public sealed record ReceivedSymbol(
    double I,
    double Q,
    int TransmittedIndex,
    int DecodedIndex,
    bool IsError
);

public class InformationTheoryEngine
{
    private readonly Random _random = new(42);

    public InformationMode Mode { get; set; } = InformationMode.HuffmanAndEntropy;
    public string PresetName { get; private set; } = "Shannon Entropy & Huffman Tree";

    // Text & Entropy Properties
    public string SourceText { get; private set; } = "INFORMATION THEORY AND SHANNON ENTROPY";
    public List<SymbolInfo> Alphabet { get; } = new();
    public HuffmanNode? HuffmanRoot { get; private set; }
    public double ShannonEntropy { get; private set; }
    public double MaxEntropy { get; private set; }
    public double Redundancy { get; private set; }
    public double AverageCodeLength { get; private set; }
    public double CodingEfficiency { get; private set; }
    public int RawBitSize { get; private set; }
    public int CompressedBitSize { get; private set; }
    public double CompressionRatio { get; private set; }

    // Channel & Modulation Properties
    public ModulationScheme CurrentModulation { get; private set; } = ModulationScheme.Qam16;
    public double SnrDb { get; private set; } = 14.0;
    public double BandwidthHz { get; set; } = 1000.0;
    public double ChannelCapacity { get; private set; }
    public double EmpiricalBer { get; private set; }
    public double TheoreticalBer { get; private set; }
    public List<ConstellationPoint> Constellation { get; } = new();
    public List<ReceivedSymbol> ReceivedSymbols { get; } = new();

    // Binary Symmetric Channel Properties
    public double CrossoverProbability { get; private set; } = 0.15;
    public double BinaryEntropy { get; private set; }
    public double BscCapacity { get; private set; }

    // KL Divergence Properties
    public double[] DistP { get; private set; } = [0.4, 0.3, 0.2, 0.1];
    public double[] DistQ { get; private set; } = [0.25, 0.25, 0.25, 0.25];
    public double KlDivergencePQ { get; private set; }
    public double KlDivergenceQP { get; private set; }
    public double CrossEntropyPQ { get; private set; }
    public double EntropyP { get; private set; }

    public InformationTheoryEngine()
    {
        SetSourceText(SourceText);
        SetModulation(ModulationScheme.Qam16);
        SetSnrDb(14.0);
        SetCrossoverProbability(0.15);
        UpdateKlDivergence();
    }

    #region Text, Entropy & Huffman Coding

    public void SetSourceText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "SHANNON";
        }

        SourceText = text.Trim();
        AnalyzeTextAndBuildHuffman();
    }

    private void AnalyzeTextAndBuildHuffman()
    {
        Alphabet.Clear();
        var counts = new Dictionary<char, int>();
        foreach (char c in SourceText)
        {
            counts[c] = counts.TryGetValue(c, out int cnt) ? cnt + 1 : 1;
        }

        int totalChars = SourceText.Length;
        ShannonEntropy = 0;

        foreach (var kvp in counts.OrderByDescending(k => k.Value).ThenBy(k => k.Key))
        {
            double p = (double)kvp.Value / totalChars;
            double info = -Math.Log2(p);
            ShannonEntropy += p * info;

            Alphabet.Add(new SymbolInfo(
                Character: kvp.Key,
                Count: kvp.Value,
                Probability: p,
                InformationBits: info,
                HuffmanCode: string.Empty,
                CodeLength: 0
            ));
        }

        MaxEntropy = Alphabet.Count > 1 ? Math.Log2(Alphabet.Count) : 1.0;
        Redundancy = MaxEntropy > 0 ? Math.Max(0.0, 1.0 - (ShannonEntropy / MaxEntropy)) : 0.0;

        BuildHuffmanTree();
    }

    private void BuildHuffmanTree()
    {
        if (Alphabet.Count == 0)
        {
            HuffmanRoot = null;
            return;
        }

        var pq = new PriorityQueue<HuffmanNode, double>();
        foreach (var sym in Alphabet)
        {
            var leaf = new HuffmanNode
            {
                Character = sym.Character,
                Frequency = sym.Probability,
                Count = sym.Count
            };
            pq.Enqueue(leaf, leaf.Frequency);
        }

        if (pq.Count == 1)
        {
            var single = pq.Dequeue();
            HuffmanRoot = new HuffmanNode
            {
                Frequency = single.Frequency,
                Count = single.Count,
                Left = single
            };
        }
        else
        {
            while (pq.Count > 1)
            {
                var left = pq.Dequeue();
                var right = pq.Dequeue();

                var parent = new HuffmanNode
                {
                    Frequency = left.Frequency + right.Frequency,
                    Count = left.Count + right.Count,
                    Left = left,
                    Right = right
                };

                pq.Enqueue(parent, parent.Frequency);
            }

            HuffmanRoot = pq.Dequeue();
        }

        // Assign prefix-free codes
        var codeMap = new Dictionary<char, string>();
        AssignCodes(HuffmanRoot, string.Empty, codeMap);

        // Update Alphabet with codes
        for (int i = 0; i < Alphabet.Count; i++)
        {
            var sym = Alphabet[i];
            string code = codeMap.TryGetValue(sym.Character, out var c) ? c : "0";
            Alphabet[i] = sym with { HuffmanCode = code, CodeLength = code.Length };
        }

        // Layout tree in normalized coordinates
        LayoutTree(HuffmanRoot);

        // Calculate Average Code Length & Compression Metrics
        AverageCodeLength = Alphabet.Sum(s => s.Probability * s.CodeLength);
        CodingEfficiency = AverageCodeLength > 0 ? (ShannonEntropy / AverageCodeLength) : 1.0;
        RawBitSize = SourceText.Length * 8;
        CompressedBitSize = Alphabet.Sum(s => s.Count * s.CodeLength);
        CompressionRatio = RawBitSize > 0 ? Math.Max(0.0, 1.0 - ((double)CompressedBitSize / RawBitSize)) : 0.0;
    }

    private void AssignCodes(HuffmanNode? node, string currentCode, Dictionary<char, string> map)
    {
        if (node == null) return;

        node.Code = currentCode;

        if (node.IsLeaf && node.Character.HasValue)
        {
            map[node.Character.Value] = string.IsNullOrEmpty(currentCode) ? "0" : currentCode;
            return;
        }

        AssignCodes(node.Left, currentCode + "0", map);
        AssignCodes(node.Right, currentCode + "1", map);
    }

    private void LayoutTree(HuffmanNode? root)
    {
        if (root == null) return;

        int maxDepth = GetMaxDepth(root, 0);
        int leafIndex = 0;
        int totalLeaves = CountLeaves(root);

        void AssignLeafPositions(HuffmanNode? node, int depth)
        {
            if (node == null) return;
            node.Depth = depth;
            node.Y = maxDepth > 0 ? (double)depth / maxDepth : 0.5;

            if (node.IsLeaf)
            {
                double step = totalLeaves > 1 ? 1.0 / (totalLeaves - 1) : 0.5;
                node.X = totalLeaves > 1 ? (leafIndex * step) * 1.8 - 0.9 : 0.0;
                leafIndex++;
                return;
            }

            AssignLeafPositions(node.Left, depth + 1);
            AssignLeafPositions(node.Right, depth + 1);

            double leftX = node.Left?.X ?? 0.0;
            double rightX = node.Right?.X ?? 0.0;
            node.X = (leftX + rightX) * 0.5;
        }

        AssignLeafPositions(root, 0);
    }

    private int GetMaxDepth(HuffmanNode? node, int current)
    {
        if (node == null) return current;
        if (node.IsLeaf) return current;
        return Math.Max(GetMaxDepth(node.Left, current + 1), GetMaxDepth(node.Right, current + 1));
    }

    private int CountLeaves(HuffmanNode? node)
    {
        if (node == null) return 0;
        if (node.IsLeaf) return 1;
        return CountLeaves(node.Left) + CountLeaves(node.Right);
    }

    #endregion

    #region Modulation & Constellations (IQ Plane)

    public void SetModulation(ModulationScheme scheme)
    {
        CurrentModulation = scheme;
        GenerateConstellation();
        SimulateTransmission();
    }

    public void SetSnrDb(double snrDb)
    {
        SnrDb = Math.Clamp(snrDb, -10.0, 35.0);
        UpdateChannelCapacity();
        SimulateTransmission();
    }

    private void GenerateConstellation()
    {
        Constellation.Clear();

        switch (CurrentModulation)
        {
            case ModulationScheme.Bpsk:
                Constellation.Add(new ConstellationPoint(1.0, 0.0, "0", 0));
                Constellation.Add(new ConstellationPoint(-1.0, 0.0, "1", 1));
                break;

            case ModulationScheme.Qpsk:
                double q = 1.0 / Math.Sqrt(2.0);
                Constellation.Add(new ConstellationPoint(q, q, "00", 0));
                Constellation.Add(new ConstellationPoint(-q, q, "01", 1));
                Constellation.Add(new ConstellationPoint(-q, -q, "11", 2));
                Constellation.Add(new ConstellationPoint(q, -q, "10", 3));
                break;

            case ModulationScheme.Psk8:
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4.0;
                    string bits = Convert.ToString(i, 2).PadLeft(3, '0');
                    Constellation.Add(new ConstellationPoint(Math.Cos(angle), Math.Sin(angle), bits, i));
                }
                break;

            case ModulationScheme.Qam16:
                double scale = 1.0 / Math.Sqrt(10.0);
                double[] levels = [-3.0, -1.0, 1.0, 3.0];
                int idx = 0;
                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        double iCoord = levels[c] * scale;
                        double qCoord = levels[r] * scale;
                        string bits = Convert.ToString(idx, 2).PadLeft(4, '0');
                        Constellation.Add(new ConstellationPoint(iCoord, qCoord, bits, idx++));
                    }
                }
                break;
        }

        UpdateChannelCapacity();
    }

    private void UpdateChannelCapacity()
    {
        double snrLinear = Math.Pow(10.0, SnrDb / 10.0);
        ChannelCapacity = BandwidthHz * Math.Log2(1.0 + snrLinear);

        // Theoretical BER approximations
        double ebN0 = snrLinear;
        switch (CurrentModulation)
        {
            case ModulationScheme.Bpsk:
            case ModulationScheme.Qpsk:
                TheoreticalBer = 0.5 * Erfc(Math.Sqrt(ebN0));
                break;
            case ModulationScheme.Psk8:
                TheoreticalBer = (1.0 / 3.0) * Erfc(Math.Sqrt(3.0 * ebN0) * Math.Sin(Math.PI / 8.0));
                break;
            case ModulationScheme.Qam16:
                TheoreticalBer = (3.0 / 8.0) * Erfc(Math.Sqrt(0.4 * ebN0));
                break;
        }
    }

    public void SimulateTransmission(int numSymbols = 220)
    {
        if (Constellation.Count == 0) return;

        ReceivedSymbols.Clear();
        double snrLinear = Math.Pow(10.0, SnrDb / 10.0);
        double sigma = Math.Sqrt(1.0 / (2.0 * snrLinear));

        int errorCount = 0;

        for (int k = 0; k < numSymbols; k++)
        {
            int txIndex = _random.Next(Constellation.Count);
            var ideal = Constellation[txIndex];

            // Additive White Gaussian Noise via Box-Muller
            (double nI, double nQ) = SampleGaussian(sigma);
            double rxI = ideal.I + nI;
            double rxQ = ideal.Q + nQ;

            // Maximum Likelihood (Minimum Distance) Demodulation
            int bestIndex = 0;
            double bestDistSq = double.MaxValue;

            for (int i = 0; i < Constellation.Count; i++)
            {
                double dI = rxI - Constellation[i].I;
                double dQ = rxQ - Constellation[i].Q;
                double distSq = dI * dI + dQ * dQ;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            bool isErr = bestIndex != txIndex;
            if (isErr) errorCount++;

            ReceivedSymbols.Add(new ReceivedSymbol(rxI, rxQ, txIndex, bestIndex, isErr));
        }

        EmpiricalBer = (double)errorCount / numSymbols;
    }

    private (double, double) SampleGaussian(double sigma)
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double r = Math.Sqrt(-2.0 * Math.Log(u1)) * sigma;
        double theta = 2.0 * Math.PI * u2;
        return (r * Math.Cos(theta), r * Math.Sin(theta));
    }

    private static double Erfc(double x)
    {
        // Complementary error function Chebyshev approximation
        double z = Math.Abs(x);
        double t = 1.0 / (1.0 + 0.5 * z);
        double ans = t * Math.Exp(-z * z - 1.26551223 +
            t * (1.00002368 +
            t * (0.37409196 +
            t * (0.09678418 +
            t * (-0.18628806 +
            t * (0.27886807 +
            t * (-1.13520398 +
            t * (1.48851587 +
            t * (-0.82215223 +
            t * 0.17087277)))))))));
        return x >= 0 ? ans : 2.0 - ans;
    }

    #endregion

    #region Binary Symmetric Channel (BSC)

    public void SetCrossoverProbability(double p)
    {
        CrossoverProbability = Math.Clamp(p, 0.0, 0.5);

        if (CrossoverProbability <= 0.0)
        {
            BinaryEntropy = 0.0;
        }
        else if (Math.Abs(CrossoverProbability - 0.5) < 1e-6)
        {
            BinaryEntropy = 1.0;
        }
        else
        {
            double q = 1.0 - CrossoverProbability;
            BinaryEntropy = -(CrossoverProbability * Math.Log2(CrossoverProbability) + q * Math.Log2(q));
        }

        BscCapacity = 1.0 - BinaryEntropy;
    }

    #endregion

    #region KL Divergence & Relative Entropy

    public void SetDistributions(double[] p, double[] q)
    {
        if (p.Length == 0 || q.Length == 0 || p.Length != q.Length) return;

        double sumP = p.Sum();
        double sumQ = q.Sum();

        DistP = p.Select(v => Math.Max(1e-9, v / sumP)).ToArray();
        DistQ = q.Select(v => Math.Max(1e-9, v / sumQ)).ToArray();

        UpdateKlDivergence();
    }

    private void UpdateKlDivergence()
    {
        KlDivergencePQ = 0.0;
        KlDivergenceQP = 0.0;
        CrossEntropyPQ = 0.0;
        EntropyP = 0.0;

        for (int i = 0; i < DistP.Length; i++)
        {
            double p = DistP[i];
            double q = DistQ[i];

            EntropyP += -p * Math.Log2(p);
            KlDivergencePQ += p * Math.Log2(p / q);
            KlDivergenceQP += q * Math.Log2(q / p);
            CrossEntropyPQ += -p * Math.Log2(q);
        }
    }

    #endregion

    #region Presets

    public void LoadPreset(string presetName)
    {
        PresetName = presetName;

        switch (presetName)
        {
            case "Shannon Entropy & Huffman Tree":
                Mode = InformationMode.HuffmanAndEntropy;
                SetSourceText("INFORMATION THEORY AND SHANNON ENTROPY");
                break;

            case "16-QAM Constellation & AWGN Channel":
                Mode = InformationMode.ConstellationAndCapacity;
                SetModulation(ModulationScheme.Qam16);
                SetSnrDb(16.0);
                break;

            case "QPSK Modulation & Bit Error Rate":
                Mode = InformationMode.ConstellationAndCapacity;
                SetModulation(ModulationScheme.Qpsk);
                SetSnrDb(8.0);
                break;

            case "Binary Symmetric Channel & Capacity":
                Mode = InformationMode.BinarySymmetricChannel;
                SetCrossoverProbability(0.12);
                break;

            case "KL Divergence Asymmetry":
                Mode = InformationMode.KlDivergence;
                SetDistributions([0.6, 0.25, 0.10, 0.05], [0.15, 0.35, 0.35, 0.15]);
                break;
        }
    }

    #endregion
}
