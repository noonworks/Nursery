using NGettext;

namespace Nursery.BasicPlugins {
	public class T {
		private const string DEFAULT_NAME = "BasicPlugins";

		private static ICatalog _Catalog { get; set; } = new Catalog(DEFAULT_NAME, "./locale");

		public static string _(string text) {
			return _Catalog.GetString(text);
		}

		public static string _(string text, params object[] args) {
			return _Catalog.GetString(text, args);
		}

		public static string _n(string text, string pluralText, long n) {
			return _Catalog.GetPluralString(text, pluralText, n);
		}

		public static string _n(string text, string pluralText, long n, params object[] args) {
			return _Catalog.GetPluralString(text, pluralText, n, args);
		}

		public static string _p(string context, string text) {
			return _Catalog.GetParticularString(context, text);
		}

		public static string _p(string context, string text, params object[] args) {
			return _Catalog.GetParticularString(context, text, args);
		}

		public static string _pn(string context, string text, string pluralText, long n) {
			return _Catalog.GetParticularPluralString(context, text, pluralText, n);
		}

		public static string _pn(string context, string text, string pluralText, long n, params object[] args) {
			return _Catalog.GetParticularPluralString(context, text, pluralText, n, args);
		}
	}
}
