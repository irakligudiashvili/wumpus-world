using System.IO;
using WumpusWorld.Logic.Propositions;

namespace WumpusWorld.Logic.Agent.Controller {
    public class AgentBrain {
        private readonly Random _random = new Random();
        private readonly KnowledgeBase _kb;
        private readonly WorldGrid _grid;
        private readonly Queue<Coordinate> _actionPlan = new Queue<Coordinate>();
        private readonly HashSet<Coordinate> _visitedCells = new HashSet<Coordinate>();
        private readonly HashSet<Coordinate> _sensedCells = new HashSet<Coordinate>();
        private readonly HashSet<Coordinate> _knownSafeCells = new HashSet<Coordinate>();
        private bool wumpusFound = false;
        private Coordinate? wumpusLocation = null;
        public bool intentToShoot = false;
        private bool wumpusDead = false;
        private bool isEscaping = false;
        public Coordinate CurrentTarget { get; private set; }

        public AgentBrain(WorldGrid grid, KnowledgeBase kb, int startX = 1, int startY = 1) {
            _grid = grid;
            _kb = kb;

            // 1. Load basic facts about starting location
            var start = new Coordinate(startX, startY);
            _visitedCells.Add(start);
            _knownSafeCells.Add(start);

            var initialAxioms = WorldRules.GetInitialLocationAxioms(start);

            foreach (var axiom in initialAxioms) {
                _kb.Tell(axiom);
            }

            // 2. Generate and load rules for the grid
            for (int y = 1; y <= _grid.Size; y++) {
                for (int x = 1; x <= _grid.Size; x++) {
                    var currentCord = new Coordinate(x, y);

                    // Generate Breeze biconditionals
                    var breezeRule = WorldRules.GenerateBreezeRule(currentCord, _grid);
                    if (breezeRule != null)
                        _kb.Tell(breezeRule);

                    // Generate Stench biconditionals
                    var stenchRule = WorldRules.GenerateStenchRule(currentCord, _grid);
                    if (stenchRule != null)
                        _kb.Tell(stenchRule);
                }
            }

            // 3. Load Wumpus constraints
            var wumpusConstrains = WorldRules.GenerateWumpusConstraints(_grid);
            foreach (var constraint in wumpusConstrains)
                _kb.Tell(constraint);
        }

