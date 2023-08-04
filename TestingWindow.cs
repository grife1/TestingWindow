#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

namespace TW
{
    public enum Display
    {
        TextArea
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class DisplayAsAttribute : Attribute
    {
        public readonly string ParameterName;
        public readonly Display ParameterDisplay;
        public readonly bool IsSlider = false;
        public readonly bool IsFloatSlider = false;
        public readonly (int, int) Int;
        public readonly (float, float) Float;

        /// <summary>
        /// Display the parameter as text area or enum flags
        /// </summary>
        public DisplayAsAttribute(string parameterName, Display displayAs)
        {
            ParameterName = parameterName;
            ParameterDisplay = displayAs;
        }

        /// <summary>
        /// Display the parameter as int slider
        /// </summary>
        public DisplayAsAttribute(string parameterName, int minValue, int maxValue)
        {
            ParameterName = parameterName;
            IsSlider = true;
            Int = (minValue, maxValue);
        }

        /// <summary>
        /// Display the parameter as float slider
        /// </summary>
        public DisplayAsAttribute(string parameterName, float minValue, float maxValue)
        {
            ParameterName = parameterName;
            IsSlider = true;
            IsFloatSlider = true;
            Float = (minValue, maxValue);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TestingCommandAttribute : Attribute
    {
        public sealed class ParameterData
        {
            public string Name;
            public Type Type;
            public object Object;
            public EditorDisplay Display;
            public (int, int) IntSliderData;
            public (float, float) FloatSliderData;
        }

        public enum EditorDisplay
        {
            None,
            ObjectSubclass,
            Property,
            Enum,
            EnumFlags,
            TextArea,
            IntSlider,
            FloatSlider,
            Array,
            List
        }

        private ParameterData[] _data;
        public ParameterData[] Data { get { return _data; } }

        private DisplayAsAttribute[] _displayAs;

        public TestingCommandAttribute Init(ParameterInfo[] parameters, Attribute[] displayAsArray)
        {
            _displayAs = new DisplayAsAttribute[displayAsArray.Length];
            for (int i = 0; i < _displayAs.Length; i++)
                _displayAs[i] = (DisplayAsAttribute)displayAsArray[i];
            return Init(parameters);
        }

        public TestingCommandAttribute Init(ParameterInfo[] parameters)
        {
            _data = new ParameterData[parameters.Length];

            bool tryOverride = true;
            if (_displayAs is null)
                tryOverride = false;
            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                _data[i] = new()
                {
                    Name = parameters[i].Name,
                    Type = type
                };

                if (!tryOverride || !TryOverride(parameters[i], i))
                {
                    EditorDisplay ed = EditorDisplay.None;
                    if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                        ed = EditorDisplay.ObjectSubclass;
                    else if (type.IsEnum)
                        if (type.IsDefined(typeof(FlagsAttribute)))
                            ed = EditorDisplay.EnumFlags;
                        else
                            ed = EditorDisplay.Enum;
                    else if (IsSupportedProperty(type))
                        ed = EditorDisplay.Property;
                    else if (IsSupportedArray(type))
                        ed = EditorDisplay.Array;
                    else if (IsSupportedList(type))
                        ed = EditorDisplay.List;
                    _data[i].Display = ed;
                }
                EditorDisplay display = _data[i].Display;

                object o;
                if (type == typeof(string))
                    o = string.Empty;
                else if (type.IsEnum)
                    o = Enum.ToObject(type, 0);
                else if (display == EditorDisplay.ObjectSubclass ||
                    type == typeof(UnityEngine.Object))
                    o = null;
                else if (display == EditorDisplay.None)
                    o = null;
                else if (display == EditorDisplay.Array)
                    o = null;
                else if (display == EditorDisplay.List)
                    o = null;
                else
                    o = Activator.CreateInstance(type);
                _data[i].Object = o;
            }

            _displayAs = null;
            return this;
        }

        private static bool IsSupportedProperty(Type type)
        {
            if (type == typeof(AnimationCurve) ||
                type == typeof(Bounds) ||
                type == typeof(BoundsInt) ||
                type == typeof(Color) ||
                type == typeof(Gradient) ||
                type == typeof(Hash128) ||
                type == typeof(Quaternion) ||
                type == typeof(Rect) ||
                type == typeof(RectInt) ||
                type == typeof(Vector2) ||
                type == typeof(Vector2Int) ||
                type == typeof(Vector3) ||
                type == typeof(Vector3Int) ||
                type == typeof(Vector4) ||
                type == typeof(Matrix4x4) ||
                (type.IsPrimitive && type != typeof(IntPtr) && type != typeof(UIntPtr)) ||
                type == typeof(string) ||
                type == typeof(UnityEngine.Object))
                return true;
            return false;     
        }

        private static bool IsSupportedArray(Type type)
        {
            if (type.IsArray && type.GetArrayRank() == 1 &&
                IsSupportedProperty(type.GetElementType()))
                return true;
            return false;
        }

        private static bool IsSupportedList(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>) &&
                IsSupportedProperty(type.GetGenericArguments()[0]))
                return true;
            return false;
        }

