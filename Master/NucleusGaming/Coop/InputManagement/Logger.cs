using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Nucleus.Gaming.Coop.InputManagement
{
	static class Logger
	{
		public static void WriteLine(object message)
		{
			WriteLine(message.ToString());
		}

		public static void WriteLine(string message)
		{
#if DEBUG
			try
			{
				using (var writer = new StreamWriter("debug-log.txt", true))
				{
					writer.WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]INPUTMANAGEMENT: {message}");
					writer.Close();
				}
			}
			catch { }
#endif
		}
	}
}
