using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nursery.Utility;

namespace Nursery.UnitTests {
	[TestClass]
	public class DateTimeMatcherTest {
		private static DateTime CreateDateTime(int hours, int minutes, int seconds = 0) {
			var now = DateTime.Now;
			return new DateTime(now.Year, now.Month, now.Day, hours, minutes, seconds, 0);
		}

		private static void AssertMatcher(DateTimeMatcher matcher, DateTime dt, bool expect) {
			Assert.AreEqual(expect, matcher.IsMatch(dt), matcher.Pattern + " / " + dt.ToString());
		}

		[TestMethod]
		public void Minute10() {
			var m = new DateTimeMatcher("****.**.**-**-**:10");
			//
			var dt000500 = CreateDateTime(0, 5);
			var dt001000 = CreateDateTime(0, 10);
			var dt001030 = CreateDateTime(0, 10, 30);
			var dt001500 = CreateDateTime(0, 15);
			var dt001530 = CreateDateTime(0, 15, 30);
			var dt001700 = CreateDateTime(0, 17);
			//
			var dt010500 = CreateDateTime(1, 5);
			var dt011000 = CreateDateTime(1, 10);
			var dt011030 = CreateDateTime(1, 10, 30);
			var dt011500 = CreateDateTime(1, 15);
			var dt011530 = CreateDateTime(1, 15, 30);
			var dt011700 = CreateDateTime(1, 17);
			// only match minute 10
			AssertMatcher(m, dt000500, false);
			AssertMatcher(m, dt001500, false);
			AssertMatcher(m, dt001530, false);
			AssertMatcher(m, dt001700, false);
			AssertMatcher(m, dt001000, true);
			AssertMatcher(m, dt001030, false);
			m.ResetPrevious();
			AssertMatcher(m, dt010500, false);
			AssertMatcher(m, dt011500, false);
			AssertMatcher(m, dt011530, false);
			AssertMatcher(m, dt011700, false);
			AssertMatcher(m, dt011030, true);
			AssertMatcher(m, dt011000, false);
		}

