using WumpusWorld.Logic.Expressions;
using WumpusWorld.Logic.Inference;

namespace WumpusWorld.Logic {
    public class KnowledgeBase {
        private readonly List<IExpression> _sentences = new List<IExpression>();
        private readonly CnfConverter _converter = new CnfConverter();
        private readonly ResolutionEngine _resolution = new ResolutionEngine();

        public void Tell(IExpression sentence) {
            if(sentence == null) {
                throw new ArgumentException(nameof(sentence));
            }

            _sentences.Add(sentence);
        }

        public bool Ask(IExpression query) {
            if(query == null) {
                throw new ArgumentNullException(nameof(query));
            }

            // 1. negate the query
            IExpression negatedQuery = new Negation(query);

            // 2. combine KB sentences and negated query
            var sentencesToConvert = new List<IExpression>(_sentences) { negatedQuery };

            // 3. convert sentences into CNF clauses
            List<Clause> clauses = _converter.ConvertToCnfClauses(sentencesToConvert);

            // 4. run resolution
            return _resolution.Evaluate(clauses);
        }
    }
}
