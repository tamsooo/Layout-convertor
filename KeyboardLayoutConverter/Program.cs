using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace KeyboardLayoutConverter
{
    public class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new KeyboardConverterForm());
        }
    }

    public class KeyboardConverterForm : Form
    {
        private NotifyIcon trayIcon;
        private KeyboardHook hook = new KeyboardHook();

        // Mapping for QWERTY (English US) to Arabic (102) AZERTY
        private Dictionary<string, string> englishToArabicMap = new Dictionary<string, string>
        {
            // QWERTY to Arabic (102) AZERTY
            {"q", "ض"}, {"w", "ص"}, {"e", "ث"}, {"r", "ق"}, {"t", "ف"}, {"y", "غ"}, {"u", "ع"}, {"i", "ه"}, {"o", "خ"}, {"p", "ح"}, {"[", "ج"}, {"]", "د"},
            {"a", "ش"}, {"s", "س"}, {"d", "ي"}, {"f", "ب"}, {"g", "ل"}, {"h", "ا"}, {"j", "ت"}, {"k", "ن"}, {"l", "م"}, {";", "ك"}, {"'", "ط"},
            {"z", "ئ"}, {"x", "ء"}, {"c", "ؤ"}, {"v", "ر"}, {"b", "لا"}, {"n", "ى"}, {"m", "ة"}, {",", "و"}, {".", "ز"}, {"/", "ظ"},
            
            // Capital letters
            {"Q", "َ"}, {"W", "ً"}, {"E", "ُ"}, {"R", "ٌ"}, {"T", "لإ"}, {"Y", "إ"}, {"U", "`"}, {"I", "÷"}, {"O", "×"}, {"P", "؛"}, {"{", "<"}, {"}", ">"},
            {"A", "ِ"}, {"S", "ٍ"}, {"D", "]"}, {"F", "["}, {"G", "لأ"}, {"H", "أ"}, {"J", "ـ"}, {"K", "،"}, {"L", "/"}, {":", "'"}, {"\"", "\""},
            {"Z", "~"}, {"X", "ْ"}, {"C", "}"}, {"V", "{"}, {"B", "لآ"}, {"N", "آ"}, {"M", "'"}, {"<", ","}, {">", "."}, {"?", "؟"},
            
            // Numbers
            {"1", "1"}, {"2", "2"}, {"3", "3"}, {"4", "4"}, {"5", "5"}, {"6", "6"}, {"7", "7"}, {"8", "8"}, {"9", "9"}, {"0", "0"},
            {"!", "!"}, {"@", "@"}, {"#", "#"}, {"$", "$"}, {"%", "%"}, {"^", "^"}, {"&", "&"}, {"*", "*"}, {"(", ")"}, {")", "("},

            // Additional characters
            {"-", "-"}, {"_", "_"}, {"=", "="}, {"+", "+"}, {"\\", "\\"}, {"|", "|"}, {"`", "`"}, {"~", "~"},
            {" ", " "}, {"\n", "\n"}, {"\r", "\r"}, {"\t", "\t"}
        };

        // Reverse mapping for Arabic to English
        private Dictionary<string, string> arabicToEnglishMap;

        public KeyboardConverterForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            this.Size = new System.Drawing.Size(1, 1);
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(-100, -100);

            // Create reverse mapping dictionary
            arabicToEnglishMap = new Dictionary<string, string>();
            foreach (var pair in englishToArabicMap)
            {
                if (!arabicToEnglishMap.ContainsKey(pair.Value))
                {
                    arabicToEnglishMap.Add(pair.Value, pair.Key);
                }
            }

            // Setup notification tray icon
            trayIcon = new NotifyIcon()
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "Keyboard Layout Converter",
                Visible = true
            };

            // Create context menu for tray icon
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("About", null, (s, e) => {
                MessageBox.Show("English-Arabic Keyboard Layout Converter\nPress Ctrl+[ to convert selected text.",
                    "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
            });

            contextMenu.Items.Add("Exit", null, (s, e) => {
                trayIcon.Visible = false;
                Application.Exit();
            });

            trayIcon.ContextMenuStrip = contextMenu;
            trayIcon.DoubleClick += (s, e) => {
                MessageBox.Show("English-Arabic Keyboard Layout Converter is running.\nPress Ctrl+[ to convert selected text.",
                    "Keyboard Layout Converter", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Register the hotkey (Ctrl+[)
            hook.KeyPressed += hook_KeyPressed;
            hook.RegisterHotKey(ModKeys.Control, Keys.OemOpenBrackets); // '[' key
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            ConvertSelectedText();
        }

        private void ConvertSelectedText()
        {
            // Save current clipboard content
            string clipboardBackup = "";
            IDataObject data = Clipboard.GetDataObject();
            if (data != null && data.GetDataPresent(DataFormats.Text))
            {
                clipboardBackup = (string)data.GetData(DataFormats.Text);
            }

            try
            {
                // Simulate Ctrl+C to copy selected text
                SendKeys.SendWait("^c");
                System.Threading.Thread.Sleep(100); // Allow time for clipboard update

                string copiedText = Clipboard.GetText();

                // Abort if copied text is same as before or is empty
                if (string.IsNullOrEmpty(copiedText) || copiedText == clipboardBackup)
                    return;

                // Determine if text is primarily Arabic or English
                bool isArabic = IsPrimarilyArabic(copiedText);

                string convertedText = isArabic
                    ? ConvertArabicToEnglish(copiedText)
                    : ConvertEnglishToArabic(copiedText);

                // Put converted text in clipboard
                Clipboard.SetText(convertedText);

                // Paste it back
                SendKeys.SendWait("^v");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error converting text: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Restore original clipboard content
                if (!string.IsNullOrEmpty(clipboardBackup))
                {
                    System.Threading.Thread.Sleep(100); // Ensure pasting completes
                    Clipboard.SetText(clipboardBackup);
                }
            }
        }


        private bool IsPrimarilyArabic(string text)
        {
            int arabicChars = 0;
            int englishChars = 0;

            foreach (char c in text)
            {
                if (c >= 0x0600 && c <= 0x06FF) // Arabic Unicode range
                    arabicChars++;
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    englishChars++;
            }

            return arabicChars > englishChars;
        }

        private string ConvertEnglishToArabic(string text)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                string ch = text[i].ToString();
                if (englishToArabicMap.ContainsKey(ch))
                    result.Append(englishToArabicMap[ch]);
                else
                    result.Append(ch);
            }

            return result.ToString();
        }

        private string ConvertArabicToEnglish(string text)
        {
            StringBuilder result = new StringBuilder();

            // Try matching multi-character sequences first
            int i = 0;
            while (i < text.Length)
            {
                bool matched = false;

                // Try matching 2-character sequences first, then single characters
                if (i < text.Length - 1)
                {
                    string twoChars = text.Substring(i, 2);
                    if (arabicToEnglishMap.ContainsKey(twoChars))
                    {
                        result.Append(arabicToEnglishMap[twoChars]);
                        i += 2;
                        matched = true;
                        continue;
                    }
                }

                // If no multi-char match, try single char
                string oneChar = text[i].ToString();
                if (arabicToEnglishMap.ContainsKey(oneChar))
                {
                    result.Append(arabicToEnglishMap[oneChar]);
                }
                else
                {
                    result.Append(oneChar);
                }

                i++;
            }

            return result.ToString();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (trayIcon != null)
                trayIcon.Visible = false;

            hook.Dispose();
            base.OnFormClosing(e);
        }
    }

    public sealed class KeyboardHook : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private class Window : NativeWindow, IDisposable
        {
            private static int WM_HOTKEY = 0x0312;

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            public Window()
            {
                // Create an invisible window to receive messages
                CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);

                if (m.Msg == WM_HOTKEY)
                {
                    Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);
                    ModKeys modifier = (ModKeys)((int)m.LParam & 0xFFFF);

                    KeyPressed?.Invoke(this, new KeyPressedEventArgs(modifier, key));
                }
            }

            public void Dispose()
            {
                DestroyHandle();
            }
        }

        private Window _window = new Window();
        private int _currentId = 0;

        public event EventHandler<KeyPressedEventArgs> KeyPressed
        {
            add { _window.KeyPressed += value; }
            remove { _window.KeyPressed -= value; }
        }

        public KeyboardHook()
        {
            // Constructor
        }

        public void RegisterHotKey(ModKeys modifier, Keys key)
        {
            _currentId = _currentId + 1;

            if (!RegisterHotKey(_window.Handle, _currentId, (uint)modifier, (uint)key))
                throw new InvalidOperationException("Couldn't register the hot key.");
        }

        public void Dispose()
        {
            // Unregister all hotkeys
            for (int i = _currentId; i > 0; i--)
            {
                UnregisterHotKey(_window.Handle, i);
            }

            // Dispose the window
            _window.Dispose();
        }
    }

    public class KeyPressedEventArgs : EventArgs
    {
        public ModKeys Modifier { get; private set; }
        public Keys Key { get; private set; }

        internal KeyPressedEventArgs(ModKeys modifier, Keys key)
        {
            Modifier = modifier;
            Key = key;
        }
    }

    [Flags]
    public enum ModKeys : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}