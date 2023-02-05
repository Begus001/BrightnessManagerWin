using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Application = System.Windows.Application;
using MenuItem = System.Windows.Forms.MenuItem;
using ContextMenu = System.Windows.Forms.ContextMenu;
using System.Drawing;
using System.Threading;
using MessageBox = System.Windows.MessageBox;
using System.IO;
using System.Reflection;
using System.Net;
using Path = System.IO.Path;
using Brushes = System.Windows.Media.Brushes;
using Control = System.Windows.Controls.Control;

namespace BrightnessManagerWin
{
	public partial class MainWindow : Window
	{
		private readonly string CFG_PATH = @"C:\Users\" + Environment.UserName + @"\AppData\Roaming\BrightnessManager\cfg.txt";

		private readonly Configurator cfg;

		private Timer timer = new Timer(1000);

		private NotifyIcon trayIcon = new NotifyIcon();

		public Version version { get; set; } = new Version("1.3.0");

		public MainWindow()
		{
			string autoUpdaterTmp = Path.Combine(Path.GetDirectoryName(CFG_PATH), "AutoUpdaterTmp.exe");
			string autoUpdater = Path.Combine(Path.GetDirectoryName(CFG_PATH), "AutoUpdater.exe");
			if (File.Exists(autoUpdaterTmp))
			{
				if (File.Exists(autoUpdater))
					File.Delete(autoUpdater);
				File.Move(autoUpdaterTmp, autoUpdater);
				File.Delete(autoUpdaterTmp);
			}

			cfg = Configurator.Load(CFG_PATH);
			if (cfg == null)
			{
				Setup setup = new Setup(CFG_PATH);
				setup.ShowDialog();
				if (!setup.done)
					Environment.Exit(0);
				cfg = Configurator.Load(CFG_PATH);
			}
			InitializeComponent();
			tbMonitor.Text = "1";

			SetupTrayIcon();
			CheckUpdate(true);

			if (cfg.OpenInTray)
				ToTray();

			timer.Elapsed += timerElapsed;
			timer.Start();
		}

		private void SetupTrayIcon()
		{
			trayIcon.Icon = SystemIcons.Information;
			trayIcon.Text = "BrightnessManager";
			trayIcon.DoubleClick += TrayIcon_DoubleClick;

			MenuItem open = new MenuItem("Open", (sender, e) => FromTray());
			MenuItem exit = new MenuItem("Exit", (sender, e) => btExit_Click(sender, null));
			trayIcon.ContextMenu = new ContextMenu(new MenuItem[] { open, new MenuItem("-"), exit });
		}

		private void timerElapsed(object sender, ElapsedEventArgs e)
		{
			cfg.UpdateTimes();

			Dispatcher.Invoke(() =>
			{
				lbTimeToSunset.Content = "Time to sunset: " + cfg.MiscCurrent.TimeToSunsetStr;
				lbTimeToSunrise.Content = "Time to sunrise: " + cfg.MiscCurrent.TimeToSunriseStr;
				lbBrightness.Content = "Current brightness: " + cfg.MiscCurrent.CurrentBrightness.ToString();
			});
		}

		private void FillMonitorConfig()
		{
			tbMonitor.Text = cfg.MonIndex.ToString();
			tbSunset.Text = cfg.MonCurrent.SunsetStr;
			tbSunrise.Text = cfg.MonCurrent.SunriseStr;
			tbDayBrght.Text = cfg.MonCurrent.DayBrightness.ToString();
			tbNightBrght.Text = cfg.MonCurrent.NightBrightness.ToString();
			updateEnabledButton();
			tbManual.Text = cfg.ManualBrightness.ToString();

			cfg.UpdateTimes();

			lbTimeToSunset.Content = "Time to sunset: " + cfg.MiscCurrent.TimeToSunsetStr;
			lbTimeToSunrise.Content = "Time to sunrise: " + cfg.MiscCurrent.TimeToSunriseStr;
			lbBrightness.Content = "Current brightness: " + cfg.MiscCurrent.CurrentBrightness.ToString();
		}

