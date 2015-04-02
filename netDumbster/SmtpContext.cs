﻿#region Header

// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

#endregion Header

namespace netDumbster.smtp
{
    using System;
    using System.Net.Sockets;
    using System.Text;

    using netDumbster.smtp.Logging;
    using System.Security.Cryptography.X509Certificates;
    using System.IO;

    /// <summary>
    /// Maintains the current state for a SMTP client connection.
    /// </summary>
    /// <remarks>
    /// This class is similar to a HTTP Session.  It is used to maintain all
    /// the state information about the current connection.
    /// </remarks>
    public class SmtpContext
    {
        #region Fields
        public X509Certificate Certificate { get; set; }
        private const string EOL = "\r\n";

        /// <summary>The client domain, as specified by the helo command.</summary>
        private string clientDomain;

        /// <summary>Encoding to use to send/receive data from the socket.</summary>
        private Encoding encoding;

        /// <summary>
        /// It is possible that more than one line will be in
        /// the queue at any one time, so we need to store any input
        /// that has been read from the socket but not requested by the
        /// ReadLine command yet.
        /// </summary>
        private StringBuilder inputBuffer;

        /// <summary>Last successful command received.</summary>
        private int lastCommand;

        /// <summary>The incoming message.</summary>
        private SmtpMessage message;

        /// <summary>The socket to the client.</summary>
        private Stream stream;
        ILog _Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Initialize this context for a given socket connection.
        /// </summary>
        public SmtpContext(Stream stream)
            : this(stream, null)
        {
          
        }
        /// <summary>
        /// Initialize this context for a given socket connection and cert for TLS
        /// </summary>
        public SmtpContext(Stream stream, X509Certificate cert)
        {
            Certificate = cert;
            this.lastCommand = -1;
            this.stream = stream;
            message = new SmtpMessage();

            // Set the encoding to ASCII.
            encoding = Encoding.ASCII;

            // Initialize the input buffer
            inputBuffer = new StringBuilder();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// The client domain, as specified by the helo command.
        /// </summary>
        public string ClientDomain
        {
            get
            {
                return clientDomain;
            }
            set
            {
                clientDomain = value;
            }
        }

        /// <summary>
        /// Last successful command received.
        /// </summary>
        public int LastCommand
        {
            get
            {
                return lastCommand;
            }
            set
            {
                lastCommand = value;
            }
        }

        /// <summary>
        /// The SMTPMessage that is currently being received.
        /// </summary>
        public SmtpMessage Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        /// The Socket that is connected to the client.
        /// </summary>
        public Stream Stream
        {
            get
            {
                return stream;
            }
        }

        #endregion Properties

        #region Methods
        public IEnumerable<string> ReceiveLines()
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                yield return line;
        }
        /// <summary>
        /// Reads an entire line from the socket.  This method
        /// will block until an entire line has been read.
        /// </summary>
        public String ReadLine()
        {
            // If we already buffered another line, just return
            // from the buffer.
            string output = ReadBuffer();
            if (output != null)
            {
                return output;
            }

            // Otherwise, read more input.
            byte[] byteBuffer = new byte[80];
            int count;

            // Read from the socket until an entire line has been read.
            do
            {
                // Read the input data.
                count = socket.Receive(byteBuffer);

                if (count == 0)
                {
                    return null;
                }

                inputBuffer.Append(encoding.GetString(byteBuffer, 0, count));
            }
            while ((output = ReadBuffer()) == null);

            // IO Log statement is in ReadBuffer...

            return output;
        }

        /// <summary>
        /// Resets this context for a new message
        /// </summary>
        public void Reset()
        {
            _Log.Debug("Resetting SmtpContext.");
            inputBuffer.Length = 0;
            message = new SmtpMessage();
            lastCommand = SmtpProcessor.COMMAND_HELO;
            _Log.Debug("Done resetting SmtpContext.");
        }

        /// <summary>
        /// Writes the string to the socket as an entire line.  This
        /// method will append the end of line characters, so the data
        /// parameter should not contain them.
        /// </summary>
        /// <param name="data">The data to write the the client.</param>
        public void WriteLine(string data)
        {
            socket.Send(encoding.GetBytes(data + EOL));
        }

        /// <summary>
        /// Helper method that returns the first full line in
        /// the input buffer, or null if there is no line in the buffer.
        /// If a line is found, it will also be removed from the buffer.
        /// </summary>
        private string ReadBuffer()
        {
            // If the buffer has data, check for a full line.
            if (inputBuffer.Length > 0)
            {
                string buffer = inputBuffer.ToString();
                int eolIndex = buffer.IndexOf(EOL);
                if (eolIndex != -1)
                {
                    string output = buffer.Substring(0, eolIndex);
                    inputBuffer = new StringBuilder(buffer.Substring(eolIndex + 2));
                    return output;
                }
            }
            return null;
        }
        private SmtpContext UpgradeContextToSSL()
        {
            if (Certificate == null)
                throw new InvalidOperationException("Unable to upgrade to SSL connection since the certificate is not provided.");
            return this;
        }
        #endregion Methods
    }
}