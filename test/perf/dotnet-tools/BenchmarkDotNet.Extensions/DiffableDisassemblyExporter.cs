using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Disassemblers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BenchmarkDotNet.Extensions
{
    // a simplified copy of internal BDN type: https://github.com/dotnet/BenchmarkDotNet/blob/0445917bf93059f17cb09e7d48cdb5e27a096c37/src/BenchmarkDotNet/Disassemblers/Exporters/GithubMarkdownDisassemblyExporter.cs#L35-L80
    internal static class DiffableDisassemblyExporter
    {
        private static readonly Lazy<Func<object, SourceCode>> GetSource = new Lazy<Func<object, SourceCode>>(() => GetElementGetter<SourceCode>("Source"));
        private static readonly Lazy<Func<object, string>> GetTextRepresentation = new Lazy<Func<object, string>>(() => GetElementGetter<string>("TextRepresentation"));

        private static readonly Lazy<Func<DisassembledMethod, DisassemblyResult, DisassemblyDiagnoserConfig, string, IReadOnlyList<object>>> Prettify
            = new Lazy<Func<DisassembledMethod, DisassemblyResult, DisassemblyDiagnoserConfig, string, IReadOnlyList<object>>>(GetPrettifyMethod);

        internal static string BuildDisassemblyString(DisassemblyResult disassemblyResult, DisassemblyDiagnoserConfig config)
        {
            StringBuilder sb = new StringBuilder();

            int methodIndex = 0;
            foreach (var method in disassemblyResult.Methods.Where(method => string.IsNullOrEmpty(method.Problem)))
            {
                sb.AppendLine("```assembly");

                sb.AppendLine($"; {method.Name}");

                var pretty = Prettify.Value.Invoke(method, disassemblyResult, config, $"M{methodIndex++:00}");

                ulong totalSizeInBytes = 0;
                foreach (var element in pretty)
                {
                    if (element.Source() is Asm asm)
                    {
                        checked
                        {
                            totalSizeInBytes += (uint)asm.Instruction.Length;
                        }

                        sb.AppendLine($"       {element.TextRepresentation()}");
                    }
                    else // it's a DisassemblyPrettifier.Label (internal type..)
                    {
                        sb.AppendLine($"{element.TextRepresentation()}:");
                    }
                }

                sb.AppendLine($"; Total bytes of code {totalSizeInBytes}");
                sb.AppendLine("```");
            }

            return sb.ToString();
        }

        private static SourceCode Source(this object element) => GetSource.Value.Invoke(element);

        private static string TextRepresentation(this object element) => GetTextRepresentation.Value.Invoke(element);

        private static Func<object, T> GetElementGetter<T>(string name)
        {
            var type = typeof(DisassemblyDiagnoser).Assembly.GetType("BenchmarkDotNet.Disassemblers.Exporters.DisassemblyPrettifier");

            type = type.GetNestedType("Element", BindingFlags.Instance | BindingFlags.NonPublic);

            var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);

            var method = property.GetGetMethod(nonPublic: true);

            var generic = typeof(Func<,>).MakeGenericType(type, typeof(T));

            var @delegate = method.CreateDelegate(generic);

            return (obj) => (T)@delegate.DynamicInvoke(obj); // cast to (Func<object, T>) throws
        }

        private static Func<DisassembledMethod, DisassemblyResult, DisassemblyDiagnoserConfig, string, IReadOnlyList<object>> GetPrettifyMethod()
        {
            var type = typeof(DisassemblyDiagnoser).Assembly.GetType("BenchmarkDotNet.Disassemblers.Exporters.DisassemblyPrettifier");

            var method = type.GetMethod("Prettify", BindingFlags.Static | BindingFlags.NonPublic);

            var @delegate = method.CreateDelegate(typeof(Func<DisassembledMethod, DisassemblyResult, DisassemblyDiagnoserConfig, string, IReadOnlyList<object>>));

            return (Func<DisassembledMethod, DisassemblyResult, DisassemblyDiagnoserConfig, string, IReadOnlyList<object>>)@delegate;
        }
    }
}
