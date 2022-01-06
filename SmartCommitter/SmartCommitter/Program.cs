using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SmartCommitter
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            int lowerChangelist = 0;
            if (args.Length == 0 || int.TryParse(args[0], out lowerChangelist) == false)
            {
                Console.WriteLine("invalid command argument!. usage: smartcommitter.exe [changelist]");
                return -1;
            }

            lowerChangelist = args.Select(s => int.Parse(s)).ToList().Min();

            try
            {
                Console.WriteLine($"start. start changelist: {lowerChangelist}");
                var p4settings = (
                        uri: ConfigurationManager.AppSettings["P4Uri"],
                        workspace: ConfigurationManager.AppSettings["P4Workspace"],
                        user: ConfigurationManager.AppSettings["P4User"],
                        pw: ConfigurationManager.AppSettings["P4Password"]);
                //p4settings.pw = Encoding.UTF8.GetString(Convert.FromBase64String(p4settings.pw));

                using var p4 = new P4Controller(
                    p4settings.uri, p4settings.workspace, p4settings.user, p4settings.pw);
                var candidates = p4.GetCandidates(lowerChangelist);

                var pattern = ConfigurationManager.AppSettings["keyPattern"];
                var commentFormat = ConfigurationManager.AppSettings["commentFormat"];
                var patternWithCaptureGroup = $"(?<key>{pattern})";
                var result = candidates?.ToJiraComments(patternWithCaptureGroup, commentFormat);
                if (result == null || result.Length == 0)
                {
                    Console.WriteLine($"completed. zero candidates");
                    return 0;
                }

                var jiraSettings = (
                    uri: ConfigurationManager.AppSettings["JiraUri"],
                    user: ConfigurationManager.AppSettings["JiraUser"],
                    pw: ConfigurationManager.AppSettings["JiraPassword"]);
                //jiraSettings.pw = Encoding.UTF8.GetString(Convert.FromBase64String(jiraSettings.pw));

                var jira = new JiraController(jiraSettings.uri, jiraSettings.user, jiraSettings.pw);
                jira.AddCommentsAsync(result).Wait();

                Console.WriteLine($"completed. {result.Length} candidates");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -2;
            }
        }

        private static (string key, string comment)[] ToJiraComments(this IList<Perforce.P4.Changelist> list, string keyPettern, string commentFormat)
        {
            if (list.Count == 0)
            {
                return new (string, string)[0];
            }

            var map = new Dictionary<string, Func<Perforce.P4.Changelist, string>>()
            {
                { "?changelist?", e => e.Id.ToString() },
                { "?user?", e => e.OwnerName },
                { "?newLine?", e => Environment.NewLine },
                { "?description?", e=> e.Description }
            };
            var result = list.SelectMany(changes => Regex.Matches(changes.Description, keyPettern)
                                        .Select(match => (key: match.Groups["key"], changes)))
                .OrderBy(e => e.changes.Id)
                .Select(e => (
                                key: e.key.Value.Substring(1), 
                                comment: Regex.Replace(commentFormat, "\\?[a-zA-Z]+\\?", m => map[m.Value](e.changes))
                            ))
                .ToArray();
            return result;
        }
    }
}