		private void btMonitorNext_Click(object sender, RoutedEventArgs e)
		{
			tbMonitor.Text = (int.Parse(tbMonitor.Text) + 1).ToString();
		}

		private void btMonitorPrev_Click(object sender, RoutedEventArgs e)
		{
			tbMonitor.Text = (int.Parse(tbMonitor.Text) - 1).ToString();
		}

		private void tbMonitor_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = cfg.MonIndex;
			try
			{
				cfg.MonIndex = int.Parse(tbMonitor.Text);
				tbMonitor.Text = cfg.MonIndex.ToString();
				if (before == cfg.MonIndex)
					tbMonitor.CaretIndex = tbMonitor.Text.Length;
			}
			catch
			{
				if (tbMonitor.Text == "") return;
				tbMonitor.Text = cfg.MonIndex.ToString();
				tbMonitor.CaretIndex = tbMonitor.Text.Length;
			}

			FillMonitorConfig();
		}

		private void TextboxLostFocus(object sender, RoutedEventArgs e)
		{
			FillMonitorConfig();
		}

		private void btDayBrghtUp_Click(object sender, RoutedEventArgs e)
		{
			tbDayBrght.Text = (int.Parse(tbDayBrght.Text) + 1).ToString();
		}

		private void btDayBrghtDown_Click(object sender, RoutedEventArgs e)
		{
			tbDayBrght.Text = (int.Parse(tbDayBrght.Text) - 1).ToString();
		}

