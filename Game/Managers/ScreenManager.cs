using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

internal class ScreenManager
{
    private static readonly int MaxScreens = 3;
    public static List<Screen> Screens = new List<Screen>();
    public static LinkedList<Screen> PreviousScreens = new LinkedList<Screen>(); // max 3

    public static Screen BlackScreen;

    private static Queue<(bool, long)> _fadeQueue = new Queue<(bool, long)>();
    public static Thread FadeThread { get; private set; }

    public static int GetFadeQueueCount()
    {
        return _fadeQueue.Count + (FadeThread != null ? 1 : 0);
    }

    public static void FadeScreen(bool fadeIn, long time)
    {
        _fadeQueue.Enqueue((fadeIn, time));
    }

    private static void _fadeScreen(bool fadeIn, long time)
    {
        var component = BlackScreen.GetComponent<Rectangle>();

        if (fadeIn)
            component.SetOpacity(0.0f);
        
        AddScreen(BlackScreen);

        FadeThread = new Thread(() =>
        {
            var watch = new Stopwatch();

            watch.Start();

            while (watch.ElapsedMilliseconds <= time + 5L)
            {
                if (fadeIn)
                    component.SetOpacity((watch.ElapsedMilliseconds / (time + 5f)));
                else
                    component.SetOpacity(1.0f - (watch.ElapsedMilliseconds / (time + 5f)));
            }

            watch.Stop();
            Remove(BlackScreen);
            FadeThread = null;
        });

        FadeThread.Start();
    }

    public static void AddScreen(Screen screen)
    {
        Screens.Insert(0, screen);
    }

    public static void RemoveScreen()
    {
        _removeScreen(true);
    }

    public static void Remove(Screen screen)
    {
        if (!Screens.Contains(screen)) return;

        screen.ResetComponentState();
        Screens.Remove(screen);
    }

    private static void _removeScreen(bool save)
    {
        if (save)
        {
            if (PreviousScreens.Count > MaxScreens) PreviousScreens.RemoveLast();

            PreviousScreens.AddFirst(Screens[0]);
        }

        Screens[0].ResetComponentState();
        Screens.RemoveAt(0);
    }

    public static void SwitchScreen(Screen screen)
    {
        RemoveScreen();
        Screens.Insert(0, screen);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public static void SetScreen(Screen screen)
    {
        Clear();

        screen.ResetComponentState();

        Screens.Add(screen);
    }

    public static void GoBack()
    {
        if (PreviousScreens.Count < 1) return;

        Screens[0].ResetComponentState();

        Screens[0] = PreviousScreens.First.Value;

        Screens[0].ResetComponentState();

        PreviousScreens.RemoveFirst();
    }

    public static void Draw()
    {
        // fade screen
        if (_fadeQueue.Count > 0 && FadeThread == null)
        {
            var (fadeIn, time) = _fadeQueue.Dequeue();
            _fadeScreen(fadeIn, time);
        }

        var screens = new List<Screen>(Screens);
        screens.Reverse();
        foreach (var screen in screens) screen.Draw();
    }

    public static void Update()
    {
        var screens = new List<Screen>(Screens);
        screens.Reverse();
        foreach (var screen in screens) screen.Update();
    }

    public static void Clear()
    {
        foreach (var screen in Screens) screen.ResetComponentState();

        Screens.Clear();
        PreviousScreens.Clear();
    }
}