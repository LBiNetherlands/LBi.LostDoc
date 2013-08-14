using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBi.LostDoc.Templating;
using Xunit;
using Xunit.Extensions;

namespace LBi.LostDoc.Test
{
    public class UriTests
    {
        [Theory]
        [InlineData("Company.Project.Library.dll/2.3/Company/Project/Bar-1/ConsumeOpen.html", "Company.Project.Library.dll/2.3/Company/Project/Bar-1.html", "../Bar-1.html")]
        [InlineData("foo/a/b", "foo/c", "../c")]
        [InlineData("foo/a/b", "foo/c/d", "../c/d")]
        [InlineData("foo/a", "foo/b", "b")]
        [InlineData("foo/a/", "foo/b", "../b")]
        [InlineData("foo/a", "foo/a", "a")]
        [InlineData("foo/a/", "foo/a", "../a")]
        [InlineData("foo/a", "foo/b/c", "b/c")]
        [InlineData("foo/a/", "foo/b/c", "../b/c")]
        [InlineData("foo/a/b/", "foo/c/d", "../../c/d")]
        [InlineData("a/b", "a/c", "c")]
        public void GetRelativeUrl(string current, string target, string relative)
        {
            Uri currentUri = new Uri(current, UriKind.RelativeOrAbsolute);
            Uri targetUri = new Uri(target, UriKind.RelativeOrAbsolute);

            Uri relativeUri = currentUri.GetRelativeUri(targetUri);

            Assert.Equal(relative, relativeUri.ToString());
        }
    }
}
