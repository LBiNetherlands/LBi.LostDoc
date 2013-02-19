using System;

namespace LBi.LostDoc.Repository.Web.Extensibility
{
    public class PackageEventArgs : EventArgs
    {
        public PackageEventArgs(AddInPackage package)
        {
            this.Package = package;
        }

        public AddInPackage Package { get; protected set; }
    }
}