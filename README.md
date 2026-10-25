<p align="center">
  <img src="Assets/app-icon.svg" width="96" height="96" alt="FryMath Logo" />
</p>

# FryMath

Interactive mathematics studio, symbolic computer algebra system (CAS), and visual simulation laboratory for the **FryWorld** ecosystem.

🌐 **Live Web Studio**: [codefrydev.in/FryMath](https://codefrydev.in/FryMath)

---

## ⚡ Highlights

- **31 Mathematical Laboratories**: Visual Calculus, Linear Algebra, Fourier waves, Chaos, 3D Surfaces, Complex Analysis, PDEs, Quantum, Relativity, Differential Geometry, 4D Topology, and the 7 Clay Millennium Prize Problems.
- **Wolfram-Style Omnibar**: Real-time expression evaluation with symbolic differentiation and indefinite integration.
- **60 FPS Vector Graphics**: Accelerated by Avalonia and SkiaSharp rendering.
- **Universal Runtime**: Native desktop application and WebAssembly browser edition.

---

## 🚀 Quick Start

### Build & Run

```bash
# Build the solution
dotnet build FryMath.slnx

# Run all 218 tests
dotnet test FryMath.slnx

# Launch desktop studio
dotnet run --project Runner/MathPlugin.Runner.csproj

# Launch WebAssembly studio locally
dotnet run --project src/FryMath.Browser/FryMath.Browser.csproj
```

### Publish WebAssembly for GitHub Pages

```bash
dotnet publish src/FryMath.Browser/FryMath.Browser.csproj -c Release -o release
```

---

## 📜 License

Part of the **FryWorld** ecosystem. MIT License.
