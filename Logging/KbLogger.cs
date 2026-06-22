namespace WumpusWorld.Logging {
    public static class KbLogger {
        private static readonly List<string> _kbLogs = new List<string>();
        public static IReadOnlyList<string> Logs => _kbLogs;

        public static void Log(string message) {
            _kbLogs.Add(message);
        }

        public static void Clear() {
            _kbLogs.Clear();
        }
    }
}