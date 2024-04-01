using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace PostClip
{
    public partial class MainForm : Form
    {
        private List<(string pattern, string replacement)> rules = new List<(string, string)>();
        private string rulesFilePath = "rules.txt";

        public MainForm()
        {
            InitializeComponent();
            LoadRulesFromFile();
            RegisterClipboardViewer();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                IDataObject iData = Clipboard.GetDataObject();
                if (iData.GetDataPresent(DataFormats.Text))
                {
                    string text = (string)iData.GetData(DataFormats.Text);
                    string replacedText = ApplyReplacementRules(text);
                    if (replacedText != null && replacedText != text)
                    {
                        Clipboard.SetText(replacedText);
                    }
                }
            }
        }

        private string ApplyReplacementRules(string text)
        {
            bool match = false;
            foreach (var rule in rules)
            {
                text = Regex.Replace(text, rule.pattern, rule.replacement);
                match = true;
            }
            if (!match)
            {
                return null;
            }
            return text;
        }

        private void btnAddRule_Click(object sender, EventArgs e)
        {
            string pattern = txtPattern.Text;
            string replacement = txtReplacement.Text;
            rules.Add((pattern, replacement));
            UpdateRulesList();
            SaveRulesToFile();
        }

        private void btnRemoveRule_Click(object sender, EventArgs e)
        {
            if (lstRules.SelectedIndex >= 0)
            {
                rules.RemoveAt(lstRules.SelectedIndex);
                UpdateRulesList();
                SaveRulesToFile();
            }
        }

        private void UpdateRulesList()
        {
            lstRules.Items.Clear();
            foreach (var rule in rules)
            {
                lstRules.Items.Add($"{rule.pattern} -> {rule.replacement}");
            }
        }

        private void LoadRulesFromFile()
        {
            if (File.Exists(rulesFilePath))
            {
                string[] lines = File.ReadAllLines(rulesFilePath);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        rules.Add((parts[0].Trim(), parts[1].Trim()));
                    }
                }
                UpdateRulesList();
            }
        }

        private void SaveRulesToFile()
        {
            List<string> lines = new List<string>();
            foreach (var rule in rules)
            {
                lines.Add($"{rule.pattern} -> {rule.replacement}");
            }
            File.WriteAllLines(rulesFilePath, lines);
        }

        private void RegisterClipboardViewer()
        {
            IntPtr hwnd = this.Handle;
            WinAPI.AddClipboardFormatListener(hwnd);
        }

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        protected void DisposeCustom(bool disposing)
        {
            if (disposing)
            {
                WinAPI.RemoveClipboardFormatListener(this.Handle);
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void lstRules_DoubleClick(object sender, EventArgs e)
        {
            var msg = "Do you want to remove the selected rule?";
            var title = "Remove Rule";
            var result = MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            var sel = lstRules.SelectedItems;
            if (sel.Count != 1)
            {
                return; 
            }

            var item = sel[0];
            if (item == null)
            {
                return;
            }

            var parts = item.ToString().Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return;
            }
            txtPattern.Text = parts[0].Trim();
            txtReplacement.Text = parts[1].Trim();

            rules.RemoveAt(lstRules.SelectedIndex);
            UpdateRulesList();
        }
    }

    internal static class WinAPI
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }
}
