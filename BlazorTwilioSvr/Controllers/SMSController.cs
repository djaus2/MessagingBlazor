using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twilio.AspNet.Common;
using Twilio.AspNet.Core;
using Twilio.TwiML;

using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BlazorTwilioSvr.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class smsController : Controller
    {
        //Modes of responses to received messages
        public enum SMSResponse { reply, noReply }
        public static SMSResponse smsResponse { get; set; } = SMSResponse.noReply;

        // Modes of actions with msg wrt forwarding
        public enum SMSAction { forwardAsSMS, forwardAsEmail, none}
        public SMSAction smsAction { get; set; } = SMSAction.forwardAsSMS;

        // If yes ,Y/N/Indet are all forwarded.  Otherwise only N and Indet
        public static bool ForwardAllToAdmin { get; set; } = true;

        
        // Responses are forwarded to this number
        public static string AdminMobile {get; set;} = "insert";

        // Perhaps should move this to ForwardUsingTwilio settings??
        public static string TwilioMobile { get; set; } = "insert";

        [HttpPost]
        public TwiMLResult Index()
        {
            var requestBody = Request.Form["Body"];
            var from = Request.Form["From"];
            // to will be the Twillio number
            var to = Request.Form["To"];

            if (smsAction == SMSAction.forwardAsSMS)
            {
                ForwardUsingTwilio tws = new ForwardUsingTwilio();
                tws.ProcessMessage(requestBody, from);
            }
            else if (smsAction == SMSAction.forwardAsEmail)
            {
                // ToDo
            }

            ////////////////////////////
            if (smsResponse == SMSResponse.reply)
            {
                var messagingResponse = new MessagingResponse();
                // Could lookup from for name
                messagingResponse.Message($"Thanks {from}. Message Received: {requestBody}");
                return new TwiMLResult(messagingResponse); // messagingResponse);
            }
            ////////////////////////////
            else if (smsResponse == SMSResponse.noReply)
            {
                var messagingResponse = new MessagingResponse();
                return new TwiMLResult(messagingResponse);
            }
            //Want to redirect
           
            return null;
        }



        public class ForwardUsingTwilio
        {
            //Use the same credentials for each message
            public static ForwardUsingTwilio Settings { get; set; } = null;

            public string AccountSid { get; set; }
            public string AuthToken { get; set; }

            /// <summary>
            /// Get the Twilio Credentials
            /// </summary>
            public static void Init()
            {
                if (Settings == null)
                {
                    var dirParent = Directory.GetParent(Directory.GetCurrentDirectory());
                    var folders = dirParent.GetDirectories("Twillio");
                    if (folders.Length != 0)
                    {
                        var builder = new ConfigurationBuilder()
                            .SetBasePath(folders[0].FullName)
                            .AddJsonFile(@"appsettings.json", optional: true, reloadOnChange: true);

                        Microsoft.Extensions.Configuration.IConfiguration Configuration = builder.Build();

                        Settings = new ForwardUsingTwilio();
                        Configuration.GetSection("ConnectionSettings").Bind(Settings);
                    }
                }
            }

            /// <summary>
            /// Forward the Y or N outcome and message to Admin
            /// </summary>
            /// <param name="outcome">The Y or N outcome</param>
            /// <param name="body">The received SMS msg</param>
            /// <param name="from">The senders Mobile No.</param>
            private void ForwardMessage(YesOrNo outcome, string body, string from)
            {
                if (Settings==null)
                    Init();
                string accountSid = Settings.AccountSid;
                string authToken = Settings.AuthToken;

                TwilioClient.Init(accountSid, authToken);


                var message = MessageResource.Create(
                    body: $"Outcome:{outcome} Message:{body} - From:{from}" ,
                    from: new Twilio.Types.PhoneNumber(TwilioMobile),
                    to: new Twilio.Types.PhoneNumber(AdminMobile)
                );
            }

            /// <summary>
            /// The three possible outcomes of a received message
            /// </summary>
            enum YesOrNo {yes,no,indeterminate };

            // ToDo:
            /// <summary>
            /// Log in database
            /// </summary>
            /// <param name="res">The Y or No outcome</param>
            /// <param name="from">The sender's Mobile No.</param>
            /// <param name="msg">The received SMS msg</param>
            private void LogMsg(YesOrNo res, string from, string msg)
            {
                // Get Name from DB using from

                // Log  from, date, res and msg

                // In task log Y or N
            }

            /// <summary>
            /// Forwards message depending upon Y or N result.
            /// </summary>
            /// <param name="msg">The received SMS msg</param>
            /// <param name="from">The sender's Mobile No.</param>
            public void ProcessMessage(string msg, string from)
            {
                YesOrNo res = HandleYesNo(msg);
                LogMsg(res, from, msg);
                switch (res)
                {
                    case YesOrNo.indeterminate:
                        // Perhaps a reply here to sender.
                        ForwardMessage(res, msg, from);
                        break;
                    case YesOrNo.yes:
                        if (smsController.ForwardAllToAdmin)
                            ForwardMessage(res, msg, from);
                        break;
                    case YesOrNo.no:
                        ForwardMessage(res, msg, from);
                        break;

                }
            }

            /// <summary>
            /// Determine if the response was yes or no. Rather apriori AI.
            /// </summary>
            /// <param name="msg">The received message with Y or N</param>
            /// <returns>Is the response a Y or N?</returns>
            YesOrNo HandleYesNo(string msg)
            {
                msg = msg.ToUpper().Trim();

                if (string.IsNullOrEmpty(msg))
                    return YesOrNo.indeterminate;
                switch (msg.Length)
                {
                    //case 0:
                    //    return YesOrNo.indeterminate;
                    //    break;
                    case 1:
                        if (msg[0] == 'N')
                            return YesOrNo.no;
                        else if (msg[0] == 'Y')
                            return YesOrNo.yes;
                        break;
                    default:
                        //Check for the word yes or no
                        string[] msgs = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var no = from n in msgs where (n == "N") || (n == "NO") select n;
                        var yes = from y in msgs where (y == "Y") || (y == "YES") select y;
                        if ((no.Count() != 0) && (yes.Count() == 0))
                            return YesOrNo.no;
                        if ((yes.Count() != 0) && (no.Count() == 0))
                            return YesOrNo.yes;
                        break;
                }
                return YesOrNo.indeterminate;


                //if (msg[0] == 'N')
                //    return YesOrNo.no;
                //else if (msg[0] == 'Y')
                //    return YesOrNo.yes;
                //else if (msg.Contains("NO"))
                //    return YesOrNo.no;
                //else if (msg.Contains("YES"))
                //    return YesOrNo.yes;
                //return YesOrNo.indeterminate;
            }

        }
    }
}
