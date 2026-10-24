# FryMath Studio

> **Interactive Mathematics Studio, Symbolic CAS & Visual Learning Laboratory for FryWorld**  
> Built with C# 10 / .NET 10, Avalonia 11.2, SkiaSharp Vector Graphics, and Native Audio DSP.

Inspired by **FryCircuit**, **Wolfram Alpha**, and **3Blue1Brown**, FryMath transforms abstract mathematics into tangible, interactive, 60 FPS visual experiences. It unifies computer algebra, real-time numerical solvers, multivariable geometric manifolds, acoustic resonance, and the **7 Clay Millennium Prize Problems** into a single coherent desktop environment.

---

## 🏛️ 31 Interactive Mathematical Laboratories

FryMath features 31 fully interactive laboratories accessible via the top drawer selector, the command omnibar, or keyboard shortcuts.

### 1. Visual Calculus & Analysis
- **Multi-Curve Graphing**: Simultaneous function plotting with distinct brush colors and analytical derivatives $f'(x)$.
- **CAS Symbolic Antiderivatives**: Closed-form indefinite integration $\int f(x) dx = F(x) + C$ computed via `SymbolicIntegrator`.
- **Textbook Typeset Cards**: Vector-rendered Leibniz derivative card $\frac{df}{dx}$ and Fundamental Theorem of Calculus card $\int_a^b f(x)dx$.
- **Secant-to-Tangent Morphing**: Continuous slider $\Delta x \to 0$ with live slope triangle and tangent line equations.
- **Osculating Curvature Circle**: Real-time radius of curvature $R = 1/\kappa$ rolling along curve inflection points.
- **Riemann Partition Sums**: Left, Right, Midpoint, and Trapezoidal integration rules with live error diagnostics.
- **Taylor Polynomial Approximations**: $P_N(x) = \sum_{k=0}^N \frac{f^{(k)}(x_0)}{k!}(x - x_0)^k$ with adjustable center $x_0$ and order $N$.

### 2. Linear Algebra (3Blue1Brown Geometric Engine)
- **Coordinate Grid Warping**: Real-time 2D spatial warping under $2 \times 2$ matrix transformations $\mathbf{x} \mapsto \mathbf{A}\mathbf{x}$.
- **Basis Vector Handles**: Interactive drag nodes for $\hat{\mathbf{i}} = \langle 1, 0 \rangle$ and $\hat{\mathbf{j}} = \langle 0, 1 \rangle$.
- **Signed Determinant Parallelogram**: $\det(\mathbf{A}) = ad - bc$ with area visualization and orientation flip warnings.
- **Eigenvector Ray Locking**: Invariant directions $\mathbf{A}\mathbf{v} = \lambda \mathbf{v}$ with real eigenvalue tracking.
- **Singular Value Decomposition (SVD)**: Unit circle mapping into principal semi-axes ellipses.
- **Typeset Matrix Card**: Live trace, determinant, characteristic equation, and inverse matrix.

### 3. Fourier Waves & Epicycles
- **Rotating DFT Phasors**: Epicycle gear trains reconstructing continuous waveforms and complex 2D shapes.
- **Harmonic Spectrum**: Frequency domain amplitude bars $|c_k|$ and phase offsets $\phi_k$.
- **Live 16-Bit PCM Audio Synthesizer**: Auditory tone synthesis for timbre and harmonic overtones.

### 4. Probability & Statistics
- **Galton Pin Board**: 12-row triangular peg board with continuous ball drops and physical collision physics.
- **Central Limit Theorem**: Discrete binomial histogram $B(n, p)$ overlaid with the continuous Gaussian distribution $\mathcal{N}(\mu, \sigma^2)$.

### 5. Dynamical Chaos
- **3D Lorenz Attractor**: RK4 numerical integration of convective atmospheric flow ($\sigma=10, \rho=28, \beta=8/3$).
- **The Butterfly Effect**: Dual trajectories initialized with $\epsilon = 10^{-5}$ divergence.
- **3D Orbit Controls**: Mouse yaw, pitch, and zoom camera controls.

### 6. Vector Fields & Autonomous ODEs
- **Velocity Arrow Grid**: Directional vector fields colored by magnitude from cyan to rose.
- **Advecting Phase Particles**: 300+ flow particles streaming at 60 FPS through dynamical phase space.
- **Interactive Streamlines**: Click-to-trace RK4 orbital streamlines with divergence $\nabla \cdot \mathbf{F}$ and curl $\nabla \times \mathbf{F}$ probes.

