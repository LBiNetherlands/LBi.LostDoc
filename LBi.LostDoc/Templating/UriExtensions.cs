using System;
using System.Text;

namespace LBi.LostDoc.Templating
{
    public static class UriExtensions
    {
        public static Uri GetRelativeUri(this Uri current, Uri target)
        {
            string targetStr = target.ToString();
            string[] targetFragments = targetStr.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string currentStr = current.ToString();
            string[] currentFragments = currentStr.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            bool targetIsDir = targetStr[targetStr.Length - 1] == '/';
            bool currentIsDir = currentStr[currentStr.Length - 1] == '/';

            int targetLength = targetIsDir ? targetFragments.Length : targetFragments.Length - 1;
            int currentLength = currentIsDir ? currentFragments.Length : currentFragments.Length - 1;

            int maxCommonFragments = Math.Min(targetLength, currentLength);

            int sharedFragments;
            for (sharedFragments = 0; sharedFragments < maxCommonFragments; sharedFragments++)
            {
                if (!StringComparer.OrdinalIgnoreCase.Equals(currentFragments[sharedFragments], targetFragments[sharedFragments]))
                    break;
            }

            StringBuilder relativeUri = new StringBuilder();

            int backtrack = currentLength - sharedFragments;
            for (int k = 0; k < backtrack; k++)
                relativeUri.Append("../");

            for (int k = sharedFragments; k < targetFragments.Length; k++)
            {
                relativeUri.Append(targetFragments[k]);
                if (k < targetFragments.Length - 1)
                    relativeUri.Append('/');
            }

            Uri ret = new Uri(relativeUri.ToString(), UriKind.Relative);
            return ret;
        }
    }
}