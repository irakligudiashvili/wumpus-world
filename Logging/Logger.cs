namespace WumpusWorld.Logging {
    public class Logger {
        public static List<string> Logs { get; private set; } = new List<string>();

        public static void Log(string message) {
            Logs.Add(message);
        }

        public static void Clear() {
            Logs.Clear();
        }
    }
}
