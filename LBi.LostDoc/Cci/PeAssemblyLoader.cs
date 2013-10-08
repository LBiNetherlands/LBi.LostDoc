using System.IO;
using Microsoft.Cci;
using AssemblyName = System.Reflection.AssemblyName;

namespace LBi.LostDoc.Cci
{
    public class PeAssemblyLoader : ICciAssemblyLoader
    {
        public PeAssemblyLoader()
        {
            this.Host = new PeReader.DefaultHost();
        }

        public IAssembly Load(string name)
        {
            AssemblyName assemblyName = new AssemblyName(name);
            AssemblyIdentity identity = UnitHelper.GetAssemblyIdentity(assemblyName, this.Host);
            IAssembly ret = this.Host.FindAssembly(identity);
            if (ret is Dummy)
                ret = this.Host.LoadAssembly(identity);

            if (ret is Dummy)
                throw new FileNotFoundException("Couldn't load assembly: " + name);

            return ret;
        }

        public IAssembly LoadFrom(string path)
        {
            AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
            AssemblyIdentity identity = UnitHelper.GetAssemblyIdentity(assemblyName, this.Host);
            AssemblyIdentity identityWithLocation = new AssemblyIdentity(identity, path);

            IAssembly ret = this.Host.FindAssembly(identityWithLocation);
            if (ret is Dummy)
                ret = this.Host.LoadAssembly(identityWithLocation);

            if (ret is Dummy)
                throw new FileNotFoundException("Couldn't load assembly: " + path);

            return ret;
        }

        public IMetadataHost Host { get; private set; }
    }
}