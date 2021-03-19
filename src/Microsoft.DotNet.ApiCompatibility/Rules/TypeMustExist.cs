using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.ApiCompatibility.Rules
{
    public class TypeMustExist : Rule
    {
        public override void Run(ITypeSymbol left, ITypeSymbol right, List<CompatDifference> differences)
        {
            if (left != null && right == null)
                differences.Add(new CompatDifference(DiagnosticIds.TypeMustExist, $"Type '{left.ToDisplayString()}' exists on the contract but not on the implementation", DifferenceType.Removed, left));
        }
    }
}