### 7. Complex Analysis & Conformal Mapping
- **Domain Coloring**: Skia-accelerated `WriteableBitmap` mapping $\text{Hue} = \arg(f(z))$ and $\text{Lightness} = \log|f(z)|$.
- **Conformal Grid Deformation**: Angle-preserving orthogonal grid line mappings.
- **Interactive HUD Probe**: Complex value $z$, evaluation $w = f(z)$, modulus $|w|$, argument $\arg(w)$, and conformal dilation $|f'(z)|$.

### 8. 3D Multivariable Calculus & Surfaces
- **Interactive 3D Orbit Mesh**: Depth-sorted quad mesh with elevation color shading.
- **Dynamic Tangent Plane**: Translucent golden plane $z - z_0 = f_x(x - x_0) + f_y(y - y_0)$ with surface normal vector $\mathbf{n}$.
- **Hessian & Extrema Classifier**: Gradient $\nabla f$, Hessian determinant $D = f_{xx}f_{yy} - f_{xy}^2$, and second partial derivative test.

### 9. Fractal Geometry & Complex Dynamics
- **Continuous Normalized Escape Time**: Smooth palette rendering $\nu = n + 1 - \frac{\ln(\ln|z|)}{\ln 2}$.
- **Deep Zoom Engine**: Magnification up to $10^{12}\times$ with multi-threaded scanline rendering.
- **Douady-Hubbard Julia Duality**: Hovering over any point $c$ in the Mandelbrot set dynamically renders the corresponding Julia set $J(c)$.

### 10. Wave Mechanics & Chladni Resonance
- **2D Leapfrog PDE Solver**: Discrete wave equation $u_{tt} = c^2 \nabla^2 u - \gamma u_t$ with interactive droplet disturbance.
- **Physical Sand Particle Advection**: 450 sand particles migrating away from antinodes to form nodal Chladni figures.
- **Bessel Drum Membranes**: Clamped circular membrane eigenmodes $J_m(\alpha_{m,n} r/R)\cos(m\theta)$.

### 11. Differential Geometry
- **Frenet-Serret Frame**: Tangent $\mathbf{T}$, Normal $\mathbf{N}$, and Binormal $\mathbf{B}$ moving trihedron along 3D curves.
- **Curvature & Torsion**: Live evaluation of $\kappa(s)$ and $\tau(s)$ with osculating circle and rectifying plane.
- **Surface Curvatures**: Principal curvatures $(k_1, k_2)$, Gaussian curvature $K = k_1 k_2$, and Mean curvature $H = (k_1 + k_2)/2$.

### 12. Heat Diffusion & PDEs
- **2D Heat Equation**: Finite-difference simulation of $u_t = \alpha \nabla^2 u$.
- **Thermal Flux & Isotherms**: Heat flux vector field $\mathbf{q} = -k \nabla u$ with isothermal contour curves.

### 13. Fluid Dynamics & Navier-Stokes (2D)
- **Incompressible Flow Solver**: Velocity advection, pressure Poisson projection, and viscous diffusion.
- **Vorticity Field**: Scalar vorticity $\omega = \nabla \times \mathbf{u}$ with live smoke and dye particle tracing.

### 14. Quantum Mechanics & Wavepackets
- **Schrödinger Equation Solver**: 1D time-dependent split-operator FFT for $i\hbar \frac{\partial\psi}{\partial t} = \hat{H}\psi$.
- **Quantum Tunneling**: Gaussian wavepacket dispersion and barrier transmission/reflection coefficients.
- **Phase Probability Visualization**: Wavefunction magnitude $|\psi(x,t)|^2$ with complex phase coloring $\arg(\psi)$.

### 15. Celestial Mechanics (N-Body)
- **Symplectic Gravitational Integrator**: Leapfrog and RK4 multi-body orbital dynamics.
- **Lagrange Points & Potentials**: Effective potential contours and $L_1 - L_5$ equilibrium locations.

### 16. Relativity & Spacetime
- **Minkowski Spacetime**: Light cones, invariant hyperbolas $s^2 = c^2 t^2 - x^2$, and Lorentz boosts $\Lambda^\mu_\nu$.
- **Twin Paradox & Geodesics**: Worldline proper time integration $\tau = \int \sqrt{1 - v^2/c^2} dt$.
- **Schwarzschild Metric**: Gravitational light bending around compact masses.

