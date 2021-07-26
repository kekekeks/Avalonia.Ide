/* eslint-disable @typescript-eslint/naming-convention */
export enum Key {
    /*      No key pressed. */
    None = 0,

    /*      The Cancel key. */
    Cancel = 1,

    /*      The Back key. */
    Back = 2,

    /*      The Tab key. */
    Tab = 3,

    /*      The Linefeed key. */
    LineFeed = 4,

    /*      The Clear key. */
    Clear = 5,

    /*      The Return key. */
    Return = 6,

    /*      The Enter key. */
    Enter = 6,

    /*      The Pause key. */
    Pause = 7,

    /*      The Caps Lock key. */
    CapsLock = 8,

    /*      The Caps Lock key. */
    Capital = 8,

    /*      The IME Hangul mode key. */
    HangulMode = 9,

    /*      The IME Kana mode key. */
    KanaMode = 9,

    /*      The IME Junja mode key. */
    JunjaMode = 10,

    /*      The IME Final mode key. */
    FinalMode = 11,

    /*      The IME Kanji mode key. */
    KanjiMode = 12,

    /*      The IME Hanja mode key. */
    HanjaMode = 12,

    /*      The Escape key. */
    Escape = 13,

    /*      The IME Convert key. */
    ImeConvert = 14,

    /*      The IME NonConvert key. */
    ImeNonConvert = 15,

    /*      The IME Accept key. */
    ImeAccept = 16,

    /*      The IME Mode change key. */
    ImeModeChange = 17,

    /*      The space bar. */
    Space = 18,

    /*      The Page Up key. */
    PageUp = 19,

    /*      The Page Up key. */
    Prior = 19,

    /*      The Page Down key. */
    PageDown = 20,

    /*      The Page Down key. */
    Next = 20,

    /*      The End key. */
    End = 21,

    /*      The Home key. */
    Home = 22,

    /*      The Left arrow key. */
    Left = 23,

    /*      The Up arrow key. */
    Up = 24,

    /*      The Right arrow key. */
    Right = 25,

    /*      The Down arrow key. */
    Down = 26,

    /*      The Select key. */
    Select = 27,

    /*      The Print key. */
    Print = 28,

    /*      The Execute key. */
    Execute = 29,

    /*      The Print Screen key. */
    Snapshot = 30,

    /*      The Print Screen key. */
    PrintScreen = 30,

    /*      The Insert key. */
    Insert = 31,

    /*      The Delete key. */
    Delete = 32,

    /*      The Help key. */
    Help = 33,

    /*      The 0 key. */
    D0 = 34,

    /*      The 1 key. */
    D1 = 35,

    /*      The 2 key. */
    D2 = 36,

    /*      The 3 key. */
    D3 = 37,

    /*      The 4 key. */
    D4 = 38,

    /*      The 5 key. */
    D5 = 39,

    /*      The 6 key. */
    D6 = 40,

    /*      The 7 key. */
    D7 = 41,

    /*      The 8 key. */
    D8 = 42,

    /*      The 9 key. */
    D9 = 43,

    /*      The A key. */
    A = 44,

    /*      The B key. */
    B = 45,

    /*      The C key. */
    C = 46,

    /*      The D key. */
    D = 47,

    /*      The E key. */
    E = 48,

    /*      The F key. */
    F = 49,

    /*      The G key. */
    G = 50,

    /*      The H key. */
    H = 51,

    /*      The I key. */
    I = 52,

    /*      The J key. */
    J = 53,

    /*      The K key. */
    K = 54,

    /*      The L key. */
    L = 55,

    /*      The M key. */
    M = 56,

    /*      The N key. */
    N = 57,

    /*      The O key. */
    O = 58,

    /*      The P key. */
    P = 59,

    /*      The Q key. */
    Q = 60,

    /*      The R key. */
    R = 61,

    /*      The S key. */
    S = 62,

    /*      The T key. */
    T = 63,

    /*      The U key. */
    U = 64,

    /*      The V key. */
    V = 65,

    /*      The W key. */
    W = 66,

    /*      The X key. */
    X = 67,

    /*      The Y key. */
    Y = 68,

    /*      The Z key. */
    Z = 69,

    /*      The left Windows key. */
    LWin = 70,

    /*      The right Windows key. */
    RWin = 71,

    /*      The Application key. */
    Apps = 72,

