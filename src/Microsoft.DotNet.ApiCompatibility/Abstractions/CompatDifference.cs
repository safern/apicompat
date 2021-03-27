using Microsoft.CodeAnalysis;
using System;

namespace Microsoft.DotNet.ApiCompatibility.Abstractions
{
    public class CompatDifference : IEquatable<CompatDifference>
    {
        public string Id { get; }
        public DifferenceType Type { get; }
        public virtual string Message { get; }
        public string MemberId { get; }

        private CompatDifference() { }

        public CompatDifference(string id, string message, DifferenceType type, ISymbol member)
            : this(id, message, type, member.GetDocumentationCommentId())
        {
        }

        public CompatDifference(string id, string message, DifferenceType type, string memberId)
        {
            Id = id;
            Message = message;
            Type = type;
            MemberId = memberId;
        }

        public bool Equals(CompatDifference other) => 
            Type == other.Type &&
            Id.Equals(other.Id, StringComparison.OrdinalIgnoreCase) &&
            MemberId.Equals(other.MemberId, StringComparison.OrdinalIgnoreCase) &&
            Message.Equals(other.Message, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() =>
            HashCode.Combine(MemberId, Id, Message, Type);

        public override string ToString() => $"{Id} : {Message}";
    }
}
