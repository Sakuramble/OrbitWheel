using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("OrbitWheel")]
[assembly: AssemblyDescription("OrbitWheel 1.0 - 径向快捷操作中心")]
[assembly: AssemblyCompany("OrbitWheel")]
[assembly: AssemblyProduct("OrbitWheel")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace OrbitWheelLite
{
    public class ActionItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Target { get; set; }
    }

    public class WheelPage
    {
        public string Name { get; set; }
        public List<ActionItem> Actions { get; set; }
    }

    public class AppConfig
    {
        public int Modifiers { get; set; }
        public int KeyCode { get; set; }
        public string Mode { get; set; }
        public string Style { get; set; }
        public bool StartWithWindows { get; set; }
        public List<WheelPage> Pages { get; set; }

        public static AppConfig Default()
        {
            return new AppConfig {
                Modifiers = 2,
                KeyCode = (int)Keys.Space,
                Mode = "Hold",
                Style = "液态玻璃",
                StartWithWindows = false,
                Pages = new List<WheelPage> {
                    new WheelPage {
                        Name = "常用",
                        Actions = new List<ActionItem> {
                            A("资源管理器", "Explorer", ""),
                            A("设置", "Settings", ""),
                            A("锁定", "Lock", ""),
                            A("音量 +", "VolumeUp", ""),
                            A("音量 -", "VolumeDown", ""),
                            A("睡眠", "Sleep", "")
                        }
                    }
                }
            };
        }

        private static ActionItem A(string name, string type, string target)
        {
            return new ActionItem { Name = name, Type = type, Target = target };
        }
    }

    static class ConfigStore
    {
        public static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OrbitWheel");
        public static readonly string FilePath = Path.Combine(Folder, "config.json");

        public static AppConfig Load()
        {
            try {
                if (File.Exists(FilePath)) {
                    AppConfig c = new JavaScriptSerializer().Deserialize<AppConfig>(File.ReadAllText(FilePath));
                    Normalize(c);
                    return c;
                }
            } catch { }
            AppConfig d = AppConfig.Default();
            Save(d);
            return d;
        }

        public static void Save(AppConfig config)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(config));
        }

        private static void Normalize(AppConfig c)
        {
            if (c.Pages == null || c.Pages.Count == 0) c.Pages = AppConfig.Default().Pages;
            foreach (WheelPage p in c.Pages) {
                if (p.Actions == null) p.Actions = new List<ActionItem>();
                while (p.Actions.Count < 6) p.Actions.Add(new ActionItem { Name = "空", Type = "None", Target = "" });
                if (p.Actions.Count > 6) p.Actions.RemoveRange(6, p.Actions.Count - 6);
            }
            if (String.IsNullOrEmpty(c.Mode)) c.Mode = "Hold";
            if (String.IsNullOrEmpty(c.Style)) c.Style = "液态玻璃";
        }
    }

    static class Native
    {
        public const int WM_HOTKEY = 0x0312;
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYUP = 0x0101;
        public const int WM_SYSKEYUP = 0x0105;
        public const int MOD_ALT = 1;
        public const int MOD_CONTROL = 2;
        public const int MOD_SHIFT = 4;
        public const int MOD_WIN = 8;

        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern IntPtr SetWindowsHookEx(int idHook, KeyboardProc callback, IntPtr module, uint threadId);
        [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] public static extern bool LockWorkStation();
        [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr data);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder text, int count);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern int GetApplicationUserModelId(IntPtr process, ref uint length, System.Text.StringBuilder appId);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SHGetFileInfo(string path, uint attributes, ref ShellFileInfo info, uint size, uint flags);
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] public static extern int SHParseDisplayName(string name, IntPtr bindingContext, out IntPtr pidl, uint attributesIn, out uint attributesOut);
        [DllImport("shell32.dll", EntryPoint = "SHGetFileInfoW", CharSet = CharSet.Unicode)] public static extern IntPtr SHGetFileInfoPidl(IntPtr pidl, uint attributes, ref ShellFileInfo info, uint size, uint flags);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("ole32.dll")] public static extern void CoTaskMemFree(IntPtr pointer);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct ShellFileInfo { public IntPtr Icon; public int IconIndex; public uint Attributes; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string DisplayName; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string TypeName; }
        public delegate IntPtr KeyboardProc(int code, IntPtr wp, IntPtr lp);
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr data);
    }

    class HotkeyWindow : NativeWindow, IDisposable
    {
        public event EventHandler Triggered;
        public HotkeyWindow() { CreateHandle(new CreateParams()); }
        public bool Set(int modifiers, int key)
        {
            Native.UnregisterHotKey(Handle, 77);
            return Native.RegisterHotKey(Handle, 77, (uint)modifiers, (uint)key);
        }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY && Triggered != null) Triggered(this, EventArgs.Empty);
            base.WndProc(ref m);
        }
        public void Dispose() { Native.UnregisterHotKey(Handle, 77); DestroyHandle(); }
    }

    class KeyboardWatcher : IDisposable
    {
        private Native.KeyboardProc callback;
        private IntPtr hook;
        private int triggerKey;
        public event EventHandler TriggerReleased;
        public KeyboardWatcher(int key)
        {
            triggerKey = key;
            callback = Proc;
            hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, callback, Native.GetModuleHandle(null), 0);
        }
        private IntPtr Proc(int code, IntPtr wp, IntPtr lp)
        {
            if (code >= 0 && (wp.ToInt32() == Native.WM_KEYUP || wp.ToInt32() == Native.WM_SYSKEYUP)) {
                int vk = Marshal.ReadInt32(lp);
                if (vk == triggerKey && TriggerReleased != null) TriggerReleased(this, EventArgs.Empty);
            }
            return Native.CallNextHookEx(hook, code, wp, lp);
        }
        public void Dispose() { if (hook != IntPtr.Zero) Native.UnhookWindowsHookEx(hook); }
    }

    static class IconFactory
    {
        public static Icon AppIcon()
        {
            Bitmap b = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(b)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (LinearGradientBrush bg = new LinearGradientBrush(new Rectangle(0,0,64,64), Color.FromArgb(64,215,255), Color.FromArgb(123,76,255), 45))
                    g.FillEllipse(bg, 3, 3, 58, 58);
                using (Pen p = new Pen(Color.FromArgb(225,255,255,255), 5)) {
                    g.DrawArc(p, 15, 15, 34, 34, 20, 275);
                    p.StartCap = LineCap.Round; p.EndCap = LineCap.Round;
                    g.DrawLine(p, 34, 32, 46, 20);
                }
                g.FillEllipse(Brushes.White, 29, 27, 9, 9);
            }
            return Icon.FromHandle(b.GetHicon());
        }
    }

    class WheelForm : Form
    {
        private AppConfig config;
        private int pageIndex;
        private int selected = -1;
        private Point center;
        private bool closing;
        private Bitmap backdrop;
        private Timer animationTimer;
        private float animationPhase;
        private const int Outer = 235;
        private const int Inner = 67;
        public event Action<ActionItem> ExecuteRequested;
        public event EventHandler CloseRequested;

        public WheelForm(AppConfig c)
        {
            config = c;
            Text = "OrbitWheel Menu";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            Size = new Size(510, 510);
            Point mouse = Cursor.Position;
            Location = new Point(mouse.X - Width / 2, mouse.Y - Height / 2);
            center = new Point(Width / 2, Height / 2);
            BackColor = Color.FromArgb(1, 2, 3);
            TransparencyKey = Color.FromArgb(1, 2, 3);
            backdrop = CaptureAndBlur(Location, Size, config.Style);
            animationTimer = new Timer { Interval = 33 };
            animationTimer.Tick += delegate { animationPhase += 1.6f; if (animationPhase >= 360) animationPhase -= 360; Invalidate(); };
            animationTimer.Start();
            MouseMove += OnMove;
            MouseDown += OnDown;
            MouseWheel += OnWheel;
            KeyDown += OnKey;
            Deactivate += delegate { if (config.Mode == "Click" && Visible) RequestClose(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
            Focus();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (animationTimer != null) { animationTimer.Stop(); animationTimer.Dispose(); }
            if (backdrop != null) backdrop.Dispose();
            base.OnFormClosed(e);
        }

        private Bitmap CaptureAndBlur(Point location, Size size, string style)
        {
            Bitmap source = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(source)) {
                g.Clear(Color.FromArgb(18, 23, 34));
                Rectangle requested = new Rectangle(location, size);
                Rectangle visible = Rectangle.Intersect(requested, SystemInformation.VirtualScreen);
                if (visible.Width > 0 && visible.Height > 0)
                    g.CopyFromScreen(visible.Location, new Point(visible.X - location.X, visible.Y - location.Y), visible.Size);
            }
            int divisor = style == "高斯模糊" ? 18 : style == "亚克力" ? 7 : 11;
            Bitmap small = new Bitmap(Math.Max(1, size.Width / divisor), Math.Max(1, size.Height / divisor));
            using (Graphics g = Graphics.FromImage(small)) {
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.DrawImage(source, new Rectangle(Point.Empty, small.Size));
            }
            Bitmap result = new Bitmap(size.Width, size.Height);
            using (Graphics g = Graphics.FromImage(result)) {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(small, new Rectangle(Point.Empty, result.Size));
                int materialAlpha = style == "亚克力" ? 188 : style == "液态玻璃" ? 105 : 58;
                Color tint = style == "亚克力" ? Color.FromArgb(materialAlpha, 23, 29, 43) : style == "液态玻璃" ? Color.FromArgb(materialAlpha, 28, 48, 88) : Color.FromArgb(materialAlpha, 20, 25, 38);
                using (SolidBrush b = new SolidBrush(tint)) g.FillRectangle(b, 0, 0, result.Width, result.Height);
                if (style == "亚克力") {
                    Random random = new Random(8);
                    using (SolidBrush grain = new SolidBrush(Color.FromArgb(11, 255, 255, 255)))
                        for (int i = 0; i < 1500; i++) g.FillRectangle(grain, random.Next(result.Width), random.Next(result.Height), 1, 1);
                }
            }
            small.Dispose();
            source.Dispose();
            return result;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.FromArgb(1, 2, 3));
            if (backdrop != null) {
                GraphicsState state = e.Graphics.Save();
                using (GraphicsPath clip = new GraphicsPath()) {
                    clip.AddEllipse(center.X - Outer + 2, center.Y - Outer + 2, (Outer - 2) * 2, (Outer - 2) * 2);
                    e.Graphics.SetClip(clip);
                    e.Graphics.DrawImageUnscaled(backdrop, Point.Empty);
                }
                e.Graphics.Restore(state);
            }
        }

        private void OnKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) RequestClose();
        }

        private void OnWheel(object sender, MouseEventArgs e)
        {
            if (config.Pages.Count < 2) return;
            pageIndex = (pageIndex + (e.Delta < 0 ? 1 : -1) + config.Pages.Count) % config.Pages.Count;
            selected = -1;
            Invalidate();
        }

        private void OnMove(object sender, MouseEventArgs e)
        {
            double dx = e.X - center.X, dy = e.Y - center.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            int old = selected;
            if (dist < Inner || dist > Outer + 35) selected = -1;
            else {
                double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
                if (angle < 0) angle += 360;
                selected = ((int)Math.Floor((angle + 30) / 60)) % 6;
            }
            if (old != selected) Invalidate();
        }

        private void OnDown(object sender, MouseEventArgs e)
        {
            double dx = e.X - center.X, dy = e.Y - center.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist <= Inner) { RequestClose(); return; }
            if (selected >= 0 && config.Mode == "Click") RequestExecute();
        }

        public void ExecuteHoldSelection()
        {
            if (selected >= 0) RequestExecute(); else RequestClose();
        }

        private void RequestExecute()
        {
            ActionItem a = config.Pages[pageIndex].Actions[selected];
            closing = true;
            Hide();
            if (ExecuteRequested != null) ExecuteRequested(a);
            Close();
        }
        private void RequestClose()
        {
            if (closing) return;
            closing = true;
            if (CloseRequested != null) CloseRequested(this, EventArgs.Empty);
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            bool acrylic = config.Style == "亚克力";
            bool blur = config.Style == "高斯模糊";
            int fillAlpha = acrylic ? 70 : blur ? 18 : 38;
            Color baseColor = acrylic ? Color.FromArgb(fillAlpha, 35, 42, 58) : blur ? Color.FromArgb(fillAlpha, 28, 33, 48) : Color.FromArgb(fillAlpha, 22, 36, 66);
            Color accent = acrylic ? Color.FromArgb(150, 92, 130, 185) : blur ? Color.FromArgb(90, 160, 200, 245) : Color.FromArgb(125, 87, 190, 255);

            for (int i = 0; i < 6; i++) {
                using (GraphicsPath path = SegmentPath(center, Inner + 14, Outer - 3, i * 60 - 28, 56)) {
                    using (SolidBrush fill = new SolidBrush(i == selected ? accent : baseColor)) g.FillPath(fill, path);
                    using (Pen border = new Pen(Color.FromArgb(i == selected ? 220 : 46, 220, 240, 255), i == selected ? 2f : 1f)) g.DrawPath(border, path);
                }
                DrawAction(g, i);
            }

            DrawRealtimeGlass(g);

            using (Pen cleanEdge = new Pen(Color.FromArgb(245, 112, 164, 224), 4f))
                g.DrawEllipse(cleanEdge, center.X - Outer + 4, center.Y - Outer + 4, (Outer - 4) * 2, (Outer - 4) * 2);

            Rectangle closeRect = new Rectangle(center.X - Inner, center.Y - Inner, Inner * 2, Inner * 2);
            using (LinearGradientBrush cb = new LinearGradientBrush(closeRect, Color.FromArgb(220,38,45,65), Color.FromArgb(210,24,30,46), 45)) g.FillEllipse(cb, closeRect);
            using (Pen ring = new Pen(Color.FromArgb(130, 210, 235, 255), 1.5f)) g.DrawEllipse(ring, closeRect);
            using (Pen x = new Pen(Color.FromArgb(235, 245, 250, 255), 3)) {
                x.StartCap = x.EndCap = LineCap.Round;
                g.DrawLine(x, center.X - 12, center.Y - 12, center.X + 12, center.Y + 12);
                g.DrawLine(x, center.X + 12, center.Y - 12, center.X - 12, center.Y + 12);
            }

            string page = (pageIndex + 1) + "/" + config.Pages.Count;
            using (Font f = new Font("Microsoft YaHei UI", 8f))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(205, 225, 235, 250)))
                g.DrawString(page, f, b, center.X - 12, center.Y + 39);
        }

        private void DrawRealtimeGlass(Graphics g)
        {
            Rectangle ring = new Rectangle(center.X - Outer + 5, center.Y - Outer + 5, (Outer - 5) * 2, (Outer - 5) * 2);
            using (Pen glow = new Pen(Color.FromArgb(config.Style == "液态玻璃" ? 115 : 55, 185, 225, 255), config.Style == "液态玻璃" ? 7f : 3f)) {
                glow.StartCap = glow.EndCap = LineCap.Round;
                g.DrawArc(glow, ring, animationPhase, config.Style == "液态玻璃" ? 64 : 38);
                g.DrawArc(glow, ring, animationPhase + 180, config.Style == "液态玻璃" ? 42 : 24);
            }
            if (config.Style == "液态玻璃") {
                double a = animationPhase * Math.PI / 180.0;
                int x = center.X + (int)(Math.Cos(a) * 122);
                int y = center.Y + (int)(Math.Sin(a) * 122);
                Rectangle sheen = new Rectangle(x - 55, y - 18, 110, 36);
                using (GraphicsPath path = new GraphicsPath()) {
                    path.AddEllipse(sheen);
                    using (PathGradientBrush b = new PathGradientBrush(path)) {
                        b.CenterColor = Color.FromArgb(42, 225, 245, 255);
                        b.SurroundColors = new Color[] { Color.FromArgb(0, 225, 245, 255) };
                        g.FillPath(b, path);
                    }
                }
            }
        }

        private GraphicsPath SegmentPath(Point c, int inner, int outer, float start, float sweep)
        {
            GraphicsPath p = new GraphicsPath();
            Rectangle ro = new Rectangle(c.X - outer, c.Y - outer, outer * 2, outer * 2);
            Rectangle ri = new Rectangle(c.X - inner, c.Y - inner, inner * 2, inner * 2);
            p.AddArc(ro, start, sweep);
            p.AddArc(ri, start + sweep, -sweep);
            p.CloseFigure();
            return p;
        }

        private void DrawAction(Graphics g, int i)
        {
            ActionItem a = config.Pages[pageIndex].Actions[i];
            double angle = i * Math.PI / 3;
            int radius = 151;
            Point p = new Point(center.X + (int)(Math.Cos(angle) * radius), center.Y + (int)(Math.Sin(angle) * radius));
            Rectangle iconRect = new Rectangle(p.X - 28, p.Y - 28, 56, 56);
            ActionIcons.Draw(g, a, iconRect, i == selected);
        }
    }

    static class ActionRunner
    {
        public static void Run(ActionItem a, Action showSettings)
        {
            try {
                switch (a.Type) {
                    case "App": ActivateOrStart(a.Target, a.Name); break;
                    case "Explorer": Start("explorer.exe", "shell:ThisPCFolder"); break;
                    case "Settings": showSettings(); break;
                    case "Lock": Native.LockWorkStation(); break;
                    case "Sleep": Application.SetSuspendState(PowerState.Suspend, true, false); break;
                    case "Shutdown": Start("shutdown.exe", "/s /t 0"); break;
                    case "Restart": Start("shutdown.exe", "/r /t 0"); break;
                    case "VolumeUp": MediaKey(0xAF); break;
                    case "VolumeDown": MediaKey(0xAE); break;
                    case "Mute": MediaKey(0xAD); break;
                    case "Command": Start("cmd.exe", "/c " + a.Target); break;
                }
            } catch (Exception ex) { MessageBox.Show("无法执行“" + a.Name + "”\n" + ex.Message, "OrbitWheel"); }
        }

        private static void MediaKey(byte key)
        {
            Native.keybd_event(key, 0, 0, UIntPtr.Zero);
            Native.keybd_event(key, 0, 2, UIntPtr.Zero);
        }

        private static void Start(string file, string args)
        {
            ProcessStartInfo info = new ProcessStartInfo(file, args);
            info.UseShellExecute = true;
            Process.Start(info);
        }

        private static void ActivateOrStart(string target, string displayName)
        {
            string executable = ShortcutResolver.ResolveTarget(target);
            string appId = target.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase) ? target.Substring("shell:AppsFolder\\".Length) : "";
            IntPtr existing = FindExistingWindow(executable, appId, displayName);
            if (existing != IntPtr.Zero) {
                Native.ShowWindow(existing, 9);
                Native.SetForegroundWindow(existing);
                return;
            }
            if (target.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase))
                Start("explorer.exe", target);
            else
                Start(target, "");
        }

        private static IntPtr FindExistingWindow(string executable, string appId, string displayName)
        {
            string expectedPath = File.Exists(executable) ? Path.GetFullPath(executable) : "";
            string expectedProcess = expectedPath.Length > 0 ? Path.GetFileNameWithoutExtension(expectedPath) : "";
            IntPtr found = IntPtr.Zero;
            Native.EnumWindows(delegate(IntPtr hwnd, IntPtr data) {
                if (!Native.IsWindowVisible(hwnd)) return true;
                uint processId;
                Native.GetWindowThreadProcessId(hwnd, out processId);
                if (processId == 0) return true;
                try {
                    using (Process process = Process.GetProcessById((int)processId)) {
                        bool matches = false;
                        if (expectedPath.Length > 0) {
                            try { matches = String.Equals(Path.GetFullPath(process.MainModule.FileName), expectedPath, StringComparison.OrdinalIgnoreCase); } catch { }
                            if (!matches && expectedProcess.Length > 0) matches = String.Equals(process.ProcessName, expectedProcess, StringComparison.OrdinalIgnoreCase);
                        }
                        if (!matches && appId.Length > 0) {
                            string runningAppId = GetAppUserModelId(process);
                            matches = runningAppId.Length > 0 && (String.Equals(runningAppId, appId, StringComparison.OrdinalIgnoreCase) || runningAppId.StartsWith(appId + "!", StringComparison.OrdinalIgnoreCase));
                        }
                        if (!matches && appId.Length > 0 && !String.IsNullOrWhiteSpace(displayName)) {
                            System.Text.StringBuilder title = new System.Text.StringBuilder(512);
                            Native.GetWindowText(hwnd, title, title.Capacity);
                            matches = title.Length > 0 && title.ToString().IndexOf(displayName, StringComparison.CurrentCultureIgnoreCase) >= 0;
                        }
                        if (matches) { found = hwnd; return false; }
                    }
                } catch { }
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static string GetAppUserModelId(Process process)
        {
            try {
                uint length = 0;
                Native.GetApplicationUserModelId(process.Handle, ref length, null);
                if (length == 0) return "";
                System.Text.StringBuilder id = new System.Text.StringBuilder((int)length);
                return Native.GetApplicationUserModelId(process.Handle, ref length, id) == 0 ? id.ToString() : "";
            } catch { return ""; }
        }
    }

    static class ShortcutResolver
    {
        public static string ResolveTarget(string path)
        {
            if (String.IsNullOrEmpty(path) || !path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase)) return path;
            try {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                object shell = Activator.CreateInstance(shellType);
                object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
                return Convert.ToString(shortcut.GetType().InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, null));
            } catch { return path; }
        }
    }

    static class ActionIcons
    {
        private static readonly Dictionary<string, Icon> Cache = new Dictionary<string, Icon>(StringComparer.OrdinalIgnoreCase);
        private static Bitmap systemIconSheet;
        private static readonly Dictionary<string, Point> SystemIconCells = new Dictionary<string, Point> {
            {"Explorer", new Point(0, 0)}, {"Settings", new Point(1, 0)}, {"Lock", new Point(2, 0)}, {"Sleep", new Point(3, 0)},
            {"Shutdown", new Point(0, 1)}, {"Restart", new Point(1, 1)}, {"VolumeUp", new Point(2, 1)}, {"VolumeDown", new Point(3, 1)},
            {"Mute", new Point(0, 2)}, {"Command", new Point(1, 2)}, {"None", new Point(2, 2)}
        };

        private static GraphicsPath Rounded(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }

        public static void Draw(Graphics g, ActionItem a, Rectangle r, bool selected)
        {
            if (a.Type == "App") {
                using (Icon appIcon = GetApplicationIcon(a.Target)) if (appIcon != null) { g.DrawIcon(appIcon, r); return; }
            }
            if (DrawGeneratedSystemIcon(g, a.Type, r)) return;
            Color fg = Color.FromArgb(245, 245, 250, 255);
            Rectangle tile = new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6);
            using (GraphicsPath tilePath = Rounded(tile, 13)) {
                using (LinearGradientBrush b = new LinearGradientBrush(tile, selected ? Color.FromArgb(190, 86, 176, 255) : Color.FromArgb(120, 78, 101, 137), selected ? Color.FromArgb(145, 65, 112, 235) : Color.FromArgb(75, 39, 53, 78), 45)) g.FillPath(b, tilePath);
                using (Pen outline = new Pen(Color.FromArgb(selected ? 210 : 85, 225, 242, 255), selected ? 1.7f : 1f)) g.DrawPath(outline, tilePath);
            }
            using (Pen p = new Pen(fg, 3f)) {
                p.StartCap = p.EndCap = LineCap.Round;
                int x = r.X, y = r.Y, w = r.Width, h = r.Height, cx = x + w / 2, cy = y + h / 2;
                switch (a.Type) {
                    case "App":
                        using (Font appFont = new Font("Segoe UI", 16f, FontStyle.Bold))
                        using (SolidBrush appText = new SolidBrush(fg)) {
                            string initial = String.IsNullOrWhiteSpace(a.Name) ? "A" : a.Name.Substring(0, 1).ToUpper();
                            SizeF size = g.MeasureString(initial, appFont);
                            g.DrawString(initial, appFont, appText, cx - size.Width / 2, cy - size.Height / 2);
                        }
                        break;
                    case "Explorer":
                        g.DrawRectangle(p, x + 9, y + 16, w - 18, h - 24);
                        g.DrawLine(p, x + 10, y + 16, x + 20, y + 10);
                        g.DrawLine(p, x + 20, y + 10, x + 29, y + 16);
                        break;
                    case "Settings":
                        g.DrawEllipse(p, x + 12, y + 12, w - 24, h - 24);
                        g.DrawEllipse(p, x + 19, y + 19, w - 38, h - 38);
                        for (int i = 0; i < 8; i++) {
                            double a0 = i * Math.PI / 4;
                            g.DrawLine(p, cx + (int)(Math.Cos(a0) * 12), cy + (int)(Math.Sin(a0) * 12), cx + (int)(Math.Cos(a0) * 17), cy + (int)(Math.Sin(a0) * 17));
                        }
                        break;
                    case "Lock":
                        g.DrawRectangle(p, x + 12, y + 20, w - 24, h - 27);
                        g.DrawArc(p, x + 15, y + 7, w - 30, 25, 180, -180);
                        break;
                    case "VolumeUp":
                    case "VolumeDown":
                    case "Mute":
                        Point[] speaker = { new Point(x+9,cy-6), new Point(x+17,cy-6), new Point(x+27,cy-15), new Point(x+27,cy+15), new Point(x+17,cy+6), new Point(x+9,cy+6) };
                        g.DrawPolygon(p, speaker);
                        if (a.Type == "Mute") { g.DrawLine(p,x+31,y+15,x+39,y+29); g.DrawLine(p,x+39,y+15,x+31,y+29); }
                        else { g.DrawArc(p,x+25,y+12,13,20,-55,110); if(a.Type=="VolumeUp"){g.DrawLine(p,x+37,cy,x+43,cy);g.DrawLine(p,x+40,cy-3,x+40,cy+3);} }
                        break;
                    case "Sleep":
                        g.DrawArc(p, x+10,y+8,w-20,h-16,70,235);
                        g.DrawArc(p, x+18,y+5,w-20,h-16,105,210);
                        break;
                    case "Shutdown":
                    case "Restart":
                        g.DrawArc(p,x+9,y+9,w-18,h-18,-55,290); g.DrawLine(p,cx,y+7,cx,cy+8);
                        if(a.Type=="Restart") g.DrawLine(p,x+10,y+14,x+10,y+24);
                        break;
                    case "Command":
                        g.DrawRectangle(p,x+8,y+10,w-16,h-20); g.DrawLine(p,x+13,y+17,x+20,y+22); g.DrawLine(p,x+20,y+22,x+13,y+27); g.DrawLine(p,x+23,y+28,x+31,y+28);
                        break;
                    case "None":
                        g.DrawLine(p,x+14,cy,x+w-14,cy);
                        break;
                    default:
                        g.DrawEllipse(p,x+12,y+12,w-24,h-24); g.DrawLine(p,cx,y+14,cx,y+h-14); g.DrawLine(p,x+14,cy,x+w-14,cy);
                        break;
                }
            }
        }

        private static bool DrawGeneratedSystemIcon(Graphics g, string type, Rectangle destination)
        {
            Point cell;
            if (!SystemIconCells.TryGetValue(type, out cell)) return false;
            try {
                if (systemIconSheet == null) {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("OrbitWheel.SystemIcons"))
                        if (stream != null) systemIconSheet = new Bitmap(stream);
                }
                if (systemIconSheet == null) return false;
                int cellWidth = systemIconSheet.Width / 4;
                int cellHeight = systemIconSheet.Height / 3;
                int insetX = 58, insetY = 54;
                Rectangle source = type == "Sleep"
                    ? new Rectangle(cell.X * cellWidth + 22, cell.Y * cellHeight + 46, cellWidth - 24, cellHeight - 92)
                    : new Rectangle(cell.X * cellWidth + insetX, cell.Y * cellHeight + insetY, cellWidth - insetX * 2, cellHeight - insetY * 2);
                InterpolationMode previous = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(systemIconSheet, destination, source, GraphicsUnit.Pixel);
                g.InterpolationMode = previous;
                return true;
            } catch { return false; }
        }

        public static Icon GetApplicationIcon(string target)
        {
            if (String.IsNullOrWhiteSpace(target)) return null;
            lock (Cache) {
                Icon cached;
                if (Cache.TryGetValue(target, out cached)) return cached == null ? null : (Icon)cached.Clone();
            }
            Icon loaded = LoadApplicationIcon(target);
            lock (Cache) Cache[target] = loaded == null ? null : (Icon)loaded.Clone();
            return loaded;
        }

        private static Icon LoadApplicationIcon(string target)
        {
            try {
                string iconTarget = ShortcutResolver.ResolveTarget(target);
                if (File.Exists(iconTarget)) { using (Icon icon = Icon.ExtractAssociatedIcon(iconTarget)) return (Icon)icon.Clone(); }
                IntPtr pidl;
                uint attributes;
                if (Native.SHParseDisplayName(target, IntPtr.Zero, out pidl, 0, out attributes) == 0 && pidl != IntPtr.Zero) {
                    try {
                        Native.ShellFileInfo pidlInfo = new Native.ShellFileInfo();
                        IntPtr parsed = Native.SHGetFileInfoPidl(pidl, 0, ref pidlInfo, (uint)Marshal.SizeOf(pidlInfo), 0x100 | 0x008);
                        if (parsed != IntPtr.Zero && pidlInfo.Icon != IntPtr.Zero) {
                            try { using (Icon icon = Icon.FromHandle(pidlInfo.Icon)) return (Icon)icon.Clone(); }
                            finally { Native.DestroyIcon(pidlInfo.Icon); }
                        }
                    } finally { Native.CoTaskMemFree(pidl); }
                }
                Native.ShellFileInfo info = new Native.ShellFileInfo();
                IntPtr result = Native.SHGetFileInfo(target, 0, ref info, (uint)Marshal.SizeOf(info), 0x100);
                if (result != IntPtr.Zero && info.Icon != IntPtr.Zero) {
                    try { using (Icon icon = Icon.FromHandle(info.Icon)) return (Icon)icon.Clone(); }
                    finally { Native.DestroyIcon(info.Icon); }
                }
            } catch { }
            return null;
        }
    }

    static class ActionNames
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string> {
            {"None","无操作"}, {"App","打开程序"}, {"Command","执行命令"},
            {"Explorer","打开资源管理器"}, {"Settings","打开 OrbitWheel 设置"},
            {"Lock","锁定电脑"}, {"Sleep","进入睡眠"}, {"Shutdown","关闭电脑"},
            {"Restart","重新启动"}, {"VolumeUp","增大音量"}, {"VolumeDown","减小音量"},
            {"Mute","静音 / 取消静音"}
        };
        public static string Chinese(string id) { return Names.ContainsKey(id) ? Names[id] : "无操作"; }
        public static string Id(string chinese)
        {
            foreach (KeyValuePair<string,string> pair in Names) if (pair.Value == chinese) return pair.Key;
            return "None";
        }
        public static object[] AllChinese()
        {
            List<object> result = new List<object>();
            foreach (string id in new string[] { "None","App","Command","Explorer","Settings","Lock","Sleep","Shutdown","Restart","VolumeUp","VolumeDown","Mute" })
                result.Add(Chinese(id));
            return result.ToArray();
        }
    }

    class ApplicationChoice
    {
        public string Name { get; set; }
        public string Target { get; set; }
        public override string ToString() { return Name; }
    }

    static class ApplicationCatalog
    {
        private static string ResolveAppsFolderPath(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return path;
            Dictionary<string, string> roots = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                {"{6D809377-6AF0-444B-8957-A3773F02200E}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)},
                {"{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)},
                {"{1AC14E77-02E7-4E5D-B744-2EB1AE5198B7}", Environment.GetFolderPath(Environment.SpecialFolder.System)},
                {"{D65231B0-B2F1-4857-A4CE-A8E7C6EA7D27}", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64")},
                {"{F38BF404-1D43-42F2-9305-67DE0B28FC23}", Environment.GetFolderPath(Environment.SpecialFolder.Windows)}
            };
            foreach (KeyValuePair<string,string> root in roots) {
                if (path.StartsWith(root.Key + "\\", StringComparison.OrdinalIgnoreCase)) {
                    string candidate = Path.Combine(root.Value, path.Substring(root.Key.Length + 1));
                    if (File.Exists(candidate)) return candidate;
                }
            }
            return path;
        }

        public static List<ApplicationChoice> Load()
        {
            List<ApplicationChoice> result = new List<ApplicationChoice>();
            try {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                object shell = Activator.CreateInstance(shellType);
                object folder = shellType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[] { "shell:AppsFolder" });
                object items = folder.GetType().InvokeMember("Items", BindingFlags.InvokeMethod, null, folder, null);
                int count = Convert.ToInt32(items.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, items, null));
                for (int i = 0; i < count; i++) {
                    object item = items.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, items, new object[] { i });
                    string name = Convert.ToString(item.GetType().InvokeMember("Name", BindingFlags.GetProperty, null, item, null));
                    string path = Convert.ToString(item.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, item, null));
                    path = ResolveAppsFolderPath(path);
                    if (!String.IsNullOrWhiteSpace(name) && !String.IsNullOrWhiteSpace(path))
                        result.Add(new ApplicationChoice { Name = name, Target = File.Exists(path) ? path : path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) ? path : "shell:AppsFolder\\" + path });
                }
            } catch { }
            result.Sort(delegate(ApplicationChoice a, ApplicationChoice b) { return String.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase); });
            return result;
        }
    }

    class ApplicationPicker : Form
    {
        private TextBox search;
        private ListBox list;
        private List<ApplicationChoice> all;
        public ApplicationChoice SelectedApplication { get; private set; }

        public ApplicationPicker()
        {
            Text = "选择应用 - Applications";
            Icon = IconFactory.AppIcon();
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(680, 720);
            BackColor = Color.FromArgb(7, 15, 30);
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 10f);
            GlassPanel shell = new GlassPanel { Left = 18, Top = 18, Width = 644, Height = 684, Radius = 22, BorderColor = Color.FromArgb(82, 112, 160, 215) };
            Controls.Add(shell);
            Label title = new Label { Text = "选择应用", Left = 28, Top = 22, Width = 400, Height = 38, Font = new Font("Microsoft YaHei UI", 20f, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.Transparent };
            Label hint = new Label { Text = "Applications 中的所有应用，也可切换为普通文件选择", Left = 30, Top = 64, Width = 540, Height = 24, ForeColor = Color.FromArgb(150, 180, 215), BackColor = Color.Transparent };
            search = new TextBox { Left = 28, Top = 104, Width = 588, Height = 36, BackColor = Color.FromArgb(18, 31, 51), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Microsoft YaHei UI", 11f) };
            list = new ListBox { Left = 28, Top = 158, Width = 588, Height = 430, BackColor = Color.FromArgb(18, 29, 47), ForeColor = Color.White, BorderStyle = BorderStyle.None, ItemHeight = 58, Font = new Font("Microsoft YaHei UI", 11f), DrawMode = DrawMode.OwnerDrawFixed };
            Button browse = new Button { Text = "浏览文件…", Left = 28, Top = 610, Width = 160, Height = 44, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(36, 55, 82), ForeColor = Color.White };
            Button choose = new Button { Text = "选择应用", Left = 456, Top = 610, Width = 160, Height = 44, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(20, 105, 224), ForeColor = Color.White };
            browse.FlatAppearance.BorderSize = 0;
            choose.FlatAppearance.BorderSize = 0;
            shell.Controls.AddRange(new Control[] { title, hint, search, list, browse, choose });
            search.TextChanged += delegate { Filter(); };
            list.DoubleClick += delegate { Accept(); };
            list.DrawItem += DrawApplication;
            browse.Click += delegate { BrowseFile(); };
            choose.Click += delegate { Accept(); };
            all = ApplicationCatalog.Load();
            Filter();
        }

        private void Filter()
        {
            string query = search.Text.Trim();
            list.BeginUpdate(); list.Items.Clear();
            foreach (ApplicationChoice app in all)
                if (query.Length == 0 || app.Name.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) list.Items.Add(app);
            list.EndUpdate();
        }

        private void Accept()
        {
            SelectedApplication = list.SelectedItem as ApplicationChoice;
            if (SelectedApplication == null) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BrowseFile()
        {
            using (OpenFileDialog dialog = new OpenFileDialog { Title = "选择应用程序或快捷方式", Filter = "应用与快捷方式 (*.exe;*.lnk)|*.exe;*.lnk|所有文件 (*.*)|*.*" }) {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                SelectedApplication = new ApplicationChoice { Name = Path.GetFileNameWithoutExtension(dialog.FileName), Target = dialog.FileName };
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void DrawApplication(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= list.Items.Count) return;
            ApplicationChoice app = (ApplicationChoice)list.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (SolidBrush background = new SolidBrush(selected ? Color.FromArgb(35, 100, 185) : Color.FromArgb(18, 29, 47))) e.Graphics.FillRectangle(background, e.Bounds);
            using (Icon icon = ActionIcons.GetApplicationIcon(app.Target)) {
                if (icon != null) e.Graphics.DrawIcon(icon, new Rectangle(e.Bounds.X + 14, e.Bounds.Y + 9, 40, 40));
                else {
                    Rectangle fallback = new Rectangle(e.Bounds.X + 14, e.Bounds.Y + 9, 40, 40);
                    using (SolidBrush fb = new SolidBrush(Color.FromArgb(70, 122, 220))) e.Graphics.FillEllipse(fb, fallback);
                    using (Font ff = new Font("Segoe UI", 12f, FontStyle.Bold))
                    using (SolidBrush ft = new SolidBrush(Color.White)) {
                        string initial = String.IsNullOrWhiteSpace(app.Name) ? "A" : app.Name.Substring(0, 1).ToUpper();
                        SizeF size = e.Graphics.MeasureString(initial, ff);
                        e.Graphics.DrawString(initial, ff, ft, fallback.X + (fallback.Width - size.Width) / 2, fallback.Y + (fallback.Height - size.Height) / 2);
                    }
                }
            }
            using (SolidBrush text = new SolidBrush(Color.White)) e.Graphics.DrawString(app.Name, Font, text, e.Bounds.X + 70, e.Bounds.Y + 18);
            using (Pen line = new Pen(Color.FromArgb(38, 58, 83))) e.Graphics.DrawLine(line, e.Bounds.X + 70, e.Bounds.Bottom - 1, e.Bounds.Right - 12, e.Bounds.Bottom - 1);
        }
    }

    class GlassPanel : Panel
    {
        public int Radius = 18;
        public Color BorderColor = Color.FromArgb(55, 105, 160, 220);

        public GlassPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(26, 38, 58);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = RoundedPath(ClientRectangle, Radius)) Region = new Region(path);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedPath(r, Radius))
            using (LinearGradientBrush fill = new LinearGradientBrush(r, Color.FromArgb(36, 49, 72), Color.FromArgb(22, 31, 48), 120f)) {
                e.Graphics.FillPath(fill, path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = RoundedPath(r, Radius))
            using (Pen border = new Pen(BorderColor, 1f)) {
                e.Graphics.DrawPath(border, path);
            }
            base.OnPaint(e);
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    class SettingsForm : Form
    {
        private AppConfig config;
        private ListBox pages;
        private DataGridView grid;
        private TextBox pageName;
        private ComboBox mode, style;
        private TextBox hotkeyRecorder;
        private int recordedModifiers;
        private int recordedKey;
        private CheckBox startup;
        private Label effectDescription;
        private Panel contentHost;
        private readonly List<Button> navigation = new List<Button>();
        private readonly List<Panel> sections = new List<Panel>();
        private bool loadingGrid;
        private bool choosingApp;
        private bool showingPage;
        private bool initializing;
        public event EventHandler ConfigSaved;

        public SettingsForm(AppConfig c)
        {
            config = c;
            Text = "OrbitWheel 设置";
            Icon = IconFactory.AppIcon();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1180, 760);
            MinimumSize = new Size(1080, 700);
            BackColor = Color.FromArgb(7, 15, 30);
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9.5f);
            initializing = true;
            Build();
            LoadPageList();
            initializing = false;
            FormClosing += delegate { if (grid != null) { grid.EndEdit(); AutoSave(); } };
        }

        private void Build()
        {
            GlassPanel shell = new GlassPanel { Left = 18, Top = 18, Width = 1144, Height = 724, Radius = 22, BorderColor = Color.FromArgb(85, 112, 161, 220) };
            Controls.Add(shell);
            Label appMark = L("◉", 26, 20, 44, 44, 24, false); appMark.ForeColor = Color.FromArgb(68, 178, 255); shell.Controls.Add(appMark);
            Label title = L("设置", 78, 24, 260, 38, 20, true); shell.Controls.Add(title);
            Panel rule = new Panel { Left = 24, Top = 76, Width = 1096, Height = 1, BackColor = Color.FromArgb(55, 112, 145, 185) }; shell.Controls.Add(rule);

            GlassPanel sidebar = new GlassPanel { Left = 22, Top = 96, Width = 220, Height = 604, Radius = 18, BorderColor = Color.FromArgb(42, 93, 130, 180) };
            shell.Controls.Add(sidebar);
            string[] navText = { "⌂   常规", "▦   页面与动作", "◉   外观效果", "⌨   快捷键", "⚙   高级", "●   关于" };
            for (int i = 0; i < navText.Length; i++) {
                Button nav = B(navText[i], 14, 18 + i * 58, 192, 46);
                nav.TextAlign = ContentAlignment.MiddleLeft;
                nav.Padding = new Padding(18, 0, 0, 0);
                int index = i;
                nav.Click += delegate { ShowSection(index); };
                sidebar.Controls.Add(nav);
                navigation.Add(nav);
            }
            Label auto = L("所有更改都会自动保存", 24, 552, 180, 24, 8, false); auto.ForeColor = Color.FromArgb(130, 165, 205); sidebar.Controls.Add(auto);

            contentHost = new Panel { Left = 262, Top = 96, Width = 858, Height = 604, BackColor = Color.Transparent };
            shell.Controls.Add(contentHost);

            Panel general = Section();
            GlassPanel startupCard = Card("启动与托盘", 0, 0, 858, 132);
            startup = new CheckBox { Left = 28, Top = 57, Width = 250, Height = 30, Text = "随 Windows 自动启动", ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Microsoft YaHei UI", 10.5f) };
            startupCard.Controls.Add(startup);
            Label trayHint = L("关闭设置窗口后，OrbitWheel 仍会留在系统托盘", 28, 91, 520, 22, 8.5f, false); trayHint.ForeColor = Color.FromArgb(145, 174, 210); startupCard.Controls.Add(trayHint);
            general.Controls.Add(startupCard);
            GlassPanel triggerCard = Card("触发模式", 0, 148, 858, 154);
            mode = C(28, 60, 390); mode.Items.AddRange(new object[] { "点击模式", "按住并松开执行" }); triggerCard.Controls.Add(mode);
            Label triggerHint = L("默认按住快捷键，移动到目标后松开执行", 28, 102, 530, 22, 8.5f, false); triggerHint.ForeColor = Color.FromArgb(145, 174, 210); triggerCard.Controls.Add(triggerHint);
            general.Controls.Add(triggerCard);
            GlassPanel pageHintCard = Card("页面切换", 0, 318, 858, 132);
            pageHintCard.Controls.Add(L("滚动鼠标滚轮切换页面", 28, 60, 330, 26, 10.5f, false));
            Label dots = L("●  ●  ●", 690, 61, 120, 24, 11, false); dots.ForeColor = Color.FromArgb(38, 157, 255); pageHintCard.Controls.Add(dots);
            general.Controls.Add(pageHintCard);
            sections.Add(general);

            Panel pageSection = Section();
            GlassPanel pageBox = Card("页面与六个扇区", 0, 0, 858, 586);
            pages = new ListBox { Left = 22, Top = 58, Width = 176, Height = 438, BackColor = Color.FromArgb(20,31,49), ForeColor = Color.White, BorderStyle = BorderStyle.None, ItemHeight = 38, Font = new Font("Microsoft YaHei UI", 10f) };
            pages.SelectedIndexChanged += delegate { ShowPage(); };
            pageBox.Controls.Add(pages);
            Button add = B("＋  添加页面", 22, 512, 112, 42); add.Click += delegate { AddPage(); }; pageBox.Controls.Add(add);
            Button del = B("−", 144, 512, 54, 42); del.BackColor = Color.FromArgb(67, 42, 58); del.Click += delegate { DeletePage(); }; pageBox.Controls.Add(del);
            ToolTip pageTips = new ToolTip();
            pageTips.SetToolTip(add, "添加新页面");
            pageTips.SetToolTip(del, "删除当前页面");

            pageName = new TextBox { Left = 220, Top = 58, Width = 608, Height = 32, BackColor = Color.FromArgb(22,37,59), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Microsoft YaHei UI", 10.5f) };
            pageName.TextChanged += delegate {
                if (!showingPage && pages.SelectedIndex >= 0) {
                    config.Pages[pages.SelectedIndex].Name = pageName.Text;
                    if (Convert.ToString(pages.Items[pages.SelectedIndex]) != pageName.Text)
                        pages.Items[pages.SelectedIndex] = pageName.Text;
                    AutoSave();
                }
            };
            pageBox.Controls.Add(pageName);

            grid = new DataGridView { Left = 220, Top = 106, Width = 608, Height = 390, BackgroundColor = Color.FromArgb(20,31,49), ForeColor = Color.White, GridColor = Color.FromArgb(42,65,94), BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersHeight = 42, RowTemplate = { Height = 48 } };
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35,47,68);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.BackColor = Color.FromArgb(24,32,48);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50,91,151);
            grid.Columns.Add("slot", "位置");
            grid.Columns.Add("name", "名称");
            DataGridViewComboBoxColumn typeCol = new DataGridViewComboBoxColumn { Name = "type", HeaderText = "动作类型" };
            typeCol.FlatStyle = FlatStyle.Flat;
            typeCol.Items.AddRange(ActionNames.AllChinese());
            grid.Columns.Add(typeCol);
            grid.Columns.Add("target", "程序路径 / 命令");
            grid.Columns[0].ReadOnly = true;
            grid.Columns[0].FillWeight = 45; grid.Columns[1].FillWeight = 80; grid.Columns[2].FillWeight = 90; grid.Columns[3].FillWeight = 170;
            grid.CurrentCellDirtyStateChanged += delegate { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellValueChanged += delegate { HandleCellChange(); };
            grid.SelectionChanged += delegate { UpdateActionEditor(); };
            pageBox.Controls.Add(grid);
            Label appHint = L("选择“打开程序”后，可从 Applications 或普通文件窗口选择。", 220, 520, 590, 25, 8, false); appHint.ForeColor = Color.FromArgb(125, 166, 210); pageBox.Controls.Add(appHint);
            pageSection.Controls.Add(pageBox);
            sections.Add(pageSection);

            Panel appearance = Section();
            GlassPanel styleCard = Card("视觉效果", 0, 0, 858, 190);
            styleCard.Controls.Add(L("效果样式", 28, 62, 110, 24, 10, false));
            style = C(142, 58, 310); style.Items.AddRange(new object[] { "液态玻璃", "高斯模糊", "亚克力" }); styleCard.Controls.Add(style);
            effectDescription = L("", 28, 112, 700, 38, 9, false); effectDescription.ForeColor = Color.FromArgb(150, 188, 225); styleCard.Controls.Add(effectDescription);
            appearance.Controls.Add(styleCard);
            GlassPanel visualInfo = Card("圆环视觉说明", 0, 206, 858, 174);
            visualInfo.Controls.Add(L("视觉效果仅应用于圆环本身，不影响整个屏幕。", 28, 62, 650, 26, 10, false));
            Label material = L("液态玻璃强调折射高光；高斯模糊强调背景虚化；亚克力带有磨砂颗粒。", 28, 100, 740, 42, 9, false); material.ForeColor = Color.FromArgb(145, 174, 210); visualInfo.Controls.Add(material);
            appearance.Controls.Add(visualInfo);
            sections.Add(appearance);

            Panel hotkeySection = Section();
            GlassPanel hotkeyCard = Card("快捷键", 0, 0, 858, 190);
            hotkeyRecorder = new TextBox { Left = 28, Top = 64, Width = 520, Height = 38, ReadOnly = true, BackColor = Color.FromArgb(15, 29, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, Font = new Font("Segoe UI", 12f) };
            hotkeyRecorder.KeyDown += RecordHotkey;
            hotkeyRecorder.Enter += delegate { hotkeyRecorder.Text = "请按下新的快捷键…"; };
            hotkeyCard.Controls.Add(hotkeyRecorder);
            Button record = B("录制快捷键", 570, 62, 180, 42); record.Click += delegate { hotkeyRecorder.Focus(); hotkeyRecorder.Text = "请按下新的快捷键…"; }; hotkeyCard.Controls.Add(record);
            Label hotkeyHint = L("点击“录制快捷键”并按下任意组合键。", 28, 122, 620, 24, 9, false); hotkeyHint.ForeColor = Color.FromArgb(145, 174, 210); hotkeyCard.Controls.Add(hotkeyHint);
            hotkeySection.Controls.Add(hotkeyCard);
            sections.Add(hotkeySection);

            Panel advanced = Section();
            GlassPanel advancedCard = Card("高级", 0, 0, 858, 190);
            advancedCard.Controls.Add(L("圆环中心始终以打开瞬间的鼠标位置为准。", 28, 62, 700, 26, 10, false));
            Label advancedHint = L("点击模式下再次按快捷键不会重复打开圆环；按 Esc 可关闭。", 28, 103, 720, 28, 9, false); advancedHint.ForeColor = Color.FromArgb(145,174,210); advancedCard.Controls.Add(advancedHint);
            advanced.Controls.Add(advancedCard);
            sections.Add(advanced);

            Panel about = Section();
            GlassPanel aboutCard = Card("关于 OrbitWheel", 0, 0, 858, 220);
            aboutCard.Controls.Add(L("OrbitWheel", 28, 62, 400, 40, 22, true));
            Label aboutHint = L("鼠标中心的六等分径向快捷操作工具", 30, 108, 620, 28, 10, false); aboutHint.ForeColor = Color.FromArgb(145, 180, 220); aboutCard.Controls.Add(aboutHint);
            aboutCard.Controls.Add(L("OrbitWheel 1.0 · 全新 UI 与图标设计", 30, 153, 500, 24, 9, false));
            about.Controls.Add(aboutCard);
            sections.Add(about);

            foreach (Panel section in sections) contentHost.Controls.Add(section);

            style.SelectedIndexChanged += delegate { UpdateEffectDescription(); };
            mode.SelectedIndexChanged += delegate { AutoSave(); };
            style.SelectedIndexChanged += delegate { AutoSave(); };
            startup.CheckedChanged += delegate { AutoSave(); };

            recordedModifiers = config.Modifiers;
            recordedKey = config.KeyCode;
            hotkeyRecorder.Text = HotkeyText(recordedModifiers, recordedKey);
            mode.SelectedIndex = config.Mode == "Hold" ? 1 : 0;
            style.SelectedItem = config.Style;
            UpdateEffectDescription();
            startup.Checked = config.StartWithWindows;
            ShowSection(0);
        }

        private void LoadPageList()
        {
            pages.Items.Clear();
            foreach (WheelPage p in config.Pages) pages.Items.Add(p.Name);
            if (pages.Items.Count > 0) pages.SelectedIndex = 0;
        }

        private void ShowPage()
        {
            if (showingPage) return;
            showingPage = true;
            loadingGrid = true;
            grid.Rows.Clear();
            if (pages.SelectedIndex < 0) { loadingGrid = false; showingPage = false; return; }
            string[] slots = { "右", "右下", "左下", "左", "左上", "右上" };
            WheelPage p = config.Pages[pages.SelectedIndex];
            pageName.Text = p.Name;
            for (int i = 0; i < 6; i++) grid.Rows.Add(slots[i], p.Actions[i].Name, ActionNames.Chinese(p.Actions[i].Type), p.Actions[i].Target);
            loadingGrid = false;
            showingPage = false;
            UpdateActionEditor();
        }

        private void HandleCellChange()
        {
            if (loadingGrid || choosingApp) return;
            CommitPage();
            UpdateActionEditor();
            if (grid.CurrentRow != null && grid.CurrentCell != null && grid.CurrentCell.ColumnIndex == 2) {
                string type = ActionNames.Id(Convert.ToString(grid.CurrentRow.Cells[2].Value));
                if (type == "App") BrowseApp();
            }
            AutoSave();
        }

        private void UpdateActionEditor()
        {
            if (grid == null || grid.CurrentRow == null) return;
            string type = ActionNames.Id(Convert.ToString(grid.CurrentRow.Cells[2].Value));
            grid.CurrentRow.Cells[3].ReadOnly = type != "App" && type != "Command";
            if (type != "App" && type != "Command") grid.CurrentRow.Cells[3].Value = "";
        }

        private void UpdateEffectDescription()
        {
            if (effectDescription == null || style == null) return;
            string value = Convert.ToString(style.SelectedItem);
            effectDescription.Text = value == "高斯模糊" ? "强背景虚化，颜色保持自然" : value == "亚克力" ? "高遮罩、磨砂颗粒、低透视" : "蓝色折射、动态高光、通透";
        }

        private void CommitPage()
        {
            if (pages.SelectedIndex < 0 || grid.Rows.Count != 6) return;
            WheelPage p = config.Pages[pages.SelectedIndex];
            for (int i = 0; i < 6; i++) {
                p.Actions[i].Name = Convert.ToString(grid.Rows[i].Cells[1].Value);
                p.Actions[i].Type = ActionNames.Id(Convert.ToString(grid.Rows[i].Cells[2].Value));
                p.Actions[i].Target = Convert.ToString(grid.Rows[i].Cells[3].Value);
            }
        }

        private void AddPage()
        {
            CommitPage();
            WheelPage p = new WheelPage { Name = "页面 " + (config.Pages.Count + 1), Actions = new List<ActionItem>() };
            for (int i = 0; i < 6; i++) p.Actions.Add(new ActionItem { Name = "空", Type = "None", Target = "" });
            config.Pages.Add(p);
            LoadPageList();
            pages.SelectedIndex = config.Pages.Count - 1;
            AutoSave();
        }

        private void DeletePage()
        {
            if (config.Pages.Count <= 1) { MessageBox.Show("至少保留一个页面。"); return; }
            int i = pages.SelectedIndex;
            config.Pages.RemoveAt(i);
            LoadPageList();
            pages.SelectedIndex = Math.Min(i, config.Pages.Count - 1);
            AutoSave();
        }

        private void BrowseApp()
        {
            if (grid.CurrentRow == null || choosingApp) return;
            choosingApp = true;
            using (ApplicationPicker d = new ApplicationPicker()) {
                if (d.ShowDialog(this) == DialogResult.OK && d.SelectedApplication != null) {
                    grid.CurrentRow.Cells[1].Value = d.SelectedApplication.Name;
                    grid.CurrentRow.Cells[2].Value = ActionNames.Chinese("App");
                    grid.CurrentRow.Cells[3].Value = d.SelectedApplication.Target;
                }
            }
            choosingApp = false;
            CommitPage();
            AutoSave();
        }

        private void AutoSave()
        {
            if (initializing || loadingGrid || showingPage || choosingApp) return;
            CommitPage();
            config.Modifiers = recordedModifiers;
            config.KeyCode = recordedKey;
            config.Mode = mode.SelectedIndex == 1 ? "Hold" : "Click";
            config.Style = Convert.ToString(style.SelectedItem);
            config.StartWithWindows = startup.Checked;
            Startup.Set(config.StartWithWindows);
            ConfigStore.Save(config);
            if (ConfigSaved != null) ConfigSaved(this, EventArgs.Empty);
        }

        private void RecordHotkey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.Menu || e.KeyCode == Keys.LWin || e.KeyCode == Keys.RWin) return;
            bool win = (Native.GetAsyncKeyState((int)Keys.LWin) & 0x8000) != 0 || (Native.GetAsyncKeyState((int)Keys.RWin) & 0x8000) != 0;
            recordedModifiers = (e.Control ? Native.MOD_CONTROL : 0) | (e.Alt ? Native.MOD_ALT : 0) | (e.Shift ? Native.MOD_SHIFT : 0) | (win ? Native.MOD_WIN : 0);
            recordedKey = (int)e.KeyCode;
            hotkeyRecorder.Text = HotkeyText(recordedModifiers, recordedKey);
            e.SuppressKeyPress = true;
            AutoSave();
        }

        private string HotkeyText(int modifiers, int keyCode)
        {
            List<string> parts = new List<string>();
            if ((modifiers & Native.MOD_CONTROL) != 0) parts.Add("Ctrl");
            if ((modifiers & Native.MOD_ALT) != 0) parts.Add("Alt");
            if ((modifiers & Native.MOD_SHIFT) != 0) parts.Add("Shift");
            if ((modifiers & Native.MOD_WIN) != 0) parts.Add("Win");
            parts.Add(((Keys)keyCode).ToString());
            return String.Join(" + ", parts.ToArray());
        }

        private Panel Section()
        {
            Panel section = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };
            return section;
        }

        private GlassPanel Card(string text, int x, int y, int w, int h)
        {
            GlassPanel card = new GlassPanel { Left = x, Top = y, Width = w, Height = h, Radius = 17, BorderColor = Color.FromArgb(54, 94, 132, 184) };
            Label heading = L(text, 28, 20, w - 56, 30, 12, true);
            heading.ForeColor = Color.FromArgb(232, 242, 255);
            card.Controls.Add(heading);
            return card;
        }

        private void ShowSection(int index)
        {
            if (index < 0 || index >= sections.Count) return;
            for (int i = 0; i < sections.Count; i++) {
                sections[i].Visible = i == index;
                navigation[i].BackColor = i == index ? Color.FromArgb(20, 105, 224) : Color.FromArgb(31, 45, 66);
                navigation[i].ForeColor = i == index ? Color.White : Color.FromArgb(210, 225, 245);
            }
            sections[index].BringToFront();
        }

        private Panel Box(string text, int x, int y, int w, int h)
        {
            Panel box = new Panel { Left = x, Top = y, Width = w, Height = h, BackColor = Color.FromArgb(27,33,47), Padding = new Padding(12) };
            Label heading = L(text, 16, 12, w - 32, 25, 11, true);
            heading.ForeColor = Color.FromArgb(220, 235, 255);
            box.Controls.Add(heading);
            return box;
        }
        private Label L(string text, int x, int y, int w, int h, float size, bool bold)
        {
            return new Label { Text = text, Left = x, Top = y, Width = w, Height = h, ForeColor = Color.White, BackColor = Color.Transparent, Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular) };
        }
        private Button B(string text, int x, int y, int w, int h)
        {
            Button b = new Button { Text = text, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(31,45,66), Cursor = Cursors.Hand, Font = new Font("Microsoft YaHei UI", 9.5f) };
            b.FlatAppearance.BorderColor = Color.FromArgb(68, 105, 150);
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 82, 145);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 105, 224);
            return b;
        }
        private ComboBox C(int x, int y, int w)
        {
            return new ComboBox { Left = x, Top = y, Width = w, Height = 36, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(20,34,55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Microsoft YaHei UI", 10.5f) };
        }
    }

    static class Startup
    {
        public static void Set(bool enabled)
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) {
                if (enabled) k.SetValue("OrbitWheel", "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue("OrbitWheel", false);
            }
        }
    }

    class OrbitContext : ApplicationContext
    {
        private AppConfig config;
        private NotifyIcon tray;
        private HotkeyWindow hotkey;
        private KeyboardWatcher watcher;
        private WheelForm wheel;
        private SettingsForm settings;

        public OrbitContext()
        {
            config = ConfigStore.Load();
            tray = new NotifyIcon { Icon = IconFactory.AppIcon(), Text = "OrbitWheel", Visible = true };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("打开径向菜单", null, delegate { ShowWheel(); });
            menu.Items.Add("设置", null, delegate { ShowSettings(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出", null, delegate { Exit(); });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ShowSettings(); };
            hotkey = new HotkeyWindow();
            hotkey.Triggered += delegate {
                if (wheel != null && !wheel.IsDisposed) return;
                ShowWheel();
            };
            ApplyHotkey();
        }

        private void ApplyHotkey()
        {
            if (!hotkey.Set(config.Modifiers, config.KeyCode))
                tray.ShowBalloonTip(3000, "OrbitWheel", "快捷键已被其他程序占用，请在设置中更换。", ToolTipIcon.Warning);
        }

        private void ShowWheel()
        {
            if (wheel != null && !wheel.IsDisposed) {
                return;
            }
            wheel = new WheelForm(config);
            wheel.ExecuteRequested += delegate(ActionItem a) { ActionRunner.Run(a, ShowSettings); };
            wheel.FormClosed += delegate { wheel = null; if (watcher != null) { watcher.Dispose(); watcher = null; } };
            if (config.Mode == "Hold") {
                watcher = new KeyboardWatcher(config.KeyCode);
                watcher.TriggerReleased += delegate {
                    if (wheel != null && !wheel.IsDisposed) wheel.BeginInvoke(new Action(delegate { wheel.ExecuteHoldSelection(); }));
                };
            }
            wheel.Show();
        }

        private void ShowSettings()
        {
            try {
                if (settings != null && !settings.IsDisposed) { settings.Show(); settings.Activate(); settings.BringToFront(); return; }
                settings = new SettingsForm(config);
                settings.ConfigSaved += delegate { ApplyHotkey(); };
                settings.FormClosed += delegate { settings = null; };
                settings.Show();
                settings.Activate();
                settings.BringToFront();
            } catch (Exception ex) {
                Directory.CreateDirectory(ConfigStore.Folder);
                File.AppendAllText(Path.Combine(ConfigStore.Folder, "error.log"), DateTime.Now + " Settings: " + ex + Environment.NewLine);
                MessageBox.Show("设置页面打开失败：\n" + ex.Message, "OrbitWheel");
            }
        }

        public void OpenSettingsOnStart()
        {
            Timer t = new Timer { Interval = 250 };
            t.Tick += delegate { t.Stop(); t.Dispose(); ShowSettings(); };
            t.Start();
        }

        public void OpenWheelOnStart()
        {
            Timer t = new Timer { Interval = 250 };
            t.Tick += delegate { t.Stop(); t.Dispose(); ShowWheel(); };
            t.Start();
        }

        private void Exit()
        {
            tray.Visible = false;
            hotkey.Dispose();
            if (watcher != null) watcher.Dispose();
            Application.Exit();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            bool created;
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "OrbitWheel.SingleInstance", out created)) {
                if (!created) return;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                OrbitContext context = new OrbitContext();
                if (Environment.CommandLine.IndexOf("/settings", StringComparison.OrdinalIgnoreCase) >= 0) context.OpenSettingsOnStart();
                if (Environment.CommandLine.IndexOf("/wheel", StringComparison.OrdinalIgnoreCase) >= 0) context.OpenWheelOnStart();
                Application.Run(context);
            }
        }
    }
}
