using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using Topshelf;

namespace netDumbster.Service
{
    public class SimpleSmtpServerProxy
    {
        netDumbster.smtp.SimpleSmtpServer server;
        public SimpleSmtpServerProxy()
        {

        }

        public void Start()
        {
            server = netDumbster.smtp.SimpleSmtpServer.Start(25);
            server.OnReceived += server_OnReceived;
        }

        void server_OnReceived(object sender, smtp.MessageReceivedArgs e)
        {
            try
            {
                string filePath = System.IO.Path.Combine(ConfigurationManager.AppSettings["InboxEmailFileStoreDirectory"], string.Format("{0}.txt", DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")));
                System.IO.File.WriteAllText(filePath, e.Message.Data);                
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error {0}", ex.Message);
            }

        }
        public void Stop()
        {
            if (server != null)
                server.Stop();
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var host = HostFactory.New(x =>                                
            {
                x.Service<SimpleSmtpServerProxy>(s =>                       
                {
                    s.ConstructUsing(name => new SimpleSmtpServerProxy());
                    s.WhenStarted(tc => {
                        tc.Start();
                    });
                    s.WhenStopped(tc => {
                        tc.Stop();
                    });               
                });
                x.RunAsLocalSystem();

                x.SetDescription("Fake Dot Net SMTP Server (netDumpster)");        
                x.SetDisplayName("Dot NET Dumpster");                       
                x.SetServiceName("netDumpster");                       
            });

            host.Run();
                               
        }
    }
}
