using System.Diagnostics.CodeAnalysis;

// Suppress a few code-smell analyzer rules for this iterative refactor.
// These suppressions reduce noise while we split responsibilities across files.
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S3776", Justification = "Large composition methods will be gradually refactored; suppress for now to reduce noise.")]
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S106", Justification = "Acceptable for composition wiring in AppHost; will be refactored later.")]
[assembly: SuppressMessage("SonarAnalyzer.CSharp", "S125", Justification = "Temporary suppression during refactor.")]

