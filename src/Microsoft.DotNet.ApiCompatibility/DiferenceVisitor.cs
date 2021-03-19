using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.ApiCompatibility
{
    public class DiferenceVisitor : MapperVisitor
    {
        private readonly DifferenceBag _differenceBag;

        public DiferenceVisitor(string noWarn = null, (string diagnosticId, string memberId)[] ignoredDifferences = null)
        {
            _differenceBag = new DifferenceBag(noWarn ?? string.Empty, ignoredDifferences ?? Array.Empty<(string, string)>());
        }

        public override void Visit(AssemblyMapper assembly)
        {
            _differenceBag.AddRange(assembly.GetDifferences());
            base.Visit(assembly);
        }

        public override void Visit(TypeMapper type)
        {
            _differenceBag.AddRange(type.GetDifferences());
        }

        public IEnumerable<CompatDifference> Differences => _differenceBag.Differences;
    }
}
