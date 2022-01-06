using Atlassian.Jira;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCommitter
{
    public sealed class JiraController
    {
        private readonly string uri;
        private readonly string user;
        private readonly string password;

        private readonly Jira jira;
        public JiraController(string uri, string user, string password)
        {
            this.uri = uri;
            this.user = user;
            this.password = password;

            this.jira = Jira.CreateRestClient(uri, user, password);
        }

        public async Task AddCommentsAsync((string key, string comment)[] args)
        {
            var sb = new StringBuilder();
            var issueDic = await this.jira.Issues.GetIssuesAsync(args.Select(e => e.key));
            foreach (var pair in args)
            {
                if (issueDic.TryGetValue(pair.key, out var issue) == false)
                {
                    sb.AppendLine($"not found issue. key:{pair.key}");
                    continue;
                }

                var lines = pair.comment.Split(Environment.NewLine);
                var headline = $"# {pair.key}. {lines[0]}";

                var comments = await issue.GetCommentsAsync();
                if (comments.Any(e => e.Author == this.user && e.Body.StartsWith(lines[0])))
                {
                    sb.AppendLine($"already commented. {headline}");
                    continue;
                }
                
                sb.AppendLine($"success. {headline}");
                await issue.AddCommentAsync(pair.comment);
            }

            Console.WriteLine(sb.ToString());
            await Task.CompletedTask;
        }
    }
}
