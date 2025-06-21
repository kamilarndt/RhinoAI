using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

using Rhino;
using Rhino.UI;
using RhinoAI.Core;
using RhinoAI.Integration;

namespace RhinoAI.UI.Panels
{
    /// <summary>
    /// Main AI Control Panel for user interactions
    /// </summary>
    [System.Runtime.InteropServices.Guid("B8F8C2A5-4D6E-4B3A-9E8F-2C1A3B4D5E6F")]
    public partial class AIControlPanel : UserControl
    {
        private AIManager _aiManager;
        private SimpleLogger? _logger;
        
        // UI Controls
        private TabControl _tabControl;
        private TextBox _inputTextBox;
        private RichTextBox _outputTextBox;
        private Button _sendButton;
        private Button _clearButton;
        private ComboBox _aiModelComboBox;
        private Panel _suggestionsPanel;
        private ListView _historyListView;
        private ProgressBar _progressBar;
        private Label _statusLabel;
        private TextBox _openAITextBox;
        private TextBox _claudeTextBox;
        private ComboBox _themeComboBox;

        public AIControlPanel()
        {
            InitializeComponent();
            InitializeAI();
        }

        private void InitializeAI()
        {
            try
            {
                if (RhinoAIPlugin.Instance != null)
                {
                    _aiManager = RhinoAIPlugin.Instance.AIManager;
                    _logger = RhinoAIPlugin.Instance.Logger;
                }

                if (_aiManager == null)
                {
                    ShowError("AI Manager not initialized");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Failed to initialize AI: {ex.Message}");
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Main layout
            Size = new Size(400, 600);
            BackColor = SystemColors.Control;
            
            // Create tab control
            CreateTabControl();
            
            // Create chat tab
            CreateChatTab();
            
            // Create settings tab
            CreateSettingsTab();
            
            // Create history tab
            CreateHistoryTab();

            ResumeLayout(false);
        }

        private void CreateTabControl()
        {
            _tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F)
            };
            
            Controls.Add(_tabControl);
        }

        private void CreateChatTab()
        {
            var chatTab = new TabPage("AI Chat");
            _tabControl.TabPages.Add(chatTab);

            // Main chat panel
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            chatTab.Controls.Add(mainPanel);

            // AI Model selection
            var modelPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40
            };
            mainPanel.Controls.Add(modelPanel);

            var modelLabel = new Label
            {
                Text = "AI Model:",
                Location = new Point(0, 12),
                Size = new Size(70, 20)
            };
            modelPanel.Controls.Add(modelLabel);

            _aiModelComboBox = new ComboBox
            {
                Location = new Point(75, 10),
                Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _aiModelComboBox.Items.AddRange(new[] { "OpenAI GPT", "Claude", "MCP Server" });
            _aiModelComboBox.SelectedIndex = 0;
            modelPanel.Controls.Add(_aiModelComboBox);

            // Output area
            _outputTextBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9F),
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            mainPanel.Controls.Add(_outputTextBox);

            // Suggestions panel
            _suggestionsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            mainPanel.Controls.Add(_suggestionsPanel);

            // Input area
            var inputPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 80
            };
            mainPanel.Controls.Add(inputPanel);

