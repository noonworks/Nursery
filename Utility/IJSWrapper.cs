namespace Nursery.Utility {
	public interface IJSWrapper {
		void Reset();
		void SetType(string Name, System.Type Type);
		void SetFunction(string Name, string Source);
		object ExecuteFunction(string Name, object arg);
	}
}
