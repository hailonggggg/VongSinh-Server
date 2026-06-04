using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Runtime inspector that displays and edits every entry of the Dictionaries
/// held by <see cref="Master"/> (Characters, Yuan Skills, Basic Attacks,
/// Status Effects, Passives). Stat fields are generated via reflection so the
/// editor automatically reflects any new fields added to those classes.
/// Edits are written back to the live objects immediately.
/// Toggle visibility with F2.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MasterDataEditor : MonoBehaviour
{
    private enum Category { Characters, YuanSkills, BasicAttacks, StatusEffects, Passives }

    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;

    private static readonly HashSet<string> BlockedComplexTypes = new()
    {
        "Battle", "BattlePlayer", "Client", "ServerNetwork", "SkillPattern", "Unit", "Map"
    };

    private VisualElement _root;
    private VisualElement _fieldContainer;
    private Label _detailTitle;
    private Label _entryColumnTitle;
    private Label _statusLabel;
    private ScrollView _entryScroll;

    private readonly Dictionary<Category, Button> _categoryButtons = new();
    private readonly List<Button> _entryButtons = new();

    private Category _activeCategory = Category.Characters;
    private bool _visible = true;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        _root = uiDocument.rootVisualElement;

        _fieldContainer = _root.Q<VisualElement>("fieldContainer");
        _detailTitle = _root.Q<Label>("detailTitle");
        _entryColumnTitle = _root.Q<Label>("entryColumnTitle");
        _statusLabel = _root.Q<Label>("statusLabel");
        _entryScroll = _root.Q<ScrollView>("entryScroll");

        _categoryButtons[Category.Characters] = _root.Q<Button>("catCharacters");
        _categoryButtons[Category.YuanSkills] = _root.Q<Button>("catYuanSkills");
        _categoryButtons[Category.BasicAttacks] = _root.Q<Button>("catBasicAttacks");
        _categoryButtons[Category.StatusEffects] = _root.Q<Button>("catStatusEffects");
        _categoryButtons[Category.Passives] = _root.Q<Button>("catPassives");

        foreach (var kvp in _categoryButtons)
        {
            var cat = kvp.Key;
            kvp.Value.clicked += () => SelectCategory(cat);
        }

        var refreshButton = _root.Q<Button>("refreshButton");
        if (refreshButton != null) refreshButton.clicked += () => SelectCategory(_activeCategory);

        SelectCategory(Category.Characters);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            _visible = !_visible;
            if (_root != null)
            {
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }

    // ---------------------------------------------------------------------
    // Category / Entry navigation
    // ---------------------------------------------------------------------

    private void SelectCategory(Category category)
    {
        _activeCategory = category;

        foreach (var kvp in _categoryButtons)
        {
            kvp.Value.EnableInClassList("nav-button--active", kvp.Key == category);
        }

        _entryColumnTitle.text = category.ToString();
        BuildEntryList(category);

        // Clear detail panel until an entry is chosen.
        _fieldContainer.Clear();
        _detailTitle.text = "Select an entry";
        SetStatus(string.Empty);
    }

    private void BuildEntryList(Category category)
    {
        _entryScroll.Clear();
        _entryButtons.Clear();

        IDictionary dict = GetDictionary(category);
        if (dict == null)
        {
            var hint = new Label("Master.Instance not available (enter Play Mode).");
            hint.AddToClassList("empty-hint");
            _entryScroll.Add(hint);
            return;
        }

        if (dict.Count == 0)
        {
            var hint = new Label("No entries loaded.");
            hint.AddToClassList("empty-hint");
            _entryScroll.Add(hint);
            return;
        }

        var entries = new List<KeyValuePair<int, object>>();
        foreach (DictionaryEntry e in dict)
        {
            entries.Add(new KeyValuePair<int, object>((int)e.Key, e.Value));
        }
        entries.Sort((a, b) => a.Key.CompareTo(b.Key));

        foreach (var entry in entries)
        {
            int id = entry.Key;
            object obj = entry.Value;
            var btn = new Button { text = GetEntryLabel(id, obj) };
            btn.AddToClassList("entry-button");
            btn.clicked += () => SelectEntry(btn, id, obj);
            _entryScroll.Add(btn);
            _entryButtons.Add(btn);
        }
    }

    private void SelectEntry(Button source, int id, object obj)
    {
        foreach (var b in _entryButtons)
        {
            b.EnableInClassList("entry-button--active", b == source);
        }

        _detailTitle.text = $"{_activeCategory} #{id}  —  {GetEntryLabel(id, obj)}";
        _fieldContainer.Clear();
        if (obj == null)
        {
            var hint = new Label("(null entry)");
            hint.AddToClassList("empty-hint");
            _fieldContainer.Add(hint);
            return;
        }

        // For Characters, edit the underlying JSON data object directly. Unit.Data is a
        // live reference into Master.TacticalSOExportData.Characters, so these edits are
        // saved automatically on quit and avoid duplicate runtime-vs-config fields.
        object target = obj is Unit u && u.Data != null ? (object)u.Data : obj;
        // Depth 2 so nested objects inside list items render (e.g. a Skill's
        // Buffs/Debuffs -> SkillOption -> StatusEffect -> EffectType).
        BuildEditorFor(target, _fieldContainer, 2);
        SetStatus("Editing live data");
    }

    // ---------------------------------------------------------------------
    // Reflection-driven field editor
    // ---------------------------------------------------------------------

    private void BuildEditorFor(object obj, VisualElement container, int depth)
    {
        if (obj == null) return;
        Type type = obj.GetType();

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // Simple editable fields first.
        foreach (var field in fields)
        {
            if (field.IsLiteral || field.IsInitOnly || field.IsStatic) continue;
            if (!IsSimpleType(field.FieldType)) continue;
            container.Add(CreateFieldRow(obj, field));
        }

        if (depth <= 0) return;

        // Recurse one level into nested data objects (e.g. Unit.Data, SkillInfo).
        foreach (var field in fields)
        {
            if (field.IsLiteral || field.IsInitOnly || field.IsStatic) continue;
            if (IsSimpleType(field.FieldType)) continue;
            if (ShouldSkipComplex(field.FieldType)) continue;

            object child;
            try { child = field.GetValue(obj); }
            catch { continue; }
            if (child == null) continue;

            var foldout = new Foldout { text = field.Name, value = true };
            foldout.AddToClassList("sub-section");
            BuildEditorFor(child, foldout, depth - 1);
            container.Add(foldout);
        }

        // Lists of complex objects (e.g. Passive.Conditions, Passive.Effects).
        foreach (var field in fields)
        {
            if (field.IsLiteral || field.IsInitOnly || field.IsStatic) continue;
            if (!IsEditableList(field.FieldType, out Type elementType)) continue;
            container.Add(BuildListEditor(obj, field, elementType, depth));
        }
    }

    // ---------------------------------------------------------------------
    // List editor (add / remove / edit elements)
    // ---------------------------------------------------------------------

    private VisualElement BuildListEditor(object owner, FieldInfo field, Type elementType, int depth)
    {
        var foldout = new Foldout { text = field.Name, value = true };
        foldout.AddToClassList("sub-section");

        var itemsContainer = new VisualElement();
        foldout.Add(itemsContainer);

        void Rebuild()
        {
            itemsContainer.Clear();
            var list = field.GetValue(owner) as IList;

            if (list == null || list.Count == 0)
            {
                var hint = new Label("(empty)");
                hint.AddToClassList("empty-hint");
                itemsContainer.Add(hint);
                return;
            }

            for (int i = 0; i < list.Count; i++)
            {
                int index = i;
                object element = list[index];

                var itemBox = new VisualElement();
                itemBox.AddToClassList("list-item");

                var headerRow = new VisualElement();
                headerRow.AddToClassList("list-item-header");

                var itemTitle = new Label($"{elementType.Name} [{index}]");
                itemTitle.AddToClassList("list-item-title");
                headerRow.Add(itemTitle);

                var removeBtn = new Button { text = "Remove" };
                removeBtn.AddToClassList("list-remove-button");
                removeBtn.clicked += () =>
                {
                    var current = field.GetValue(owner) as IList;
                    if (current != null && index < current.Count)
                    {
                        current.RemoveAt(index);
                        OnFieldEdited(field.Name);
                        Rebuild();
                    }
                };
                headerRow.Add(removeBtn);
                itemBox.Add(headerRow);

                if (element != null)
                {
                    BuildEditorFor(element, itemBox, depth - 1);
                }
                itemsContainer.Add(itemBox);
            }
        }

        Rebuild();

        var addBtn = new Button { text = $"+ Add {elementType.Name}" };
        addBtn.AddToClassList("list-add-button");
        addBtn.clicked += () =>
        {
            var list = field.GetValue(owner) as IList;
            if (list == null)
            {
                list = (IList)Activator.CreateInstance(field.FieldType);
                field.SetValue(owner, list);
            }
            list.Add(Activator.CreateInstance(elementType));
            OnFieldEdited(field.Name);
            Rebuild();
        };
        foldout.Add(addBtn);

        return foldout;
    }

    private static bool IsEditableList(Type t, out Type elementType)
    {
        elementType = null;
        if (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(List<>)) return false;

        elementType = t.GetGenericArguments()[0];

        // Only handle complex element types we can both build an editor for and instantiate.
        if (ShouldSkipComplex(elementType)) return false;
        if (elementType.GetConstructor(Type.EmptyTypes) == null) return false;
        return true;
    }

    private VisualElement CreateFieldRow(object target, FieldInfo field)
    {
        var row = new VisualElement();
        row.AddToClassList("field-row");

        var label = new Label(field.Name);
        label.AddToClassList("field-label");
        row.Add(label);

        Type t = field.FieldType;
        object current = SafeGet(field, target);
        VisualElement input;

        if (t.IsEnum)
        {
            var ef = new EnumField((Enum)(current ?? Activator.CreateInstance(t)));
            ef.RegisterValueChangedCallback(e =>
            {
                field.SetValue(target, e.newValue);
                OnFieldEdited(field.Name);
            });
            input = ef;
        }
        else if (t == typeof(bool))
        {
            var tg = new Toggle { value = current is bool b && b };
            tg.RegisterValueChangedCallback(e =>
            {
                field.SetValue(target, e.newValue);
                OnFieldEdited(field.Name);
            });
            input = tg;
        }
        else if (t == typeof(string))
        {
            var tf = new TextField { value = current as string ?? string.Empty };
            tf.RegisterValueChangedCallback(e =>
            {
                field.SetValue(target, e.newValue);
                OnFieldEdited(field.Name);
            });
            input = tf;
        }
        else if (t == typeof(float) || t == typeof(double))
        {
            var ff = new FloatField { value = current == null ? 0f : Convert.ToSingle(current) };
            ff.RegisterValueChangedCallback(e =>
            {
                field.SetValue(target, Convert.ChangeType(e.newValue, t));
                OnFieldEdited(field.Name);
            });
            input = ff;
        }
        else // integer family
        {
            var inf = new IntegerField { value = current == null ? 0 : Convert.ToInt32(current) };
            inf.RegisterValueChangedCallback(e =>
            {
                field.SetValue(target, Convert.ChangeType(e.newValue, t));
                OnFieldEdited(field.Name);
            });
            input = inf;
        }

        input.AddToClassList("field-input");
        row.Add(input);
        return row;
    }

    private static object SafeGet(FieldInfo field, object target)
    {
        try { return field.GetValue(target); }
        catch { return null; }
    }

    private static bool IsSimpleType(Type t)
    {
        if (t.IsEnum) return true;
        return t == typeof(int) || t == typeof(uint) || t == typeof(long) || t == typeof(short)
            || t == typeof(byte) || t == typeof(float) || t == typeof(double)
            || t == typeof(bool) || t == typeof(string);
    }

    private static bool ShouldSkipComplex(Type t)
    {
        if (t == typeof(string)) return true;
        if (typeof(IEnumerable).IsAssignableFrom(t)) return true; // lists, dictionaries, arrays
        if (!t.IsClass) return true;                              // structs (Vector3Int, etc.)
        if (t.Namespace != null &&
            (t.Namespace.StartsWith("UnityEngine") || t.Namespace.StartsWith("System")))
            return true;
        if (BlockedComplexTypes.Contains(t.Name)) return true;
        return false;
    }

    // ---------------------------------------------------------------------
    // Data access helpers
    // ---------------------------------------------------------------------

    private IDictionary GetDictionary(Category category)
    {
        var m = Master.Instance;
        if (m == null) return null;
        return category switch
        {
            Category.Characters => m.CharactersById as IDictionary,
            Category.YuanSkills => m.YuanSkillsById as IDictionary,
            Category.BasicAttacks => m.BasicAttackSkillsById as IDictionary,
            Category.StatusEffects => m.StatusEffectByIds as IDictionary,
            Category.Passives => m.PassiveByIds as IDictionary,
            _ => null
        };
    }

    private static string GetEntryLabel(int id, object obj)
    {
        return obj switch
        {
            Unit u => $"{id}: {u.Data?.DisplayName ?? u.Data?.AssetName ?? "Unit"}",
            Skill s => $"{id}: {(string.IsNullOrEmpty(s.SkillName) ? "Skill" : s.SkillName)}",
            StatusEffect se => $"{id}: {se.EffectType}",
            Passive p => $"{id}: {(string.IsNullOrEmpty(p.PassiveName) ? "Passive" : p.PassiveName)}",
            _ => id.ToString()
        };
    }

    private void OnFieldEdited(string fieldName)
    {
        Master.TacticalDataDirty = true;
        SetStatus($"Edited {fieldName} — will save on quit");
    }

    private void SetStatus(string text)
    {
        if (_statusLabel != null) _statusLabel.text = text;
    }
}
