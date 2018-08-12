using System;
using System.Text.RegularExpressions;

namespace Nursery.Utility {
	// "yyyy.MM.dd-ND-HH:mm"
	//   N = number of day of week in this month
	//   D = day of week (int)
	public class DateTimeMatcher {
		private static readonly Regex FormatRegex = new Regex(@"^([0-9\*]{4})\.([0-9\*]{2})\.([0-9\*]{2})\-([0-9\*])([0-9\*])\-([0-9\*]{2}):([0-9\*]{2})$");
		private static readonly Regex ShortFormatRegex = new Regex(@"^([0-9\*]{4})\.([0-9\*]{2})\.([0-9\*]{2})\-([0-9\*]{2}):([0-9\*]{2})$");

		public static string ToMatcherString(DateTime dt) {
			return dt.ToString("yyyy.MM.dd-") + dt.GetNumberOfDayOfWeek() + "" + (int)dt.DayOfWeek + dt.ToString("-HH:mm");
		}

		public static DateTime? ParseDateTimeString(string dtstr) {
			if (ShortFormatRegex.IsMatch(dtstr)) {
				DateTime dt;
				if (DateTime.TryParse(dtstr.Replace(".", "/").Replace("-", " "), out dt)) {
					return dt;
				}
			}
			var lm = FormatRegex.Match(dtstr);
			if (lm != null && lm.Success) {
				var str = lm.Groups[1].Value + "/" + lm.Groups[2].Value + "/" + lm.Groups[3].Value + " " + lm.Groups[6].Value + ":" + lm.Groups[7].Value;
				DateTime dt;
				if (DateTime.TryParse(str, out dt)) {
					return dt;
				}
			}
			return null;
		}

		private static string[] Split(DateTime dt) {
			var ret = new string[MatchersCount];
			ret[0] = dt.ToString("yyyy");
			ret[1] = dt.ToString("MM");
			ret[2] = dt.ToString("dd");
			ret[3] = dt.GetNumberOfDayOfWeek().ToString();
			ret[4] = ((int)dt.DayOfWeek).ToString();
			ret[5] = dt.ToString("HH");
			ret[6] = dt.ToString("mm");
			return ret;
		}

		public string Pattern { get; } = "";
		public bool Valid { get; } = false;
		private static readonly int MatchersCount = 7;
		private NumberMatcher[] Matchers = new NumberMatcher[MatchersCount];
		private string MatchedPostfix = "";
		private string Previous = "";

		public void ResetPrevious() {
			this.Previous = "";
		}

		public void SetPrevious(DateTime dt) {
			this.Previous = ToMatchedString(dt);
			Logger.DebugLog("[DateTimeMatcher] Set Previous [" + this.Previous + "]");
		}

		public bool IsMatch(DateTime dt, bool UsePrevious = true) {
			var now = ToMatchedString(dt);
			if (UsePrevious && now == this.Previous) { return false; }
			if (DoIsMatch(dt)) {
				this.SetPrevious(dt);
				return true;
			}
			return false;
		}

		public string ToMatchedString(DateTime dt) {
			var ret = string.Join("", Split(dt));
			return ret.Substring(0, ret.Length - this.MatchedPostfix.Length) + this.MatchedPostfix;
		}

		private bool DoIsMatch(DateTime dt) {
			if (!this.Valid) { return false; }
			string[] Values = Split(dt);
			for (int i = 0; i < MatchersCount; i++) {
				switch (this.Matchers[i].MatcherType) {
					case NumberMatcherType.AllWildcard:
						// always true
						continue;
					case NumberMatcherType.ConstInt:
					case NumberMatcherType.Mix:
						// check with regex
						if (!this.Matchers[i].RegexValue.IsMatch(Values[i])) { return false; }
						continue;
					case NumberMatcherType.AllLast:
						if (i == 2 && dt.IsLastDayInMonth()) { // date
							continue;
						}
						if (i == 3 && dt.IsLastDayOfWeekInMonth()) { // number of day of week
							continue;
						}
						return false;
					case NumberMatcherType.Unknown:
					default:
						// unexpected error
						return false;
				}
			}
			return true;
		}

		private static int GetMaxDate(NumberMatcher month) {
			if (month.MatcherType == NumberMatcherType.ConstInt) {
				switch (month.IntValue) {
					case 2:
						return 29;
					case 4:
					case 6:
					case 9:
					case 11:
						return 30;
				}
			}
			return 31;
		}

