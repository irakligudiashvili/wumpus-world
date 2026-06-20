namespace WumpusWorld.Logic.Agent.Controller {
    public class AgentBrain {
        private readonly Random _random = new Random();
        private readonly KnowledgeBase _kb;
        private readonly WorldGrid _grid;

        private readonly HashSet<Coordinate> _visited = new HashSet<Coordinate>();
        private readonly HashSet<Coordinate> _safeUnvisited = new HashSet<Coordinate>();

        public AgentBrain(WorldGrid grid, KnowledgeBase kb, int startX = 1, int startY = 1) {
            _grid = grid;
            _kb = kb;

            var start = new Coordinate(startX, startY);
            var initialAxioms = WorldRules.GetInitialLocationAxioms(start);

            foreach (var axiom in initialAxioms) {
                _kb.Tell(axiom);
            }
        }

        private void ProcessSensoryInputs(Coordinate currentPos, Percept percept) {
            IExpression breezeFact = new Symbol($"B_{currentPos.X}_{currentPos.Y}");

            if (!percept.Breeze) {
                breezeFact = new Negation(breezeFact);
            }

            _kb.Tell(breezeFact);

            IExpression stenchFact = new Symbol($"S_{currentPos.X}_{currentPos.Y}");

            if (!percept.Stench) {
                stenchFact = new Negation(stenchFact);
            }

            _kb.Tell(stenchFact);

            var breezeRule = WorldRules.GenerateBreezeRule(currentPos, _grid);
            var stenchRule = WorldRules.GenerateStenchRule(currentPos, _grid);

            if (breezeRule != null)
                _kb.Tell(breezeRule);
            if (stenchRule != null)
                _kb.Tell(stenchRule);
        }

        private void EvaluateNeighborSafety(Coordinate currentPos) {
            var neighbors = _grid.GetNeighbors(currentPos);

            foreach (var neighbor in neighbors) {
                if (_visited.Contains(neighbor))
                    continue;

                var pitSearch = new Symbol($"P_{neighbor.X}_{neighbor.Y}");
                bool isPitThere = _kb.Ask(pitSearch);
                bool isPitNotThere = _kb.Ask(new Negation(pitSearch));

                var wumpusSearch = new Symbol($"W_{neighbor.X}_{neighbor.Y}");
                bool isWumpusThere = _kb.Ask(wumpusSearch);
                bool isWumpusNotThere = _kb.Ask(new Negation(wumpusSearch));

                if (isPitNotThere && isWumpusNotThere)
                    _safeUnvisited.Add(neighbor);
            }
        }

        private Coordinate? SelectNextTarget(Coordinate currentPos) {
            var neighbors = _grid.GetNeighbors(currentPos);

            var valid = new List<Coordinate>();
            foreach (var neighbor in neighbors) {
                if (_safeUnvisited.Contains(neighbor)) {
                    valid.Add(neighbor);
                }
            }

            if (valid.Count > 0) {
                int rng = _random.Next(valid.Count);
                return valid[rng];
            }

            if (_safeUnvisited.Count > 0) {
                var targets = new List<Coordinate>(_safeUnvisited);
                int rng = _random.Next(targets.Count);

                return targets[rng];
            }

            return null;
        }

        public Coordinate? ProcessTurn(Coordinate currentPos, Percept percept) {
            _visited.Add(currentPos);
            _safeUnvisited.Remove(currentPos);

            ProcessSensoryInputs(currentPos, percept);
            EvaluateNeighborSafety(currentPos);

            return SelectNextTarget(currentPos);
        }
    }
}