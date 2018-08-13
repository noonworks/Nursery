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
	}
}
