using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Resources;
using Timer = System.Windows.Forms.Timer;

namespace CatTest
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var procMutex = new Mutex(true, "_RUNCAT_MUTEX", out var result);
            if (!result)
            {
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new RunCatApplicationContext());
            procMutex.ReleaseMutex();
        }
    }
    public class RunCatApplicationContext : ApplicationContext
    {
        private const int CPU_TIMER_DEFAULT_INTERVAL = 2000;
        private const int ANIMATE_TIMER_DEFAULT_INTERVAL = 200;

        private PerformanceCounter cpuCounter;
        private NotifyIcon notifyIcon;
        private int current = 0;
        private float minCpu;
        private float interval;
        private Icon[] icons;
        private static bool IsContiniu = true;

        public RunCatApplicationContext()
        {
            UserSetting.Default.Reload();
            Application.ApplicationExit += new EventHandler(onapplication);
            cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            _ = cpuCounter.NextValue();
            var contextMenuStrip = new ContextMenuStrip(new Container());
            contextMenuStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("Exit", null, Exit)
            });
            notifyIcon = new NotifyIcon()
            {
                Icon = Resource.light_cat_0,
                Text = "0.0%",
                Visible = true,
                ContextMenuStrip=contextMenuStrip
            };
            notifyIcon.DoubleClick += (sender, e) =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    UseShellExecute = false,
                    Arguments = " -c Start-Process taskmgr.exe",
                    CreateNoWindow = true,
                };
                Process.Start(startInfo);
            };
            var theme = GetAppsUseTheme();
            string themename = string.Empty;
            if(theme== "dark")
            {
                themename = "dark_cat_";
            }
            else
            {
                themename = "light_cat_";
            }
            ResourceManager rm = Resource.ResourceManager;
            List<Icon> iconList = new List<Icon>();
            for (int i = 0; i < 5; i++)
            {
                Icon? ic = (Icon)rm.GetObject($"{themename}{i}");
                if (ic == null)
                {
                    continue;
                }
                iconList.Add(ic);
            }
            icons = iconList.ToArray();
            current = 1;
            Task.Run(async () => await WhileFoeve());
            Task.Run(async () => await GetCpu());


        }

        private string GetAppsUseTheme()
        {
            string keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using (RegistryKey rKey = Registry.CurrentUser.OpenSubKey(keyName))
            {
                object value;
                if (rKey == null || (value = rKey.GetValue("SystemUsesLightTheme")) == null)
                {
                    return "light";
                }
                int theme = (int)value;
                return theme == 0 ? "dark" : "light";
            }
        }
        private void onapplication(object? sender, EventArgs e)
        {
            IsContiniu = false;
        }

        public async Task WhileFoeve()
        {
            interval = ANIMATE_TIMER_DEFAULT_INTERVAL;
            while (IsContiniu)
            {
                if (icons.Length <= current) current = 0;
                notifyIcon.Icon = icons[current];
                current = (current + 1) % icons.Length;
                await Task.Delay((int)interval);
            }
        }

        public async Task GetCpu()
        {
            float a = 0f;
            while (IsContiniu)
            {
                a = Math.Min(100, cpuCounter.NextValue());
                notifyIcon.Text = $"CPU: {a:f1}%";
                float manualInterval = 200.0f / (float)Math.Max(1.0f, Math.Min(20.0f, a) / 5.0f);
                interval = (float)Math.Max(minCpu, manualInterval);
                await Task.Delay(CPU_TIMER_DEFAULT_INTERVAL);
            }   
        }
        private void Exit(object sender, EventArgs e)
        {
            IsContiniu = false;
            Application.Exit();
        }
    }
}