    /*      The Sleep key. */
    Sleep = 73,

    /*      The 0 key on the numeric keypad. */
    NumPad0 = 74,

    /*      The 1 key on the numeric keypad. */
    NumPad1 = 75,

    /*      The 2 key on the numeric keypad. */
    NumPad2 = 76,

    /*      The 3 key on the numeric keypad. */
    NumPad3 = 77,

    /*      The 4 key on the numeric keypad. */
    NumPad4 = 78,

    /*      The 5 key on the numeric keypad. */
    NumPad5 = 79,

    /*      The 6 key on the numeric keypad. */
    NumPad6 = 80,

    /*      The 7 key on the numeric keypad. */
    NumPad7 = 81,

    /*      The 8 key on the numeric keypad. */
    NumPad8 = 82,

    /*      The 9 key on the numeric keypad. */
    NumPad9 = 83,

    /*      The Multiply key. */
    Multiply = 84,

    /*      The Add key. */
    Add = 85,

    /*      The Separator key. */
    Separator = 86,

    /*      The Subtract key. */
    Subtract = 87,

    /*      The Decimal key. */
    Decimal = 88,

    /*      The Divide key. */
    Divide = 89,

    /*      The F1 key. */
    F1 = 90,

    /*      The F2 key. */
    F2 = 91,

    /*      The F3 key. */
    F3 = 92,

    /*      The F4 key. */
    F4 = 93,

    /*      The F5 key. */
    F5 = 94,

    /*      The F6 key. */
    F6 = 95,

    /*      The F7 key. */
    F7 = 96,

    /*      The F8 key. */
    F8 = 97,

    /*      The F9 key. */
    F9 = 98,

    /*      The F10 key. */
    F10 = 99,

    /*      The F11 key. */
    F11 = 100,

    /*      The F12 key. */
    F12 = 101,

    /*      The F13 key. */
    F13 = 102,

    /*      The F14 key. */
    F14 = 103,

    /*      The F15 key. */
    F15 = 104,

    /*      The F16 key. */
    F16 = 105,

    /*      The F17 key. */
    F17 = 106,

    /*      The F18 key. */
    F18 = 107,

    /*      The F19 key. */
    F19 = 108,

    /*      The F20 key. */
    F20 = 109,

    /*      The F21 key. */
    F21 = 110,

    /*      The F22 key. */
    F22 = 111,

    /*      The F23 key. */
    F23 = 112,

    /*      The F24 key. */
    F24 = 113,

    /*      The Numlock key. */
    NumLock = 114,

    /*      The Scroll key. */
    Scroll = 115,

    /*      The left Shift key. */
    LeftShift = 116,

    /*      The right Shift key. */
    RightShift = 117,

    /*      The left Ctrl key. */
    LeftCtrl = 118,

    /*      The right Ctrl key. */
    RightCtrl = 119,

    /*      The left Alt key. */
    LeftAlt = 120,

    /*      The right Alt key. */
    RightAlt = 121,

    /*      The browser Back key. */
    BrowserBack = 122,

    /*      The browser Forward key. */
    BrowserForward = 123,

    /*      The browser Refresh key. */
    BrowserRefresh = 124,

    /*      The browser Stop key. */
    BrowserStop = 125,

    /*      The browser Search key. */
    BrowserSearch = 126,

    /*      The browser Favorites key. */
    BrowserFavorites = 127,

    /*      The browser Home key. */
    BrowserHome = 128,

    /*      The Volume Mute key. */
    VolumeMute = 129,

    /*      The Volume Down key. */
    VolumeDown = 130,

    /*      The Volume Up key. */
    VolumeUp = 131,

    /*      The media Next Track key. */
    MediaNextTrack = 132,

    /*      The media Previous Track key. */
    MediaPreviousTrack = 133,

    /*      The media Stop key. */
    MediaStop = 134,

    /*      The media Play/Pause key. */
    MediaPlayPause = 135,

    /*      The Launch Mail key. */
    LaunchMail = 136,

    /*      The Select Media key. */
    SelectMedia = 137,

    /*      The Launch Application 1 key. */
    LaunchApplication1 = 138,

    /*      The Launch Application 2 key. */
    LaunchApplication2 = 139,

    /*      The OEM Semicolon key. */
    OemSemicolon = 140,

    /*      The OEM 1 key. */
    Oem1 = 140,

