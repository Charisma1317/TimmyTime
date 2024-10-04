using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class EditSubMenu : UIComponent
{
    private int currentPropIndex = 0;

    private int propsCount = 0;

    private int propsCounter = 0;

    private bool _typing = false;

    private Dictionary<int, Bounds2> _propValues = new Dictionary<int, Bounds2>();

    private JObject _originalJObject;
    private JObject _jObject;
    private GameObject _obj;

    public EditSubMenu(Vector2 position, Vector2 size, Color color) : base(position, size, color)
    {
    }

    private void CalculateTotalPropsCount(JObject start)
    {
        // recursively calculate the total number of properties
        foreach (var prop in start.Properties())
        {
            if (prop.Value is JObject jObject)
            {
                CalculateTotalPropsCount(jObject);
            }
            else
            {
                propsCount++;
            }
        }
    }

    private JProperty GetPropertyOnCursor()
    {
        var i = 0;
        return _getPropertyOnIndex(_jObject, ref i, currentPropIndex);
    }

    private JProperty _getPropertyOnIndex(JObject start, ref int i, int index)
    {
        // recursively calculate the total number of properties
        foreach (var prop in start.Properties())
        {
            if (prop.Value is JObject jObject)
            {
                var p = _getPropertyOnIndex(jObject, ref i, index);
                if (p != null) return p;
            }
            else
            {
                if (i == index) return prop;
                i++;
            }
        }

        return null;
    }

    private void FillBlankProperties(JObject start, ref int i)
    {
        foreach (var prop in start.Properties())
        {
            if (prop.Value is JObject jObject)
            {
                FillBlankProperties(jObject, ref i);
            }
            else
            {
                if (prop.Value is JValue jValue)
                {
                    var j = 0;
                    var originalProp = _getPropertyOnIndex(_originalJObject, ref j, i);
                    if (originalProp == null) return;

                    var originalValue = originalProp.Value as JValue;

                    if (jValue.Value.GetType() != originalValue.Value.GetType())
                    {
                        // attempt to convert the value to the original type
                        try
                        {
                            var convertedValue = Convert.ChangeType(jValue.Value, originalValue.Value.GetType());
                            jValue.Value = convertedValue;
                        }
                        catch (Exception _)
                        {
                            jValue.Value = originalValue.Value;
                        }
                    }
                }

                i++;
            }
        }
    }

    private void SavePrefabParams(JObject json)
    {
        // update prefab arguments
        var currParams = PrefabManager.PrefabArguments[_obj];
        var newParams = new object[currParams.Length];

        int i = 0;
        foreach (var prop in json.Properties())
        {
            var propTypeStr = prop.Name.Split("$")[1];

            var propType = Type.GetType(propTypeStr);

            Debug.Assert(propType != null, nameof(propType) + " != null");

            var propValue = prop.Value.ToObject(propType);
            newParams[i++] = propValue;
        }

        PrefabManager.PrefabArguments[_obj] = newParams;

        // update object to reflect new prefab arguments
        _obj = PrefabManager.UpdateObject(_obj);

        // get edit tool
        var editTool = (EditTool)GameManager.MainCamera.GetComponent<EditorCamera>().State.ToolProps;
        editTool.Object = _obj;

        // strip components
        var components = _obj.GetComponents();
        foreach (var component in components)
        {
            if (component is Transform || component is SpriteRenderer) continue;
            _obj.RemoveComponent(component);
        }

        JsonLevelWriter.ConvertLevelToJson(LevelManager.CurrentLevel);
    }

    public override void Draw()
    {
        var editorCameraObj = GameManager.MainCamera;
        if (editorCameraObj == null) return;

        if (!editorCameraObj.HasComponent<EditorCamera>()) return;

        var editorCamera = editorCameraObj.GetComponent<EditorCamera>();
        if (editorCamera.State.Tool != EditorTool.Edit) return;

        var editTool = (EditTool)editorCamera.State.ToolProps;
        if (editTool == null) return;

        var obj = editTool.Object;
        if (obj == null) return;

        if (_obj != obj)
        {
            _originalJObject = null;
            _jObject = null;
        }

        ParentScreen.DisabledKeyboardInput = _typing;

        if (!PrefabManager.PrefabArguments.ContainsKey(obj)) return;

        // draw the object's main prefab name
        var name = obj.OriginalPrefab;

        _obj = obj;

        // center the name
        var size = Bounds.Size;

        var stringPos = new Vector2(Position.X + size.X / 2, Position.Y);
        Engine.DrawString(name, stringPos, Color, Game.MediumSmallFont, TextAlignment.Center);
        Engine.DrawString("Arrow keys to edit (pos=" + currentPropIndex + ")", stringPos + new Vector2(0, 23), Color,
            Game.SmallFont, TextAlignment.Center);
        Engine.DrawString("Space to append, Delete to Replace (count=" + propsCount + ")",
            stringPos + new Vector2(0, 35), Color,
            Game.SmallFont, TextAlignment.Center);

        if (_jObject == null)
        {
            // get the object's properties
            var props = PrefabManager.GetPrefabParams(obj);
            if (props == null) return;

            var json = JsonConvert.SerializeObject(props);

            // the params are gonna convert to something like this
            /*
             {
                "Position$Vector2": {
                    "X": 320.0,
                    "Y": 128.0
                },
                "Length$System.Int32": 4,
                "Right$System.Boolean": false
            }
             */

            // we want to convert it to something like this
            /*
                Position: Vector2(320.0, 128.0)
                Length: 4
                Right: false
            */

            // convert to JObject to make it easier to loop through and add adjustable params
            _jObject = JObject.Parse(json);
            _originalJObject = _jObject.DeepClone() as JObject;
        }

        var jObject = _jObject;

        propsCount = 0;

        // calculate the total number of properties
        CalculateTotalPropsCount(jObject);

        // loop through each property
        var startY = stringPos.Y + Game.MediumSmallFont.PointSize + 32;
        var y = startY;

        propsCounter = 0;
        foreach (var prop in jObject.Properties())
        {
            y = DrawProperty(prop, new Vector2(Position.X, y));
        }
        // {"Position$Vector2":{"X":320.0,"Y":384.0}}

        // draw the current (sub-)property at cursor
        // calculate the y position of the current property
        if (!_propValues.TryGetValue(currentPropIndex, out var b)) return;
        // draw the current property
        Engine.DrawRectEmpty(b, _typing ? Color.Green : Color.White);

        _propValues.Clear();

        // if the current prop is a part of a vector2, draw a point at it's position
        var currentProp = GetPropertyOnCursor();
        if (currentProp == null) return;

        if (currentProp.Parent != null && currentProp.Parent.Parent != null)
        {
            var parent = currentProp.Parent.Parent;
            if (parent is JProperty prop)
            {
                bool isVector2 = false;

                if (prop.Name.Contains("$"))
                {
                    isVector2 = prop.Name.Split("$")[1] == "Vector2";
                }
                else
                {
                    // try to convert the value to a vector2
                    try
                    {
                        var v = prop.Value.ToObject<Vector2>();
                        isVector2 = true;
                    }
                    catch (Exception _)
                    {
                        // ignored
                    }
                }


                if (!isVector2) return;

                if (prop.Value["X"].ToString() == "" || prop.Value["Y"].ToString() == "") return;

                var x1 = 0f;
                var y1 = 0f;

                try
                {
                    x1 = (float)prop.Value["X"];
                    y1 = (float)prop.Value["Y"];
                }
                catch (Exception _)
                {
                    // ignored
                }


                var pos = new Vector2(x1, y1);

                var localPos = new Vector2(pos.X - editorCameraObj.Transform.Position.X,
                    pos.Y - editorCameraObj.Transform.Position.Y);

                var bounds = new Bounds2(localPos, new Vector2(5, 5));

                Engine.DrawRectSolid(bounds, Color.Green);
            }
        }
    }

    public override void OnKeyEvent(KeyEvent e)
    {
        if (e.Type != KeyEventType.Down) return;

        if (e.Key == Key.Space)
        {
            _typing = !_typing;

            if (!_typing)
            {
                var i = 0;
                FillBlankProperties(_jObject, ref i);
                SavePrefabParams(_jObject);
            }
            else
            {
                var prop = GetPropertyOnCursor();
                if (prop == null) return;
                if (prop.Value is JValue jValue && jValue.Value.ToString() == "0") jValue.Value = "";
            }

            return;
        }

        if (e.Key == Key.Delete && !_typing)
        {
            _typing = true;

            var prop = GetPropertyOnCursor();
            if (prop == null) return;

            prop.Value = "";
            return;
        }

        if (_typing)
        {
            var prop = GetPropertyOnCursor();
            if (prop == null) return;

            if (!(prop.Value is JValue jValue)) return;

            if (e.Key == Key.Backspace)
            {
                var str = jValue.Value.ToString();
                if (str.Length > 0)
                {
                    str = str.Substring(0, str.Length - 1);
                    jValue.Value = str;
                }

                return;
            }

            if (e.Key == Key.Return)
            {
                _typing = false;

                var i = 0;
                FillBlankProperties(_jObject, ref i);
                SavePrefabParams(_jObject);
                return;
            }

            Debug.Assert(e.Key != null, "e.Key != null");

            var c = (char)e.Key;
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            {
                var str = jValue.Value.ToString();
                str += c;
                jValue.Value = str;
            }

            return;
        }

        if (e.Key == Key.Down) currentPropIndex++;
        if (e.Key == Key.Up) currentPropIndex--;

        if (currentPropIndex < 0) currentPropIndex = propsCount - 1;
        if (currentPropIndex >= propsCount) currentPropIndex = 0;
    }

    private float DrawProperty(JProperty property, Vector2 titlePos)
    {
        string propName;

        // the property name is in the format of "PropName$PropType"
        propName = property.Name.Contains("$")
            ? property.Name.Split("$")[0]
            :
            // the property name is in the format of "PropName"
            property.Name;

        var propValue = property.Value;

        // if the propValue is a JObject, it means it's another serialized object
        // otherwise the type is likely a primitive type

        // draw the property name
        var bounds = Engine.DrawString(propName, titlePos, Color, Game.SmallFont);
        var valuePos = new Vector2(bounds.Max.X + 5f, titlePos.Y);

        // if it's a primitive type, draw the value
        if (propValue is JValue jValue)
        {
            var v = Engine.DrawString(jValue.Value.ToString(), valuePos, Color, Game.SmallFont);
            if (v.Size.Y == 0) v.Size = new Vector2(5f, Game.SmallFont.PointSize + 2f);

            _propValues.Add(propsCounter++, v);

            return titlePos.Y + Game.SmallFont.PointSize + 5;
        }

        if (propValue is JObject jObjectValue)
        {
            return DrawJObject(jObjectValue, valuePos);
        }

        return titlePos.Y;
    }

    private float DrawJObject(JObject jObject, Vector2 pos)
    {
        var y = pos.Y;
        foreach (var prop in jObject.Properties())
        {
            y = DrawProperty(prop, new Vector2(pos.X, y));
        }

        return y;
    }
}