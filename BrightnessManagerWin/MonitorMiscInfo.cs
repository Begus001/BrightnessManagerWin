using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightnessManagerWin
{
	public class MonitorMiscInfo
	{
		public POINT Pos { get; set; } = new POINT(0, 0);
		public string PosStr
		{
			get { return $"{Pos.x}/{Pos.y}"; }
			set
			{
				if (value.Contains('/'))
				{
					string[] split = value.Split('/');
					Pos = new POINT(int.Parse(split[0]), int.Parse(split[1]));
				}
			}
		}

		public long TimeToSunset { get; set; } = 0;
		public string TimeToSunsetStr
		{
			get
			{
				return $"{TimeToSunset / 60 / 60:00}:{TimeToSunset / 60 % 60:00}:{TimeToSunset % 60:00}";
			}
		}

		public long TimeToSunrise { get; set; } = 0;
		public string TimeToSunriseStr
		{
			get
			{
				return $"{TimeToSunrise / 60 / 60:00}:{TimeToSunrise / 60 % 60:00}:{TimeToSunrise % 60:00}";
			}
		}

		public int CurrentBrightness = 100;
	}
}