### 17. Knot Theory & 3D Topology
- **Parametric Knot Embeddings**: 3D interactive rendering of Trefoils, Figure-Eight knots, and $(p, q)$-Torus knots.
- **Knot Invariants**: Dowker notation, Alexander polynomials, and Jones polynomials $V(t)$.

### 18. Hyperbolic Geometry & Non-Euclidean Planes
- **Poincaré Disk Model ($\mathbb{D}^2$)**: Geodesics as circular arcs orthogonal to the boundary circle.
- **Hyperbolic Tilings**: Regular $\{p, q\}$ Escher-style uniform tessellations with Poincaré and Klein projections.

### 19. Catastrophe Theory & Bifurcations
- **Thom's Elementary Catastrophes**: Fold, Cusp, Swallowtail, and Butterfly potential surfaces $V(x; c)$.
- **Bifurcation Manifolds**: Hysteresis jumps, stability exchange, and control space phase portraits.

### 20. Lie Algebras & Root Systems
- **Semisimple Classification**: Root lattices for $A_n, B_n, C_n, D_n$ and exceptional algebras ($G_2, F_4, E_6, E_7, E_8$).
- **Weyl Chambers & Symmetries**: Cartan matrices, Dynkian diagrams, and reflection group symmetries.

### 21. Hamiltonian Mechanics & Symplectic Geometry
- **Phase Space $(q, p)$**: Canonical equations $\dot{q} = \frac{\partial H}{\partial p}$, $\dot{p} = -\frac{\partial H}{\partial q}$.
- **Liouville's Theorem**: Conservation of symplectic phase space volume $d\omega = dq \wedge dp$.
- **Poincaré Sections**: Quasiperiodic vs chaotic phase space cross-sections.

### 22. Lagrangian Mechanics & Variational Principles
- **Principle of Least Action**: $\delta S = \delta \int L dt = 0$ with Euler-Lagrange equations.
- **Double Pendulum Dynamics**: Chaotic energy surface exploration and generalized coordinates $(\theta_1, \theta_2)$.

### 23. 4D Topology & Higher Dimensions
- **Regular 4D Polytopes**: Tesseract (8-cell), 16-Cell, 24-Cell, 120-Cell, and 600-Cell.
- **4D Double-Plane Rotations**: Continuous rotation in $SO(4) \cong (SU(2)\times SU(2))/\mathbb{Z}_2$ projected via 3D stereographic mapping.

### 24. Information Theory & Entropy
- **Shannon Entropy**: $H(X) = -\sum p_i \log_2 p_i$ with source coding bounds.
- **Channel Capacity**: Binary symmetric channel capacity $C = 1 - H_b(p)$ and Kullback-Leibler divergence $D_{KL}(P \parallel Q)$.

### 25. Discrete Dynamics & Cellular Automata
- **Conway's Game of Life**: High-speed toroidal grid simulation with glider presets.
- **Wolfram 1D Automata**: Exploration of all 256 elementary rules (Rule 30, Rule 90, Rule 110).

### 26. Algebraic Topology & Homology
- **Simplicial Complexes**: Vertices, edges, triangles, and tetrahedra.
- **Chain Complexes & Boundary Operators**: Matrix representations of $\partial_k$ verifying $\partial^2 = 0$.
- **Homology Groups & Betti Numbers**: $H_k = \ker \partial_k / \operatorname{im} \partial_{k+1}$, Betti numbers $(\beta_0, \beta_1, \beta_2)$, and Euler characteristic $\chi = V - E + F$.

### 27. Clifford Algebras & Geometric Algebra
- **Multivectors in $\mathcal{C}\ell(p, q)$**: Geometric product $ab = a \cdot b + a \wedge b$.
- **Rotors & Reflections**: 3D spatial rotations represented by grade-even rotors $R = e^{-\theta B / 2}$.

### 28. Elliptic Curves & Cryptography
- **Weierstrass Equation**: $y^2 = x^3 + ax + b$ with discriminant $\Delta = -16(4a^3 + 27b^2)$.
- **Chord-and-Tangent Group Law**: Interactive point doubling and secant addition $P + Q = R$.
- **Finite Field Arithmetic**: Elliptic curves over $\mathbb{F}_p$ and ECDH cryptographic key exchange.

### 29. Riemann Zeta & Analytic Number Theory
- **Zeta Critical Strip**: $\zeta(s) = \sum n^{-s}$ along the critical line $\operatorname{Re}(s) = 1/2$.
- **Gram Points & Non-Trivial Zeros**: Visualization of non-trivial roots ($\gamma_1 \approx 14.1347, \gamma_2 \approx 21.0220$).
- **Acoustic Zero Sonification**: Non-trivial zeros synthesized as acoustic sine waves.
- **Montgomery-Odlyzko Law**: GUE random matrix eigenvalue spacing statistics.

