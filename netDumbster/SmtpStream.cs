using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace netDumbster.smtp
{
	public class SmtpStream : IDisposable
    {   

        private SmtpMessage message;
		private readonly Stream stream;
		private readonly StreamWriter writer;
		private readonly StreamReader reader;
        private readonly IPEndPoint endpoint;

		public SmtpStream(Stream stream, IPEndPoint endpoint)
		{
			this.stream = stream;
            this.endpoint = endpoint;
			writer = new StreamWriter(stream, Encoding.ASCII);
			
			reader = new StreamReader(stream);
            message = new SmtpMessage();
		}


		public void SendResponse(int code, string response)
		{
			writer.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0} {1}", code,response));
			writer.Flush();
		}

        //public void SendResponse(string responseWithCode)
        //{
        //    writer.WriteLine(responseWithCode);
        //    writer.Flush();
        //}


		public void SendPartial(int code, string response)
		{
			writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0}-{1}", code, response));
		}


		public string Receive()
		{
			return reader.ReadLine();
		}

		public IEnumerable<string> ReceiveLines()
		{
			string line;
			while ((line = reader.ReadLine()) != null)
				yield return line;
		}

		public void Close()
		{
			stream.Close();
		}

		public SmtpStream ToSsl(X509Certificate certificate)
		{
			var sslStream = new SslStream(stream);
			sslStream.AuthenticateAsServer(certificate);
			sslStream.Flush();
			return new SmtpStream(sslStream, Endpoint);
		}



		public IEnumerable<string> ReceiveData()
		{
			return ReceiveLines()
				.TakeWhile(l => l != ".")
				.Select(l => l.StartsWith(".") ? l.Substring(1) : l);
		}

		public IEnumerable<string> ReceiveCommands()
		{
			return ReceiveLines().TakeWhile(l => l != "QUIT");
		}

        //public void Negociate()
        //{
        //    const string ehloCommand = "EHLO ";
        //    string line = Receive();
        //    if (!line.StartsWith(ehloCommand))
        //        throw new Exception();
        //    var helo = line.Substring(ehloCommand.Length);
        //    SendPartial(250,"Hello " + helo);
        //    SendResponse(250,"STARTTLS");
        //}


		#region Implementation of IDisposable

		public void Dispose()
		{
			Close();
		}

		#endregion




        /// <summary>
        /// Last successful command received.
        /// </summary>
        public int LastCommand { get; set; }
        public string ClientDomain { get; set; }

        public SmtpMessage Message { get { return message; } set { message = value; } }
        public IPEndPoint Endpoint { get { return this.endpoint; } }

	}
}