using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Windows;
using System.Security.Policy;
using System.Runtime.InteropServices;

namespace BrightnessManagerWin
{

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct POINT
	{
		public int x;
		public int y;

		public POINT(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public class Configurator
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct PHYSICAL_MONITOR
		{
			public IntPtr hPhysicalMonitor;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szPhysicalMonitorDescription;
		}

		[DllImport("user32.dll")]
		private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

		[DllImport("dxva2.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

		[DllImport("dxva2.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, PHYSICAL_MONITOR[] pPhysicalMonitorArray); 

		[DllImport("dxva2.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

		[DllImport("dxva2.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetMonitorBrightness(IntPtr hMonitor, int dwNewBrightness);

		public int NumMonitors { get; set; } = 1;
		public int FadeDuration { get; set; } = 60;
		public int UpdateInterval { get; set; } = 10;
		public bool OpenInTray { get; set; } = false;

		private int monIndex = 0;
		public int MonIndex
		{
			get => monIndex + 1;
			set
			{
				if (value > 0 && value <= NumMonitors)
				{
					monIndex = value - 1;
				}
			}
		}

		private Thread brightnessThread;

		public MonitorConfig MonCurrent => monitorConfigsTmp[monIndex];
		public MonitorConfig MonCurrentImmediate => monitorConfigs[monIndex];
		public MonitorMiscInfo MiscCurrent => monitorMisc[monIndex];

		private List<MonitorConfig> monitorConfigs = new List<MonitorConfig>();
		private List<MonitorConfig> monitorConfigsTmp = new List<MonitorConfig>();
		private List<MonitorMiscInfo> monitorMisc = new List<MonitorMiscInfo>();
		public List<MonitorMiscInfo> MonitorMisc { get => monitorMisc; }
		public List<POINT> MonitorPoints
		{
			get
			{
				List<POINT> points = new List<POINT>();
				for (int i = 0; i < NumMonitors; i++)
				{
					points.Add(new POINT(monitorMisc[i].Pos.x, monitorMisc[i].Pos.y)); 
				}
				return points;
			}
		}

		private string cfgPath;

		public bool ShouldClose { get; set; } = false;

		public static Configurator Load(string path)
		{
			if (Directory.Exists(Path.GetDirectoryName(path)))
				if (File.Exists(path))
					return new Configurator(path);
			return null;
		}

		public Configurator(string path)
		{
			cfgPath = path;

			LoadConfig();

			brightnessThread = new Thread(BrightnessLoop);
			brightnessThread.Start();
		}

		public void SetBrightness(int i, int brightness)
		{
			monitorMisc[i].CurrentBrightness = brightness;

			IntPtr hmon = MonitorFromPoint(new POINT { x = monitorMisc[i].Pos.x, y = monitorMisc[i].Pos.y }, 0);
			PHYSICAL_MONITOR[] hpmon = new PHYSICAL_MONITOR[1];
			GetPhysicalMonitorsFromHMONITOR(hmon, 1, hpmon);
			SetMonitorBrightness(hpmon[0].hPhysicalMonitor, brightness);
			DestroyPhysicalMonitor(hpmon[0].hPhysicalMonitor);
		}

		public void UpdateTimes()
		{
			for (int i = 0; i < NumMonitors; i++)
			{
				CalculateBrightness(i);
			}
		}

		public int CalculateBrightness(int i)
		{
			long now = (long)DateTime.Now.TimeOfDay.TotalSeconds;
			long sunset = (long)monitorConfigs[i].Sunset.TotalSeconds - now;
			long sunrise = (long)monitorConfigs[i].Sunrise.TotalSeconds - now;
			int night = monitorConfigs[i].NightBrightness;
			int day = monitorConfigs[i].DayBrightness;
			int fade = FadeDuration * 60;

			if (sunset < 0)
				sunset += 24 * 60 * 60;

			if (sunrise < 0)
				sunrise += 24 * 60 * 60;

			monitorMisc[i].TimeToSunset = sunset;
			monitorMisc[i].TimeToSunrise = sunrise;

			if (sunset <= sunrise)
				if (sunset <= fade)
					return (int)((sunset / (double)fade) * (day - night) + night);
				else
					return day;
			else
				if (sunrise <= fade)
					return (int)(((fade - sunrise) / (double)fade) * (day - night) + night);
				else
					return night;
		}

		private void BrightnessLoop()
		{
			while (!ShouldClose)
			{
				for (int i = 0; i < NumMonitors; i++)
				{
					if (monitorConfigs[i].Enabled)
						SetBrightness(i, CalculateBrightness(i));
				}

				for (int i = 0; i < UpdateInterval * 2; i++)
				{
					Thread.Sleep(500);
					if (ShouldClose)
						return;
				}
			}
		}

		public void Apply()
		{
			for (int i = 0; i < NumMonitors; i++)
				monitorConfigs[i] = (MonitorConfig)monitorConfigsTmp[i].Clone();
			SaveConfig();
			for (int i = 0; i < NumMonitors; i++)
			{
				if (monitorConfigs[i].Enabled)
					SetBrightness(i, CalculateBrightness(i));
			}
		}

		public void Revert()
		{
			for (int i = 0; i < NumMonitors; i++)
				monitorConfigsTmp[i] = (MonitorConfig)monitorConfigs[i].Clone();
		}

		public void CopyToAll()
		{
			for (int i = 0; i < NumMonitors; i++)
			{
				if (i == monIndex) continue;
				monitorConfigsTmp[i] = (MonitorConfig)monitorConfigsTmp[monIndex].Clone();
			}
		}

		public void LoadConfig()
		{
			string str = "";
			try
			{
				using (StreamReader s = new StreamReader(cfgPath))
					str = s.ReadToEnd();
			}
			catch { }

			string[] lines = str.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

			try
			{
				int mon = 0;
				foreach(string line in lines)
				{
					string name = line.Split('=')[0].Trim();
					string value = line.Split('=')[1].Trim();
					
					switch (name)
					{
						case "numMonitors":
							NumMonitors = int.Parse(value);
							monitorConfigs.Clear();
							monitorConfigsTmp.Clear();
							monitorMisc.Clear();

							for (int i = 0; i < NumMonitors; i++)
							{
								monitorConfigs.Add(new MonitorConfig());
								monitorConfigsTmp.Add(new MonitorConfig());
								monitorMisc.Add(new MonitorMiscInfo());
							}

							break;

						case "fadeDuration":
							FadeDuration = int.Parse(value);
							break;

						case "updateInterval":
							UpdateInterval = int.Parse(value);
							break;

						case "openInTray":
							OpenInTray = bool.Parse(value);
							break;

						case "mon":
							mon = int.Parse(value);
							break;

						case "pos":
							monitorMisc[mon].PosStr = value;
							break;

						case "sunset":
							monitorConfigs[mon].SunsetStr = value;
							monitorConfigsTmp[mon].SunsetStr = value;
							break;

						case "sunrise":
							monitorConfigs[mon].SunriseStr = value;
							monitorConfigsTmp[mon].SunriseStr = value;
							break;

						case "nightBrightness":
							monitorConfigs[mon].NightBrightness = int.Parse(value);
							monitorConfigsTmp[mon].NightBrightness = int.Parse(value);
							break;

						case "dayBrightness":
							monitorConfigs[mon].DayBrightness = int.Parse(value);
							monitorConfigsTmp[mon].DayBrightness = int.Parse(value);
							break;

						case "enabled":
							monitorConfigs[mon].Enabled = bool.Parse(value);
							monitorConfigsTmp[mon].Enabled = bool.Parse(value);
							break;

						default:
							throw new Exception("Unexpected value encountered in config file");
					}
				}
			}
			catch
			{
				MessageBox.Show($"Could not load config file at \"{cfgPath}\"", "Error loading settings", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			SaveConfig();
		}

		public void SaveConfig()
		{
			if (!Directory.Exists(Path.GetDirectoryName(cfgPath)))
				Directory.CreateDirectory(Path.GetDirectoryName(cfgPath));

			try
			{
				using (StreamWriter s = new StreamWriter(cfgPath, false))
				{
					s.WriteLine($"numMonitors={NumMonitors}");
					s.WriteLine($"fadeDuration={FadeDuration}");
					s.WriteLine($"updateInterval={UpdateInterval}");
					s.WriteLine($"openInTray={OpenInTray}");
					for (int i = 0; i < NumMonitors; i++)
					{
						s.WriteLine();
						s.WriteLine($"mon={i}");
						s.WriteLine($"pos={monitorMisc[i].PosStr}");
						s.WriteLine($"sunset={monitorConfigs[i].SunsetStr}");
						s.WriteLine($"sunrise={monitorConfigs[i].SunriseStr}");
						s.WriteLine($"nightBrightness={monitorConfigs[i].NightBrightness}");
						s.WriteLine($"dayBrightness={monitorConfigs[i].DayBrightness}");
						s.WriteLine($"enabled={monitorConfigs[i].Enabled}");
					}
				}
			}
			catch
			{
				MessageBox.Show($"Could not modify config file at \"{cfgPath}\"", "Error saving settings", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		public void AddMonitors(int num)
		{
			for (int i = 0; i < num; i++)
			{
				monitorConfigs.Add(new MonitorConfig());
				monitorConfigsTmp.Add(new MonitorConfig());
				monitorMisc.Add(new MonitorMiscInfo());
			}

			NumMonitors += num;

			SaveConfig();
		}

		public void RemoveMonitors(int num)
		{
			for (int i = 0; i < num; i++)
			{
				monitorConfigs.RemoveAt(monitorConfigs.Count - 1);
				monitorConfigsTmp.RemoveAt(monitorConfigsTmp.Count - 1);
				monitorMisc.RemoveAt(monitorMisc.Count - 1);
			}

			NumMonitors -= num;

			SaveConfig();
		}
	}
}