### 30. Projective Geometry
- **Real Projective Plane ($\mathbb{RP}^2$)**: Homogeneous coordinates $[x : y : z]$ and line-point duality.
- **Cross-Ratio Invariance**: $(A, B; C, D)$ preserved under central projections.
- **Classic Theorems**: Interactive Pascal's Mystic Hexagram and Desargues' Perspective Triangles.

### 31. Clay Millennium Prize Problems & Solutions (CMI $7M)
Comprehensive research simulators, invariant telemetry, and interactive controls for the seven $1,000,000 problems established by the Clay Mathematics Institute:

1. **Poincaré Conjecture** *(Solved by Grigori Perelman, 2002–2003)*:
   - Hamilton-Perelman Ricci flow $\partial_t g_{ij} = -2 R_{ij}$ on closed 3-manifolds.
   - Perelman $\mathcal{W}$-entropy functional monotonicity and geometric surgery simulator.
   - Live continuous 60 FPS Ricci flow animation with neckpinch surgery telemetry.
2. **Riemann Hypothesis**:
   - $\zeta(s) = 0 \implies \operatorname{Re}(s) = \frac{1}{2}$ critical line zero distribution.
   - Montgomery-Odlyzko Gaussian Unitary Ensemble (GUE) pair correlation $1 - \left(\frac{\sin \pi r}{\pi r}\right)^2$.
   - Live acoustic audio sonification for critical zeros ($\gamma_1 \approx 14.1347, \gamma_2 \approx 21.0220$).
