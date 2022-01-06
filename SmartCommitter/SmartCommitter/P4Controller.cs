using Perforce.P4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCommitter
{
    public sealed class P4Controller : IDisposable
    {
        private readonly string uri;
        private readonly string workspace;
        private readonly string user;
        private readonly string password;

        private readonly Repository repository;
        public P4Controller(string uri, string workspace, string user, string password)
        {
            this.uri = uri;
            this.workspace = workspace;
            this.user = user;
            this.password = password;

            var server = new Server(new ServerAddress(uri));
            this.repository = new Repository(server);
        }

        public IList<Changelist> GetCandidates(int lowerChangelist)
        {
            var connection = this.repository.Connection;
            connection.UserName = user;
            connection.Client = new Client { Name = this.workspace };
            try
            {
                if (connection.Connect(null) == false)
                {
                    throw new Exception("p4 connect failed!");
                }

                var credential = connection.Login(this.password);
                var info = this.repository.GetServerMetaData(null);
                Console.WriteLine($"connected to {info.Address.Uri}.");

                var range = new VersionRange(new ChangelistIdVersion(lowerChangelist), VersionSpec.Head);
                var opt = new ChangesCmdOptions(
                    ChangesCmdFlags.FullDescription, null, 0, ChangeListStatus.Submitted, null);
                //var file = new FileSpec(new DepotPath("//Stream/Main/..."), range);
                var file = new FileSpec(new DepotPath("//depot/..."), range);
                return this.repository.GetChangelists(opt, file); 
            }
            finally
            {
                if (connection.Status == ConnectionStatus.Connected)
                {
                    connection.Disconnect();
                }
            }
        }

        public void Dispose()
        {
            if (this.repository.Connection.Status == ConnectionStatus.Connected)
            {
                this.repository.Connection.Disconnect();
            }
            
            this.repository.Dispose();
        }
    }
}
