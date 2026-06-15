namespace WumpusWorld.Logic.Expressions {
    public class BiConditional : IExpression {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public BiConditional(IExpression left, IExpression right) {
            Left = left;
            Right = right;
        }

        public bool Evaluate(Dictionary<string, bool> model) {
            return Left.Evaluate(model) == Right.Evaluate(model);
        }

        public override string ToString() => $"{Left} ↔ {Right}";
    }
}
