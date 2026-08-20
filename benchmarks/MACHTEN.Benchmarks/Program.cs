using BenchmarkDotNet.Running;

// dotnet run -c Release --project benchmarks/MACHTEN.Benchmarks -- --filter *
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

public partial class Program;
