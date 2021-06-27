/*
 * Web2Mail Version 1.1
 * 
 * Copyright 2014 kemunpus@hotmail.com.
 * 
 * Project Page:
 * https://kemunpus.visualstudio.com/DefaultCollection/KemunpusProject
 * 
 * See also:
 * http://kemunpus.azurewebsites.net/
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Kemunpus.Web2Mail.Common {

    public sealed class SmtpClient : IDisposable {

        private readonly string Server;
        private readonly int Port;
        private readonly string Id;
        private readonly string Password;
        private readonly Encoding Encoding;
        private readonly bool UseAttachment;
        private readonly bool UseSingleLineSubject;

        public SmtpClient(string server, int port, string id, string password, Encoding encoding, bool useAttachment, bool useSingleLineSubject) {
            Debug.Assert(server != null);
            Debug.Assert(port > 0);
            Debug.Assert(encoding != null);

            Server = server;
            Port = port;
            Id = id;
            Password = password;
            Encoding = encoding;
            UseAttachment = useAttachment;
            UseSingleLineSubject = useSingleLineSubject;
        }

        public void Dispose() {
        }

        public async Task SendMailAsync(string from, string to, string cc, string subject, string body) {
            Debug.WriteLine("SmtpClient : connecting to [{0}] port [{1}]...", Server.ToString(), Port.ToString());

            HostName hostName = new HostName(Server);

            const string myFQDN = "localhost";

            using (StreamSocket streamSocket = new StreamSocket()) {
                await streamSocket.ConnectAsync(hostName, Port.ToString());

                using (DataReader dataReader = new DataReader(streamSocket.InputStream))
                using (DataWriter dataWriter = new DataWriter(streamSocket.OutputStream)) {
                    dataReader.InputStreamOptions = InputStreamOptions.Partial;

                    string[] replies = await ReceiveRepliesAsync(dataReader);

                    EnsureReplyCode(replies, 220);

                    await SendCommandAsync(dataWriter, string.Format("EHLO {0}", myFQDN));

                    replies = await ReceiveRepliesAsync(dataReader);

                    if (EnsureReplyCode(replies, 250, "250-STARTTLS")) {
                        await SendCommandAsync(dataWriter, "STARTTLS");

                        replies = await ReceiveRepliesAsync(dataReader);

                        EnsureReplyCode(replies, 220);

                        await streamSocket.UpgradeToSslAsync(SocketProtectionLevel.Tls10, hostName);

                        await SendCommandAsync(dataWriter, string.Format("EHLO {0}", myFQDN));

                        replies = await ReceiveRepliesAsync(dataReader);
                    }

                    if (!string.IsNullOrEmpty(Id) || !string.IsNullOrEmpty(Password)) {

                        if (EnsureReplyCode(replies, 250, "AUTH LOGIN")) {
                            await SendCommandAsync(dataWriter, "AUTH LOGIN");

                            EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 334);

                            if (!string.IsNullOrEmpty(Id)) {
                                await SendCommandAsync(dataWriter, Convert.ToBase64String(Encoding.GetBytes(Id)));

                                EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 334);
                            }

                            if (!string.IsNullOrEmpty(Password)) {
                                await SendCommandAsync(dataWriter, Convert.ToBase64String(Encoding.GetBytes(Password)));

                                EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 235);
                            }
                        }
                    }

                    await SendCommandAsync(dataWriter, string.Format("MAIL FROM:<{0}>", from));

                    EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 250);

                    await SendCommandAsync(dataWriter, string.Format("RCPT TO:<{0}>", to));

                    EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 250);

                    if (!string.IsNullOrEmpty(cc)) {
                        await SendCommandAsync(dataWriter, string.Format("RCPT TO:<{0}>", cc));

                        EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 250);
                    }

                    if (body != null) {
                        await SendCommandAsync(dataWriter, "DATA");

                        EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 354);

                        string boundary = string.Format("_{0}_", DateTime.Now.Ticks.ToString());

                        if (UseAttachment) {
                            await SendCommandAsync(dataWriter, string.Format("Content-Type: multipart/mixed; boundary=\"{0}\"", boundary));

                        } else {
                            await SendCommandAsync(dataWriter, string.Format("Content-Type: text/html; charset=\"{0}\"", Encoding.WebName));
                            await SendCommandAsync(dataWriter, "Content-Transfer-Encoding: base64");
                        }

                        await SendCommandAsync(dataWriter, "Mime-Version: 1.0");
                        await SendCommandAsync(dataWriter, string.Format("X-Mailer: {0}", AppUtils.GetResourceString("AppName")));
                        await SendCommandAsync(dataWriter, string.Format("From: {0}", from));
                        await SendCommandAsync(dataWriter, string.Format("To: {0}", to));

                        if (!string.IsNullOrEmpty(cc)) {
                            await SendCommandAsync(dataWriter, string.Format("Cc: {0}", cc));
                        }

                        string fileName = "body.html";

                        if (subject.Length > 0) {
                            await SendCommandAsync(dataWriter, "Subject: ", false);

                            foreach (string encodedSubject in CreateBase64EncodedLines(content: subject, isSubject: true)) {
                                await SendCommandAsync(dataWriter, encodedSubject);
                            }

                            fileName = CreateBase64EncodedLines(content: string.Format("{0}.html", subject), isSubject: true)[0];
                        }

                        await SendCommandAsync(dataWriter);

                        if (UseAttachment) {
                            await SendCommandAsync(dataWriter, string.Format("--{0}", boundary));
                            await SendCommandAsync(dataWriter, "Content-Transfer-Encoding: base64");
                            await SendCommandAsync(dataWriter, string.Format("Content-Type: text/plain; charset=\"{0}\"", Encoding.WebName));
                            await SendCommandAsync(dataWriter);

                            await SendCommandAsync(dataWriter, string.Format("--{0}", boundary));
                            await SendCommandAsync(dataWriter, string.Format("Content-Type: text/html; charset=\"{0}\"; name=\"{1}\"", Encoding.WebName, fileName));
                            await SendCommandAsync(dataWriter, string.Format("Content-Description: {0}", fileName));
                            await SendCommandAsync(dataWriter, string.Format("Content-Disposition: attachment; name=\"{0}\"", fileName));
                            await SendCommandAsync(dataWriter, "Content-Transfer-Encoding: base64");
                            await SendCommandAsync(dataWriter);

                            if (Encoding == Encoding.UTF8) {
                                await SendCommandAsync(dataWriter, "77u/", false); // send UTF-8 BOM at first.                                
                            }
                        }

                        foreach (string base64EncodedLine in CreateBase64EncodedLines(body)) {
                            await SendCommandAsync(dataWriter, base64EncodedLine);
                        }

                        if (UseAttachment) {
                            await SendCommandAsync(dataWriter);
                            await SendCommandAsync(dataWriter, string.Format("--{0}--", boundary));
                        }

                        await SendCommandAsync(dataWriter, ".");

                        EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 250);
                    }

                    await SendCommandAsync(dataWriter, "QUIT");

                    EnsureReplyCode(await ReceiveRepliesAsync(dataReader), 221);
                }
            }
        }

        private string[] CreateBase64EncodedLines(string content, bool isSubject = false) {
            Debug.Assert(content != null);

            byte[] byteContent = Encoding.GetBytes(content);

            int length = isSubject && UseSingleLineSubject ? 1024 : 57; // see RFC2047.

            List<string> lines = new List<string>((content.Length / length) + 1);

            StringBuilder line = new StringBuilder((length * 3) / 2); // base64.

            int index = 0;
            int size = 0;

            while ((size = Math.Min(byteContent.Length - index, length)) > 0) {

                if (isSubject) {

                    if (lines.Count > 0) {
                        line.Append(' ');
                    }

                    line.Append("=?");
                    line.Append(Encoding.WebName);
                    line.Append("?B?");
                }

                line.Append(Convert.ToBase64String(byteContent, index, size));

                index += size;

                if (isSubject) {
                    line.Append("?=");
                }

                lines.Add(line.ToString());

                line.Clear();
            }

            return lines.ToArray();
        }

        private async Task SendCommandAsync(DataWriter dataWriter, string command = "", bool flush = true) {
            Debug.Assert(dataWriter != null);
            Debug.Assert(command != null);

            Debug.WriteLine("SmtpClient : send command  [{0}].", command);

            if (command.Length > 0) {
                dataWriter.WriteBytes(Encoding.GetBytes(command));
            }

            if (flush) {
                dataWriter.WriteString("\r\n");

                await dataWriter.StoreAsync();
            }
        }

        private static async Task<string[]> ReceiveRepliesAsync(DataReader dataReader) {
            Debug.Assert(dataReader != null);

            List<string> replies = new List<string>(20);

            StringBuilder line = new StringBuilder(50);

            await dataReader.LoadAsync((uint)(replies.Capacity * line.Capacity));

            while (dataReader.UnconsumedBufferLength > 0) {
                char c = (char)dataReader.ReadByte();

                if (c == '\r') {
                    string reply = line.ToString();

                    line.Clear();

                    Debug.WriteLine("SmtpClient : receive reply [{0}].", reply);

                    replies.Add(reply);

                } else if (c >= ' ') {
                    line.Append(c);
                }
            }

            return replies.ToArray();
        }

        private static bool EnsureReplyCode(string[] replies, int expectingReplyCode, string query = null) {
            Debug.Assert(replies != null);
            Debug.Assert(expectingReplyCode > 0);

            if (replies.Length < 1) {
                throw new IOException();
            }

            string lastReply = replies[replies.Length - 1];

            int replyCode = int.Parse(lastReply.Substring(0, 3), CultureInfo.CurrentCulture);

            if (replyCode != expectingReplyCode) {
                throw new IOException(lastReply);
            }

            if (query != null) {
                Regex regex = new Regex(query, RegexOptions.IgnoreCase);

                foreach (string reply in replies) {

                    if (regex.Match(reply).Success) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
