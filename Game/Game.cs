using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

internal class Game
{
    public enum GameResult
    {
        Win,
        Defeat,
        Unknown
    }

    public class ScoreboardData
    {
        public ScoreboardEntry[] Entries { get; set; }
    }

    public class ScoreboardEntry
    {
        public string Name { get; set; }
        public int Score { get; set; }
    }

    public static readonly string Title = "Minimalist Game Framework";
    public static readonly Vector2 Resolution = new Vector2(960, 540);

    public static readonly Font SmallFont = Engine.LoadFont("Retro Gaming.ttf", 11);
    public static readonly Font MediumSmallFont = Engine.LoadFont("Retro Gaming.ttf", 20);
    public static readonly Font MediumFont = Engine.LoadFont("Retro Gaming.ttf", 30);
    public static readonly Font BigFont = Engine.LoadFont("Retro Gaming.ttf", 50);

    public static Music MenuMusic = Engine.LoadMusic("menu.mp3");

    public static Music[] LevelMusic = new[]
    {
        Engine.LoadMusic("menu.mp3"), // debug
        Engine.LoadMusic("heros_journey.mp3"), // level 1
        Engine.LoadMusic("fight.mp3"), // level 2
        Engine.LoadMusic("turning_red_inspo.mp3"), // level 3
        Engine.LoadMusic("arch_nemisis.mp3"), // level 4
    };


    public static readonly string AppFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimmyTime");

    public static GameObject MainPlayer;
    private readonly Screen _creditScreen = new Screen();

    private readonly Screen _editorScreen = new Screen();
    private readonly Screen _endScreen = new Screen();

    private readonly Screen _gameEditorScreen = new Screen();
    private readonly Screen _gameScreen = new Screen();
    private readonly Screen _instructionScreen = new Screen();
    private readonly Screen _scoreboardScreen = new Screen();

    private readonly Screen _startScreen = new Screen();

    private int frames = 0;
    private int framesShown = 0;
    private long lastUpdate;

    private static bool visible = true;
    public static ScreenState CurrentScreenState = ScreenState.Start;

    public enum ScreenState
    {
        Start,
        Instructions,
        Leaderboard,
        Credits,
        Game,
        Editor,
        End
    }

    public static float LevelTimer { get; set; } = 0f;
    public static int Score { get; set; } = 0;

    private ScoreboardData _scoreboard = new ScoreboardData
    {
        Entries = Array.Empty<ScoreboardEntry>()
    };


    public Game()
    {
        Instance = this;

        // setup sprite sheet
        var defaultSpriteSheet = Engine.GetAssetPath("spritesheet.json");
        if (File.Exists(defaultSpriteSheet))
        {
            SpriteRenderer.DefaultSpriteSheet = JsonConvert.DeserializeObject<SpriteSheet>(File.ReadAllText(defaultSpriteSheet));
            LoadSpriteSheet(SpriteRenderer.DefaultSpriteSheet);
        }
        else throw new Exception("Default sprite sheet not found");

        RegisterStartScreen();
        RegisterInstructionScreen();
        RegisterScoreboardScreen();
        RegisterCreditScreen();
        RegisterEndScreen();

        RegisterEditorScreen();
        RegisterGameEditorScreen();

        ScreenManager.BlackScreen = new Screen();
        ScreenManager.BlackScreen.Add(new Rectangle(new Vector2(0, 0), Resolution, true, Color.Black));

        PrefabManager.CachePrefabs();
        LevelManager.CacheLevels();

        if (!Directory.Exists(AppFolder)) Directory.CreateDirectory(AppFolder);
        if (!File.Exists(GetPath("scoreboard.json")))
        {
            var json = JsonConvert.SerializeObject(_scoreboard);

            var sw = File.CreateText(GetPath("scoreboard.json"));
            sw.Write(json);
            sw.Close();
        }

        StartScreen();
    }

    public static Game Instance { get; private set; }

    public void StartScreen()
    {
        GameManager.Clear();
        ReloadScores();
        ScreenManager.SetScreen(_startScreen);
        CurrentScreenState = ScreenState.Start;

        Engine.PlayMusic(MenuMusic);
        Score = 0;
        Physics.setJump();
        Physics.setSpeed();
    }


    public void EndScreen()
    {
        Score = 0;
        _gameScreen.Components.Clear();
        LevelManager.CurrentLevel = null;
        GameManager.Clear();
        ScreenManager.SetScreen(_endScreen);
        CurrentScreenState = ScreenState.End;
    }

    public static string GetPath(string file)
    {
        return AppFolder + Path.DirectorySeparatorChar + file;
    }

