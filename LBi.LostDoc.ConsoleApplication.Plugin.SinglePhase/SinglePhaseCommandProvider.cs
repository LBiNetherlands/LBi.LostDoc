using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBi.LostDoc.Core;

namespace LBi.LostDoc.ConsoleApplication.Plugin.SinglePhase
{
    [Export(typeof(ICommandProvider))]
    public class SinglePhaseCommandProvider : ICommandProvider
    {
        public Type[] GetCommands()
        {
            return new[] { typeof(SinglePhaseCommand) };
        }
    }
}
