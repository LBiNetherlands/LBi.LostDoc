using System.Reflection;

namespace LBi.LostDoc.Core
{
    public class QualifiedName
    {
        public QualifiedName(string fullyQualifiedName)
        {
            this.TypeName = fullyQualifiedName.Substring(0, fullyQualifiedName.IndexOf(','));
            this.AssemblyName = new AssemblyName(fullyQualifiedName.Substring(fullyQualifiedName.IndexOf(',') + 1));
        }

        public string TypeName { get; protected set; }

        public AssemblyName AssemblyName { get; protected set; }
    }
}