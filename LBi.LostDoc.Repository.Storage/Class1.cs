using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBi.LostDoc.Repository.Storage
{
    public interface IVersionedFileSystem
    {
        IFileSystem GetSnapshot(RevisionIdentifier identifier);
        IEnumerable<RevisionIdentifier> GetRevisions();
    }

    public abstract class RevisionIdentifier
    {
        public abstract DateTimeOffset Created { get; }
        public abstract string Author { get; }
        public abstract string Comment { get; }
    }

    public interface IFileSystem
    {
        // something goes here
    }
}
