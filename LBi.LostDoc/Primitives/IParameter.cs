namespace LBi.LostDoc.Primitives
{
    public interface IParameter
    {
        string Name { get; }

        bool IsOut { get; }

        bool IsIn { get; }
    }
}