namespace LBi.LostDoc.Primitives
{
    public interface IArray : IType
    {
        int Rank { get; }

        IType ElementType { get; }
    }
}