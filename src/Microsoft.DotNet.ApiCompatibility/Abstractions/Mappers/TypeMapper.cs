using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.ApiCompatibility.Abstractions
{
    public class TypeMapper : ElementMapper<ITypeSymbol>
    {
        public TypeMapper(DiffingSettings settings) : base(settings) { }
    }
}