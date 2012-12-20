using System.ComponentModel.DataAnnotations;
using LBi.Cli.Arguments;

namespace LBi.LostDoc.ConsoleApplication.Plugin.Repository
{
    [ParameterSet("Push ldoc file to repository", Command = "Push")]
    public class PushCommand :ICommand
    {
        [Parameter(HelpMessage = "Path to ldoc files."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Server address."), Required]
        public string Server { get; set; }

        [Parameter(HelpMessage = "Security key."), Required]
        public string ApiKey { get; set; }

        public void Invoke()
        {
            
        }
    }
}