        private bool TryOverride(ParameterInfo parameter, int parameterIndex)
        {
            string name = parameter.Name;
            for (int i = 0; i < _displayAs.Length; i++)
            {
                if (name == _displayAs[i].ParameterName)
                {
                    Type type = parameter.ParameterType;
                    if (_displayAs[i].IsSlider)
                    {
                        if (_displayAs[i].IsFloatSlider)
                        {
                            if (type == typeof(float))
                            {
                                _data[parameterIndex].Display = EditorDisplay.FloatSlider;
                                _data[parameterIndex].FloatSliderData = _displayAs[i].Float;
                                return true;
                            }
                            return false;
                        }
                        else
                        {
                            if (type == typeof(int))
                            {
                                _data[parameterIndex].Display = EditorDisplay.IntSlider;
                                _data[parameterIndex].IntSliderData = _displayAs[i].Int;
                                return true;
                            }
                            return false;
                        }
                    }
                    else
                    {
                        //Only TextArea exists
                        if (type == typeof(string))
                        {
                            _data[parameterIndex].Display = EditorDisplay.TextArea;
                            return true;
                        }
                        return false;
                    }
                }
            }
            return false;
        }
    }

    public class TestingWindow : EditorWindow
    {
        private class Container : ScriptableObject
        {
            private FieldInfo[] _fields;

            public void Init() =>
                _fields = GetType().GetFields();

            public string GetFieldName(Type type)
            {
                for (int i = 0; i < _fields.Length; i++)
                    if (_fields[i].FieldType == type)
                        return _fields[i].Name;
                return string.Empty;
            }

            public void Set(object obj, Type type)
            {
                for (int i = 0; i < _fields.Length; i++)
                    if (_fields[i].FieldType == type)
                    {
                        _fields[i].SetValue(this, obj);
                        return;
                    }
            }

            public object Get(Type type)
            {
                for (int i = 0; i < _fields.Length; i++)
                    if (_fields[i].FieldType == type)
                        return _fields[i].GetValue(this);
                return null;
            }
        }

        private class PropertyContainer : Container
        {
            public AnimationCurve Curve;
            public Bounds Bounds;
            public BoundsInt BoundsInt;
            public Color Color;
            public Gradient Gradient;
            public Hash128 Hash;
            public Quaternion Quaternion;
            public Rect Rect;
            public RectInt RectInt;
            public Vector2 Vector2;
            public Vector2Int Vector2Int;
            public Vector3 Vector3;
            public Vector3Int Vector3Int;
            public Vector4 Vector4;
            public Matrix4x4 Matrix4x4;
            public UnityEngine.Object Object;
            public bool Bool;
            public byte Byte;
            public sbyte SByte;
            public short Short;
            public ushort UShort;
            public int Int;
            public uint UInt;
            public long Long;
            public ulong ULong;
            public float Float;
            public double Double;
            public char Char;
            public string String;
        }

