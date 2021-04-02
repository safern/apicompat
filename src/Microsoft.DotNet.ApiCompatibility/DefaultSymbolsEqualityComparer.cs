﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.ApiCompatibility
{
    public class DefaultSymbolsEqualityComparer : IEqualityComparer<ISymbol>
    {
        public bool Equals(ISymbol x, ISymbol y) =>
            string.Equals(GetKey(x), GetKey(y), StringComparison.OrdinalIgnoreCase);

        public int GetHashCode(ISymbol obj) =>
            GetKey(obj).GetHashCode();

        private static string GetKey(ISymbol symbol) =>
            symbol switch
            {
                IMethodSymbol => symbol.ToDisplayString(),
                IFieldSymbol => symbol.ToDisplayString(),
                IPropertySymbol => symbol.ToDisplayString(),
                IEventSymbol => symbol.ToDisplayString(),
                _ => symbol.Name,
            };
    }
}