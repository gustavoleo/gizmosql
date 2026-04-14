using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using CaptureRunner.Models;

namespace CaptureRunner.Services;

public sealed class OperatorNoticeService : IDisposable
{
    private readonly DisplayRuntimeInfo? _operatorDisplay;
    private readonly string _message;
    private Thread? _uiThread;
    private OperatorNoticeForm? _form;
    private readonly ManualResetEventSlim _ready = new(false);

    public OperatorNoticeService(DisplayRuntimeInfo? operatorDisplay, string message)
    {
        _operatorDisplay = operatorDisplay;
        _message = string.IsNullOrWhiteSpace(message)
            ? ScannerOptions.DefaultOperatorMessage
            : message.Trim();
    }

    public void Show()
    {
        if (_uiThread is not null)
        {
            return;
        }

        if (_operatorDisplay is null)
        {
            Console.WriteLine("Operator notice skipped: no certified operator display was configured or resolved.");
            return;
        }

        _uiThread = new Thread(() =>
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _form = new OperatorNoticeForm(_operatorDisplay, _message);
            _form.Shown += (_, _) => _ready.Set();
            Application.Run(_form);
        })
        {
            IsBackground = true,
            Name = "CaptureRunnerOperatorNotice"
        };

        _uiThread.SetApartmentState(ApartmentState.STA);
        _uiThread.Start();
        _ready.Wait(TimeSpan.FromSeconds(3));
    }

    public void Dispose()
    {
        if (_form is not null && !_form.IsDisposed)
        {
            try
            {
                _form.BeginInvoke(new Action(() => _form.Close()));
            }
            catch
            {
                // Best effort only.
            }
        }

        if (_uiThread is not null && _uiThread.IsAlive)
        {
            _uiThread.Join(TimeSpan.FromSeconds(2));
        }

        _ready.Dispose();
    }

    private sealed class OperatorNoticeForm : Form
    {
        public OperatorNoticeForm(DisplayRuntimeInfo targetDisplay, string message)
        {
            Text = "CaptureRunner Operator Notice";
            TopMost = true;
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(32, 32, 32);
            ForeColor = Color.White;
            ClientSize = new Size(640, 220);
            Location = CenterInWorkingArea(targetDisplay, ClientSize);

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(24, 18, 24, 0),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Text = "Automation Running",
                ForeColor = Color.Gold
            };

            var body = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 8, 24, 24),
                Font = new Font("Segoe UI", 13, FontStyle.Regular),
                Text = message,
                AutoSize = false
            };

            Controls.Add(body);
            Controls.Add(title);
        }

        private static Point CenterInWorkingArea(DisplayRuntimeInfo display, Size formSize)
        {
            return new Point(
                display.WorkingLeft + Math.Max(0, (display.WorkingWidth - formSize.Width) / 2),
                display.WorkingTop + Math.Max(0, (display.WorkingHeight - formSize.Height) / 2));
        }
    }
}
