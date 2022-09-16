using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;

namespace AutoUpdater
{
	class Program
	{
		private static readonly string tmp = Environment.GetEnvironmentVariable("tmp") + @"\BrightnessManagerWin.exe";
		private static readonly string dir = Environment.GetEnvironmentVariable("tmp");
		static void Main(string[] args)
		{
			string exepath, exedir, selfpath;
			if (args.Length < 1)
			{
				exepath = @"C:\Program Files\BrightnessManager\BrightnessManagerWin.exe";
			}
			else
			{
				exepath = args[0];
			}

			exedir = Path.GetDirectoryName(exepath);

			if (!Directory.Exists(exedir))
				Directory.CreateDirectory(exedir);

			selfpath = Path.Combine(Environment.GetEnvironmentVariable("appdata"), @"BrightnessManager\AutoUpdaterTmp.exe");

			if (!Directory.Exists(Path.GetDirectoryName(selfpath)))
				Directory.CreateDirectory(Path.GetDirectoryName(selfpath));

			Thread.Sleep(200);

			Directory.CreateDirectory(dir);
			WebRequest req = WebRequest.CreateHttp("http://begus.ddns.net/bmupdate/BrightnessManagerWin.exe");
			WebResponse resp = req.GetResponse();
			Stream r = resp.GetResponseStream();
			FileStream w = File.Open(tmp, FileMode.OpenOrCreate);

			for (long i = 0; i < resp.ContentLength; i++)
				w.WriteByte((byte)r.ReadByte());

			w.Close();

			if (File.Exists(selfpath))
				File.Delete(selfpath);

			req = WebRequest.CreateHttp("http://begus.ddns.net/bmupdate/AutoUpdater.exe");
			resp = req.GetResponse();
			r = resp.GetResponseStream();
			w = File.Open(selfpath, FileMode.OpenOrCreate);

			for (long i = 0; i < resp.ContentLength; i++)
				w.WriteByte((byte)r.ReadByte());

			if (File.Exists(exepath))
				File.Delete(exepath);

			Thread.Sleep(500);

			try
			{
				File.Move(tmp, exepath);
			}
			catch (Exception e)
			{ 
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}

			r.Close();
			resp.Close();

			Process.Start(exepath);
		}
	}
}
