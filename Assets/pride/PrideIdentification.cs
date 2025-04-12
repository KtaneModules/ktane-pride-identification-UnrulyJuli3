using UnityEngine;
using wawa.Modules;
using wawa.Extensions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public sealed class PrideIdentification : ModdedModule
{
    [SerializeField]
    private KMSelectable[] _keySelectables;

    [SerializeField]
    private TextMesh _textBox;

    [SerializeField]
    private SpriteRenderer _spriteRenderer;

    [SerializeField]
    private Sprite[] _flags;

    [SerializeField]
    private MeshRenderer[] _stageLights;

    private bool _capsLock;

    private bool _shift;

    private bool IsCaps => _capsLock || _shift;

    private static readonly KeyCode[] s_keyCodes = { KeyCode.None, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Colon, KeyCode.Backspace, KeyCode.None, KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket, KeyCode.Backslash, KeyCode.CapsLock, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote, KeyCode.Return, KeyCode.LeftShift, KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period, KeyCode.Slash, KeyCode.RightShift, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.Space, KeyCode.None, KeyCode.None, KeyCode.None, KeyCode.None };

    private static readonly int[] s_letterKeys = { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 29, 30, 31, 32, 33, 34, 35, 36, 37, 42, 43, 44, 45, 46, 47, 48 };

    private TextMesh[] _keyTexts;

    private List<Sprite> _unusedFlags;

    private Sprite _currentFlag;

    private int _currentStage;

    private bool _isFocused;

    private void Start()
    {
        _unusedFlags = new List<Sprite>(_flags);
        _keyTexts = new TextMesh[_keySelectables.Length];
        for (int i = 0; i < _keySelectables.Length; i++)
            SetupKey(i);

        InitStage();

        Get<KMSelectable>().Add(onFocus: () => _isFocused = true, onDefocus: () => _isFocused = false);
    }

    private void Update()
    {
        if (_isFocused)
        {
            for (int i = 0; i < s_keyCodes.Length; i++)
            {
                var code = s_keyCodes[i];
                switch (code)
                {
                    case KeyCode.None:
                        break;
                    case KeyCode.LeftShift:
                    case KeyCode.RightShift:
                        if (_shift ? Input.GetKeyUp(code) : Input.GetKeyDown(code))
                            _keySelectables[i].OnInteract();
                        break;
                    default:
                        if (Input.GetKeyDown(code))
                            _keySelectables[i].OnInteract();
                        break;
                }
            }
        }
    }

    private void UpdateCaps()
    {
        foreach (int index in s_letterKeys)
        {
            var textMesh = _keyTexts[index];
            textMesh.text = IsCaps ? textMesh.text.ToUpper() : textMesh.text.ToLower();
        }
    }

    private void SetupKey(int index)
    {
        var selectable = _keySelectables[index];
        var text = _keyTexts[index] = selectable.GetComponentInChildren<TextMesh>();
        var keyCode = s_keyCodes[index];

        selectable.Add(onInteract: () =>
        {
            Shake(selectable, 0.25f, Sound.ButtonPress);
            if (!Status.IsSolved)
            {
                switch (keyCode)
                {
                    case KeyCode.None:
                        break;
                    case KeyCode.LeftShift:
                    case KeyCode.RightShift:
                        _shift = !_shift;
                        UpdateCaps();
                        break;
                    case KeyCode.CapsLock:
                        _capsLock = !_capsLock;
                        UpdateCaps();
                        break;
                    case KeyCode.Backspace:
                    case KeyCode.Delete:
                        if (_textBox.text.Length > 0)
                            _textBox.text = _textBox.text.Substring(0, _textBox.text.Length - 1);
                        break;
                    case KeyCode.Return:
                        Submit();
                        break;
                    default:
                        _textBox.text += keyCode == KeyCode.Space ? " " : text.text;
                        _shift = false;
                        UpdateCaps();
                        break;
                }
            }
        });
    }

    private void InitStage()
    {
        _currentFlag = _unusedFlags.PickRandom();
        _unusedFlags.Remove(_currentFlag);
        _spriteRenderer.sprite = _currentFlag;
        Log($"The selected flag is {_currentFlag.name}");
    }

    private void Submit()
    {
        Log($"Submitted {_textBox.text}");
        string text = Regex.Replace(_textBox.text, "[^a-z]", "", RegexOptions.IgnoreCase).ToLower();
        if (_currentFlag.name == text)
        {
            _stageLights[_currentStage].material.color = Color.green;
            _currentStage++;
            _textBox.text = "";
            if (_currentStage >= 3)
                Solve();
            else
                InitStage();
        }
        else
        {
            Strike("Strike");
        }
    }
}
