#if WINDOWS
using System.Drawing;
using System.Security.Policy;
using System.Text.Json.Nodes;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static RedisMan.Program;

namespace WinTestForm
{
    public partial class MainForm : Form
    {
        private string _currentDB = "0";
        private bool _ignoreSelectDBEvent = false;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        private void timerFirstPositionMove_Tick(object sender, EventArgs e)
        {
            string consoleTitle = CONSOLE_TITLE;
            IntPtr consoleHandle = FindWindow(null!, consoleTitle);

            // 폼 위치를 콘솔 오른쪽에 위치시킴
            if (consoleHandle != IntPtr.Zero && GetWindowRect(consoleHandle, out RECT rect))
            {
                int consoleRight = rect.Right;
                int consoleTop = rect.Top;

                this.Left = consoleRight - 14;
                this.Top = consoleTop;

                // Timer 끄기
                timerFirstPositionMove.Stop();
            }
        }

        private void setFocusToConsole()
        {
            // 콘솔 창으로 포커스 이동
            string consoleTitle = CONSOLE_TITLE;
            IntPtr consoleHandle = FindWindow(null!, consoleTitle);
            if (consoleHandle != IntPtr.Zero)
            {
                SetForegroundWindow(consoleHandle); // 콘솔에 포커스 줌
            }
        }

        private void sendTextInputToConsole(string text)
        {
            // 콘솔 창에 문자열 키보드 입력
            string consoleTitle = CONSOLE_TITLE;
            IntPtr consoleHandle = FindWindow(null!, consoleTitle);
            if (consoleHandle != IntPtr.Zero)
            {
                SetForegroundWindow(consoleHandle); // 콘솔에 포커스 줌

                foreach (char c in text)
                {
                    SendKeys.Send(c.ToString());
                }
                SendKeys.Send("{ENTER}");
                SendKeys.Flush();
            }
        }

        public void sendFastTextInputToConsole(string text)
        {
            string consoleTitle = CONSOLE_TITLE;
            IntPtr consoleHandle = FindWindow(null!, consoleTitle);
            if (consoleHandle != IntPtr.Zero)
            {
                SetForegroundWindow(consoleHandle);

                var sim = new InputSimulator();
                sim.Keyboard.TextEntry(text); // 빠르게 문자 전체 입력
                // 잠시 대기
                System.Threading.Thread.Sleep(200); // 대기
                sim.Keyboard.KeyPress(VirtualKeyCode.RETURN); // Enter 키 입력
            }
        }

        private void buttonBringConsoleWindow_Click(object sender, EventArgs e)
        {
            // 콘솔창 가져오기
            string consoleTitle = CONSOLE_TITLE;
            IntPtr consoleHandle = FindWindow(null!, consoleTitle);

            // 콘솔 창 위치를 폼 위치의 왼쪽에 위치시키도록
            if (consoleHandle != IntPtr.Zero)
            {
                RECT consoleRect;
                if (GetWindowRect(consoleHandle, out consoleRect))
                {
                    int consoleWidth = consoleRect.Right - consoleRect.Left;

                    // 콘솔 창의 오른쪽이 폼의 왼쪽과 맞닿도록 위치 조정
                    int targetX = this.Left - consoleWidth + 14;
                    int targetY = this.Top;

                    SetWindowPos(consoleHandle, HWND_TOP, targetX, targetY, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
                }
            }
        }

        //protected override void WndProc(ref Message m)
        //{
        //    const int WM_ENTERSIZEMOVE = 0x0231;
        //    const int WM_EXITSIZEMOVE = 0x0232;

        //    switch (m.Msg)
        //    {
        //    case WM_ENTERSIZEMOVE:
        //        //Console.WriteLine("이동 시작");
        //        break;
        //    case WM_EXITSIZEMOVE:
        //        //Console.WriteLine("이동 종료");
        //        buttonBringConsoleWindow_Click(null, null);
        //        break;
        //    }

        //    base.WndProc(ref m);
        //}

        ///////////////////////////////////////////////////////////
        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            buttonBringConsoleWindow_Click(null!, null!);
        }

        private void buttonAuth_Click(object sender, EventArgs e)
        {
            string password = "idoladmin9876";
            sendFastTextInputToConsole($"AUTH {password}");
        }

        private void buttonKeysAll_Click(object sender, EventArgs e)
        {
            //sendTextInputToConsole($"KEYS *");
            sendFastTextInputToConsole($"KEYS *");
        }

        private void buttonClientInfo_Click(object sender, EventArgs e)
        {
            sendFastTextInputToConsole($"client info");
        }

        private void comboBoxSelectDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_ignoreSelectDBEvent == true)
            {
                _ignoreSelectDBEvent = false;
                return;
            }

            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.Text == "")
            {
                return;
            }
            _currentDB = comboBox.Text;
            sendFastTextInputToConsole($"select {comboBox.Text}");
        }

        private void comboBoxSelectDB_KeyDown(object sender, KeyEventArgs e)
        {
            // 엔터키를 눌렀을 때
            if (e.KeyCode == Keys.Enter)
            {
                ComboBox comboBox = (ComboBox)sender;
                if (comboBox.Text == "")
                {
                    return;
                }
                _currentDB = comboBox.Text;
                sendFastTextInputToConsole($"select {comboBox.Text}");
                e.SuppressKeyPress = true; // 엔터 키 입력을 방지
            }
        }

        private void comboBoxSelectDB_Leave(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            _ignoreSelectDBEvent = true;
            comboBox.Text = _currentDB;
        }



        ///////////////////////////////////////////////
    }
}
#endif // WINDOWS
