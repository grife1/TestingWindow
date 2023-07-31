#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace TestingWindow
{
    public enum DisplayAs
    {
        /// <summary>
        /// Works with doubles, floats, ints, strings
        /// <br/>
        /// <see href="https://docs.unity3d.com/ScriptReference/EditorGUI.DelayedDoubleField.html"/>
        /// </summary>
        Delayed,
        /// <summary>
        /// Works with enums with [Flags] attribute
        /// </summary>
        Flags,      //Enum
        /// <summary>
        /// Works with strings
        /// </summary>
        TextArea    //String
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestingCommandAttribute : Attribute
    {
        public enum EditorDisplay
        {
            Bounds,
            BoundsInt,
            Color,
            Curve,
            DelayedDouble,
            DelayedFloat,
            DelayedInt,
            DelayedText,
            Double,
            EnumFlags,
            EnumMask,
            Float,
            Gradient,
            Int,
            Long,
            Object,
            Rect,
            RectInt,
            Text,
            TextArea,
            Vector2,
            Vector2Int,
            Vector3,
            Vector3Int,
            Vector4
        }

        private EditorDisplay[] _displayParameters;
        public EditorDisplay[] DisplayParameters { get { return _displayParameters; } }
        private object[] _objects;
        public object[] Objects { get { return _objects; } }
        private string[] _parametersNames;
        public string[] ParametersNames { get { return _parametersNames; } }
        private Dictionary<int, Type> _unityTypes;
        public Dictionary<int, Type> UnityTypes { get { return _unityTypes; } }

        private string[] _names;
        private DisplayAs[] _displayAs;

        public TestingCommandAttribute Init(ParameterInfo[] parameters)
        {
            _displayParameters = new EditorDisplay[parameters.Length];
            _objects = new object[parameters.Length];
            _parametersNames = new string[parameters.Length];
            _unityTypes = new Dictionary<int, Type>();
            bool tryOverride = true;
            if (_names is null)
                tryOverride = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                if (!tryOverride || !TryOverride(parameters[i], i))
                {
                    if (type == typeof(Bounds))
                        _displayParameters[i] = EditorDisplay.Bounds;
                    else if (type == typeof(BoundsInt))
                        _displayParameters[i] = EditorDisplay.BoundsInt;
                    else if (type == typeof(Color))
                        _displayParameters[i] = EditorDisplay.Color;
                    else if (type == typeof(AnimationCurve))
                        _displayParameters[i] = EditorDisplay.Curve;
                    else if (type == typeof(double))
                        _displayParameters[i] = EditorDisplay.Double;
                    else if (type.IsEnum)
                        _displayParameters[i] = EditorDisplay.EnumMask;
                    else if (type == typeof(float))
                        _displayParameters[i] = EditorDisplay.Float;
                    else if (type == typeof(Gradient))
                        _displayParameters[i] = EditorDisplay.Gradient;
                    else if (type == typeof(int))
                        _displayParameters[i] = EditorDisplay.Int;
                    else if (type == typeof(long))
                        _displayParameters[i] = EditorDisplay.Long;
                    else if (type == typeof(Rect))
                        _displayParameters[i] = EditorDisplay.Rect;
                    else if (type == typeof(RectInt))
                        _displayParameters[i] = EditorDisplay.RectInt;
                    else if (type == typeof(string))
                        _displayParameters[i] = EditorDisplay.Text;
                    else if (type == typeof(Vector2))
                        _displayParameters[i] = EditorDisplay.Vector2;
                    else if (type == typeof(Vector2Int))
                        _displayParameters[i] = EditorDisplay.Vector2Int;
                    else if (type == typeof(Vector3))
                        _displayParameters[i] = EditorDisplay.Vector3;
                    else if (type == typeof(Vector3Int))
                        _displayParameters[i] = EditorDisplay.Vector3Int;
                    else if (type == typeof(Vector4))
                        _displayParameters[i] = EditorDisplay.Vector4;
                    else if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                    {
                        _displayParameters[i] = EditorDisplay.Object;
                        _unityTypes.Add(i, type);
                    }
                }
                
                _objects[i] = _displayParameters[i] switch
                {
                    EditorDisplay.Text => string.Empty,
                    EditorDisplay.TextArea => string.Empty,
                    EditorDisplay.DelayedText => string.Empty,
                    EditorDisplay.EnumFlags => Enum.ToObject(type, 0),
                    EditorDisplay.EnumMask => Enum.ToObject(type, 0),
                    EditorDisplay.Object => null,
                    _ => Activator.CreateInstance(type),
                };
                _parametersNames[i] = parameters[i].Name;
            }

            _names = null;
            _displayAs = null;
            return this;
        }

        private bool TryOverride(ParameterInfo parameter, int parameterIndex)
        {
            string name = parameter.Name;
            for (int i = 0; i < _names.Length; i++)
            {
                if (name == _names[i])
                {
                    if (_displayAs.Length <= i)
                        return false;
                    Type type = parameter.ParameterType;
                    switch (_displayAs[i])
                    {
                        case DisplayAs.Delayed:
                            if (type == typeof(double))
                            {
                                _displayParameters[parameterIndex] = EditorDisplay.DelayedDouble;
                                return true;
                            }
                            else if (type == typeof(float))
                            {
                                _displayParameters[parameterIndex] = EditorDisplay.DelayedFloat;
                                return true;
                            }
                            else if (type == typeof(int))
                            {
                                _displayParameters[parameterIndex] = EditorDisplay.DelayedInt;
                                return true;
                            }
                            else if (type == typeof(string))
                            {
                                _displayParameters[parameterIndex] = EditorDisplay.DelayedText;
                                return true;
                            }
                            return false;
                        case DisplayAs.Flags:
                            if (!type.IsEnum)
                                return false;
                            if (!type.IsDefined(typeof(FlagsAttribute)))
                                return false;
                            _displayParameters[parameterIndex] = EditorDisplay.EnumFlags;
                            return true;
                        case DisplayAs.TextArea:
                            if (type != typeof(string))
                                return false;
                            _displayParameters[parameterIndex] = EditorDisplay.TextArea;
                            return true;
                        default:
                            return false;
                    }
                }
            }
            return false;
        }

        public TestingCommandAttribute()
        {
            
        }

        /// <summary>
        /// <example>
        /// Tells unity how to display method's parameters
        /// <code>
        /// [TestingCommand(new string[] { "second", "fourth" }, new DisplayAs[] { DisplayAs.Delayed, DisplayAs.TextArea })]
        /// public static void Test(string first, int second, bool third, string fourth) {}
        /// </code>
        /// </example>
        /// </summary>
        public TestingCommandAttribute(string[] parametersNames, DisplayAs[] parametersDisplay)
        {
            _names = parametersNames;
            _displayAs = parametersDisplay;
        }
    }

    public partial class TestingWindow : EditorWindow
    {
        private static MethodInfo[] s_commands;
        private static TestingCommandAttribute[] s_attributes;
        private static bool s_isInitialized = false;
        private static bool s_areCommandsFound = false;

        private static int s_historySize = 10;
        private static string[] s_historyNames = new string[0];
        private static float[] s_historyTimes = new float[0];

        private static System.Diagnostics.Stopwatch s_stopwatch = new();

        [MenuItem("Window/TestingWindow")]
        private static void Init()
        {
            TestingWindow w = GetWindow<TestingWindow>();
            w.Show();
        }

        private void OnEnable()
        {
            ChangeHistorySize();

            s_historyFadeValue = new(false);
            s_historyFadeValue.valueChanged.AddListener(Repaint);

            FindCommands();
        }

        private async void FindCommands()
        {
            if (s_isInitialized) return;
            s_isInitialized = true;
            await Task.Run(() =>
            {
                s_commands = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes()
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.IsDefined(typeof(TestingCommandAttribute))))).ToArray();
            });

            //Parameters
            ParameterInfo[][] parameters = new ParameterInfo[s_commands.Length][];
            for (int i = 0; i < s_commands.Length; i++)
                parameters[i] = s_commands[i].GetParameters().ToArray();

            //Attributes
            s_attributes = new TestingCommandAttribute[s_commands.Length];
            for (int i = 0; i < s_commands.Length; i++)
                s_attributes[i] = s_commands[i].GetCustomAttribute<TestingCommandAttribute>()
                    .Init(parameters[i]);

            //Foldouts
            s_commandsFoldouts = new bool[s_commands.Length];

            s_areCommandsFound = true;
            Repaint();
        }

        private static Vector2 s_scrollCommands = Vector2.zero;
        private static Vector2 s_scrollTime = Vector2.zero;
        private static AnimBool s_historyFadeValue;
        private static bool[] s_commandsFoldouts;

        private void OnGUI()
        {
            GUIStyle boldStyle = new(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            };

            //Wait for commands
            if (!s_areCommandsFound)
            {
                GUILayout.Label("Initializing", boldStyle);
                return;
            }

            //Commands
            GUILayout.BeginArea(new(0f, 0f, position.width, position.height * .7f));
            s_scrollCommands = GUILayout.BeginScrollView(s_scrollCommands);
            for (int i = 0; i < s_commands.Length; i++)
            {
                bool foldout = s_commandsFoldouts[i];
                foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, s_commands[i].Name);
                s_commandsFoldouts[i] = foldout;
                if (foldout)
                {
                    TestingCommandAttribute attribute = s_attributes[i];
                    string[] names = attribute.ParametersNames;
                    TestingCommandAttribute.EditorDisplay[] displays = attribute.DisplayParameters;
                    object[] objects = attribute.Objects;
                    Dictionary<int, Type> unityTypes = attribute.UnityTypes;

                    //Display parameters
                    for (int j = 0; j < names.Length; j++)
                    {
                        switch (displays[j])
                        {
                            case TestingCommandAttribute.EditorDisplay.Bounds:
                                objects[j] = EditorGUILayout.BoundsField(names[j], (Bounds)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.BoundsInt:
                                objects[j] = EditorGUILayout.BoundsIntField(names[j], (BoundsInt)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Color:
                                objects[j] = EditorGUILayout.ColorField(names[j], (Color)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Curve:
                                objects[j] = EditorGUILayout.CurveField(names[j], (AnimationCurve)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.DelayedDouble:
                                objects[j] = EditorGUILayout.DelayedDoubleField(names[j], (double)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.DelayedFloat:
                                objects[j] = EditorGUILayout.DelayedFloatField(names[j], (float)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.DelayedInt:
                                objects[j] = EditorGUILayout.DelayedIntField(names[j], (int)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.DelayedText:
                                objects[j] = EditorGUILayout.DelayedTextField(names[j], (string)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Double:
                                objects[j] = EditorGUILayout.DoubleField(names[j], (double)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.EnumFlags:
                                objects[j] = EditorGUILayout.EnumFlagsField(names[j], (Enum)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.EnumMask:
#pragma warning disable CS0618 //Obsolet
                                objects[j] = EditorGUILayout.EnumMaskField(names[j], (Enum)objects[j]);
#pragma warning restore CS0618
                                break;
                            case TestingCommandAttribute.EditorDisplay.Float:
                                objects[j] = EditorGUILayout.FloatField(names[j], (float)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Gradient:
                                objects[j] = EditorGUILayout.GradientField(names[j], (Gradient)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Int:
                                objects[j] = EditorGUILayout.IntField(names[j], (int)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Long:
                                objects[j] = EditorGUILayout.LongField(names[j], (long)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Rect:
                                objects[j] = EditorGUILayout.RectField(names[j], (Rect)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.RectInt:
                                objects[j] = EditorGUILayout.RectIntField(names[j], (RectInt)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Text:
                                objects[j] = EditorGUILayout.TextField(names[j], (string)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.TextArea:
                                objects[j] = EditorGUILayout.TextArea((string)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Vector2:
                                objects[j] = EditorGUILayout.Vector2Field(names[j], (Vector2)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Vector2Int:
                                objects[j] = EditorGUILayout.Vector2IntField(names[j], (Vector2Int)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Vector3:
                                objects[j] = EditorGUILayout.Vector3Field(names[j], (Vector3)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Vector3Int:
                                objects[j] = EditorGUILayout.Vector3IntField(names[j], (Vector3Int)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Vector4:
                                objects[j] = EditorGUILayout.Vector4Field(names[j], (Vector4)objects[j]);
                                break;
                            case TestingCommandAttribute.EditorDisplay.Object:
                                objects[j] = EditorGUILayout.ObjectField(names[j], (UnityEngine.Object)objects[j], unityTypes[j], true);
                                break;
                            default:
                                Debug.LogWarning($"Can't display {names[j]} parameter of {s_commands[i].Name}");
                                break;
                        }
                    }

                    if (GUILayout.Button("Run"))
                        RunCommand(i);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            //Time
            GUILayout.BeginArea(new Rect(0f, position.height * .7f, position.width, position.height * .3f));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Command history", boldStyle);
            s_historySize = EditorGUILayout.IntSlider("History size", s_historySize, 1, 100);
            GUILayout.EndHorizontal();
            if (s_historySize != s_historyNames.Length)
                ChangeHistorySize();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(s_historyNames[0] + ':', s_historyTimes[0].ToString());
            s_historyFadeValue.target = EditorGUILayout.ToggleLeft("Show full history", s_historyFadeValue.target);
            if (EditorGUILayout.BeginFadeGroup(s_historyFadeValue.faded))
            {
                s_scrollTime = GUILayout.BeginScrollView(s_scrollTime);
                EditorGUI.indentLevel++;
                for (int i = 1; i < s_historySize; i++)
                    EditorGUILayout.LabelField(s_historyNames[i] + ':', s_historyTimes[i].ToString());
                EditorGUI.indentLevel--;
                GUILayout.EndScrollView();
            }
            EditorGUILayout.EndFadeGroup();
            GUILayout.EndArea();
        }

        private static void ChangeHistorySize()
        {
            int oldSize = s_historyNames.Length;
            string[] newNames = new string[s_historySize];
            float[] newTimes = new float[s_historySize];
            if (oldSize > s_historySize)
                for (int i = 0; i < s_historySize; i++)
                {
                    newNames[i] = s_historyNames[i];
                    newTimes[i] = s_historyTimes[i];
                }
            else
            {
                for (int i = 0; i < oldSize; i++)
                {
                    newNames[i] = s_historyNames[i];
                    newTimes[i] = s_historyTimes[i];
                }
                for (int i = oldSize; i < s_historySize; i++)
                {
                    newNames[i] = "None";
                    newTimes[i] = 0f;
                }

            }
            s_historyNames = newNames;
            s_historyTimes = newTimes;
        }

        private static void PushHistory(string name, float time)
        {
            int j = s_historyNames.Length - 1;
            for (int i = s_historyNames.Length - 2; i >= 0; i--)
            {
                s_historyNames[j] = s_historyNames[i];
                s_historyTimes[j] = s_historyTimes[i];
                j--;
            }
            s_historyNames[0] = name;
            s_historyTimes[0] = time;
        }

        private static void RunCommand(int index)
        {
            s_stopwatch.Restart();
            s_commands[index].Invoke(null, s_attributes[index].Objects);
            s_stopwatch.Stop();
            PushHistory(s_commands[index].Name, (float)s_stopwatch.Elapsed.TotalMilliseconds);
        }

        public static void StartTimer() =>
            s_stopwatch.Restart();

        public static void StopTimer() =>
            s_stopwatch.Stop();
    }
}
#endif