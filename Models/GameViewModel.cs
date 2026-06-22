using WumpusWorld.Logic.Agent.Data;
using WumpusWorld.Logic.Simulation;

namespace WumpusWorld.Models {
    public class GameViewModel {
        public GameSimulation Simulation { get; set; }
        public int GridSize => Simulation.Environment.Grid.Size;
        public bool ShowSlowThinkingWarning { get; set; } = false;

        public GameViewModel(GameSimulation simulation) {
            Simulation = simulation;
        }

        public CellAssetModel GetCellAssets(int x, int y) {
            var currentCell = new Coordinate(x, y);
            var assets = new CellAssetModel();

            // 1. Check Agent Status
            if (Simulation.AgentState.Position.Equals(currentCell)) {
                assets.AgentImage = Simulation.AgentState.IsAlive ? "agent.png" : "dead_agent.png";
            }

            // 2. Check Game Objects
            if (Simulation.Environment.GoldPosition.Equals(currentCell) && !Simulation.AgentState.HasGold) {
                assets.HasGold = true;
            }
            if (Simulation.Environment.IsWumpusAlive && Simulation.Environment.WumpusPosition.Equals(currentCell)) {
                assets.HasWumpus = true;
            }
            if (Simulation.Environment.PitPositions.Contains(currentCell)) {
                assets.HasPit = true;
            }

            // 3. Check Environmental Percepts
            var cellPercept = Simulation.Environment.GetPerceptAt(currentCell, false);
            if (cellPercept.Stench) {
                assets.HasStench = true;
            }
            if (cellPercept.Breeze) {
                assets.HasBreeze = true;
            }

            return assets;
        }
    }
}