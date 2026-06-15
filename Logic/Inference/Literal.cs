namespace WumpusWorld.Logic.Inference {
    public class Literal {
        public string SymbolName { get; }
        public bool IsPositive { get; }

        public Literal(string symbolName, bool isPositive) {
            SymbolName = symbolName;
            IsPositive = isPositive;
        }

        public Literal Negate() => new Literal(SymbolName, !IsPositive);

        public bool Equals(Literal? other) {
            if (other == null) {
                return false;
            }

            return SymbolName == other.SymbolName && IsPositive == other.IsPositive;
        }

        public override bool Equals(object? obj) => Equals(obj as Literal);

        public override int GetHashCode() {
            return HashCode.Combine(SymbolName, IsPositive);
        }

        public override string ToString() {
            if (!IsPositive) {
                return $"¬{SymbolName}";
            }

            return SymbolName;
        }
    }
}
