using System.Collections.Generic;
using System.IO;

namespace LBi.LostDoc.Repository.Web.Host.Areas.Administration.Models
{
    public class DirectoryListModel
    {
        public DirectoryInfo Root { get; set; }
        public IEnumerable<DirectoryInfo> Directories { get; set; }
        public IEnumerable<FileInfo> Files { get; set; }
    }
}