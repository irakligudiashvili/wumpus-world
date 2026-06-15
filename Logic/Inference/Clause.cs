namespace WumpusWorld.Logic.Inference {
    public class Clause {
        public HashSet<Literal> Literals { get; } = new HashSet<Literal>();

        public Clause() { }

        public Clause(IEnumerable<Literal> literals) {
            foreach(var l in literals) {
                Literals.Add(l);
            }
        }

        public bool IsEmpty => Literals.Count == 0;

        public bool Equals(Clause? other) {
            if(other == null) {
                return false;
            }

            if(ReferenceEquals(this, other)) {
                return true;
            }

            if(Literals.Count != other.Literals.Count) {
                return false;
            }

            return Literals.SetEquals(other.Literals);
        }

        public override bool Equals(object? obj) => Equals(obj as Clause);

        public override int GetHashCode() {
            var hash = new HashCode();

            var sorted = Literals
                .OrderBy(l => l.SymbolName)
                .ThenBy(l => l.IsPositive);

            foreach (var lit in sorted) {
                hash.Add(lit);
            }

            return hash.ToHashCode();
        }

        public override string ToString() {
            if(Literals.Count == 0) {
                return "Empty";
            }

            var sortedLiterals = Literals
                .OrderBy(l => l.SymbolName)
                .ThenBy(l => l.IsPositive)
                .Select(l => l.ToString());

            return string.Join(" ∨ ", sortedLiterals);
        }
    }
}
