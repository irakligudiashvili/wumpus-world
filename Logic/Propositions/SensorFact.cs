namespace WumpusWorld.Logic.Propositions {
    public class SensorFact : IKnowledgeFact {
        public Coordinate Coordinate { get; }
        public Percept Percept { get; }

        public SensorFact(Coordinate cord, Percept percept) {
            Coordinate = cord;
            Percept = percept;
        }

        public IEnumerable<IExpression> ToSentences() {
            var sentences = new List<IExpression>();
            int x = Coordinate.X;
            int y = Coordinate.Y;

            // Unpack Breeze status
            if (Percept.Breeze)
                sentences.Add(new Symbol($"B_{x}_{y}"));
            else
                sentences.Add(new Negation(new Symbol($"B_{x}_{y}")));

            // Unpack Stench status
            if (Percept.Stench)
                sentences.Add(new Symbol($"S_{x}_{y}"));
            else
                sentences.Add(new Negation(new Symbol($"S_{x}_{y}")));

            return sentences;
        }
    }
}
