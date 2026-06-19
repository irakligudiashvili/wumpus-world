namespace WumpusWorld.Logic.Agent.Data {
    public class WorldGrid {
        public int Size { get; } = 4;
        public HashSet<Coordinate> Pits { get; } = new HashSet<Coordinate>();
        public Coordinate Wumpus { get; set; }
        public Coordinate Gold { get; set; }
        public bool IsWumpusAlive { get; set; } = true;
        public bool HasScreamOccurred { get; set; } = false;

        public Percept GetPerceptAt(Coordinate cord) {
            bool breeze = false;
            bool stench = false;
            bool glitter = cord == Gold;
            bool bump = false;
            bool scream = HasScreamOccurred;

            foreach(var neighbor in GetNeighbors(cord)) {
                if (Pits.Contains(neighbor)) {
                    breeze = true;
                }

                if(IsWumpusAlive && neighbor == Wumpus) {
                    stench = true;
                }
            }

            return new Percept(stench, breeze, glitter, bump, scream);
        }

        public List<Coordinate> GetNeighbors(Coordinate cord) {
            var neighbors = new List<Coordinate>();

            if (cord.X > 1)
                neighbors.Add(new Coordinate(cord.X - 1, cord.Y));
            if (cord.X < Size)
                neighbors.Add(new Coordinate(cord.X + 1, cord.Y));
            if (cord.Y > 1)
                neighbors.Add(new Coordinate(cord.X, cord.Y - 1));
            if (cord.Y < Size)
                neighbors.Add(new Coordinate(cord.X, cord.Y + 1));

            return neighbors;
        }
    }
}
