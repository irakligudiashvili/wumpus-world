using WumpusWorld.Logic.Expressions;

namespace WumpusWorld.Logic {
    public class KnowledgeBase {
        private readonly List<IExpression> _sentences = new List<IExpression>();

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

            // TODO: combine KB sentences and negated query into set

            // TODO: convert sentences into CNF clauses

            // TODO: run resolution
        }
    }
}
