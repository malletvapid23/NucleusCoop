using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nucleus.Gaming.Coop.Generic
{
	public class Hub
	{
		private bool checkedUpdate = false;
		private bool updateAvailable = false;

		public bool IsUpdateAvailable(bool fetch)
		{
			if (fetch && !checkedUpdate)
			{
				checkedUpdate = true;
				updateAvailable = CheckUpdateAvailable();
			}

			return updateAvailable;
		}

		public _Maintainer Maintainer = new _Maintainer();
		public _Handler Handler = new _Handler();

		public class _Maintainer
		{
			public string Name = "";
			public string Id = "";
		}

		public class _Handler
		{
			public int Version = -1;
			public string Id = "";
		}
		
		private bool CheckUpdateAvailable()
		{
			if (Handler.Version < 0)
				return false;

			string id = Handler.Id;
			int newVersion = -1;

			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			ServicePointManager.DefaultConnectionLimit = 9999;

			var resp = Get("https://hub.splitscreen.me/api/v1/" + "handler/" + id);

			if (resp == null)
			{
				return false;
			}
			else if (resp == "{}")
			{
				return false;
			}

			JObject jObject = JsonConvert.DeserializeObject(resp) as JObject;

			if (jObject == null)
			{
				return false;
			}

			JArray array = jObject["Handlers"] as JArray;

			if (array == null)
			{
				return false;
			}
			else if (array.Count != 1)
			{
				return false;
			}

			newVersion = int.TryParse(array[0]["currentVersion"].ToString(), out int _v) ? _v : -1;

			return newVersion > Handler.Version;
		}

		public string Get(string uri)
		{
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
			ServicePointManager.DefaultConnectionLimit = 9999;


			try
			{
				//X509Certificate2Collection certificates = new X509Certificate2Collection();
				//certificates.Import(certName, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
				
				HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
				//request.ClientCertificates.Add(X509Certificate.CreateFromCertFile(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "hub_cert.cer")));
				// request.Timeout = 10000;
				//request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

				using (HttpWebResponse response = (HttpWebResponse) request.GetResponse())
				using (Stream stream = response.GetResponseStream())
				using (StreamReader reader = new StreamReader(stream))
				{
					return reader.ReadToEnd();
				}
			}
			catch (Exception ex)
			{
				//MessageBox.Show(string.Format("{0}: {1}", ex.ToString(), ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return null;
			}
		}
	}
}
