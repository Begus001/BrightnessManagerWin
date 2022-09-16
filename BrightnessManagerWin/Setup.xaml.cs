using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using Path = System.IO.Path;

namespace BrightnessManagerWin
{
	public partial class Setup : Window
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(out POINT pt);

		private bool setPosMode = false;

		private int numMonitors = 1;
		public int NumMonitors
		{
			get => numMonitors;
			set
			{
				if (value > 0 && value < 100)
				{
					numMonitors = value;
				}
			}
		}

		private int currentMon = 1;
		private readonly List<POINT> points = new List<POINT>();

		private string cfgPath;

		public bool done = false;

		public Setup(string path)
		{
			cfgPath = path;
			InitializeComponent();
		}

		private void btNumMonitorsUp_Click(object sender, RoutedEventArgs e)
		{
			NumMonitors += 1;
			tbNumMonitors.Text = NumMonitors.ToString();
		}

		private void btNumMonitorsDown_Click(object sender, RoutedEventArgs e)
		{
			NumMonitors -= 1;
			tbNumMonitors.Text = NumMonitors.ToString();
		}

		private void tbNumMonitors_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = NumMonitors;
			try
			{
				NumMonitors = int.Parse(tbNumMonitors.Text);
				tbNumMonitors.Text = NumMonitors.ToString();
				if (before == NumMonitors)
					tbNumMonitors.SelectAll();
			}
			catch
			{
				if (tbNumMonitors.Text == "") return;
				tbNumMonitors.Text = NumMonitors.ToString();
				tbNumMonitors.SelectAll();
			}
		}

		private void btSetPos_Click(object sender, RoutedEventArgs e)
		{
			points.Clear();

			setPosMode = true;
			grMain.IsEnabled = false;
			btSetPos.Visibility = Visibility.Hidden;

			currentMon = 1;
			lbInstructions.Visibility = Visibility.Visible;
			lbInstructions.Text = $"Move the cursor to Monitor {currentMon} and press any key...\n";
			lbInstructions.Text += $"Escape to cancel";
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (setPosMode && e.Key == Key.Escape)
			{
				setPosMode = false;
				grMain.IsEnabled = true;
				btSetPos.Visibility = Visibility.Visible;
				lbInstructions.Visibility = Visibility.Hidden;
				return;
			}
			else if (setPosMode)
			{
				GetCursorPos(out POINT p);

				points.Add(p);

				currentMon += 1;
				if (currentMon > NumMonitors)
					SaveSettings();

				lbInstructions.Text = $"Move the cursor to Monitor {currentMon} and press any key...\n";
				lbInstructions.Text += $"Escape to cancel";
			}
		}

		private void SaveSettings()
		{
			if (!Directory.Exists(Path.GetDirectoryName(cfgPath)))
				Directory.CreateDirectory(Path.GetDirectoryName(cfgPath));

			if (File.Exists(cfgPath))
				File.Move(cfgPath, Path.GetDirectoryName(cfgPath) + @"\cfg.bak");

			try
			{
				using (StreamWriter s = new StreamWriter(cfgPath, false))
				{
					s.WriteLine($"numMonitors={numMonitors}");
					s.WriteLine($"fadeDuration=60");
					s.WriteLine($"updateInterval=10");
					for (int i = 0; i < numMonitors; i++)
					{
						s.WriteLine();
						s.WriteLine($"mon={i}");
						s.WriteLine($"pos={points[i].x}/{points[i].y}");
						s.WriteLine($"sunset=20:00");
						s.WriteLine($"sunrise=06:00");
						s.WriteLine($"nightBrightness=0");
						s.WriteLine($"dayBrightness=100");
					}

					done = true;
				}
			}
			catch
			{
				MessageBox.Show($"Could not create config file at \"{cfgPath}\"", "Error saving settings", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			Close();
		}
	}
}
