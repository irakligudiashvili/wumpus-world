using WumpusWorld.Logic.Agent.Data;

namespace WumpusWorld.Logic.Simulation {
    public class WorldEnvironment {
        public WorldGrid Grid { get; init; }
        public Coordinate WumpusPosition { get; init; }
        public Coordinate GoldPosition { get; init; }
        public HashSet<Coordinate> PitPositions { get; init; }
        public bool IsWumpusAlive { get; set; } = true;

        public bool wumpusScreamed = false;

        public WorldEnvironment(WorldGrid grid, Coordinate wumpus, Coordinate gold, HashSet<Coordinate> pits) {
            Grid = grid;
            WumpusPosition = wumpus;
            GoldPosition = gold;
            PitPositions = pits;
        }

        public Percept GetPerceptAt(Coordinate cord, bool bumped) {
            bool breeze = false;
            bool stench = false;
            bool glitter = (cord.X == GoldPosition.X && cord.Y == GoldPosition.Y);

            var neighbors = Grid.GetNeighbors(cord);
            foreach(var n in neighbors) {
                if (PitPositions.Contains(n))
                    breeze = true;

                if (n.X == WumpusPosition.X && n.Y == WumpusPosition.Y && IsWumpusAlive)
                    stench = true;
            }

            return new Percept(stench, breeze, glitter, bumped, wumpusScreamed);
        }

        public void TriggerScream() {
            if (IsWumpusAlive) {
                IsWumpusAlive = false;
                wumpusScreamed = true;
            }
        }

        public void ClearScream() {
            wumpusScreamed = false;
        }

        public bool IsDangerous(Coordinate cord) {
            bool isPit = PitPositions.Contains(cord);
            bool isWumpus = IsWumpusAlive && (cord.X == WumpusPosition.X && cord.Y == WumpusPosition.Y);

            return isPit || isWumpus;
        }
    }
}