        public Coordinate? ProcessTurn(Coordinate currentPos, Percept percept, AgentState state) {
            if (intentToShoot && !state.HasArrow) {
                intentToShoot = false;
                CurrentTarget = default(Coordinate);
                _actionPlan.Clear();
            }

            // Chec if a scream was let out by Wumpus
            if (percept.Scream) {
                Logger.Log("[BRAIN] A scream has been heard! Erasing Wumpus threats from Knowledge Base");

                CurrentTarget = default(Coordinate);
                _actionPlan.Clear();

                Coordinate? deadWumpusTile = wumpusLocation;

                wumpusFound = false;
                wumpusLocation = null;
                intentToShoot = false;
                wumpusDead = true;

                _knownSafeCells.Clear();
                _knownSafeCells.Add(new Coordinate(1, 1));
                foreach (var cell in _visitedCells)
                    _knownSafeCells.Add(cell);

                if (deadWumpusTile.HasValue)
                    _knownSafeCells.Add(deadWumpusTile.Value);

                for (int y = 1; y <= _grid.Size; y++) {
                    for (int x = 1; x <= _grid.Size; x++) {
                        var cell = new Coordinate(x, y);

                        if (IsSafe(cell)) {
                            _knownSafeCells.Add(cell);
                        }
                    }
                }
            }

            // Check for glitter if we don't have gold
            if (percept.Glitter && !state.HasGold && !isEscaping) {
                Logger.Log($"[BRAIN] Perceived glitter at {currentPos}! Grabbing the gold and escaping...");
                state.HasGold = true;
                isEscaping = true;

                _actionPlan.Clear();

                List<Coordinate> escapePath = FindSafePath(currentPos, new Coordinate(1, 1));
                if (escapePath != null && escapePath.Count > 1) {
                    for (int i = 1; i < escapePath.Count; i++) {
                        _actionPlan.Enqueue(escapePath[i]);
                    }
                }

                CurrentTarget = currentPos;
                return CurrentTarget;
            }

            // If we are escaping with gold and reached the exit, end the game
            if ((state.HasGold || isEscaping) && currentPos.Equals(new Coordinate(1, 1))) {
                Logger.Log($"[BRAIN] Reached the exit with the gold!");
                state.HasWon = true;
                state.AdjustScore(1000);
                return null;
            }



            // 1. If we have a planned backtracking path step queued up, execute it
            if (_actionPlan.Count > 0) {
                if (!currentPos.Equals(CurrentTarget))
                    return CurrentTarget;

                CurrentTarget = _actionPlan.Dequeue();

                if (currentPos.Equals(CurrentTarget) && _actionPlan.Count == 0) {

                } else {
                    return CurrentTarget;
                }
            }

            if (CurrentTarget != default(Coordinate) && !currentPos.Equals(CurrentTarget) && _grid.GetNeighbors(currentPos).Contains(CurrentTarget) && !CurrentTarget.Equals(wumpusLocation)) { 
                return CurrentTarget;
            }

            // 2. Tell the KB new facts
            _kb.Tell(new VisitedFact(currentPos));

            if (!_sensedCells.Contains(currentPos)) {
                var effectivePercept = percept;

                if(wumpusDead && !percept.Stench) {
                    effectivePercept = new Percept(
                        true,
                        percept.Breeze,
                        percept.Glitter,
                        percept.Bump,
                        percept.Scream
                    );
                }

                _kb.Tell(new SensorFact(currentPos, effectivePercept));
                _sensedCells.Add(currentPos);
            }

            _visitedCells.Add(currentPos);
            Logger.Log($"[BRAIN] Added current position and sensors to Knowledge Base");

            for (int y = 1; y <= _grid.Size; y++) {
                for (int x = 1; x <= _grid.Size; x++) {
                    IsSafe(new Coordinate(x, y));
                }
            }

            // 3. Build logs with KB queries
            var visitedSb = new StringBuilder();
            var safeUnvisitedSB = new StringBuilder();

            foreach (var visited in _visitedCells) {
                visitedSb.Append(visited).Append(" ");
            }

            foreach (var safe in _knownSafeCells) {
                if(!_visitedCells.Contains(safe))
                    safeUnvisitedSB.Append(safe).Append(" ");
            }


            Logger.Log($"[BRAIN] Current safe visiteds: {visitedSb}");
            Logger.Log($"[BRAIN] Current safe unvisiteds: {safeUnvisitedSB}");

            // 4. Select where we want to go next
            Coordinate? nextTarget = SelectNextTarget(currentPos, state);

            if (nextTarget != null) {
                // 1. Target is an immediate safe neighbor
                if (_grid.GetNeighbors(currentPos).Contains(nextTarget.Value)) {
                    CurrentTarget = nextTarget.Value;
                    return CurrentTarget;
                }

                // 2. Global target was selected, find a safe path through visited nodes
                List<Coordinate> safePath = FindSafePath(currentPos, nextTarget.Value);

                if (safePath != null && safePath.Count > 1) {
                    // safePath[0] is currentPos. Enqueue intermediate steps:
                    for (int i = 1; i < safePath.Count; i++) {
                        _actionPlan.Enqueue(safePath[i]);
                    }

                    // Append final destination step to the end of the queue
                    _actionPlan.Enqueue(nextTarget.Value);

                    // Dequeue and return the very first step right now
                    CurrentTarget = _actionPlan.Dequeue();
                    return CurrentTarget;
                }
            }

            return null;
        }

