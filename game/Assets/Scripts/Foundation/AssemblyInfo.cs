using System.Runtime.CompilerServices;

// Test assemblies construct fresh ...State instances and read FoundationBootstrap's
// internal diagnostics directly (ADR-0001 testability pattern).
[assembly: InternalsVisibleTo("EditModeTests")]
[assembly: InternalsVisibleTo("PlayModeTests")]
