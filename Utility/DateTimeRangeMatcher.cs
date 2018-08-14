using System;

namespace Nursery.Utility {
	class TimeRange {
		int Start;
		int End;

		public TimeRange(int start, int end) {
			this.Start = start;
			this.End = end;
		}

		public bool Contains(DateTime dt) {
			return Contains(dt.Hour, dt.Minute);
		}

		public bool Contains(int hour, int minute) {
			return Contains(hour * 100 + minute);
		}

		public bool Contains(int hourminute) {
			return this.Start <= hourminute && hourminute <= this.End;
		}
	}

	public class DateTimeRangeMatch {
		public bool Success { get; }
		public DateTime BaseDate { get; }

		public DateTimeRangeMatch(bool success, DateTime basedate) {
			this.Success = success;
			this.BaseDate = basedate;
		}

		public override string ToString() {
			if (this.Success) {
				return "Success " + this.BaseDate.ToShortDateString();
			} else {
				return "Fail";
			}
		}
	}

	public class DateTimeRangeMatcher {
		public bool Valid { get; private set; } = false;
		private DateTimeMatcher BaseDateMatcher;
		private TimeRange PreviousRange = null;
		private TimeRange BaseRange = null;
		private TimeRange NextRange = null;
		private const int START_OF_DAY = 0;
		private const int END_OF_DAY = 2359;
		private const string BASEDATE_STR = "****.**.**-**-**:**";

		public DateTimeRangeMatcher(int StartTime, int EndTime, string BaseDate = "", bool UsePreviousDay = false) {
			if (StartTime < 0 || EndTime < 0 || StartTime >= 2400 || EndTime >= 2400 || StartTime == EndTime) {
				return;
			}
			// base date
			if (BaseDate == null || BaseDate.Length == 0) {
				this.BaseDateMatcher = null;
			} else {
				if (BaseDate.Length < 10 || BaseDate.Length > BASEDATE_STR.Length) { return; }
				if (BaseDate.Length < BASEDATE_STR.Length) {
					BaseDate = BaseDate + BASEDATE_STR.Substring(BaseDate.Length, BASEDATE_STR.Length - BaseDate.Length);
				}
				this.BaseDateMatcher = new DateTimeMatcher(BaseDate);
				if (!this.BaseDateMatcher.Valid) { return; }
			}
			// set ranges
			if (StartTime <= EndTime) {
				this.BaseRange = new TimeRange(StartTime, EndTime);
			} else {
				if (UsePreviousDay) {
					this.PreviousRange = new TimeRange(StartTime, END_OF_DAY);
					this.BaseRange = new TimeRange(START_OF_DAY, EndTime);
				} else {
					this.BaseRange = new TimeRange(StartTime, END_OF_DAY);
					this.NextRange = new TimeRange(START_OF_DAY, EndTime);
				}
			}
			// set valid
			this.Valid = true;
		}

		private bool IsMatchToBaseDate(DateTime dt) {
			if (this.BaseDateMatcher == null) { return true; }
			this.BaseDateMatcher.ResetPrevious();
			return this.BaseDateMatcher.IsMatch(dt);
		}

		public DateTimeRangeMatch Match(DateTime dt) {
			if (IsMatchToBaseDate(dt) && this.BaseRange.Contains(dt)) {
				return new DateTimeRangeMatch(true, dt.Date);
			}
			if (this.PreviousRange != null) {
				var tomorrow = dt.AddDays(1);
				if (IsMatchToBaseDate(tomorrow) && this.PreviousRange.Contains(dt)) {
					return new DateTimeRangeMatch(true, tomorrow.Date);
				}
			}
			if (this.NextRange != null) {
				var yesterday = dt.AddDays(-1);
				if (IsMatchToBaseDate(yesterday) && this.NextRange.Contains(dt)) {
					return new DateTimeRangeMatch(true, yesterday.Date);
				}
			}
			return new DateTimeRangeMatch(false, DateTime.MinValue);
		}
	}
}
