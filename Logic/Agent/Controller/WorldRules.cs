namespace WumpusWorld.Logic.Agent.Controller {
    public class WorldRules {
        public static IExpression? GenerateBreezeRule(Coordinate cord, WorldGrid grid) {
            var neighbors = grid.GetNeighbors(cord);

            if (neighbors.Count == 0)
                return null;

            var breezeSymbol = new Symbol($"B_{cord.X}_{cord.Y}");

            IExpression potentialPit = new Symbol($"P_{neighbors[0].X}_{neighbors[0].Y}");
            for(int i = 1; i < neighbors.Count; i++) {
                potentialPit = new OrExpression(potentialPit, new Symbol($"P_{neighbors[i].X}_{neighbors[i].Y}"));
            }

            return new BiConditional(breezeSymbol, potentialPit);
        }

        public static IExpression? GenerateStenchRule(Coordinate cord, WorldGrid grid) {
            var neighbors = grid.GetNeighbors(cord);

            if (neighbors.Count == 0)
                return null;

            var stenchSymbol = new Symbol($"S_{cord.X}_{cord.Y}");

            IExpression potentialWumpus = new Symbol($"W_{neighbors[0].X}_{neighbors[0].Y}");
            for (int i = 1; i < neighbors.Count; i++) {
                potentialWumpus = new OrExpression(potentialWumpus, new Symbol($"W_{neighbors[i].X}_{neighbors[i].Y}"));
            }

            return new BiConditional(stenchSymbol, potentialWumpus);
        }

        public static List<IExpression> GetInitialLocationAxioms(Coordinate start) {
            return new List<IExpression> {
                new Negation(new Symbol($"P_{start.X}_{start.Y}")),
                new Negation(new Symbol($"W_{start.X}_{start.Y}"))
            };
        }

        public static List<IExpression> GenerateWumpusConstraints(WorldGrid grid) {
            var constraints = new List<IExpression>();
            var allLocations = new List<Coordinate>();

            for(int x = 1; x <= grid.Size; x++) {
                for(int y = 1; y <= grid.Size; y++) {
                    allLocations.Add(new Coordinate(x, y));
                }
            }

            if (allLocations.Count == 0)
                return constraints;

            // At least 1 wumpus constraint
            IExpression rule = new Symbol($"W_{allLocations[0].X}_{allLocations[0].Y}");
            for(int i = 1; i < allLocations.Count; i++) {
                rule = new OrExpression(rule, new Symbol($"W_{allLocations[i].X}_{allLocations[i].Y}"));
            }
            constraints.Add(rule);

            // At most 1 wumpus constraint
            for(int i = 0; i < allLocations.Count; i++) {
                for(int j = i + 1; j < allLocations.Count; j++) {
                    var loc1 = allLocations[i];
                    var loc2 = allLocations[j];

                    IExpression notAtBoth = new OrExpression(
                        new Negation(new Symbol($"W_{loc1.X}_{loc1.Y}")),
                        new Negation(new Symbol($"W_{loc2.X}_{loc2.Y}"))
                    );
                    constraints.Add(notAtBoth);
                }
            }

            return constraints;
        }
    }
}
