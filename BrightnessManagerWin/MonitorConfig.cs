using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace BrightnessManagerWin
{
	public class MonitorConfig : ICloneable
	{
		public TimeSpan Sunset = new TimeSpan(20, 0, 0);
		public TimeSpan Sunrise = new TimeSpan(6, 0, 0);

		public string SunsetStr
		{
			get
			{
				return Sunset.ToString("hh':'mm");
			}
			set
			{
				try
				{
					if (value.Contains(":"))
						Sunset = TimeSpan.Parse(value);
				}
				catch { }
			}
		}

		public string SunriseStr
		{
			get
			{
				return Sunrise.ToString("hh':'mm");
			}
			set
			{
				try
				{
					if (value.Contains(":"))
						Sunrise = TimeSpan.Parse(value);
				}
				catch { }
			}
		}

		private int dayBrght = 100;
		public int DayBrightness
		{
			get => dayBrght;
			set
			{
				if (value >= 0 && value <= 100)
					dayBrght = value;
			}
		}
		private int nightBrght = 0;
		public int NightBrightness
		{
			get => nightBrght;
			set
			{
				if (value >= 0 && value <= 100)
					nightBrght = value;
			}
		}

		public object Clone()
		{
			return new MonitorConfig() { Sunset = Sunset, Sunrise = Sunrise, dayBrght= dayBrght, nightBrght = nightBrght };
		}
	}
}
