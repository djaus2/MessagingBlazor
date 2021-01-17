// Install the C# / .NET helper library from twilio.com/docs/csharp/install

using System;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Twillio
{

    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Microsoft.Extensions.Configuration.IConfiguration Configuration = builder.Build();

            var twillioSettings = new TwillioSettings();
            Configuration.GetSection("ConnectionSettings").Bind(twillioSettings);

            string accountSid = twillioSettings.AccountSid;
            string authToken = twillioSettings.AuthToken;

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
                body: "I can now send SMS from an app. Don't reply as not set up for that yet. Thanks. Had to upgrade account. Could omly use US numbers to senbd to with trial",
                from: new Twilio.Types.PhoneNumber("insert"),
                to: new Twilio.Types.PhoneNumber("insert")
            );

            Console.WriteLine(message.Sid);
        }
        public class TwillioSettings
        {
            public string AccountSid { get; set; }
            public string AuthToken { get; set; }
        }
    }
}

