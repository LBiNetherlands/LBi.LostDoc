namespace Company.Project.Library
{
    /// <summary>
    /// A generic interface
    /// </summary>
    public interface IGenericInterface<in T1, out T2>  where T1 : struct
                                                       where T2 : class, new()
    {
        
    }
}