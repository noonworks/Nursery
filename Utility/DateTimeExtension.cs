using System;

namespace Nursery.Utility {
	public static class DateTimeExtension {
		public static int GetNumberOfDayOfWeek(this DateTime dt) {
			return (dt.Day - 1) / 7 + 1;
		}
		
		public static bool IsLastDayInMonth(this DateTime dt) {
			return dt.Day == DateTime.DaysInMonth(dt.Year, dt.Month);
		}
		
		public static bool IsLastDayOfWeekInMonth(this DateTime dt) {
			return dt.Day > DateTime.DaysInMonth(dt.Year, dt.Month) - 7;
		}

		public static DateTime TruncateSeconds(this DateTime dt) {
			return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0, 0);
		}

		public static DateTime TruncateMilliseconds(this DateTime dt) {
			return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, 0);
		}
	}
}
