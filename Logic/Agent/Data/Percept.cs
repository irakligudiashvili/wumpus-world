namespace WumpusWorld.Logic.Agent.Data {
    public class Percept {
        public bool Stench { get; init; }
        public bool Breeze { get; init; }
        public bool Glitter { get; init; }
        public bool Bump { get; init; }
        public bool Scream { get; init; }

        public Percept(bool stench, bool breeze, bool glitter, bool bump, bool scream) {
            Stench = stench;
            Breeze = breeze;
            Glitter = glitter;
            Bump = bump;
            Scream = scream;
        }
    }
}
