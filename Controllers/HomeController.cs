using Microsoft.AspNetCore.Mvc;
using WumpusWorld.Logic;
using WumpusWorld.Logic.Simulation;
using WumpusWorld.Models;

namespace WumpusWorld.Controllers {
    public class HomeController : Controller {
        private static GameSimulation? _currentSim;
        public static int currentTurn = 1;

        private static Coordinate _savedWumpus = new Coordinate(3, 3);
        private static Coordinate _savedGold = new Coordinate(4, 3);
        private static HashSet<Coordinate> _savedPits = new HashSet<Coordinate> {
            new Coordinate(2, 4),
            new Coordinate(4, 1)
        };

        public IActionResult Index() {
            GameViewModel? viewModel = _currentSim != null ? new GameViewModel(_currentSim) : null;

            if(HttpContext.Session.GetString("ShowSlowThinking") == "true") {
                viewModel.ShowSlowThinkingWarning = true;
                HttpContext.Session.Remove("ShowSlowThinking");
            }

            return View(viewModel);
        }

        public static void SetupGame() {
            Logger.Clear();
            KbLogger.Clear();
            currentTurn = 1;

            var grid = new WorldGrid();
            var kb = new KnowledgeBase();
            var brain = new AgentBrain(grid, kb);

            var env = new WorldEnvironment(grid, _savedWumpus, _savedGold, new HashSet<Coordinate>(_savedPits));
            _currentSim = new GameSimulation(env, brain);
        }

        [HttpPost]
        public IActionResult InitializeGame() {
            SetupGame();

            HttpContext.Session.Remove("ShowSlowThinking");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult NextMove() {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (_currentSim == null)
                return RedirectToAction("Index");

            Logger.Log($"[TURN {currentTurn}]");
            KbLogger.Log($"[TURN {currentTurn}]");

            Coordinate? target = _currentSim.ProcessNextMove();

            if (target != null) {
                var action = new AgentAction(ActionType.Move, target.Value);
                _currentSim.ExecuteAction(target, currentTurn);
                currentTurn++;
            }

            stopwatch.Stop();

            if (currentTurn > 5 && stopwatch.Elapsed.TotalSeconds > 5) {
                HttpContext.Session.SetString("ShowSlowThinking", "true");
            } else {
                HttpContext.Session.Remove("ShowSlowThinking");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RandomizeMap() {
            var random = new Random();
            var newPits = new HashSet<Coordinate>();
            var potentialWumpusOrGoldTiles = new List<Coordinate>();

            // 1. Generate pits
            for (int x = 1; x <= 4; x++) {
                for (int y = 1; y <= 4; y++) {
                    if (x == 1 && y == 1) continue;

                    if (random.NextDouble() < 0.20) {
                        newPits.Add(new Coordinate(x, y));
                    } else {
                        potentialWumpusOrGoldTiles.Add(new Coordinate(x, y));
                    }
                }
            }

            // 2. Place Wumpus in a safe tile
            Coordinate newWumpus;
            if (potentialWumpusOrGoldTiles.Any()) {
                int index = random.Next(potentialWumpusOrGoldTiles.Count);
                newWumpus = potentialWumpusOrGoldTiles[index];
                potentialWumpusOrGoldTiles.RemoveAt(index);
            } else {
                newWumpus = new Coordinate(2, 2);
                newPits.Remove(newWumpus);
            }

            // 3. Place gold in a non-pit tile
            Coordinate newGold;
            if (potentialWumpusOrGoldTiles.Any()) {
                int index = random.Next(potentialWumpusOrGoldTiles.Count);
                newGold = potentialWumpusOrGoldTiles[index];
            } else {
                newGold = new Coordinate(3, 3);
                newPits.Remove(newGold);
            }

            // 4. Save the placements
            _savedWumpus = newWumpus;
            _savedGold = newGold;
            _savedPits = newPits;

            SetupGame();
            return RedirectToAction("Index");
        }
    }
}