    public void Update()
    {
        if (Engine.GetKeyHeld(Key.LeftControl) && Engine.GetKeyDown(Key.NumRow0))
        {
            ScreenManager.FadeScreen(true, 500);
            ScreenManager.FadeScreen(false, 500);
        }

        if (CurrentScreenState == ScreenState.Game) LevelTimer += Engine.TimeDelta;

        ScreenManager.Update();
        GameManager.UpdateObjects();

        GameManager.Draw(); // draw all game objects
        ScreenManager.Draw(); // draw UI on top of everything else

        if (LevelManager.CurrentLevel != null)
            LevelManager.CurrentLevel.Draw(); // draw level on top of everything else

        frames++;

        // toggle debug text
        if (Engine.GetKeyUp(Key.Z))
        {
            ToggleDebugText();
            if (!visible) ScreenManager.Remove(_gameScreen);
            else ScreenManager.AddScreen(_gameScreen);
        }

        var now = DateTime.Now.Ticks;
        if (now - lastUpdate >= 10000000)
        {
            framesShown = frames;
            frames = 0;
            lastUpdate = now;
        }
    }

    private void RegisterObjects()
    {
        // default camera
        // reset score for all levels

        LevelManager.LoadLevel(1);

        new Thread(() =>
        {
            while (LevelManager.CurrentLevel == null) ;
            if (visible) RegisterGameScreen();
            ScreenManager.SetScreen(_gameScreen);
        }).Start();
    }

    private void ReloadScores()
    {
        var file = GetPath("scoreboard.json");
        if (!File.Exists(file)) return;

        var json = File.ReadAllText(file);
        _scoreboard = JsonConvert.DeserializeObject<ScoreboardData>(json);
    }

    private void LoadSpriteSheet(SpriteSheet sheet)
    {
        var file = sheet.Meta.Image;
        sheet.Texture = Engine.LoadTexture(file);
    }

    public static Sprite[] GetFrames(SpriteSheet sheet, string prefix)
    {
        var frames = sheet.Frames.Where(f => f.FileName.StartsWith(prefix)).ToArray();
        return frames.Select(f => new Sprite
        {
            SpriteSheet = sheet,
            Bounds = f.Frame.ToBounds(),
            FrameContainer = f
        }).ToArray();
    }

    // SCREENS START -----------------------------------------------------------------------------------------------

    private void RegisterStartScreen()
    {
        var components = new HashSet<UIComponent>();

        var title = Text.TitleText("Timmy Time", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 50));

        var playAction = new Action<ClickEvent>(e =>
        {
            RegisterObjects();
            CurrentScreenState = ScreenState.Game;
        });

        var leaderboardAction = new Action<ClickEvent>(e =>
        {
            ReloadScores();
            ScreenManager.SwitchScreen(_scoreboardScreen);
        });

        var play = Button.GameButton(playAction, "Start", Color.DarkGray, Color.Ivory,
            new Vector2(Resolution.X / 2, 175));
        var instruction = Button.MenuButton(ScreenState.Instructions, _instructionScreen, "Instructions",
            Color.DarkGray, Color.Ivory,
            new Vector2(Resolution.X / 2, 250));
        var leaderboard = Button.GameButton(leaderboardAction, "Leaderboard", Color.DarkGray, Color.Ivory,
            new Vector2(Resolution.X / 2, 325));
        var credit = Button.MenuButton(ScreenState.Credits, _creditScreen, "Credits", Color.DarkGray, Color.Ivory,
            new Vector2(Resolution.X / 2, 400));

        // in the corner add a editor button
        var editor = Button.SmallMenuButton(ScreenState.Editor, _editorScreen, "Editor", Color.DarkGray, Color.Ivory,
            new Vector2(50, Resolution.Y - 30));

        components.Add(title);
        components.Add(play);
        components.Add(instruction);
        components.Add(leaderboard);
        components.Add(credit);
        components.Add(editor);

