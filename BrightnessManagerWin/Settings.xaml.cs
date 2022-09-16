using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BrightnessManagerWin
{
	public partial class Settings : Window
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(out POINT pt);

		private int numMonitors;
		public int NumMonitors 
		{
			get => numMonitors;
			set
			{
				if (value > 0 && value < 100)
					numMonitors = value;
			}
		}
		private int fadeDuration;
		public int FadeDuration
		{
			get => fadeDuration;
			set
			{
				if (value > 0 && value <= 300)
					fadeDuration = value;
			}
		}
		private int updateInterval;
		public int UpdateInterval
		{
			get => updateInterval;
			set
			{
				if (value > 0 && value <= 300)
					updateInterval = value;
			}
		}
		public List<POINT> Points { get; set; }

		private int pointIndex = 0;
		public int PointIndex 
		{
			get => pointIndex + 1; 
			set
			{
				if (value > 0 && value <= NumMonitors)
					pointIndex = value - 1;
			}
		}

		private bool setPosMode = false;

		public bool Cancelled { get; private set; } = true;

		public Settings()
		{
			InitializeComponent();
		}

		private void AdjustPointsList()
		{
			if (NumMonitors > Points.Count)
				for (int i = Points.Count; i < NumMonitors; i++)
					Points.Add(new POINT(0, 0));
		}

		public void FillValues()
		{
			if (PointIndex > NumMonitors)
				PointIndex = NumMonitors;

			tbFadeDuration.Text = FadeDuration.ToString();
			tbUpdateInterval.Text = UpdateInterval.ToString();
			tbNumMonitors.Text = NumMonitors.ToString();
			tbMonitorPos.Text = PointIndex.ToString();

			AdjustPointsList();

			btSetMonitorPos.Content = $"Set ({Points[pointIndex].x}/{Points[pointIndex].y})";
		}

		private void btOK_Click(object sender, RoutedEventArgs e)
		{
			Cancelled = false;
			Close();
		}

		private void btCancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void tbFadeDuration_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = FadeDuration;
			try
			{
				int val = int.Parse(tbFadeDuration.Text);
				FadeDuration = val;
				if (FadeDuration == before)
				{
					FillValues();
					tbFadeDuration.CaretIndex = tbFadeDuration.Text.Length;
				}
			}
			catch
			{
				if (tbFadeDuration.Text == "") return;
				FillValues();
				tbFadeDuration.CaretIndex = tbFadeDuration.Text.Length;
			}
		}

		private void TextBoxLostFocus(object sender, RoutedEventArgs e)
		{
			FillValues();
		}

		private void tbUpdateInterval_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = UpdateInterval;
			try
			{
				int val = int.Parse(tbUpdateInterval.Text);
				UpdateInterval = val;
				if (UpdateInterval == before)
				{
					FillValues();
					tbUpdateInterval.CaretIndex = tbUpdateInterval.Text.Length;
				}
			}
			catch
			{
				if (tbUpdateInterval.Text == "") return;
				FillValues();
				tbUpdateInterval.CaretIndex = tbUpdateInterval.Text.Length;
			}
		}

		private void btNumMonitorsDown_Click(object sender, RoutedEventArgs e)
		{
			NumMonitors -= 1;
			FillValues();
		}

		private void btNumMonitorsUp_Click(object sender, RoutedEventArgs e)
		{
			NumMonitors += 1;
			FillValues();
		}

		private void tbNumMonitors_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = NumMonitors;
			try
			{
				int val = int.Parse(tbNumMonitors.Text);
				NumMonitors = val;
				if (NumMonitors == before)
				{
					FillValues();
					tbNumMonitors.CaretIndex = tbNumMonitors.Text.Length;
				}
			}
			catch
			{
				FillValues();
				tbNumMonitors.CaretIndex = tbNumMonitors.Text.Length;
			}
		}

		private void btMonitorPosDown_Click(object sender, RoutedEventArgs e)
		{
			PointIndex -= 1;
			FillValues();
		}

		private void btMonitorPosUp_Click(object sender, RoutedEventArgs e)
		{
			PointIndex += 1;
			FillValues();
		}

		private void tbMonitorPos_TextChanged(object sender, TextChangedEventArgs e)
		{
			int before = PointIndex;
			try
			{
				int val = int.Parse(tbMonitorPos.Text);
				PointIndex = val;
				if (PointIndex == before)
				{
					FillValues();
					tbMonitorPos.CaretIndex = tbMonitorPos.Text.Length;
				}
			}
			catch
			{
				if (tbMonitorPos.Text == "") return;
				FillValues();
				tbMonitorPos.CaretIndex = tbMonitorPos.Text.Length;
			}
		}

		private void btSetMonitorPos_Click(object sender, RoutedEventArgs e)
		{
			setPosMode = true;
			grMain.IsEnabled = false;
			btSetMonitorPos.Visibility = Visibility.Hidden;
			lbInstructions.Visibility = Visibility.Visible;
			lbInstructions.Text = $"Move the cursor to Monitor {PointIndex} and\npress any key.   Escape to cancel";
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (setPosMode && e.Key == Key.Escape)
			{
				grMain.IsEnabled = true;
				btSetMonitorPos.Visibility = Visibility.Visible;
				lbInstructions.Visibility = Visibility.Hidden;
			}
			else if (setPosMode)
			{
				GetCursorPos(out POINT p);
				Points[pointIndex] = new POINT(p.x, p.y);

				setPosMode = false;
				grMain.IsEnabled = true;
				btSetMonitorPos.Visibility = Visibility.Visible;
				lbInstructions.Visibility = Visibility.Hidden;

				FillValues();
			}
		}
	}
}
