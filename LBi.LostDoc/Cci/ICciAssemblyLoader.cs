using Microsoft.Cci;

namespace LBi.LostDoc.Cci
{

    public interface ICciAssemblyLoader
    {
        IAssembly Load(string name);
        IAssembly LoadFrom(string path);
        IMetadataHost Host { get; }
    }
}