        private class ArrayContainer : Container
        {
            public AnimationCurve[] Curve;
            public Bounds[] Bounds;
            public BoundsInt[] BoundsInt;
            public Color[] Color;
            public Gradient[] Gradient;
            public Hash128[] Hash;
            public Quaternion[] Quaternion;
            public Rect[] Rect;
            public RectInt[] RectInt;
            public Vector2[] Vector2;
            public Vector2Int[] Vector2Int;
            public Vector3[] Vector3;
            public Vector3Int[] Vector3Int;
            public Vector4[] Vector4;
            public Matrix4x4[] Matrix4x4;
            public UnityEngine.Object[] Object;
            public bool[] Bool;
            public byte[] Byte;
            public sbyte[] SByte;
            public short[] Short;
            public ushort[] UShort;
            public int[] Int;
            public uint[] UInt;
            public long[] Long;
            public ulong[] ULong;
            public float[] Float;
            public double[] Double;
            public char[] Char;
            public string[] String;
        }

        private class ListContainer : Container
        {
            public List<AnimationCurve> Curve;
            public List<Bounds> Bounds;
            public List<BoundsInt> BoundsInt;
            public List<Color> Color;
            public List<Gradient> Gradient;
            public List<Hash128> Hash;
            public List<Quaternion> Quaternion;
            public List<Rect> Rect;
            public List<RectInt> RectInt;
            public List<Vector2> Vector2;
            public List<Vector2Int> Vector2Int;
            public List<Vector3> Vector3;
            public List<Vector3Int> Vector3Int;
            public List<Vector4> Vector4;
            public List<Matrix4x4> Matrix4x4;
            public List<UnityEngine.Object> Object;
            public List<bool> Bool;
            public List<byte> Byte;
            public List<sbyte> SByte;
            public List<short> Short;
            public List<ushort> UShort;
            public List<int> Int;
            public List<uint> UInt;
            public List<long> Long;
            public List<ulong> ULong;
            public List<float> Float;
            public List<double> Double;
            public List<char> Char;
            public List<string> String;
        }

        private static PropertyContainer s_container;
        private static SerializedObject s_so;
        private static ArrayContainer s_arrayContainer;
        private static SerializedObject s_soArray;
        private static ListContainer s_listContainer;
        private static SerializedObject s_soList;

        private static MethodInfo[] s_commands;
        private static string[] s_names;
        private static TestingCommandAttribute[] s_attributes;
        private static bool s_isInitialized = false;
        private static bool s_areCommandsFound = false;

        private static int s_historySize = 10;
        private static string[] s_historyNames = new string[0];
        private static float[] s_historyTimes = new float[0];

        private static readonly System.Diagnostics.Stopwatch s_stopwatch = new();

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

            s_container = CreateInstance<PropertyContainer>();
            s_container.Init();
            s_so = new(s_container);
            s_arrayContainer = CreateInstance<ArrayContainer>();
            s_arrayContainer.Init();
            s_soArray = new(s_arrayContainer);
            s_listContainer = CreateInstance<ListContainer>();
            s_listContainer.Init();
            s_soList = new(s_listContainer);

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

            //Names
            s_names = new string[s_commands.Length];
            for (int i = 0; i < s_commands.Length; i++)
                s_names[i] = s_commands[i].Name;

            //Parameters
            ParameterInfo[][] parameters = new ParameterInfo[s_commands.Length][];
            for (int i = 0; i < s_commands.Length; i++)
                parameters[i] = s_commands[i].GetParameters().ToArray();

