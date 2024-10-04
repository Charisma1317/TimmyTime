using System;
using System.Collections.Generic;
using System.Text;

internal class ScrollablePrefabList : UIComponent
{
    private List<Button> buttons = new List<Button>();
    public ScrollablePrefabList(Vector2 position, Vector2 size, Color color) : base(position, size, color)
    {
    }

    private void UpdateButtonsList()
    {
        var editorCameraObj = GameManager.MainCamera;
        if (editorCameraObj == null) return;

        if (!editorCameraObj.HasComponent<EditorCamera>()) return;

        var editorCamera = editorCameraObj.GetComponent<EditorCamera>();

        if (PrefabManager.Prefabs.Count <= buttons.Count && editorCamera.State.Tool == EditorTool.Draw) return;

        foreach (var button in buttons)
        {
            if (!ParentScreen.Contains(button)) continue;
            ParentScreen.Remove(button);
        }

        buttons.Clear();

        if (editorCamera.State.Tool != EditorTool.Draw) return;

        var drawTool = (DrawTool)editorCamera.State.ToolProps;

        var prefabs = PrefabManager.Prefabs.Keys;
        var y = Position.Y;

        foreach (var prefab in prefabs)
        {
            var button = new Button((clickEvent) =>
            {
                drawTool.Prefab = prefab;
            }, new Text.TextProperties
            {
                Alignment = TextAlignment.Center,
                Font = Game.SmallFont,
                TextColor = Color.Black,
                Text = prefab,
            }, new Vector2(Position.X, y), Color.White);

            y += button.Bounds.Size.Y + 2;

            buttons.Add(button);
        }
    }

    public override void Draw()
    {
        UpdateButtonsList();

        foreach (var button in buttons)
        {
            if (!ParentScreen.Contains(button))
                ParentScreen.Add(button);
        }
    }
}
