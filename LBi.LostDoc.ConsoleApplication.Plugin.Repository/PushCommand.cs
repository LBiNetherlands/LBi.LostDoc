using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Net.Http;
using LBi.Cli.Arguments;

namespace LBi.LostDoc.ConsoleApplication.Plugin.Repository
{
    [ParameterSet("Push ldoc file to repository", Command = "Push")]
    public class PushCommand : ICommand
    {
        [Parameter(HelpMessage = "Include verbose output.")]
        public LBi.Cli.Arguments.Switch Verbose { get; set; }

        [Parameter(HelpMessage = "Path to ldoc file."), Required]
        public string Path { get; set; }

        [Parameter(HelpMessage = "Server address."), Required]
        public string Server { get; set; }

        [Parameter(HelpMessage = "Security key."), Required]
        public string ApiKey { get; set; }

        public void Invoke()
        {
            UriBuilder requestUri = new UriBuilder(this.Server);
            requestUri.Query = "apiKey=" + this.ApiKey;
            using (FileStream inputStream = File.OpenRead(this.Path))
            using (HttpClient client = new HttpClient())
            {
                MultipartFormDataContent form = new MultipartFormDataContent();

                HttpContent fileContent = new StreamContent(inputStream);
                form.Add(fileContent, "file");

                fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data");
                fileContent.Headers.ContentDisposition.FileName = System.IO.Path.GetFileName(this.Path);

                client.PostAsync(requestUri.Uri, form);
            }
        }
    }
}