		private void tbDayBrght_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = cfg.MonCurrent.DayBrightness;
			try
			{
				cfg.MonCurrent.DayBrightness = int.Parse(tbDayBrght.Text);
				tbDayBrght.Text = cfg.MonCurrent.DayBrightness.ToString();
				if (before == cfg.MonCurrent.DayBrightness)
					tbDayBrght.CaretIndex = tbDayBrght.Text.Length;
			}
			catch
			{
				if (tbDayBrght.Text == "") return;
				tbDayBrght.Text = cfg.MonCurrent.DayBrightness.ToString();
				tbDayBrght.CaretIndex = tbDayBrght.Text.Length;
			}
		}

		private void btNightBrghtUp_Click(object sender, RoutedEventArgs e)
		{
			tbNightBrght.Text = (int.Parse(tbNightBrght.Text) + 1).ToString();
		}

		private void btNightBrghtDown_Click(object sender, RoutedEventArgs e)
		{
			tbNightBrght.Text = (int.Parse(tbNightBrght.Text) - 1).ToString();
		}

		private void tbNightBrght_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = cfg.MonCurrent.NightBrightness;
			try
			{
				cfg.MonCurrent.NightBrightness = int.Parse(tbNightBrght.Text);
				tbNightBrght.Text = cfg.MonCurrent.NightBrightness.ToString();
				if (before == cfg.MonCurrent.NightBrightness)
					tbNightBrght.CaretIndex = tbNightBrght.Text.Length;
			}
			catch
			{
				if (tbNightBrght.Text == "") return;
				tbNightBrght.Text = cfg.MonCurrent.NightBrightness.ToString();
				tbNightBrght.CaretIndex = tbNightBrght.Text.Length;
			}
		}

		private void btSunsetUp_Click(object sender, RoutedEventArgs e)
		{
			cfg.MonCurrent.Sunset = cfg.MonCurrent.Sunset.Add(new TimeSpan(0, 15, 0));
			if (cfg.MonCurrent.Sunset.Days > 0)
				cfg.MonCurrent.Sunset = cfg.MonCurrent.Sunset.Subtract(new TimeSpan(24, 0, 0));
			FillMonitorConfig();
		}

		private void btSunsetDown_Click(object sender, RoutedEventArgs e)
		{
			if (cfg.MonCurrent.Sunset.TotalSeconds == 0)
				cfg.MonCurrent.Sunset = new TimeSpan(24, 0, 0);
			cfg.MonCurrent.Sunset = cfg.MonCurrent.Sunset.Subtract(new TimeSpan(0, 15, 0));
			FillMonitorConfig();
		}

		private void tbSunset_LostFocus(object sender, RoutedEventArgs e)
		{
			cfg.MonCurrent.SunsetStr = tbSunset.Text;
			FillMonitorConfig();
		}

		private void tbSunset_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Return)
			{
				tbSunset_LostFocus(sender, null);
				tbSunset.CaretIndex = tbSunset.Text.Length;
			}
		}

		private void btSunriseUp_Click(object sender, RoutedEventArgs e)
		{
			cfg.MonCurrent.Sunrise = cfg.MonCurrent.Sunrise.Add(new TimeSpan(0, 15, 0));
			if (cfg.MonCurrent.Sunrise.Days > 0)
				cfg.MonCurrent.Sunrise = cfg.MonCurrent.Sunrise.Subtract(new TimeSpan(24, 0, 0));
			FillMonitorConfig();
		}

		private void btSunriseDown_Click(object sender, RoutedEventArgs e)
		{
			if (cfg.MonCurrent.Sunrise.TotalSeconds == 0)
				cfg.MonCurrent.Sunrise = new TimeSpan(24, 0, 0);
			cfg.MonCurrent.Sunrise = cfg.MonCurrent.Sunrise.Subtract(new TimeSpan(0, 15, 0));
			FillMonitorConfig();
		}

		private void tbSunrise_LostFocus(object sender, RoutedEventArgs e)
		{
			cfg.MonCurrent.SunriseStr = tbSunrise.Text;
			FillMonitorConfig();
		}

		private void tbSunrise_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.Key == Key.Return)
			{
				tbSunrise_LostFocus(sender, null);
				tbSunrise.CaretIndex = tbSunrise.Text.Length;
			}
		}

		private void btApply_Click(object sender, RoutedEventArgs e)
		{
			cfg.Apply();
			FillMonitorConfig();
		}

		private void btRevert_Click(object sender, RoutedEventArgs e)
		{
			cfg.Revert();
			FillMonitorConfig();
		}

		private void btCopyToAll_Click(object sender, RoutedEventArgs e)
		{
			cfg.CopyToAll();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			cfg.ShouldClose = true;
		}

		private void ToTray()
		{
			trayIcon.Visible = true;
			Visibility = Visibility.Hidden;
		}

		private void FromTray()
		{
			trayIcon.Visible = false;
			Visibility = Visibility.Visible;
			Activate();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!cfg.ShouldClose)
			{
				e.Cancel = true;
				ToTray();
			}
		}

		private void TrayIcon_DoubleClick(object sender, EventArgs e)
		{
			FromTray();
		}

		private void btExit_Click(object sender, RoutedEventArgs e)
		{
			timer.Stop();
			Thread.Sleep(100);
			cfg.ShouldClose = true;
			trayIcon.Visible = false;
			Application.Current.Shutdown();
		}

		private void btSettings_Click(object sender, RoutedEventArgs e)
		{
			Settings settings = new Settings();
			settings.NumMonitors = cfg.NumMonitors;
			settings.FadeDuration = cfg.FadeDuration;
			settings.UpdateInterval = cfg.UpdateInterval;
			settings.OpenInTray = cfg.OpenInTray;
			settings.Points = cfg.MonitorPoints.ToList();
			settings.FillValues();
			settings.ShowDialog();

			if (!settings.Cancelled)
			{
				cfg.FadeDuration = settings.FadeDuration;
				cfg.UpdateInterval = settings.UpdateInterval;
				cfg.OpenInTray = settings.OpenInTray;
				if (settings.NumMonitors > cfg.NumMonitors)
				{
					cfg.AddMonitors(settings.NumMonitors - cfg.NumMonitors);
				}
				else if (settings.NumMonitors < cfg.NumMonitors)
				{
					cfg.RemoveMonitors(cfg.NumMonitors - settings.NumMonitors);
					tbMonitor.Text = "1";
				}

				for (int i = 0; i < settings.NumMonitors; i++)
				{
					cfg.MonitorMisc[i].Pos = new POINT(settings.Points[i].x, settings.Points[i].y);
				}

				cfg.SaveConfig();
			}
		}

		private void btAbout_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show($"BrightnessManagerWin {version}\nBenjamin Goisser 2022\nhttps://github.com/Begus001", "About");
		}

		private void Update()
		{
			string path = Path.Combine(Environment.GetEnvironmentVariable("appdata"), @"BrightnessManager\AutoUpdater.exe");
			if (!File.Exists(path))
			{
				MessageBox.Show("Could not find AutoUpdater.exe!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			Process p = new Process();
			p.StartInfo.FileName = path;
			p.StartInfo.Arguments = "\"" + Assembly.GetExecutingAssembly().Location + "\"";
			p.StartInfo.Verb = "runas";
			p.Start();

			cfg.ShouldClose = true;
			Thread.Sleep(250);
			Application.Current.Shutdown();
		}

		private void CheckUpdate(bool onStartup)
		{
			WebRequest req = WebRequest.CreateHttp("http://begus.ddns.net/bmupdate/version.txt");
			WebResponse resp = req.GetResponse();
			Version newVersion;
			StreamReader s = new StreamReader(resp.GetResponseStream());

			if (!Version.TryParse(s.ReadToEnd(), out newVersion))
			{
				MessageBox.Show("Couldn't check if new version available", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (version < newVersion)
			{
				MessageBoxResult res = MessageBox.Show(string.Format("New version available! ({0} -> {1})\nDo you want to update?", version.ToString(), newVersion.ToString()), "Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
				if (res == MessageBoxResult.Yes)
				{
					Update();
					return;
				}
			}
			else
			{
				if (!onStartup)
					MessageBox.Show("Already up to date!", "No update", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void btCheckUpdate_Click(object sender, RoutedEventArgs e)
		{
			CheckUpdate(false);
		}

		private void btEnabled_Click(object sender, RoutedEventArgs e)
		{
			cfg.MonCurrent.Enabled = !cfg.MonCurrent.Enabled;
			cfg.MonCurrentImmediate.Enabled = cfg.MonCurrent.Enabled;
			updateEnabledButton();
		}

		private void updateEnabledButton()
		{
			if (cfg.MonCurrent.Enabled)
			{
				btEnabled.Background = Brushes.MediumSeaGreen;
				btEnabled.Content = "Enabled";
				cfg.SetBrightness(cfg.MonIndex - 1, cfg.CalculateBrightness(cfg.MonIndex - 1));
			}
			else
			{
				btEnabled.ClearValue(Control.BackgroundProperty);
				btEnabled.Content = "Enable";
			}

			cfg.SaveConfig();
		}

		private void btManualUp_Click(object sender, RoutedEventArgs e)
		{
			tbManual.Text = (int.Parse(tbManual.Text) +1).ToString();
		}

		private void btManualDown_Click(object sender, RoutedEventArgs e)
		{
			tbManual.Text = (int.Parse(tbManual.Text) - 1).ToString();
		}

		private void tbManual_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = cfg.ManualBrightness;
			try
			{
				cfg.ManualBrightness = int.Parse(tbManual.Text);
				tbManual.Text = cfg.ManualBrightness.ToString();
				if (before == cfg.ManualBrightness)
					tbManual.CaretIndex = tbManual.Text.Length;
			}
			catch
			{
				if (tbManual.Text == "") return;
				tbManual.Text = cfg.ManualBrightness.ToString();
				tbManual.CaretIndex = tbManual.Text.Length;
			}
		}

		private void btManualSet_Click(object sender, RoutedEventArgs e)
		{
			cfg.MonCurrent.Enabled = false;
			cfg.MonCurrentImmediate.Enabled = false;
			cfg.SetBrightness(cfg.MonIndex - 1, cfg.ManualBrightness);
			updateEnabledButton();
		}
	}
}