        private Coordinate? SelectNextTarget(Coordinate currentPos, AgentState state) {
            // 1. Check neighbors
            Logger.Log("[BRAIN] Checking for safe immediate neigbhor(s)");
            var adjacentNeighbors = _grid.GetNeighbors(currentPos);
            var validAdjacent = new List<Coordinate>();

            foreach (var neighbor in adjacentNeighbors) {
                if(IsSafe(neighbor) && !IsVisited(neighbor)) {
                    validAdjacent.Add(neighbor);
                }
            }

            if (validAdjacent.Count > 0) {
                Logger.Log($"[BRAIN] Found {validAdjacent.Count} safe unvisited immediate neighbor(s)");
                int rng = _random.Next(validAdjacent.Count);

                Logger.Log($"[BRAIN] Picked randomly: {validAdjacent[rng]}");
                return validAdjacent[rng];
            }

            // 2. Backtracking
            Logger.Log("[BRAIN] Found no safe immediate neighbor(s), checking globally for safe unvisited nodes");

            var globalSafeUnvisited = new List<Coordinate>();

            foreach (var cell in _knownSafeCells) {
                if (!IsVisited(cell)) {
                    globalSafeUnvisited.Add(cell);
                }
            }

            if (globalSafeUnvisited.Count > 0) {
                Logger.Log($"[BRAIN] Found {globalSafeUnvisited.Count} global safe unvisited nodes");
                int rng = _random.Next(globalSafeUnvisited.Count);
                Logger.Log($"[BRAIN] Picked randomly: {globalSafeUnvisited[rng]}");
                return globalSafeUnvisited[rng];
            }


            // 3. Try to determine exact Wumpus location
            if(!wumpusFound && !wumpusDead) {
                Logger.Log("[BRAIN] Found no safe unvisited nodes in the Knowledge Base. Checking KB if Wumpus location can be deduced");

                Coordinate? discoveredWumpus = FindWumpus();
                if(discoveredWumpus != null) {
                    wumpusFound = true;
                    wumpusLocation = discoveredWumpus;

                    Logger.Log($"[BRAIN] Re-evaluating safe cells...");
                    var freshlyClearedCells = new List<Coordinate>();
                    for(int y = 1; y <= _grid.Size; y++) {
                        for (int x = 1; x <= _grid.Size; x++) {
                            var cell = new Coordinate(x, y);
                            if (!IsVisited(cell) && IsSafe(cell)) {
                                freshlyClearedCells.Add(cell);
                            }
                        }
                    }

                    if (freshlyClearedCells.Count > 0) {
                        Logger.Log($"[BRAIN] Found {freshlyClearedCells.Count} new safe cells");
                        int rng = _random.Next(freshlyClearedCells.Count);
                        
                        Logger.Log($"[BRAIN] Randomly chose: {freshlyClearedCells[rng]}");
                        return freshlyClearedCells[rng];
                    }

                    Logger.Log("[BRAIN] Wumpus found, but no new safe nodes");  
                }
            }


            // 4. If we have wumpus location and have exhausted all safe nodes, shoot the wumpus
            if(state.HasArrow && wumpusFound && wumpusLocation.HasValue) {

                Logger.Log($"[BRAIN] Agent has arrow and knows Wumpus location, attempting to kill Wumpus");

                // If we are already next to Wumpus, return the position
                if (_grid.GetNeighbors(currentPos).Contains(wumpusLocation.Value)) {
                    Logger.Log($"[BRAIN] Adjacent to Wumpus, firing arrow into: {wumpusLocation.Value}");
                    //state.ShootArrow();
                    intentToShoot = true;
                    return wumpusLocation.Value;
                }

                // Otherwise find valid cell to shoot from
                var allShootingPositions = _grid.GetNeighbors(wumpusLocation.Value);
                var validShootingPositions = new List<Coordinate>();
                foreach (var pos in allShootingPositions) {
                    if(_visitedCells.Contains(pos)) {
                        //Logger.Log($"[BRAIN] Found safe firing position: {pos}, navigating towards it");
                        //return pos;
                        validShootingPositions.Add(pos);
                    }
                }

                if (validShootingPositions.Count > 0) {
                    var rng = _random.Next(validShootingPositions.Count);
                    Coordinate chosenFiringPos = validShootingPositions[rng];
                    Logger.Log($"[BRAIN] Randomly chose shooting position: {chosenFiringPos}");

                    // If we are already standing on the chosen position, shoot the Wumpus!
                    if (currentPos.Equals(chosenFiringPos)) {
                        intentToShoot = true; // <--- PLACE 2: Confirmed action is a shot
                        return wumpusLocation.Value;
                    }

                    // If we are not there yet, find a path to walk there (DO NOT set intentToShoot)
                    List<Coordinate> safePath = FindSafePath(currentPos, chosenFiringPos);
                    if (safePath != null && safePath.Count > 1) {
                        for (int i = 1; i < safePath.Count; i++) {
                            _actionPlan.Enqueue(safePath[i]);
                        }
                        _actionPlan.Enqueue(chosenFiringPos);

                        CurrentTarget = _actionPlan.Dequeue();
                        return CurrentTarget; // Safe walk path step
                    }

                }
            }






            // 5. If we dont have exact wumpus location but have potential coordinates and exhausted all safe nodes, choose one potential area randomly and shoot there
            if (state.HasArrow && !wumpusFound && !wumpusDead) {
                Logger.Log($"[BRAIN] No safe nodes left and agent doesn't know exact location of Wumpus");
                var potentialSpots = new HashSet<Coordinate>();

                // 1. Find all visited locations where we perceived a stench
                var stenchLocations = new List<Coordinate>();
                foreach (var visited in _visitedCells) {
                    var stenchQuery = new Symbol($"S_{visited.X}_{visited.Y}");

                    if (_kb.Ask(stenchQuery))
                        stenchLocations.Add(visited);
                }

                // 2. Get the unvisited neighbors of those stench locatiosn
                foreach (var stenchPos in stenchLocations) {
                    var neighbors = _grid.GetNeighbors(stenchPos);

                    foreach (var neighbor in neighbors) {
                        if (!_visitedCells.Contains(neighbor))
                            potentialSpots.Add(neighbor);
                    }
                }

                var sb = new StringBuilder();
                sb.Append($"[BRAIN] Potential Wumpus spots based on stenches: ");
                foreach (var spot in potentialSpots) {
                    sb.Append(spot).Append(" ");
                }

                Logger.Log(sb.ToString());

                if (potentialSpots.Count > 0) {

                    intentToShoot = true;
                    var spotsList = potentialSpots.ToList();
                    int rng = _random.Next(spotsList.Count);
                    Coordinate guessedTarget = spotsList[rng];

                    Logger.Log($"[BRAIN] Randomly chose potential Wumpus location: {guessedTarget}");

                    // If the random location is a neighbor, return it to shoot
                    if (_grid.GetNeighbors(currentPos).Contains(guessedTarget)) {
                        CurrentTarget = guessedTarget;
                        //state.ShootArrow();

                        return CurrentTarget;
                    }

                    // If the agent is far, navigate to it
                    var shootingPositions = _grid.GetNeighbors(guessedTarget);
                    var validShootingPositions = shootingPositions
                                                .Where(pos => _visitedCells.Contains(pos))
                                                .ToList();

                    sb = new StringBuilder();
                    sb.Append($"[BRAIN] Potential shooting positions: ");
                    foreach (var valid in validShootingPositions) {
                        sb.Append(valid).Append(" ");
                    }

                    Logger.Log(sb.ToString());

                    if (validShootingPositions.Count > 0 ) {
                        int posRng = _random.Next(validShootingPositions.Count);
                        Coordinate chosenFiringPos = validShootingPositions[posRng];

                        Logger.Log($"[BRAIN] Randomly chose position: {chosenFiringPos}");

                        if (currentPos.Equals(chosenFiringPos)) {
                            CurrentTarget = guessedTarget;
                            //state.ShootArrow();
                            return CurrentTarget;
                        }

                        List<Coordinate> safePath = FindSafePath(currentPos, chosenFiringPos);
                        if (safePath != null && safePath.Count > 1) {
                            for (int i = 1; i < safePath.Count; i++) {
                                _actionPlan.Enqueue(safePath[i]);
                            }

                            _actionPlan.Enqueue(chosenFiringPos);

                            CurrentTarget = _actionPlan.Dequeue();
                            return CurrentTarget;
                        }
                    }
                }


            }




            // 6. Randomly try one of the potential pit areas
            Logger.Log("[BRAIN] Exhausted all safe options. Trying luck with potential pit areas");

            var riskCandidates = new HashSet<Coordinate>();

            foreach (var visited in _visitedCells) {
                foreach (var neighbor in _grid.GetNeighbors(visited)) {
                    if (!IsVisited(neighbor)) {
                        bool isWumpusSafe = false;

                        if (wumpusDead) {
                            isWumpusSafe = true;
                        } else if (wumpusFound && wumpusLocation.HasValue) {
                            isWumpusSafe = !neighbor.Equals(wumpusLocation.Value);
                        } else {
                            var noWumpusQuery = new Negation(new Symbol($"W_{neighbor.X}_{neighbor.Y}"));
                            isWumpusSafe = _kb.Ask(noWumpusQuery);
                        }

                        if (isWumpusSafe) {
                            riskCandidates.Add(neighbor);
                        }
                    }
                }
            }

            if (riskCandidates.Count > 0) {
                var candidatesList = riskCandidates.ToList();
                int rng = _random.Next(candidatesList.Count);
                Coordinate chosenRiskTile = candidatesList[rng];

                Logger.Log($"[BRAIN] Gambling on a safe tile: {chosenRiskTile}");

                var noPitFact = new Negation(new Symbol($"P_{chosenRiskTile.X}_{chosenRiskTile.Y}"));
                _kb.Tell(noPitFact);

                _knownSafeCells.Add(chosenRiskTile);

                return chosenRiskTile;
            }


            Logger.Log("[BRAIN] Found no safe nodes or valid further actions");
            return null;
        }