            //Attributes
            s_attributes = new TestingCommandAttribute[s_commands.Length];
            for (int i = 0; i < s_commands.Length; i++)
            {
                MethodInfo method = s_commands[i];
                TestingCommandAttribute tca = s_commands[i].GetCustomAttribute<TestingCommandAttribute>();
                if (method.IsDefined(typeof(DisplayAsAttribute)))
                    tca.Init(parameters[i], method.GetCustomAttributes(typeof(DisplayAsAttribute)).ToArray());
                else
                    tca.Init(parameters[i]);
                s_attributes[i] = tca;
            }

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
            GUIStyle commandStyle = new(EditorStyles.foldoutHeader)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            GUIStyle redStyle = new(GUI.skin.label);
            redStyle.normal.textColor = Color.red;

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
                foldout = EditorGUILayout.Foldout(foldout, s_names[i], commandStyle);
                s_commandsFoldouts[i] = foldout;
                if (foldout)
                {
                    TestingCommandAttribute attribute = s_attributes[i];
                    TestingCommandAttribute.ParameterData[] data = attribute.Data;

                    //Display parameters
                    bool canRun = true;
                    for (int j = 0; j < data.Length; j++)
                    {
                        switch (data[j].Display)
                        {
                            case TestingCommandAttribute.EditorDisplay.Enum:
                                data[j].Object = EditorGUILayout.EnumPopup(data[j].Name, (Enum)data[j].Object);
                                break;
                            case TestingCommandAttribute.EditorDisplay.EnumFlags:
                                data[j].Object = EditorGUILayout.EnumFlagsField(data[j].Name, (Enum)data[j].Object);
                                break;
                            case TestingCommandAttribute.EditorDisplay.FloatSlider:
                                data[j].Object = EditorGUILayout.Slider(data[j].Name, (float)data[j].Object, data[j].FloatSliderData.Item1, data[j].FloatSliderData.Item2);
                                break;
                            case TestingCommandAttribute.EditorDisplay.IntSlider:
                                data[j].Object = EditorGUILayout.IntSlider(data[j].Name, (int)data[j].Object, data[j].IntSliderData.Item1, data[j].IntSliderData.Item2);
                                break;
                            case TestingCommandAttribute.EditorDisplay.None:
                                GUILayout.Label($"Can't display \"{data[j].Name}\" parameter", redStyle);
                                canRun = false;
                                break;
                            case TestingCommandAttribute.EditorDisplay.ObjectSubclass:
                                data[j].Object = EditorGUILayout.ObjectField(data[j].Name, (UnityEngine.Object)data[j].Object, data[j].Type, true);
                                break;
                            case TestingCommandAttribute.EditorDisplay.TextArea:
                                GUILayout.Label(data[j].Name);
                                data[j].Object = EditorGUILayout.TextArea((string)data[j].Object);
                                break;
                            default: //Property || Array || List
                                Type type = data[j].Type;
                                Container c = null;
                                SerializedObject so = null;
                                switch (data[j].Display)
                                {
                                    case TestingCommandAttribute.EditorDisplay.Property:
                                        c = s_container;
                                        so = s_so;
                                        break;
                                    case TestingCommandAttribute.EditorDisplay.Array:
                                        c = s_arrayContainer;
                                        so = s_soArray;
                                        break;
                                    case TestingCommandAttribute.EditorDisplay.List:
                                        c = s_listContainer;
                                        so = s_soList;
                                        break;
                                }
                                c.Set(data[j].Object, type);
                                SerializedProperty prop = so.FindProperty(c.GetFieldName(type));
                                so.Update();
                                EditorGUILayout.PropertyField(prop, new GUIContent(data[j].Name));
                                so.ApplyModifiedProperties();
                                data[j].Object = c.Get(type);
                                break;
                        }
                    }

                    if (canRun)
                    {
                        if (GUILayout.Button("Run"))
                            RunCommand(i);
                    }
                    else
                        GUILayout.Label("Replace all unsupported parameters with supported ones to run", redStyle);
                }
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
            TestingCommandAttribute.ParameterData[] data = s_attributes[index].Data;
            object[] objects = new object[data.Length];
            for (int i = 0; i < data.Length; i++)
                objects[i] = data[i].Object;
            s_stopwatch.Restart();
            s_commands[index].Invoke(null, objects);
            s_stopwatch.Stop();
            PushHistory(s_commands[index].Name, (float)s_stopwatch.Elapsed.TotalMilliseconds);
        }

        public static void StartTimer() =>
            s_stopwatch.Restart();

        public static void StopTimer() =>
            s_stopwatch.Stop();
    }

    public static class TestingTimer
    {
        public static void Start() =>
            TestingWindow.StartTimer();

        public static void Stop() =>
            TestingWindow.StopTimer();
    }
}
#endif
