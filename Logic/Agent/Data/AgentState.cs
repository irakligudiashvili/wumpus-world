namespace WumpusWorld.Logic.Agent.Data {
    public class AgentState {
        public Coordinate Position { get; set; } = new Coordinate(1, 1);
        public Direction Facing { get; set; } = Direction.Right;
        public bool HasArrow { get; set; } = true;
        public bool IsAlive { get; set; } = true;
        public bool HasGold { get; set; } = false;
        public int Score { get; set; } = 0;
        public bool HasWon { get; set; } = false;

        public void MoveForward() {
            Position = Facing switch {
                Direction.Up => new Coordinate(Position.X, Position.Y + 1),
                Direction.Down => new Coordinate(Position.X, Position.Y - 1),
                Direction.Left => new Coordinate(Position.X - 1, Position.Y),
                Direction.Right => new Coordinate(Position.X + 1, Position.Y),
                _ => Position
            };

            Score--;
        }

        public void TurnLeft() {
            Facing = Facing switch {
                Direction.Up => Direction.Left,
                Direction.Left => Direction.Down,
                Direction.Down => Direction.Right,
                Direction.Right => Direction.Up,
                _ => Facing
            };
            Score--;
        }

        public void TurnRight() {
            Facing = Facing switch {
                Direction.Up => Direction.Right,
                Direction.Right => Direction.Down,
                Direction.Down => Direction.Left,
                Direction.Left => Direction.Up,
                _ => Facing
            };
            Score--;
        }

        public void AdjustScore(int amount) {
            Score += amount;
        }

        public void ShootArrow() {
            if (HasArrow) {
                HasArrow = false;
                Score -= 10;
                Logger.Log($"[ACTION] Agent has fired an arrow");
            }
        }
    }
}