		public DateTimeMatcher(string Pattern) {
			this.Pattern = Pattern;
			if (!FormatRegex.IsMatch(Pattern)) { return; }
			// create matchers
			var mt = FormatRegex.Match(Pattern);
			// year
			this.Matchers[0] = new NumberMatcher(mt.Groups[1].Value, -1, -1, false);
			// month : 01 to 12
			this.Matchers[1] = new NumberMatcher(mt.Groups[2].Value, 1, 12, false);
			// date : 01 to maxdate (or "LL")
			this.Matchers[2] = new NumberMatcher(mt.Groups[3].Value, 1, GetMaxDate(this.Matchers[1]), true);
			// number of day of week : 1 to 5 (or "L")
			this.Matchers[3] = new NumberMatcher(mt.Groups[4].Value, 1, 5, true);
			// day of week : 0 to 6
			this.Matchers[4] = new NumberMatcher(mt.Groups[5].Value, 0, 6, false);
			// hour : 00 to 23
			this.Matchers[5] = new NumberMatcher(mt.Groups[6].Value, 0, 23, false);
			// minute : 00 to 59
			this.Matchers[6] = new NumberMatcher(mt.Groups[7].Value, 0, 59, false);
			// check if all matcher allows wildcard
			this.Valid = true;
			var allwild = true;
			foreach (var m in this.Matchers) {
				if (m.MatcherType != NumberMatcherType.AllWildcard) { allwild = false; }
				if (!m.Valid) { this.Valid = false; }
			}
			if (allwild) { this.Valid = false; }
			// create matched postfix
			if (this.Valid) {
				for (var i = MatchersCount - 1; i >= 0; i--) {
					if (this.Matchers[i].MatcherType != NumberMatcherType.AllWildcard) { break; }
					this.MatchedPostfix = "****".Substring(0, mt.Groups[i + 1].Value.Length) + this.MatchedPostfix;
				}
			}
		}

		private enum NumberMatcherType {
			Unknown,
			AllWildcard,
			AllLast,
			ConstInt,
			Mix
		}

		private class NumberMatcher {
			private static readonly Regex AllWildcardRegex = new Regex("^\\*+$");
			private static readonly Regex AllLastRegex = new Regex("^L+$");
			private static readonly Regex AllNumberRegex = new Regex("^[0-9]+$");

			public NumberMatcherType MatcherType { get; private set; } = NumberMatcherType.Unknown;
			public bool Valid { get; private set; } = false;
			public int IntValue { get; private set; } = 0;
			public Regex RegexValue { get; private set; } = null;

			public NumberMatcher(string Pattern, int Min, int Max, bool AllowLast) {
				// all char is wildcard
				if (AllWildcardRegex.IsMatch(Pattern)) {
					this.MatcherType = NumberMatcherType.AllWildcard;
					this.Valid = true;
					this.RegexValue = new Regex("^[0-9]{" + Pattern.Length + "}$");
					return;
				}
				// all char is "L"
				if (AllLastRegex.IsMatch(Pattern)) {
					if (AllowLast) {
						this.MatcherType = NumberMatcherType.AllLast;
						this.Valid = true;
					}
					return;
				}
				// all char is numeric
				if (AllNumberRegex.IsMatch(Pattern)) {
					int i;
					if (Int32.TryParse(Pattern, out i)) {
						this.MatcherType = NumberMatcherType.ConstInt;
						this.IntValue = i;
						this.RegexValue = new Regex("^" + i + "$");
						this.Valid = (Max < 0 || i <= Max) && (Min < 0 || i >= Min);
					}
					return;
				}
				// mix of wildcard and numeric
				int p_min, p_max;
				try {
					p_min = Int32.Parse(Pattern.Replace("*", "0"));
					p_max = Int32.Parse(Pattern.Replace("*", "9"));

				} catch (Exception) {
					return;
				}
				this.MatcherType = NumberMatcherType.Mix;
				this.Valid = (Max < 0 || p_min <= Max) && (Min < 0 || p_max >= Min);
				if (this.Valid) {
					this.RegexValue = new Regex("^" + Pattern.Replace("*", "[0-9]") + "$");
				}
			}
		}
	}
}
