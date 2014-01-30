namespace LBi.LostDoc.Primitives
{
    public interface IMember : IAsset
    {
        bool IsPrivate { get; }

        bool IsFamily { get; }

        bool IsAssembly { get; }

        bool IsFamilyAndAssembly { get; }

        bool IsFamilyOrAssembly { get; }

        bool IsPublic { get; }
    }
}