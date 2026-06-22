namespace WumpusWorld.Logic.Propositions {
    public class VisitedFact : IKnowledgeFact {
        public Coordinate Coordinate { get; }

        public VisitedFact(Coordinate cord) {
            Coordinate = cord;
        }

        public IEnumerable<IExpression> ToSentences() {
            int x = Coordinate.X;
            int y = Coordinate.Y;

            return new List<IExpression> {
                new Symbol($"V_{x}_{y}")
            };
        }
    }
}