        public void HandleMissedShot(Coordinate target) {
            var noWumpusFact = new Negation(new Symbol($"W_{target.X}_{target.Y}"));
            _kb.Tell(noWumpusFact);

            Logger.Log($"[BRAIN] No scream heard after shooting at {target}, there is no Wumpus there");
        }

        private Coordinate? FindWumpus() {
            List<HashSet<Coordinate>> stenchNeighborhoods = new List<HashSet<Coordinate>>();

            // 1. Find all locations where we found a stench
            foreach (var visited in _visitedCells) {
                var stenchQuery = new Symbol($"S_{visited.X}_{visited.Y}");

                if (_kb.Ask(stenchQuery)) {
                    var possibleWumpusLocations = new HashSet<Coordinate>();

                    foreach (var neighbor in _grid.GetNeighbors(visited)) {
                        if (!_visitedCells.Contains(neighbor)) {
                            var noWumpusQuery = new Negation(new Symbol($"W_{neighbor.X}_{neighbor.Y}"));
                            if (!_kb.Ask(noWumpusQuery))
                                possibleWumpusLocations.Add(neighbor);
                        }
                    }

                    if (possibleWumpusLocations.Count > 0) {
                        stenchNeighborhoods.Add(possibleWumpusLocations);
                    }
                }
            }

            // 2. If we haven't found any stenches yet, we can't find Wumpus
            if (stenchNeighborhoods.Count == 0)
                return null;

            // 3. Find the common intersection across all recorded stenches
            HashSet<Coordinate> intersection = new HashSet<Coordinate>(stenchNeighborhoods[0]);
            for (int i = 1; i < stenchNeighborhoods.Count; i++) {
                intersection.IntersectWith(stenchNeighborhoods[i]);
            }

            // 4. If one coordinate overlaps all stenches, it is Wumpus
            if (intersection.Count == 1) {
                Coordinate trueWumpus = intersection.First();
                Logger.Log($"[BRAIN] Deduction successful: Wumpus must be at {trueWumpus}");
                return trueWumpus;
            }

            return null;
        }

