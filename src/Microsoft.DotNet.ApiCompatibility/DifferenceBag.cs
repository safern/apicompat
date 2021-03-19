using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Abstractions;
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.ApiCompatibility
{
    internal class DifferenceBag
    {
        private readonly Dictionary<string, HashSet<string>> _ignore;
        private readonly HashSet<string> _noWarn;

        private readonly List<CompatDifference> _differences = new List<CompatDifference>();

        internal DifferenceBag(string noWarn, (string diagId, string memberId)[] ignoredDifferences)
        {
            _noWarn = new HashSet<string>(noWarn?.Split(';'));
            _ignore = new Dictionary<string, HashSet<string>>();

            foreach ((string diagnosticId, string memberId) ignored in ignoredDifferences)
            {
                if (!_ignore.TryGetValue(ignored.diagnosticId, out HashSet<string> members))
                {
                    members = new HashSet<string>();
                    _ignore.Add(ignored.diagnosticId, members);
                }

                members.Add(ignored.memberId);
            }
        }

        internal void AddRange(IEnumerable<CompatDifference> differences)
        {
            foreach (CompatDifference difference in differences)
                Add(difference);
        }

        internal void Add(CompatDifference difference)
        {
            if (_noWarn.Contains(difference.Id))
                return;

            if (_ignore.TryGetValue(difference.Id, out HashSet<string> members))
            {
                if (members.Contains(difference.MemberId))
                {
                    return;
                }
            }

            _differences.Add(difference);
        }

        internal IEnumerable<CompatDifference> Differences => _differences;
    }
}