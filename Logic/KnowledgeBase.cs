using WumpusWorld.Logic.Expressions;
using WumpusWorld.Logic.Inference;

namespace WumpusWorld.Logic {
    public class KnowledgeBase {
        private readonly List<IExpression> _sentences = new List<IExpression>();
        private readonly CnfConverter _converter = new CnfConverter();

        public void Tell(IExpression sentence) {
            if(sentence == null) {
                throw new ArgumentException(nameof(sentence));
            }

            _sentences.Add(sentence);
        }

        public void Ask(IExpression query) {
            if(query == null) {
                throw new ArgumentNullException(nameof(query));
            }

            // TODO: negate the query
            IExpression negatedQuery = new Negation(query);

            // TODO: combine KB sentences and negated query
            var sentencesToConvert = new List<IExpression>(_sentences) { negatedQuery };

            // TODO: convert sentences into CNF clauses
            List<Clause> clauses = _converter.ConvertToCnfClauses(sentencesToConvert);

            // TODO: run resolution

        }
    }
}