        private bool IsSafe(Coordinate cord) {
            if (_knownSafeCells.Contains(cord))
                return true;

            var noPitQuery = new Negation(new Symbol($"P_{cord.X}_{cord.Y}"));
            bool noPit = _kb.Ask(noPitQuery);

            if (!noPit)
                return false;

            bool noWumpus = false;
            if (wumpusDead) {
                noWumpus = true;
            } else if (wumpusFound && wumpusLocation.HasValue) {
                noWumpus = !cord.Equals(wumpusLocation.Value);
            } else {
                var noWumpusQuery = new Negation(new Symbol($"W_{cord.X}_{cord.Y}"));
                noWumpus = _kb.Ask(noWumpusQuery);
            }

            bool safe = noPit && noWumpus;

            if (safe) {
                _knownSafeCells.Add(cord);
            }

            return safe;
        }

        private bool IsVisited(Coordinate cord) {
            return _visitedCells.Contains(cord);
        }

        private List<Coordinate> FindSafePath(Coordinate start, Coordinate target) {
            var queue = new Queue<List<Coordinate>>();
            var visited = new HashSet<Coordinate>();

            queue.Enqueue(new List<Coordinate> { start });
            visited.Add(start);

            while (queue.Count > 0) {
                var path = queue.Dequeue();
                var current = path.Last();

                if (current.Equals(target))
                    return path;

                foreach (var neighbor in _grid.GetNeighbors(current)) {
                    if(!visited.Contains(neighbor) && (IsVisited(neighbor) || neighbor.Equals(target))) {
                        visited.Add(neighbor);
                        var newPath = new List<Coordinate>(path);

                        newPath.Add(neighbor);
                        queue.Enqueue(newPath);
                    }
                }
            }

            return null;
        }
    }
}