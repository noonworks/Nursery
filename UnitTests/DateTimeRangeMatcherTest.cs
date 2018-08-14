using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nursery.Utility;

namespace Nursery.UnitTests {
	[TestClass]
	public class DateTimeRangeMatcherTest {
		[TestMethod]
		public void Invalid() {
			DateTimeRangeMatcher m;
			//
			m = new DateTimeRangeMatcher(0, 0);
			Assert.IsFalse(m.Valid);
			m = new DateTimeRangeMatcher(2359, 2359);
			Assert.IsFalse(m.Valid);
			m = new DateTimeRangeMatcher(-1, -1);
			Assert.IsFalse(m.Valid);
			m = new DateTimeRangeMatcher(2400, 2400);
			Assert.IsFalse(m.Valid);
			//
			m = new DateTimeRangeMatcher(0, 2359);
			Assert.IsTrue(m.Valid);
			m = new DateTimeRangeMatcher(500, 459);
			Assert.IsTrue(m.Valid);
			m = new DateTimeRangeMatcher(1230, 1229);
			Assert.IsTrue(m.Valid);
			//
			m = new DateTimeRangeMatcher(0, 2359, "2018.08.14-**-**:**");
			Assert.IsTrue(m.Valid);
			m = new DateTimeRangeMatcher(0, 2359, "2018.08.14-**");
			Assert.IsTrue(m.Valid);
			m = new DateTimeRangeMatcher(0, 2359, "2018.08.14");
			Assert.IsTrue(m.Valid);
			m = new DateTimeRangeMatcher(0, 2359, "2018.08.14-**-**:**abcd");
			Assert.IsFalse(m.Valid);
		}

		[TestMethod]
		public void InSameDay() {
			DateTimeRangeMatcher m;
			DateTime dt;
			DateTimeRangeMatch result;
			//
			m = new DateTimeRangeMatcher(1000, 1500);
			//
			dt = new DateTime(2018, 8, 14, 9, 59, 59, 999);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			//
			dt = dt.AddMilliseconds(1);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate);
			//
			dt = new DateTime(2018, 8, 14, 15, 1, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			//
			dt = dt.AddMilliseconds(-1);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate);
			//
			dt = new DateTime(2018, 8, 14, 14, 25, 39, 870);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate);
		}

		[TestMethod]
		public void WithPreviousDay() {
			DateTimeRangeMatcher m;
			DateTime dt;
			DateTimeRangeMatch result;
			//
			m = new DateTimeRangeMatcher(1500, 1000);
			//
			dt = new DateTime(2018, 8, 14, 10, 1, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			//
			dt = dt.AddMilliseconds(-1);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date.AddDays(-1), result.BaseDate); // Base is PREVIOUS DAY. THIS DAY is next day of the base.
			//
			dt = new DateTime(2018, 8, 14, 14, 59, 59, 999);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			//
			dt = dt.AddMilliseconds(1);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate); // Base is THIS DAY.
			//
			dt = new DateTime(2018, 8, 14, 21, 35, 39, 874);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate); // Base is THIS DAY.
			//
			dt = new DateTime(2018, 8, 14, 7, 35, 39, 824);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date.AddDays(-1), result.BaseDate); // Base is PREVIOUS DAY. THIS DAY is next day of the base.
		}

		[TestMethod]
		public void WithNextDay() {
			DateTimeRangeMatcher m;
			DateTime dt;
			DateTimeRangeMatch result;
			//
			m = new DateTimeRangeMatcher(1500, 1000, "", true);
			//
			dt = new DateTime(2018, 8, 14, 10, 1, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			//
			dt = dt.AddMilliseconds(-1);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate); // Base is THIS DAY.
			//
			dt = new DateTime(2018, 8, 14, 14, 59, 59, 999);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			//
			dt = dt.AddMilliseconds(1);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date.AddDays(1), result.BaseDate); // Base is NEXT DAY. THIS DAY is previous day of the base.
			//
			dt = new DateTime(2018, 8, 14, 21, 35, 39, 874);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date.AddDays(1), result.BaseDate); // Base is NEXT DAY. THIS DAY is previous day of the base.
			//
			dt = new DateTime(2018, 8, 14, 7, 35, 39, 824);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate); // Base is THIS DAY.
		}

		[TestMethod]
		public void DateMatch() {
			DateTimeRangeMatcher m;
			DateTime dt;
			DateTimeRangeMatch result;
			// "LAST SUNDAY"
			m = new DateTimeRangeMatcher(1000, 1500, "****.**.**-L0");
			Assert.IsTrue(m.Valid);
			// not sunday
			dt = new DateTime(2018, 8, 14, 13, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// sunday but not last
			dt = new DateTime(2018, 8, 19, 13, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// last sunday
			dt = new DateTime(2018, 8, 26, 13, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(dt.Date, result.BaseDate);
			// last sunday but out of range (1)
			dt = new DateTime(2018, 8, 26, 9, 59, 59, 999);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// last sunday but out of range (2)
			dt = new DateTime(2018, 8, 26, 15, 1, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
		}

		[TestMethod]
		public void DateMatchWithPreviousDay() {
			DateTimeRangeMatcher m;
			DateTime dt;
			DateTimeRangeMatch result;
			DateTime SUNDAY;
			// "LAST SUNDAY 1500 - Next 1000"
			m = new DateTimeRangeMatcher(1500, 1000, "****.**.**-L0");
			Assert.IsTrue(m.Valid);
			// last sunday
			dt = new DateTime(2018, 8, 26, 21, 0, 0, 0);
			SUNDAY = dt.Date;
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(SUNDAY, result.BaseDate);
			// last sunday but out of range
			dt = new DateTime(2018, 8, 26, 9, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// the next day
			dt = new DateTime(2018, 8, 27, 9, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(SUNDAY, result.BaseDate);
			// the next day but out of range
			dt = new DateTime(2018, 8, 27, 21, 1, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// the previous day
			dt = new DateTime(2018, 8, 25, 9, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			dt = new DateTime(2018, 8, 25, 21, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
		}

		[TestMethod]
		public void DateMatchWithNextDay() {
			DateTimeRangeMatcher m;
			DateTime dt;
			DateTimeRangeMatch result;
			DateTime SUNDAY;
			// "Prev 1500 - LAST SUNDAY 1000"
			m = new DateTimeRangeMatcher(1500, 1000, "****.**.**-L0", true);
			Assert.IsTrue(m.Valid);
			// last sunday
			dt = new DateTime(2018, 8, 26, 9, 0, 0, 0);
			SUNDAY = dt.Date;
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(SUNDAY, result.BaseDate);
			// last sunday but out of range
			dt = new DateTime(2018, 8, 26, 21, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// the previous day
			dt = new DateTime(2018, 8, 25, 21, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsTrue(result.Success);
			Assert.AreEqual(SUNDAY, result.BaseDate);
			// the previous day but out of range
			dt = new DateTime(2018, 8, 27, 9, 1, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			// the next day
			dt = new DateTime(2018, 8, 27, 9, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
			dt = new DateTime(2018, 8, 27, 21, 0, 0, 0);
			result = m.Match(dt);
			Assert.IsFalse(result.Success);
		}
	}
}
