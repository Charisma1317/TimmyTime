using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

internal class LevelManager
{

    public static Level CurrentLevel { get; set; }

    public static void CacheLevels()
    {
        JsonLevelReader.CacheLevels();
    }

    public static void Clear()
    {
        JsonLevelReader.ClearCache();
    }

    public static void ReloadLevel(int level)
    {
        JsonLevelReader.DeleteLevel(level);
        Engine.ReloadAssets();
        JsonLevelReader.CacheLevel(level);
        LoadLevel(level);
    }

    public static void EditLevel(int level)
    {

        GameManager.Clear();

        var cam = new GameObject
        {
            Transform =
            {
                Position = new Vector2(0, 320),
                Size = new Vector2(Game.Resolution.X, Game.Resolution.Y)
            }
        };

        cam.AddComponent<EditorCamera>();
        GameManager.Load(cam);

        CurrentLevel = JsonLevelReader.ReadLevel(level);
        CurrentLevel.EditMode();

        GameManager.Load(CurrentLevel);
        CurrentLevel.Load();
    }


    public static void LoadLevel(int level)
    {
        // while music is fading out, load the level & fade in new music
        // and have a black screen to transition between levels

        // load black screen
        Physics.setJump();
        Physics.setSpeed();
        var a = new Thread(() => Engine.StopMusic(0.5f));
        ScreenManager.FadeScreen(true, 500);
        a.Start();

        var l = JsonLevelReader.ReadLevel(level);

        new Thread(() =>
        {
            a.Join();

            // wait for first fade to finish
            while (ScreenManager.GetFadeQueueCount() > 0) ;

            // load level
            Game.LevelTimer = 0f;
            GameManager.Clear();

            // load camera
            var cam = new GameObject
            {
                Transform =
                {
                    Position = new Vector2(0, 320),
                    Size = new Vector2(Game.Resolution.X, Game.Resolution.Y)
                }
            };

            cam.AddComponent<Camera>();

            var b = new Thread(() => Engine.PlayMusic(Game.LevelMusic[level], fadeTime: 0.5f));
            ScreenManager.FadeScreen(false, 500);
            b.Start();

            Thread.Sleep(500);

            GameManager.Load(cam);
            GameManager.Load(l);
            l.Load();

            CurrentLevel = l;
        }).Start();
    }

    public static void LoadNextLevel()
    {
        var newScore = 15 * (WinTrigger.TimeInLevel / Game.LevelTimer);
        Game.Score += (int)Math.Round(newScore);
        LoadLevel(CurrentLevel.LevelNumber + 1);
    }

    public static void LoadPreviousLevel()
    {
        if (!HasPreviousLevel()) return;
        LoadLevel(CurrentLevel.LevelNumber - 1);
    }

    public static bool HasNextLevel()
    {
        return JsonLevelReader.HasLevel(CurrentLevel.LevelNumber + 1);
    }

    public static bool HasPreviousLevel()
    {
        return JsonLevelReader.HasLevel(CurrentLevel.LevelNumber - 1);
    }
}