        _startScreen.Components = components;
        ScreenManager.AddScreen(_startScreen);
    }

    private void RegisterInstructionScreen()
    {
        var components = new HashSet<UIComponent>();

        var title = Text.TitleText("Instructions", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 50));
        var move = Text.InfoText("WASD to Move", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 150));
        var arrow = Text.InfoText("Q to Draw Bow", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 225));
        var arrowKeys = Text.InfoText("Arrow Keys to Aim Bow", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 300));
        var knife = Text.InfoText("E to Use Knife", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 375));

        var back = Button.BackButton("Back", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 480));

        components.Add(title);
        components.Add(move);
        components.Add(arrow);
        components.Add(arrowKeys);
        components.Add(knife);
        components.Add(back);

        _instructionScreen.Components = components;
    }

    private void RegisterScoreboardScreen()
    {
        var components = new HashSet<UIComponent>();

        var title = Text.TitleText("Leaderboard", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 30));
        var back = Button.BackButton("Back", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 480));

        var list = new UpdatingTextList(() =>
        {
            // load the file
            var l = new List<string>();

            foreach (var entry in _scoreboard.Entries)
            {
                l.Add(entry.Name + ": " + entry.Score);
            }

            return l;
        }, new Vector2(270, 150), Color.Ivory);


        components.Add(title);
        components.Add(back);
        components.Add(list);
        _scoreboardScreen.Components = components;
    }

    private void RegisterCreditScreen()
    {
        var components = new HashSet<UIComponent>();

        var title = Text.TitleText("Credits", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 30));

        var devs = Text.InfoText("Devs : Aryan N, Rohit K, Chris M, Timason W", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 140));
        var mentor = Text.InfoText("Mentor : Andrew M", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 210));
        var music = Text.InfoText("Music : Aditi N", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 280));
        var graphics = Text.InfoText("Graphics : Chris M, Maman S, and Arydian* (ItchIO)", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 350));
        var license = Text.SmallerInfoText("* Licensed under CC (Attribution)", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 440));
        var back = Button.BackButton("Back", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 480));

        components.Add(title);
        components.Add(devs);
        components.Add(mentor);
        components.Add(music);
        components.Add(graphics);
        components.Add(license);
        components.Add(back);

        _creditScreen.Components = components;
    }

    private void RegisterGameScreen()
    {
        var components = new HashSet<UIComponent>();

        // the overlay when the game is running
        frames = 0;
        lastUpdate = DateTime.Now.Ticks;

        // player position
        var textList = new UpdatingTextList(() =>
        {
            var list = new List<string>();
            if (MainPlayer != null)
            {
                var player = MainPlayer.GetComponent<Player>();
                if (player == null) return list;

                list.Add("Player: " + MainPlayer.RigidBody.Position);
                list.Add("Player Velocity: " + MainPlayer.RigidBody.Velocity);
                list.Add("Player Acceleration: " + MainPlayer.RigidBody.Acceleration);
                list.Add("Player Grid: " + string.Join(',', LevelManager.CurrentLevel.GetSpaces(MainPlayer)));
                list.Add("Player Arrow Held: " + string.Join(',', player.arrowHeld));
            }

            list.Add("Frames: " + framesShown);

            if (GameManager.MainCamera != null)
                list.AddRange(from o in GameManager.MainCamera.GetComponent<Camera>().RenderedObjects
                              where o.HasComponent<AIEntity>() || o.HasComponent<SimpleMovingEnemy>()
                              select o.GetType().Name + ": " + o.RigidBody.Position);

            list.Add("Keys Down: " + string.Join(',', Engine.KeysHeld));
            list.Add("Mouse: " + string.Join(',', Engine.MouseButtonsHeld));
            list.Add("Health: " + String.Join(',', MainPlayer.GetComponent<Damageable>().Health));

            list.Add("Time: " + Math.Round(LevelTimer, 1) + "s/" + WinTrigger.TimeInLevel + "s");
            list.Add("Score: " + String.Join(',', Score));

            return list;
        }, new Vector2(0, 0), Color.AliceBlue);

        components.Add(textList);
        var Health = new HealthBar(new Vector2(400, 10), new Vector2(200, 20));
        components.Add(Health);
        _gameScreen.Components = components;
    }

    private void RegisterGameEditorScreen()
    {
        var components = new HashSet<UIComponent>();

        var backButton = Button.SmallGameButton((e) =>
        {
            GameManager.Clear();
            ReloadScores();

            LevelManager.Clear();
            LevelManager.CacheLevels();

            ScreenManager.SetScreen(_editorScreen);
            ScreenManager.PreviousScreens.AddFirst(_startScreen);
            CurrentScreenState = ScreenState.Editor;
        }, "Back", Color.DarkGray, Color.Ivory, new Vector2(45, 25));

        var updatingTextList = new UpdatingTextList(() =>
        {
            var list = new List<string>();

            var cam = GameManager.MainCamera;

            if (cam == null) return list;
            if (!cam.HasComponent<EditorCamera>()) return list;

            var editor = cam.GetComponent<EditorCamera>();
            var mousePos = Engine.MousePosition;
            var mouseWorldPos = editor.WorldToScreen(mousePos);
            list.Add("1 = Move");
            list.Add("2 = Draw");
            list.Add("3 = Delete");
            list.Add("4 = Edit");

            list.Add("Mouse: " + mousePos);
            list.Add("Mouse World: " + mouseWorldPos);

            var gridPos = editor.MouseToGrid(mousePos);
            list.Add("Mouse Grid: " + gridPos);
            var editorState = editor.State;

            list.Add("Editor Tool: " + Enum.GetName(typeof(EditorTool), editorState.Tool));
            list.Add("Editor Params: " + editorState.ToolProps.ToString());

            return list;
        }, new Vector2(Resolution.X - 280, Resolution.Y - 105), Color.Black);

        var scrollablePrefabList = new ScrollablePrefabList(new Vector2(Resolution.X - 100, 100), Vector2.Zero, Color.Black);

        var editSubMenu = new EditSubMenu(new Vector2(Resolution.X - 350, 100), new Vector2(150, 250), Color.Black);

        components.Add(scrollablePrefabList);
        components.Add(backButton);
        components.Add(editSubMenu);
        components.Add(updatingTextList);

        _gameEditorScreen.Components = components;
    }

    private void RegisterEditorScreen()
    {
        var components = new HashSet<UIComponent>();

        var title = Text.TitleText("Editor", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 50));

        var buttonGrid = new ButtonGrid(new Vector2(100, 150), new Vector2(Resolution.X - 100, Resolution.Y),
            () =>
            {
                var list = new List<Button>();
                foreach (var (num, name) in JsonLevelReader.GetLevelNames())
                {
                    // create a button for each level
                    var action = new Action<ClickEvent>(e =>
                    {
                        LevelManager.EditLevel(num);
                        ScreenManager.SetScreen(_gameEditorScreen);
                    });

                    var button = Button.GameButton(action, name, Color.DarkGray, Color.Ivory,
                        new Vector2(0, 0));
                    list.Add(button);
                }

                return list;
            });


        var back = Button.BackButton("Back", Color.DarkGray, Color.Ivory, new Vector2(Resolution.X / 2, 420));

        components.Add(title);
        components.Add(buttonGrid);
        components.Add(back);

        _editorScreen.Components = components;
    }

    private void RegisterEndScreen()
    {
        var components = new HashSet<UIComponent>();

        var endingText = UpdatingText.TitleText(() => Enum.GetName(typeof(GameResult), GameManager.GameResult),
            Color.White, new Vector2(Resolution.X / 2, Resolution.Y / 2 - 120));
        var scoreText = UpdatingText.InfoText(() => "Score: " + Score, Color.White,
            new Vector2(Resolution.X / 2, Resolution.Y / 2 - 60));

        var label = Text.SmallerInfoText("Enter your name (blank if no save):", Color.White, Color.Transparent,
            new Vector2(Resolution.X / 2, Resolution.Y / 2));

        var errorText =
            UpdatingText.DebugText(() => "", Color.Red, new Vector2(Resolution.X / 2, Resolution.Y / 2 + 100));

        var textBox = new TextBox(SubmitScore, new Vector2(Resolution.X / 2, Resolution.Y / 2 + 30),
            new Text.TextProperties
            {
                Alignment = TextAlignment.Left,
                Font = BigFont,
                Text = "",
                TextColor = Color.White
            }, Color.Transparent, 3);

        var restartAction = new Action<ClickEvent>(e => { StartScreen(); });
        var restartButton =
            Button.GameButton(restartAction, "Save & Restart", Color.DarkGray, Color.Ivory,
                new Vector2(Resolution.X / 2, 400), textBox);

        components.Add(errorText);
        components.Add(endingText);
        components.Add(scoreText);
        components.Add(label);
        components.Add(textBox);
        components.Add(restartButton);

        _endScreen.Components = components;
        return;

        bool SubmitScore(string name)
        {
            name = name.Trim();
            ReloadScores();

            if (name.Length == 0)
            {
                StartScreen();
                return false;
            }

            // check if the name is already in the scoreboard
            foreach (var entry in _scoreboard.Entries)
            {
                var lineName = entry.Name;

                if (lineName.Equals(name) && lineName.Length > 0)
                {
                    errorText.Text = "Error: Name already in scoreboard!";

                    Task.Run(() =>
                    {
                        Task.Delay(2000).Wait();
                        errorText.Text = "";
                    });

                    return true;
                }
            }

            var newScore = new ScoreboardEntry
            {
                Name = name,
                Score = Score
            };

            var newBoard = new ScoreboardData
            {
                Entries = _scoreboard.Entries.Append(newScore).OrderByDescending(e => e.Score).ToArray()
            };

            var json = JsonConvert.SerializeObject(newBoard);

            var sw = File.CreateText(GetPath("scoreboard.json"));
            sw.Write(json);
            sw.Close();
            return false;
        }
    }

    public void ToggleDebugText()
    {
        visible = !visible;
    }
    // SCREENS END -----------------------------------------------------------------------------------------------
}