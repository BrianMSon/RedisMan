#if WINDOWS
using System.Windows.Forms;
using System.Drawing;

namespace WinTestForm
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            timerFirstPositionMove = new System.Windows.Forms.Timer(components);
            buttonKeys = new Button();
            buttonBringConsoleWindow = new Button();
            buttonAuth = new Button();
            comboBoxSelectDB = new ComboBox();
            buttonClientInfo = new Button();
            textBoxKeyword = new TextBox();
            SuspendLayout();
            // 
            // timerFirstPositionMove
            // 
            timerFirstPositionMove.Enabled = true;
            timerFirstPositionMove.Interval = 500;
            timerFirstPositionMove.Tick += timerFirstPositionMove_Tick;
            // 
            // buttonKeys
            // 
            buttonKeys.Location = new Point(12, 41);
            buttonKeys.Name = "buttonKeys";
            buttonKeys.Size = new Size(54, 25);
            buttonKeys.TabIndex = 2;
            buttonKeys.Text = "keys";
            buttonKeys.UseVisualStyleBackColor = true;
            buttonKeys.Click += buttonKeys_Click;
            // 
            // buttonBringConsoleWindow
            // 
            buttonBringConsoleWindow.BackColor = Color.Chocolate;
            buttonBringConsoleWindow.Location = new Point(638, 7);
            buttonBringConsoleWindow.Name = "buttonBringConsoleWindow";
            buttonBringConsoleWindow.Size = new Size(75, 23);
            buttonBringConsoleWindow.TabIndex = 16;
            buttonBringConsoleWindow.Text = "CONSOLE";
            buttonBringConsoleWindow.UseVisualStyleBackColor = false;
            buttonBringConsoleWindow.Click += buttonBringConsoleWindow_Click;
            // 
            // buttonAuth
            // 
            buttonAuth.Location = new Point(12, 12);
            buttonAuth.Name = "buttonAuth";
            buttonAuth.Size = new Size(54, 23);
            buttonAuth.TabIndex = 17;
            buttonAuth.Text = "Auth";
            buttonAuth.UseVisualStyleBackColor = true;
            buttonAuth.Click += buttonAuth_Click;
            // 
            // comboBoxSelectDB
            // 
            comboBoxSelectDB.FormattingEnabled = true;
            comboBoxSelectDB.Items.AddRange(new object[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" });
            comboBoxSelectDB.Location = new Point(72, 13);
            comboBoxSelectDB.Name = "comboBoxSelectDB";
            comboBoxSelectDB.Size = new Size(53, 23);
            comboBoxSelectDB.TabIndex = 18;
            comboBoxSelectDB.Text = "0";
            comboBoxSelectDB.SelectedIndexChanged += comboBoxSelectDB_SelectedIndexChanged;
            comboBoxSelectDB.KeyDown += comboBoxSelectDB_KeyDown;
            comboBoxSelectDB.Leave += comboBoxSelectDB_Leave;
            // 
            // buttonClientInfo
            // 
            buttonClientInfo.Location = new Point(131, 13);
            buttonClientInfo.Name = "buttonClientInfo";
            buttonClientInfo.Size = new Size(78, 23);
            buttonClientInfo.TabIndex = 19;
            buttonClientInfo.Text = "Client Info";
            buttonClientInfo.UseVisualStyleBackColor = true;
            buttonClientInfo.Click += buttonClientInfo_Click;
            // 
            // textBoxKeyword
            // 
            textBoxKeyword.Location = new Point(72, 42);
            textBoxKeyword.Name = "textBoxKeyword";
            textBoxKeyword.Size = new Size(100, 23);
            textBoxKeyword.TabIndex = 20;
            textBoxKeyword.Text = "*";
            textBoxKeyword.KeyDown += textBoxKeyword_KeyDown;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 705);
            Controls.Add(textBoxKeyword);
            Controls.Add(buttonClientInfo);
            Controls.Add(comboBoxSelectDB);
            Controls.Add(buttonAuth);
            Controls.Add(buttonBringConsoleWindow);
            Controls.Add(buttonKeys);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "RedisForm";
            Load += MainForm_Load;
            ResizeEnd += MainForm_ResizeEnd;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox textBoxAuthId;
        private System.Windows.Forms.Timer timerFirstPositionMove;
        private Button buttonKeys;
        private Button buttonBringConsoleWindow;
        private Button buttonAuth;
        private ComboBox comboBoxSelectDB;
        private Button buttonClientInfo;
        private TextBox textBoxKeyword;
    }
}
#endif // WINDOWS
