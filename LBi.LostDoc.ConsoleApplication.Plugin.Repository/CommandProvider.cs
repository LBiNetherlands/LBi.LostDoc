using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LBi.LostDoc.ConsoleApplication.Plugin.Repository
{
    [Export(typeof(ICommandProvider))]
    public class CommandProvider : ICommandProvider
    {
        public Type[] GetCommands()
        {
            return new[] {typeof(PushCommand)};
        }
    }
}
