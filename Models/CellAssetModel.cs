namespace WumpusWorld.Models {
    public class CellAssetModel {
        public string? AgentImage { get; set; }
        public bool HasGold { get; set; }
        public bool HasWumpus { get; set; }
        public bool HasPit { get; set; }
        public bool HasStench { get; set; }
        public bool HasBreeze { get; set; }

        public bool IsEmpty => AgentImage == null && !HasGold && !HasWumpus && !HasPit && !HasStench && !HasBreeze;
    }
}