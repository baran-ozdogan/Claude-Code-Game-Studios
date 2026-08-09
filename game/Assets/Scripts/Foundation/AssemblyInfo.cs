using System.Runtime.CompilerServices;

// Test assemblies construct fresh ...State instances and read FoundationBootstrap's
// internal diagnostics directly (ADR-0001 testability pattern).
[assembly: InternalsVisibleTo("EditModeTests")]
[assembly: InternalsVisibleTo("PlayModeTests")]

// Build-validation check'leri sahne objelerinin internal alanlarını okur
// (ShiftZone._lights/_triggerMode vb. — isik-volume Story 006).
[assembly: InternalsVisibleTo("BuildValidation")]
