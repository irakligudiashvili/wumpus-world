namespace WumpusWorld.Logic.Expressions {
    public class OrExpression {
        public IExpression Left { get; }
        public IExpression Right { get; }
        
        public OrExpression(IExpression left, IExpression right) {
            Left = left;
            Right = right;
        }

        public bool Evaluate(Dictionary<string, bool> model) => Left.Evaluate(model) || Right.Evaluate(model);

        public override string ToString() => $"({Left} ∨ {Right})";
    }
}
