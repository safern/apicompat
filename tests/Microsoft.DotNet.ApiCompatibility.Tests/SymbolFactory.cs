using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.ApiCompatibility.Tests
{
    internal static class SymbolFactory
    {
        internal static IAssemblySymbol GetAssemblyFromSyntax(string syntax, bool enableNullable = false, [CallerMemberName] string assemblyName = "")
        {
            CSharpCompilation compilation = CreateCSharpCompilationFromSyntax(syntax, assemblyName, enableNullable);
            return compilation.Assembly;
        }

        internal static IAssemblySymbol GetAssemblyFromSyntaxWithReferences(string syntax, IEnumerable<string> referencesSyntax, bool enableNullable = false, [CallerMemberName] string assemblyName = "")
        {
            CSharpCompilation compilation = CreateCSharpCompilationFromSyntax(syntax, assemblyName, enableNullable);
            CSharpCompilation compilationWithReferences = CreateCSharpCompilationFromSyntax(referencesSyntax, $"{assemblyName}_reference", enableNullable);

            compilation = compilation.AddReferences(compilationWithReferences.ToMetadataReference());
            return compilation.Assembly;
        }

        private static CSharpCompilation CreateCSharpCompilationFromSyntax(string syntax, string name, bool enableNullable)
        {
            CSharpCompilation compilation = CreateCSharpCompilation(name, enableNullable);
            return compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(syntax));
        }

        private static CSharpCompilation CreateCSharpCompilationFromSyntax(IEnumerable<string> syntax, string name, bool enableNullable)
        {
            CSharpCompilation compilation = CreateCSharpCompilation(name, enableNullable);
            IEnumerable<SyntaxTree> syntaxTrees = syntax.Select(s => CSharpSyntaxTree.ParseText(s));
            return compilation.AddSyntaxTrees(syntaxTrees);
        }

        private static CSharpCompilation CreateCSharpCompilation(string name, bool enableNullable)
        {
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                                  nullableContextOptions: enableNullable ? NullableContextOptions.Enable : NullableContextOptions.Disable);

            return CSharpCompilation.Create(name, options: compilationOptions, references: DefaultReferences);
        }

        private static IEnumerable<MetadataReference> DefaultReferences { get; } = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };
    }
}
