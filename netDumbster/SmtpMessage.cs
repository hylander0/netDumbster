#region Header

// Copyright (c) 2003, Eric Daugherty (http://www.ericdaugherty.com)
// All rights reserved.
// Modified by Carlos Mendible

#endregion Header

namespace netDumbster.smtp
{
    using System;
    using System.Collections;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Stores an incoming SMTP Message.
    /// </summary>
    public class SmtpMessage
    {
        #region Fields

        private static readonly string DOUBLE_NEWLINE = Environment.NewLine + Environment.NewLine;

        private StringBuilder data;
        private Hashtable headerFields = null;
        private ArrayList recipientAddresses;
        private EmailAddress senderAddress;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// Creates a new message.
        /// </summary>
        public SmtpMessage()
        {
            recipientAddresses = new ArrayList();
            data = new StringBuilder();
        }

        #endregion Constructors

        #region Properties

        /// <summary>Message data.</summary>
        public string Data
        {
            get
            {
                return data.ToString();
            }
        }

        /// <summary>
        /// The email address of the person
        /// that sent this email.
        /// </summary>
        public EmailAddress FromAddress
        {
            get
            {
                return senderAddress;
            }
            set
            {
                senderAddress = value;
            }
        }

        /// <summary>
        /// A hash table of all the Headers in the email message.  They keys
        /// are the header names, and the values are the assoicated values, including
        /// any sub key/value pairs is the header.
        /// </summary>
        public Hashtable Headers
        {
            get
              {
            if( headerFields == null )
            {
              headerFields = ParseHeaders( data.ToString() );
            }
            return headerFields;
              }
        }

        public string Importance
        {
            get
            {
                if (Headers.ContainsKey("importance"))
                    return Headers["importance"].ToString();
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// Parses the message body and creates an Attachment object
        /// for each attachment in the message.
        /// </summary>
        public SmtpMessagePart[] MessageParts
        {
            get
              {
            return parseMessageParts();
              }
        }

        public string Priority
        {
            get
            {
                if (Headers.ContainsKey("priority"))
                    return Headers["priority"].ToString();
                else
                    return string.Empty;
            }
        }

        /// <summary>
        /// The addresses that this message will be
        /// delivered to.
        /// </summary>
        public EmailAddress[] ToAddresses
        {
            get
            {
                return (EmailAddress[]) recipientAddresses.ToArray( typeof( EmailAddress ) );
            }
        }

        public string XPriority
        {
            get
            {
                if (Headers.ContainsKey("x-priority"))
                    return Headers["x-priority"].ToString();
                else
                    return string.Empty;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>Append data to message data.</summary>
        public void AddData( String data )
        {
            this.data.Append( data );
        }

        /// <summary>Addes an address to the recipient list.</summary>
        public void AddToAddress( EmailAddress address )
        {
            recipientAddresses.Add( address );
        }

        /// <summary>
        /// Parses an entire message or message part and returns the header entries
        /// as a hashtable.
        /// </summary>
        /// <param name="partData">The raw message or message part data.</param>
        /// <returns>A hashtable of the header keys and values.</returns>
        internal static Hashtable ParseHeaders( string partData )
        {
            Hashtable headerFields = new Hashtable();

            string[] parts = Regex.Split( partData, DOUBLE_NEWLINE );
            string headerString = parts[0] + DOUBLE_NEWLINE;

            MatchCollection headerKeyCollectionMatch = Regex.Matches( headerString, @"^(?<key>\S*):", RegexOptions.Multiline );
            string headerKey = null;
            foreach( Match headerKeyMatch in headerKeyCollectionMatch )
            {
                headerKey = headerKeyMatch.Result("${key}");
                Match valueMatch = Regex.Match( headerString, headerKey + @":(?<value>.*?)\r\n[\S\r]", RegexOptions.Singleline );
                if( valueMatch.Success )
                {
                    string headerValue = valueMatch.Result( "${value}" ).Trim();
                    headerValue = Regex.Replace( headerValue, "\r\n", "" );
                    headerValue = Regex.Replace( headerValue, @"\s+", " " );
                    // TODO: Duplicate headers (like Received) will be overwritten by the 'last' value.
                    // Header key are lower cased to avoid case sensitive issues.
                    headerFields[headerKey.ToLowerInvariant()] = headerValue;
                }
            }

            return headerFields;
        }

        private SmtpMessagePart[] parseMessageParts()
        {
            string message = data.ToString();
            string contentType = (string)Headers["content-type"];

            // Check to see if it is a Multipart Messages
            if (contentType != null && Regex.Match(contentType, "multipart/mixed", RegexOptions.IgnoreCase).Success)
            {
                // Message parts are seperated by boundries.  Parse out what the boundry is so we can easily
                // parse the parts out of the message.
                Match boundryMatch = Regex.Match(contentType, "boundary=(?<boundry>\\S+)", RegexOptions.IgnoreCase);
                if (boundryMatch.Success)
                {
                    string boundry = boundryMatch.Result("${boundry}");

                    ArrayList messageParts = new ArrayList();

                    //TODO Improve this Regex.
                    MatchCollection matches = Regex.Matches(message, "--" + boundry + ".*\r\n");

                    int lastIndex = -1;
                    int currentIndex = -1;
                    int matchLength = -1;
                    string messagePartText = null;
                    foreach (Match match in matches)
                    {
                        currentIndex = match.Index;
                        matchLength = match.Length;

                        if (lastIndex != -1)
                        {
                            messagePartText = message.Substring(lastIndex, currentIndex - lastIndex);
                            messageParts.Add(new SmtpMessagePart(messagePartText));
                        }

                        lastIndex = currentIndex + matchLength;
                    }

                    return (SmtpMessagePart[])messageParts.ToArray(typeof(SmtpMessagePart));
                }
            }
            else
            {
                return new SmtpMessagePart[] { new SmtpMessagePart(data.ToString()) };
            }
            return new SmtpMessagePart[0];
        }

        #endregion Methods
    }
}