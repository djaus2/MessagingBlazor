using System;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SmtpMail {
	class Program {
		static void Main() {

			var builder = new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

			Microsoft.Extensions.Configuration.IConfiguration Configuration = builder.Build();

			var office365Settings = new Office365Settings();
			Configuration.GetSection("ConnectionSettings").Bind(office365Settings);

			string office365EmailAccount = office365Settings.Office365EmailAccount;
			string pwd = office365Settings.Pwd;
			string clientHost = office365Settings.ClientHost;
			int clientPort = office365Settings.ClientPort;


			MailMessage msg = new MailMessage();
			msg.To.Add(new MailAddress("insert", "The Recipient"));
			msg.From = new MailAddress("insert", "The Sender");
			msg.Subject = "Test Email from Azure Web App using Office365";
			msg.Body = "<p>Test emails on Azure from a Web App via Office365</p>";
			msg.IsBodyHtml = true;
			SmtpClient client = new SmtpClient();
			client.UseDefaultCredentials = false;
			client.Credentials = new System.Net.NetworkCredential(office365EmailAccount, pwd); //insert your credentials
			client.Port = clientPort; // 587;
			client.Host = clientHost; // "smtp.office365.com";
			client.DeliveryMethod = SmtpDeliveryMethod.Network;
			client.EnableSsl = true;
			try {
				client.Send(msg);
				Console.WriteLine("Email Successfully Sent");
			}
			catch (Exception ex) {
				Console.WriteLine(ex.ToString());
			}
		}

		public class Office365Settings
		{
			public string Office365EmailAccount {get;set;}
			public string Pwd {get; set;}
			public string ClientHost {get; set;}
			public int ClientPort { get; set; }
        }
	}
}