    /*      The OEM Plus key. */
    OemPlus = 141,

    /*      The OEM Comma key. */
    OemComma = 142,

    /*      The OEM Minus key. */
    OemMinus = 143,

    /*      The OEM Period key. */
    OemPeriod = 144,

    /*      The OEM Question Mark key. */
    OemQuestion = 145,

    /*      The OEM 2 key. */
    Oem2 = 145,

    /*      The OEM Tilde key. */
    OemTilde = 146,

    /*      The OEM 3 key. */
    Oem3 = 146,

    /*      The ABNT_C1 (Brazilian) key. */
    AbntC1 = 147,

    /*      The ABNT_C2 (Brazilian) key. */
    AbntC2 = 148,

    /*      The OEM Open Brackets key. */
    OemOpenBrackets = 149,

    /*      The OEM 4 key. */
    Oem4 = 149,

    /*      The OEM Pipe key. */
    OemPipe = 150,

    /*      The OEM 5 key. */
    Oem5 = 150,

    /*      The OEM Close Brackets key. */
    OemCloseBrackets = 151,

    /*      The OEM 6 key. */
    Oem6 = 151,

    /*      The OEM Quotes key. */
    OemQuotes = 152,

    /*      The OEM 7 key. */
    Oem7 = 152,

    /*      The OEM 8 key. */
    Oem8 = 153,

    /*      The OEM Backslash key. */
    OemBackslash = 154,

    /*      The OEM 3 key. */
    Oem102 = 154,

    /*      A special key masking the real key being processed by an IME. */
    ImeProcessed = 155,

    /*      A special key masking the real key being processed as a system key. */
    System = 156,

    /*      The OEM ATTN key. */
    OemAttn = 157,

    /*      The DBE_ALPHANUMERIC key. */
    DbeAlphanumeric = 157,

    /*      The OEM Finish key. */
    OemFinish = 158,

    /*      The DBE_KATAKANA key. */
    DbeKatakana = 158,

    /*      The DBE_HIRAGANA key. */
    DbeHiragana = 159,

    /*      The OEM Copy key. */
    OemCopy = 159,

    /*      The DBE_SBCSCHAR key. */
    DbeSbcsChar = 160,

    /*      The OEM Auto key. */
    OemAuto = 160,

    /*      The DBE_DBCSCHAR key. */
    DbeDbcsChar = 161,

    /*      The OEM ENLW key. */
    OemEnlw = 161,

    /*      The OEM BackTab key. */
    OemBackTab = 162,

    /*      The DBE_ROMAN key. */
    DbeRoman = 162,

    /*      The DBE_NOROMAN key. */
    DbeNoRoman = 163,

    /*      The ATTN key. */
    Attn = 163,

    /*      The CRSEL key. */
    CrSel = 164,

    /*      The DBE_ENTERWORDREGISTERMODE key. */
    DbeEnterWordRegisterMode = 164,

    /*      The EXSEL key. */
    ExSel = 165,

    /*      The DBE_ENTERIMECONFIGMODE key. */
    DbeEnterImeConfigureMode = 165,

    /*      The ERASE EOF Key. */
    EraseEof = 166,

    /*      The DBE_FLUSHSTRING key. */
    DbeFlushString = 166,

    /*      The Play key. */
    Play = 167,

    /*      The DBE_CODEINPUT key. */
    DbeCodeInput = 167,

    /*      The DBE_NOCODEINPUT key. */
    DbeNoCodeInput = 168,

    /*      The Zoom key. */
    Zoom = 168,

    /*      Reserved for future use. */
    NoName = 169,

    /*      The DBE_DETERMINESTRING key. */
    DbeDetermineString = 169,

    /*      The DBE_ENTERDLGCONVERSIONMODE key. */
    DbeEnterDialogConversionMode = 170,

    /*      The PA1 key. */
    Pa1 = 170,

    /*      The OEM Clear key. */
    OemClear = 171,

    /*      The key is used with another key to create a single combined character. */
    DeadCharProcessed = 172,


    /*      OSX Platform-specific Fn+Left key */
    FnLeftArrow = 10001,
    /*      OSX Platform-specific Fn+Right key */
    FnRightArrow = 10002,
    /*      OSX Platform-specific Fn+Up key */
    FnUpArrow = 10003,
    /*      OSX Platform-specific Fn+Down key */
    FnDownArrow = 10004,
}