            // Progress bar
            _progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 3,
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };
            inputPanel.Controls.Add(_progressBar);

            // Status label
            _statusLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = "Ready",
                ForeColor = Color.Green
            };
            inputPanel.Controls.Add(_statusLabel);

            // Input text box
            _inputTextBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                PlaceholderText = "Type your modeling request here..."
            };
            _inputTextBox.KeyDown += InputTextBox_KeyDown;
            inputPanel.Controls.Add(_inputTextBox);

            // Button panel
            var buttonPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 30
            };
            inputPanel.Controls.Add(buttonPanel);

            _sendButton = new Button
            {
                Text = "Send",
                Size = new Size(60, 25),
                Location = new Point(0, 2),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _sendButton.Click += SendButton_Click;
            buttonPanel.Controls.Add(_sendButton);

            _clearButton = new Button
            {
                Text = "Clear",
                Size = new Size(60, 25),
                Location = new Point(70, 2)
            };
            _clearButton.Click += ClearButton_Click;
            buttonPanel.Controls.Add(_clearButton);

            var suggestionsButton = new Button
            {
                Text = "Get Suggestions",
                Size = new Size(120, 25),
                Location = new Point(140, 2)
            };
            suggestionsButton.Click += GetSuggestionsButton_Click;
            buttonPanel.Controls.Add(suggestionsButton);
        }

        private void CreateSettingsTab()
        {
            var settingsTab = new TabPage("Settings");
            _tabControl.TabPages.Add(settingsTab);

            var settingsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                AutoScroll = true
            };
            settingsTab.Controls.Add(settingsPanel);

            // API Keys section
            var apiKeysGroupBox = new GroupBox
            {
                Text = "API Keys",
                Size = new Size(350, 120),
                Location = new Point(0, 0)
            };
            settingsPanel.Controls.Add(apiKeysGroupBox);

            var openAILabel = new Label
            {
                Text = "OpenAI API Key:",
                Location = new Point(10, 25),
                Size = new Size(100, 20)
            };
            apiKeysGroupBox.Controls.Add(openAILabel);

            _openAITextBox = new TextBox
            {
                Location = new Point(10, 45),
                Size = new Size(320, 23),
                UseSystemPasswordChar = true,
                PlaceholderText = "Enter OpenAI API key"
            };
            apiKeysGroupBox.Controls.Add(_openAITextBox);

            var claudeLabel = new Label
            {
                Text = "Claude API Key:",
                Location = new Point(10, 75),
                Size = new Size(100, 20)
            };
            apiKeysGroupBox.Controls.Add(claudeLabel);

            _claudeTextBox = new TextBox
            {
                Location = new Point(10, 95),
                Size = new Size(320, 23),
                UseSystemPasswordChar = true,
                PlaceholderText = "Enter Claude API key"
            };
            apiKeysGroupBox.Controls.Add(_claudeTextBox);

            // MCP Settings section
            var mcpGroupBox = new GroupBox
            {
                Text = "MCP Server Settings",
                Size = new Size(350, 80),
                Location = new Point(0, 130)
            };
            settingsPanel.Controls.Add(mcpGroupBox);

            var mcpUrlLabel = new Label
            {
                Text = "Server URL:",
                Location = new Point(10, 25),
                Size = new Size(80, 20)
            };
            mcpGroupBox.Controls.Add(mcpUrlLabel);

            var mcpUrlTextBox = new TextBox
            {
                Location = new Point(10, 45),
                Size = new Size(320, 23),
                PlaceholderText = "http://localhost:8080"
            };
            mcpGroupBox.Controls.Add(mcpUrlTextBox);

            // UI Settings section
            var uiGroupBox = new GroupBox
            {
                Text = "UI Settings",
                Size = new Size(350, 60),
                Location = new Point(0, 220)
            };
            settingsPanel.Controls.Add(uiGroupBox);

            var themeLabel = new Label
            {
                Text = "Theme:",
                Location = new Point(10, 25),
                Size = new Size(50, 20)
            };
            uiGroupBox.Controls.Add(themeLabel);

            _themeComboBox = new ComboBox
            {
                Location = new Point(70, 22),
                Size = new Size(100, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _themeComboBox.Items.AddRange(new[] { "Light", "Dark", "Auto" });
            _themeComboBox.SelectedIndex = 0;
            uiGroupBox.Controls.Add(_themeComboBox);

            // Save button
            var saveButton = new Button
            {
                Text = "Save Settings",
                Size = new Size(100, 30),
                Location = new Point(0, 290),
                BackColor = Color.Green,
                ForeColor = Color.White
            };
            saveButton.Click += SaveButton_Click;
            settingsPanel.Controls.Add(saveButton);
        }

        private void CreateHistoryTab()
        {
            var historyTab = new TabPage("History");
            _tabControl.TabPages.Add(historyTab);

            _historyListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            _historyListView.Columns.Add("Time", 100);
            _historyListView.Columns.Add("Input", 200);
            _historyListView.Columns.Add("Model", 80);
            _historyListView.DoubleClick += HistoryListView_DoubleClick;
            historyTab.Controls.Add(_historyListView);
        }

        private async void SendButton_Click(object? sender, EventArgs e)
        {
            var input = _inputTextBox.Text;
            if (string.IsNullOrWhiteSpace(input))
                return;

            SetBusyState(true);
            LogInteraction("User", input);

            try
            {
                var response = await _aiManager.NlpProcessor.ProcessNaturalLanguageAsync(input);
                LogInteraction("AI", response);
                AddHistoryItem(input, response);
            }
            catch (Exception ex)
            {
                AddToOutput($"Error: {ex.Message}", Color.Red);
                SetStatus("Error occurred", Color.Red);
                _logger?.LogError(ex, "AI request processing failed");
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Control)
            {
                SendButton_Click(sender, e);
                e.Handled = true;
            }
        }

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            _outputTextBox.Clear();
            _inputTextBox.Clear();
            SetStatus("Cleared", Color.Blue);
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save API keys from text boxes
                _aiManager?.Config.SaveApiKey("OpenAI", _openAITextBox.Text);
                _aiManager?.Config.SaveApiKey("Claude", _claudeTextBox.Text);

                // Save other settings
                _aiManager?.Config.SetSetting("UI:Theme", _themeComboBox.SelectedItem?.ToString() ?? "Light");

                // Save configuration
                _aiManager?.Config.SaveConfiguration();

                SetStatus("Settings saved", Color.Green);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save settings: {ex.Message}");
            }
        }

        private void HistoryListView_DoubleClick(object? sender, EventArgs e)
        {
            if (_historyListView.SelectedItems.Count > 0)
            {
                var item = _historyListView.SelectedItems[0];
                _inputTextBox.Text = item.SubItems[1].Text;
            }
        }

        private void AddToOutput(string text, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddToOutput(text, color)));
                return;
            }

            _outputTextBox.SelectionStart = _outputTextBox.TextLength;
            _outputTextBox.SelectionLength = 0;
            _outputTextBox.SelectionColor = color;
            _outputTextBox.AppendText($"{DateTime.Now:HH:mm:ss} - {text}\n");
            _outputTextBox.SelectionColor = _outputTextBox.ForeColor;
            _outputTextBox.ScrollToCaret();
        }

        private void AddToHistory(string input, string model)
        {
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(input.Length > 50 ? input.Substring(0, 47) + "..." : input);
            item.SubItems.Add(model);
            _historyListView.Items.Insert(0, item);

            // Keep only last 100 items
            while (_historyListView.Items.Count > 100)
            {
                _historyListView.Items.RemoveAt(_historyListView.Items.Count - 1);
            }
        }

        private void SetStatus(string message, Color color)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetStatus(message, color)));
                return;
            }

            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;
        }

        private void SetBusyState(bool busy)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetBusyState(busy)));
                return;
            }

            _sendButton.Enabled = !busy;
            _progressBar.Visible = busy;
            
            if (busy)
            {
                SetStatus("Processing...", Color.Orange);
            }
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "RhinoAI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _logger?.LogError($"UI Error: {message}");
        }

        private void LogInteraction(string role, string message)
        {
            AddToOutput($"{role}: {message}", role == "User" ? Color.Blue : Color.DarkGreen);
        }

        private void AddHistoryItem(string input, string response)
        {
            AddToHistory(input, _aiModelComboBox.SelectedItem?.ToString() ?? "OpenAI GPT");
            _inputTextBox.Clear();
            SetStatus("Request completed", Color.Green);
        }

        private async void GetSuggestionsButton_Click(object? sender, EventArgs e)
        {
            await FetchAndShowSuggestions();
        }

        private async Task FetchAndShowSuggestions()
        {
            if (_aiManager?.MCPClient == null)
            {
                ShowError("MCP Client not available.");
                return;
            }

            if (!_aiManager.MCPClient.IsConnected)
            {
                ShowError("MCP Server is not connected.");
                return;
            }

            try
            {
                SetBusyState(true);
                SetStatus("Getting suggestions...", Color.Black);

                var suggestions = await _aiManager.MCPClient.GetSuggestionsAsync();

                _suggestionsPanel.Controls.Clear();
                if (suggestions != null && suggestions.Any())
                {
                    var yPos = 5;
                    foreach (var suggestion in suggestions)
                    {
                        var label = new Label
                        {
                            Text = suggestion,
                            AutoSize = true,
                            Location = new Point(5, yPos),
                            Cursor = Cursors.Hand,
                            ForeColor = Color.Blue
                        };
                        label.Click += (s, args) => {
                            _inputTextBox.Text = suggestion;
                        };
                        _suggestionsPanel.Controls.Add(label);
                        yPos += label.Height + 2;
                    }
                    _suggestionsPanel.Visible = true;
                }
                else
                {
                    _suggestionsPanel.Visible = false;
                }

                SetStatus("Ready", Color.Green);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to get suggestions: {ex.Message}");
                _logger?.LogError(ex, "Error fetching suggestions");
            }
            finally
            {
                SetBusyState(false);
            }
        }
    }
} 