		[TestMethod]
		public void Minute1X() {
			var m = new DateTimeMatcher("****.**.**-**-**:1*");
			//
			var dt000500 = CreateDateTime(0, 5);
			var dt001000 = CreateDateTime(0, 10);
			var dt001030 = CreateDateTime(0, 10, 30);
			var dt001500 = CreateDateTime(0, 15);
			var dt001530 = CreateDateTime(0, 15, 30);
			var dt001700 = CreateDateTime(0, 17);
			//
			var dt010500 = CreateDateTime(1, 5);
			var dt011000 = CreateDateTime(1, 10);
			var dt011030 = CreateDateTime(1, 10, 30);
			var dt011500 = CreateDateTime(1, 15);
			var dt011530 = CreateDateTime(1, 15, 30);
			var dt011700 = CreateDateTime(1, 17);
			// only match minute 1X
			AssertMatcher(m, dt000500, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001000, true);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001500, true);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001530, true);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001700, true);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001000, true);
			AssertMatcher(m, dt001500, false);
			AssertMatcher(m, dt001530, false);
			AssertMatcher(m, dt001700, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001500, true);
			AssertMatcher(m, dt001530, false);
			AssertMatcher(m, dt001700, false);
			AssertMatcher(m, dt001000, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt001700, true);
			AssertMatcher(m, dt001000, false);
			AssertMatcher(m, dt001500, false);
			AssertMatcher(m, dt001530, false);
			AssertMatcher(m, dt011500, true);
			AssertMatcher(m, dt011530, false);
			AssertMatcher(m, dt011700, false);
			m.ResetPrevious();
		}

		[TestMethod]
		public void EveryX5Minute() {
			var m = new DateTimeMatcher("****.**.**-**-**:*5");
			//
			var dt000000 = CreateDateTime(0, 0);
			var dt000500 = CreateDateTime(0, 5);
			var dt000530 = CreateDateTime(0, 5, 30);
			var dt001000 = CreateDateTime(0, 10);
			var dt001500 = CreateDateTime(0, 15);
			var dt001700 = CreateDateTime(0, 17);
			var dt002500 = CreateDateTime(0, 25);
			var dt003500 = CreateDateTime(0, 35);
			var dt004500 = CreateDateTime(0, 45);
			var dt005500 = CreateDateTime(0, 55);
			// only match X5 minutes
			AssertMatcher(m, dt000000, false);
			AssertMatcher(m, dt001000, false);
			AssertMatcher(m, dt001700, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt000500, true);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt000530, true);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt000500, true);
			AssertMatcher(m, dt000530, false);
			AssertMatcher(m, dt001000, false);
			AssertMatcher(m, dt001500, true);
			AssertMatcher(m, dt001700, false);
			AssertMatcher(m, dt002500, true);
			AssertMatcher(m, dt003500, true);
			AssertMatcher(m, dt004500, true);
			AssertMatcher(m, dt005500, true);
		}

		[TestMethod]
		public void Any13hour() {
			var m = new DateTimeMatcher("****.**.**-**-13:**");
			//
			var dt125959 = CreateDateTime(12, 59, 59);
			var dt130000 = CreateDateTime(13, 0);
			var dt130015 = CreateDateTime(13, 0, 15);
			var dt130800 = CreateDateTime(13, 8);
			var dt130830 = CreateDateTime(13, 8, 30);
			var dt135959 = CreateDateTime(13, 59, 59);
			var dt140000 = CreateDateTime(14, 0);
			// only match 13 hour
			AssertMatcher(m, dt125959, false);
			AssertMatcher(m, dt140000, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt130000, true);
			AssertMatcher(m, dt130015, false);
			AssertMatcher(m, dt130800, false);
			AssertMatcher(m, dt130830, false);
			AssertMatcher(m, dt135959, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt130015, true);
			AssertMatcher(m, dt130000, false);
			AssertMatcher(m, dt130800, false);
			AssertMatcher(m, dt130830, false);
			AssertMatcher(m, dt135959, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt130800, true);
			AssertMatcher(m, dt130000, false);
			AssertMatcher(m, dt130015, false);
			AssertMatcher(m, dt130830, false);
			AssertMatcher(m, dt135959, false);
			m.ResetPrevious();
			//
			AssertMatcher(m, dt135959, true);
			AssertMatcher(m, dt130000, false);
			AssertMatcher(m, dt130015, false);
			AssertMatcher(m, dt130800, false);
			AssertMatcher(m, dt130830, false);
			m.ResetPrevious();
		}

		[TestMethod]
		public void EveryMinutes() {
			var matchers = new DateTimeMatcher[] {
				new DateTimeMatcher("****.**.**-**-**:*0"),
				new DateTimeMatcher("****.**.**-**-**:*1"),
				new DateTimeMatcher("****.**.**-**-**:*2"),
				new DateTimeMatcher("****.**.**-**-**:*3"),
				new DateTimeMatcher("****.**.**-**-**:*4"),
				new DateTimeMatcher("****.**.**-**-**:*5"),
				new DateTimeMatcher("****.**.**-**-**:*6"),
				new DateTimeMatcher("****.**.**-**-**:*7"),
				new DateTimeMatcher("****.**.**-**-**:*8"),
				new DateTimeMatcher("****.**.**-**-**:*9")
			};
			var dt = CreateDateTime(12, 59);
			for (var i = 0; i < 60 * 24 + 1; i++) {
				Assert.IsTrue(matchers.Any(m => m.IsMatch(dt)));
				dt = dt.AddMinutes(1);
			}
		}

		[TestMethod]
		public void Tuesday() {
			var m = new DateTimeMatcher("****.**.**-*2-21:05");
			Assert.IsTrue(m.Valid);
			var tue = new DateTime(2018, 8, 14, 21, 5, 0);
			for (var i = 0; i < 200; i++) {
				if (i % 7 == 0) {
					// tuesday
					Assert.IsTrue(m.IsMatch(tue.AddDays(i)));
					Assert.IsFalse(m.IsMatch(tue.AddDays(i).AddSeconds(30)));
					Assert.IsFalse(m.IsMatch(tue.AddDays(i).AddSeconds(630)));
				} else {
					// other
					Assert.IsFalse(m.IsMatch(tue.AddDays(i)));
				}
			}
		}

		[TestMethod]
		public void ThirdThursday() {
			var m = new DateTimeMatcher("****.**.**-34-21:05");
			Assert.IsTrue(m.Valid);
			var first = new DateTime(2018, 1, 1, 21, 5, 0);
			for (var i = 0; i < 380; i++) {
				var dt = first.AddDays(i);
				var isThirdThursday = dt.DayOfWeek == DayOfWeek.Thursday && dt.GetNumberOfDayOfWeek() == 3;
				if (isThirdThursday) {
					Assert.IsTrue(m.IsMatch(dt));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(30)));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(630)));
				} else {
					// other
					Assert.IsFalse(m.IsMatch(dt));
				}
			}
		}

		[TestMethod]
		public void Every1st() {
			var m = new DateTimeMatcher("****.**.01-**-00:00");
			Assert.IsTrue(m.Valid);
			var first = new DateTime(2018, 1, 1, 0, 0, 0);
			for (var i = 0; i < 380; i++) {
				var dt = first.AddDays(i);
				if (dt.Day == 1) {
					Assert.IsTrue(m.IsMatch(dt));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(30)));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(630)));
				} else {
					// other
					Assert.IsFalse(m.IsMatch(dt));
				}
			}
		}

		[TestMethod]
		public void EveryLast() {
			var m = new DateTimeMatcher("****.**.LL-**-00:00");
			Assert.IsTrue(m.Valid);
			var first = new DateTime(2018, 1, 1, 0, 0, 0);
			for (var i = 0; i < 380; i++) {
				var dt = first.AddDays(i);
				if (dt.IsLastDayInMonth()) {
					Assert.IsTrue(m.IsMatch(dt));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(30)));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(630)));
				} else {
					// other
					Assert.IsFalse(m.IsMatch(dt));
				}
			}
		}

		[TestMethod]
		public void EveryMD() {
			var m = new DateTimeMatcher("****.08.14-**-00:00");
			Assert.IsTrue(m.Valid);
			var first = new DateTime(2018, 1, 1, 0, 0, 0);
			for (var i = 0; i < 366 * 10; i++) {
				var dt = first.AddDays(i);
				if (dt.Month == 8 && dt.Day == 14) {
					Assert.IsTrue(m.IsMatch(dt));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(30)));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(630)));
				} else {
					// other
					Assert.IsFalse(m.IsMatch(dt));
				}
			}
		}

		[TestMethod]
		public void EveryLastThursday() {
			var m = new DateTimeMatcher("****.**.**-L4-00:00");
			Assert.IsTrue(m.Valid);
			var first = new DateTime(2018, 1, 1, 0, 0, 0);
			for (var i = 0; i < 366 * 10; i++) {
				var dt = first.AddDays(i);
				var isLastThursday = dt.DayOfWeek == DayOfWeek.Thursday && dt.AddDays(7).Month != dt.Month;
				if (isLastThursday) {
					Assert.IsTrue(m.IsMatch(dt));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(30)));
					Assert.IsFalse(m.IsMatch(dt.AddSeconds(630)));
				} else {
					// other
					Assert.IsFalse(m.IsMatch(dt));
				}
			}
		}
	}
}
