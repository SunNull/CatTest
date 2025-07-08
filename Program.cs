using System.ComponentModel;
using System.Diagnostics;
using System.Resources;
using Timer = System.Windows.Forms.Timer;

namespace CatTest
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            var procMutex = new Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result)
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
        }
    }
    public class RunCatApplicationContext : ApplicationContext
    {
        private const int CPU_TIMER_DEFAULT_INTERVAL = 3000;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;

        private PerformanceCounter cpuCounter;
        private NotifyIcon notifyIcon;
        private string runner = string.Empty;
        private int current = 0;
        private float minCpu;
        private float interval;
        private string systemTheme = string.Empty;
        private string manualTheme = UserSetting.Default.Theme;
        private string speed = UserSetting.Default.Speed;
        private Icon[] icons;
        private Timer animateTimer = new Timer();
        private Timer cpuTimer = new Timer();

        public RunCatApplicationContext()
        {
            UserSetting.Default.Reload();
            runner = UserSetting.Default.Runner;
            manualTheme = UserSetting.Default.Theme;
            Application.ApplicationExit += new EventHandler(onapplication);
            cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            _ = cpuCounter.NextValue();

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripSeparator(),
                new ToolStripMenuItem($"{Application.ProductName} v{Application.ProductVersion}")
                {
                    Enabled = false
                },
                new ToolStripMenuItem("Exit", null, Exit)
            });

            notifyIcon = new NotifyIcon()
            {
                Icon = Resource.light_cat_0,
                ContextMenuStrip = contextMenuStrip,
                Text = "0.0%",
                Visible = true
            };

            SetAnimation();
            StartObserveCPU();
            current = 1;
        }

        private void onapplication(object? sender, EventArgs e)
        {
            UserSetting.Default.Speed = speed;
            UserSetting.Default.Theme = manualTheme;
            UserSetting.Default.Runner = runner;
            UserSetting.Default.Save();
        }

        private void SetAnimation()
        {
            animateTimer.Interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            animateTimer.Tick += new EventHandler(AnimationTick);
        }
        private void AnimationTick(object sender, EventArgs e)
        {
            if (icons.Length <= current) current = 0;
            notifyIcon.Icon = icons[current];
            current = (current + 1) % icons.Length;
        }

        private void StartObserveCPU()
        {
            cpuTimer.Interval = CPU_TIMER_DEFAULT_INTERVAL;
            cpuTimer.Tick += new EventHandler(ObserveCPUTick);
            cpuTimer.Start();
        }

        private void ObserveCPUTick(object sender, EventArgs e)
        {
            CPUTick();
        }
        private void CPUTick()
        {
            interval = Math.Min(100, cpuCounter.NextValue()); // Sometimes got over 100% so it should be limited to 100%
            notifyIcon.Text = $"CPU: {interval:f1}%";
            interval = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, interval / 5.0f));
            _ = interval;
            CPUTickSpeed();
        }
        private void CPUTickSpeed()
        {
            float manualInterval = (float)Math.Max(minCpu, interval);
            animateTimer.Stop();
            animateTimer.Interval = (int)manualInterval;
            animateTimer.Start();

        }
        private void Exit(object sender, EventArgs e)
        {
            cpuCounter.Close();
            animateTimer.Stop();
            cpuTimer.Stop();
            notifyIcon.Visible = false;
            Application.Exit();
        }
    }
}