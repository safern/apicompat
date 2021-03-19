using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.ApiCompatibility.Abstractions
{
    public abstract class Rule
    {
        public virtual void Run(IAssemblySymbol left, IAssemblySymbol right, List<CompatDifference> differences)
        {
        }
        public virtual void Run(ITypeSymbol left, ITypeSymbol right, List<CompatDifference> differences)
        {
        }
    }
}
