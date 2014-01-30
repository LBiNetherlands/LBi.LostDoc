namespace LBi.LostDoc.Primitives
{
    public interface IEnum : IType
    {
        IValueType UnderlyingType { get; }
    }
}