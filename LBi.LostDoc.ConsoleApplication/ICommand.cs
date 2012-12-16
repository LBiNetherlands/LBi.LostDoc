using System.IO;

namespace LBi.LostDoc.ConsoleApplication
{
    public interface ICommand
    {
        string[] Name { get; }
        void Invoke();
        void Usage(TextWriter output);
    }
}