3. **Birch and Swinnerton-Dyer Conjecture (BSD)**:
   - Elliptic curve L-functions $L(E, s)$ and analytic rank $r = \operatorname{ord}_{s=1} L(E, s)$.
   - Full leading coefficient formula: $\frac{L^{(r)}(E, 1)}{r!} = \frac{\Omega_E \cdot R_E \cdot \#\Sha(E) \cdot \prod c_p}{(\#E_{\text{tors}})^2}$.
   - Interactive Mordell-Weil group addition calculator: tangent doubling $2P_0$, chord secant sums $kP_0$, and generator coordinates.
4. **Navier-Stokes Existence & Smoothness**:
   - 3D incompressible Navier-Stokes equations: $\partial_t \mathbf{u} + (\mathbf{u}\cdot\nabla)\mathbf{u} = -\nabla p + \nu \Delta \mathbf{u}$.
   - Enstrophy $\Omega(t) = \int |\nabla \times \mathbf{u}|^2 d^3x$ vs viscous dissipation rate $\epsilon(t)$.
   - Real-time blowup time-scrubber with critical time singularity diagnostics.
5. **Yang-Mills Mass Gap**:
   - Non-Abelian $SU(3)$ gauge theory on 4D spacetime lattice.
   - Wilson loop area law $W(C) \sim \exp(-\sigma \cdot \operatorname{Area}(C))$ with non-zero mass gap $\Delta > 0$.
   - Interactive coupling constant $\beta$ slider with string tension telemetry.
6. **P versus NP**:
   - Polynomial vs non-deterministic polynomial verification complexity.
   - 3-SAT clause-to-variable ratio $\alpha = m/n$ phase transition at $\alpha_c \approx 4.267$.
   - Interactive variable count slider ($N = 12 \dots 64$) with empirical exponential runtime curve.
7. **Hodge Conjecture**:
   - Projective algebraic varieties with Hodge decomposition $H^k(X, \mathbb{C}) = \bigoplus_{p+q=k} H^{p,q}(X)$.
   - Interactive Hodge diamond visualizer ($K3$ surface, Calabi-Yau 3-fold, Abelian 4-fold).
   - Invariant tracking for algebraic cycles and rational $(p, p)$-cohomology classes.

---

## ⌨️ Wolfram-Style Omnibar Intelligence & Math Keyboard

The top omnibar dynamically dispatches expressions and commands to their respective laboratories:

- `x^3 - 3*x` $\to$ **Visual Calculus** (with analytical derivative & antiderivative)
- `[[1, 2], [3, 4]]` $\to$ **Linear Algebra** (matrix warping & eigenvectors)
- `<-y, x>` or `dx/dt` $\to$ **Vector Fields & ODEs** (streamlines & phase portraits)
- `z = x^2 - y^2` $\to$ **3D Surfaces** (tangent plane & Hessian classification)
- `f(z) = z^3 - 1` $\to$ **Complex Conformal Mapping** (domain coloring)
- `mandelbrot`, `julia`, `newton` $\to$ **Fractal Geometry**
- `chladni`, `ripple`, `drum` $\to$ **Wave Mechanics & PDEs**
- `frenet`, `curvature` $\to$ **Differential Geometry**
- `heat` $\to$ **Heat Diffusion**
- `fluid`, `navier` $\to$ **Fluid Dynamics**
- `schrodinger`, `tunneling` $\to$ **Quantum Mechanics**
- `nbody`, `kepler` $\to$ **Celestial Mechanics**
- `lorentz`, `minkowski` $\to$ **Relativity & Spacetime**
- `trefoil`, `knot` $\to$ **Knot Theory**
- `poincare`, `hyperbolic` $\to$ **Hyperbolic Geometry**
- `cusp`, `bifurcation` $\to$ **Catastrophe Theory**
- `lie`, `su3`, `e8` $\to$ **Lie Algebras**
- `hamiltonian`, `phase` $\to$ **Hamiltonian Mechanics**
- `lagrangian`, `leastaction` $\to$ **Lagrangian Mechanics**
- `tesseract`, `4d` $\to$ **4D Topology**
- `entropy`, `shannon` $\to$ **Information Theory**
- `conway`, `life` $\to$ **Discrete Dynamics**
- `homology`, `betti` $\to$ **Algebraic Topology**
- `clifford`, `rotor` $\to$ **Clifford Algebras**
- `elliptic`, `weierstrass` $\to$ **Elliptic Curves**
- `zeta`, `riemann` $\to$ **Riemann Zeta & Primes**
- `projective`, `pascal` $\to$ **Projective Geometry**
- `millennium`, `poincare`, `bsd`, `hodge`, `pvsnp`, `yangmills` $\to$ **Millennium Prize Problems**

### Quick Math Symbol Chips
One-click insertion chips for $\pi, e, \sqrt{x}, x^2, \sin, \cos, e^x, \ln, z=, \langle P, Q \rangle, [[M]], \text{Mandelbrot}, \text{Chladni}$, and `👑 $7M` (instant jump to Millennium Problems).

---

## 🧪 Verification & Building

```bash
# Build the entire solution (.NET 10 / Avalonia 11.2)
dotnet build FryMath.slnx

# Run all 210 unit and UI tests (0 Warnings, 0 Errors)
dotnet test FryMath.slnx

# Generate headless UI inspection snapshots
dotnet run --project tools/UiSnapshots/UiSnapshots.csproj
```

---

## 🏗️ Solution Architecture

```
FryMath/
├── src/
│   ├── FryMath.Core/               # High-performance mathematical algorithms & CAS engine
│   │   ├── CAS/                    # Symbolic differentiation, integration & AST simplification
│   │   ├── Calculus/               # Numerical integration, differentiation & Taylor series
│   │   ├── LinearAlgebra/          # Matrix operations, eigenvalues, SVD & determinants
│   │   ├── Millennium/             # Clay Millennium Prize problems mathematical engine & types
│   │   ├── Quantum/                # Split-operator FFT Schrödinger solver
│   │   ├── Relativity/             # Minkowski spacetime & Lorentz boost geometry
│   │   ├── Topology4D/             # 4D polytope vertices & double-plane rotations
│   │   ├── Homology/               # Simplicial chain complexes & Betti numbers
│   │   ├── Clifford/               # Geometric algebra multivector arithmetic
│   │   └── Presets/                # Curated mathematical preset catalog
│   ├── FryMath.UI/                 # Avalonia 11 desktop application & Skia canvases
│   │   ├── Views/                  # MathStudioView.axaml & navigation layouts
│   │   ├── ViewModels/             # Reactive MVVM view models with CommunityToolkit
│   │   ├── Controls/               # Custom vector controls (TextbookFormulaCard, etc.)
│   │   ├── Audio/                  # Native PCM audio tone generator
│   │   └── Theming/                # Dark/light mathematical theme tokens
│   └── FryMath.Plugin/             # FryWorld plugin integration host
├── tests/
│   ├── FryMath.Core.Tests/         # 185 mathematical unit tests
│   └── FryMath.UI.Tests/           # 25 UI, ViewModel & integration tests
└── tools/
    └── UiSnapshots/                # Headless Skia snapshot renderer for visual testing
```

---

## 📜 License
Part of the **FryWorld** ecosystem. MIT License.
