using System;

namespace LBi.LostDoc.Primitives
{
    public interface IAsset : IEquatable<IAsset>
    {
        Primitive PrimitiveType { get; }
        string Name { get; }
        AssetIdentifier Id { get; }
        AssetType Type { get; }

        void Visit(IVisitor visitor);
    }
}