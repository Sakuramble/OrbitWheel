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

[assembly: AssemblyTitle("OrbitWheel-Lite")]
[assembly: AssemblyDescription("OrbitWheel-Lite Fin - 径向快捷操作中心")]
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
        public int Opacity { get; set; }
        public bool StartWithWindows { get; set; }
        public List<WheelPage> Pages { get; set; }

        public static AppConfig Default()
        {
            return new AppConfig {
                Modifiers = 2,
                KeyCode = (int)Keys.Space,
                Mode = "Hold",
                Style = "液态玻璃",
                Opacity = 92,
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
            if (c.Opacity < 20 || c.Opacity > 100) c.Opacity = 92;
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
        [DllImport("user32.dll")] public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
        [DllImport("dwmapi.dll")] public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
        [StructLayout(LayoutKind.Sequential)]
        public struct Margins { public int Left; public int Right; public int Top; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy { public int AccentState; public int AccentFlags; public int GradientColor; public int AnimationId; }
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData { public int Attribute; public IntPtr Data; public int SizeOfData; }
        public delegate IntPtr KeyboardProc(int code, IntPtr wp, IntPtr lp);

        public static bool ApplyRealtimeEffect(IntPtr handle, string style, int opacity)
        {
            Margins margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);
            AccentPolicy accent = new AccentPolicy();
            accent.AccentState = style == "高斯模糊" ? 3 : 4;
            accent.AccentFlags = 2;
            int alpha = Math.Max(20, Math.Min(210, (int)(opacity * (style == "亚克力" ? 1.9 : style == "液态玻璃" ? 1.25 : 0.65))));
            int red = style == "液态玻璃" ? 38 : 25;
            int green = style == "液态玻璃" ? 48 : 31;
            int blue = style == "液态玻璃" ? 72 : 45;
            accent.GradientColor = (alpha << 24) | (blue << 16) | (green << 8) | red;
            int size = Marshal.SizeOf(accent);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try {
                Marshal.StructureToPtr(accent, ptr, false);
                WindowCompositionAttributeData data = new WindowCompositionAttributeData { Attribute = 19, Data = ptr, SizeOfData = size };
                return SetWindowCompositionAttribute(handle, ref data) != 0;
            } finally { Marshal.FreeHGlobal(ptr); }
        }
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
            Text = "OrbitWheel-Lite Menu";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;
            Opacity = 1.0;
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
                int materialAlpha = (int)((config.Opacity / 100.0) * (style == "亚克力" ? 205 : style == "液态玻璃" ? 118 : 70));
                Color tint = style == "亚克力" ? Color.FromArgb(materialAlpha, 23, 29, 43) : style == "液态玻璃" ? Color.FromArgb(materialAlpha, 28, 48, 88) : Color.FromArgb(materialAlpha, 20, 25, 38);
                using (SolidBrush b = new SolidBrush(tint)) g.FillRectangle(b, 0, 0, result.Width, result.Height);
                if (style == "亚克力") {
                    Random random = new Random(8);
                    using (SolidBrush grain = new SolidBrush(Color.FromArgb(Math.Max(8, config.Opacity / 7), 255, 255, 255)))
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
                using (TextureBrush texture = new TextureBrush(backdrop))
                    e.Graphics.FillEllipse(texture, center.X - Outer + 2, center.Y - Outer + 2, (Outer - 2) * 2, (Outer - 2) * 2);
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
            int fillAlpha = (int)((config.Opacity / 100.0) * (acrylic ? 80 : blur ? 22 : 45));
            Color baseColor = acrylic ? Color.FromArgb(fillAlpha, 35, 42, 58) : blur ? Color.FromArgb(fillAlpha, 28, 33, 48) : Color.FromArgb(fillAlpha, 22, 36, 66);
            Color accent = acrylic ? Color.FromArgb(150, 92, 130, 185) : blur ? Color.FromArgb(90, 160, 200, 245) : Color.FromArgb(125, 87, 190, 255);

            for (int i = 0; i < 6; i++) {
                using (GraphicsPath path = SegmentPath(center, Inner + 11, Outer, i * 60 - 30, 60)) {
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
            Rectangle iconRect = new Rectangle(p.X - 22, p.Y - 31, 44, 44);
            ActionIcons.Draw(g, a, iconRect, i == selected);
            using (Font f = new Font("Microsoft YaHei UI", 9.5f, i == selected ? FontStyle.Bold : FontStyle.Regular))
            using (SolidBrush b = new SolidBrush(Color.White)) {
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(a.Name, f, b, new RectangleF(p.X - 63, p.Y + 17, 126, 26), sf);
            }
        }
    }

    static class ActionRunner
    {
        public static void Run(ActionItem a, Action showSettings)
        {
            try {
                switch (a.Type) {
                    case "App": Start(a.Target, ""); break;
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
            } catch (Exception ex) { MessageBox.Show("无法执行“" + a.Name + "”\n" + ex.Message, "OrbitWheel-Lite"); }
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
    }

    static class ActionIcons
    {
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
            if (a.Type == "App" && File.Exists(a.Target)) {
                try { using (Icon icon = Icon.ExtractAssociatedIcon(a.Target)) { g.DrawIcon(icon, r); return; } } catch { }
            }
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
    }

    static class ActionNames
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string> {
            {"None","无操作"}, {"App","打开程序"}, {"Command","执行命令"},
            {"Explorer","打开资源管理器"}, {"Settings","打开 OrbitWheel-Lite 设置"},
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

    class SettingsForm : Form
    {
        private AppConfig config;
        private ListBox pages;
        private DataGridView grid;
        private TextBox pageName;
        private ComboBox modifier, key, mode, style;
        private TrackBar opacity;
        private CheckBox startup;
        private Label opacityLabel;
        private Label effectDescription;
        private bool loadingGrid;
        private bool choosingApp;
        private bool showingPage;
        private bool initializing;
        public event EventHandler ConfigSaved;

        public SettingsForm(AppConfig c)
        {
            config = c;
            Text = "OrbitWheel-Lite 设置";
            Icon = IconFactory.AppIcon();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(940, 620);
            MinimumSize = new Size(850, 560);
            BackColor = Color.FromArgb(22, 27, 39);
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
            Label title = L("OrbitWheel-Lite", 24, 18, 300, 36, 20, true);
            Controls.Add(title);
            Controls.Add(L("径向快捷操作中心", 26, 53, 300, 24, 9, false));

            Panel pageBox = Box("页面与六个扇区", 22, 92, 650, 470);
            pages = new ListBox { Left = 18, Top = 42, Width = 150, Height = 358, BackColor = Color.FromArgb(31,38,54), ForeColor = Color.White, BorderStyle = BorderStyle.None };
            pages.SelectedIndexChanged += delegate { ShowPage(); };
            pageBox.Controls.Add(pages);
            Button add = B("＋", 18, 414, 70, 40); add.Font = new Font("Segoe UI", 17f); add.Click += delegate { AddPage(); }; pageBox.Controls.Add(add);
            Button del = B("−", 98, 414, 70, 40); del.Font = new Font("Segoe UI", 17f); del.BackColor = Color.FromArgb(68, 49, 65); del.Click += delegate { DeletePage(); }; pageBox.Controls.Add(del);
            ToolTip pageTips = new ToolTip();
            pageTips.SetToolTip(add, "添加新页面");
            pageTips.SetToolTip(del, "删除当前页面");

            pageName = new TextBox { Left = 184, Top = 42, Width = 446, Height = 28, BackColor = Color.FromArgb(40,48,66), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pageName.TextChanged += delegate {
                if (!showingPage && pages.SelectedIndex >= 0) {
                    config.Pages[pages.SelectedIndex].Name = pageName.Text;
                    if (Convert.ToString(pages.Items[pages.SelectedIndex]) != pageName.Text)
                        pages.Items[pages.SelectedIndex] = pageName.Text;
                    AutoSave();
                }
            };
            pageBox.Controls.Add(pageName);

            grid = new DataGridView { Left = 184, Top = 78, Width = 446, Height = 338, BackgroundColor = Color.FromArgb(31,38,54), ForeColor = Color.White, GridColor = Color.FromArgb(48,58,78), BorderStyle = BorderStyle.None, RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersHeight = 34, RowTemplate = { Height = 38 } };
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(43,52,72);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.BackColor = Color.FromArgb(31,38,54);
            grid.DefaultCellStyle.ForeColor = Color.White;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(72,105,160);
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
            pageBox.Controls.Add(L("选择“打开程序”时会自动打开开始菜单程序目录。", 184, 430, 446, 25, 8, false));
            Controls.Add(pageBox);

            Panel prefs = Box("行为与外观", 690, 92, 226, 470);
            prefs.Controls.Add(L("快捷键", 16, 43, 180, 22, 9, false));
            modifier = C(16, 66, 90); modifier.Items.AddRange(new object[] { "Ctrl", "Alt", "Shift", "Win", "Ctrl+Alt", "Ctrl+Shift" }); prefs.Controls.Add(modifier);
            key = C(112, 66, 90); foreach (Keys k in new Keys[] { Keys.Space, Keys.Q, Keys.W, Keys.E, Keys.R, Keys.F1, Keys.F2, Keys.F8, Keys.F9, Keys.F10, Keys.F11, Keys.F12 }) key.Items.Add(k); prefs.Controls.Add(key);
            prefs.Controls.Add(L("触发模式", 16, 105, 180, 22, 9, false));
            mode = C(16, 128, 186); mode.Items.AddRange(new object[] { "点击模式", "按住并松开执行" }); prefs.Controls.Add(mode);
            prefs.Controls.Add(L("视觉材质", 16, 168, 180, 22, 9, false));
            style = C(16, 191, 186); style.Items.AddRange(new object[] { "液态玻璃", "高斯模糊", "亚克力" }); prefs.Controls.Add(style);
            effectDescription = L("", 16, 224, 190, 38, 8, false); effectDescription.ForeColor = Color.FromArgb(160, 185, 215); prefs.Controls.Add(effectDescription);
            style.SelectedIndexChanged += delegate { UpdateEffectDescription(); };
            prefs.Controls.Add(L("材质透明度", 16, 270, 110, 22, 9, false));
            opacityLabel = L("", 150, 270, 52, 22, 9, false); opacityLabel.TextAlign = ContentAlignment.TopRight; prefs.Controls.Add(opacityLabel);
            opacity = new TrackBar { Left = 12, Top = 293, Width = 194, Minimum = 20, Maximum = 100, TickFrequency = 10, BackColor = prefs.BackColor };
            opacity.ValueChanged += delegate { opacityLabel.Text = opacity.Value + "%"; AutoSave(); }; prefs.Controls.Add(opacity);
            startup = new CheckBox { Left = 16, Top = 345, Width = 190, Height = 28, Text = "随 Windows 自动启动", ForeColor = Color.White }; prefs.Controls.Add(startup);
            prefs.Controls.Add(L("滚轮切换页面 · Esc 关闭", 16, 382, 190, 25, 8, false));
            prefs.Controls.Add(L("所有更改都会自动保存", 16, 423, 190, 25, 8, false));
            Controls.Add(prefs);

            modifier.SelectedIndexChanged += delegate { AutoSave(); };
            key.SelectedIndexChanged += delegate { AutoSave(); };
            mode.SelectedIndexChanged += delegate { AutoSave(); };
            style.SelectedIndexChanged += delegate { AutoSave(); };
            startup.CheckedChanged += delegate { AutoSave(); };

            modifier.SelectedItem = ModifierName(config.Modifiers);
            key.SelectedItem = (Keys)config.KeyCode;
            mode.SelectedIndex = config.Mode == "Hold" ? 1 : 0;
            style.SelectedItem = config.Style;
            UpdateEffectDescription();
            opacity.Value = config.Opacity;
            startup.Checked = config.StartWithWindows;
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
            string commonPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs");
            string userPrograms = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");
            string initial = Directory.Exists(commonPrograms) ? commonPrograms : userPrograms;
            using (OpenFileDialog d = new OpenFileDialog { Title = "选择要打开的应用", InitialDirectory = initial, Filter = "应用与开始菜单快捷方式 (*.exe;*.lnk)|*.exe;*.lnk|所有文件 (*.*)|*.*" }) {
                if (d.ShowDialog() == DialogResult.OK) {
                    grid.CurrentRow.Cells[1].Value = Path.GetFileNameWithoutExtension(d.FileName);
                    grid.CurrentRow.Cells[2].Value = ActionNames.Chinese("App");
                    grid.CurrentRow.Cells[3].Value = d.FileName;
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
            config.Modifiers = ModifierValue(Convert.ToString(modifier.SelectedItem));
            if (key.SelectedItem != null) config.KeyCode = (int)(Keys)key.SelectedItem;
            config.Mode = mode.SelectedIndex == 1 ? "Hold" : "Click";
            config.Style = Convert.ToString(style.SelectedItem);
            config.Opacity = opacity.Value;
            config.StartWithWindows = startup.Checked;
            Startup.Set(config.StartWithWindows);
            ConfigStore.Save(config);
            if (ConfigSaved != null) ConfigSaved(this, EventArgs.Empty);
        }

        private string ModifierName(int n)
        {
            if (n == 1) return "Alt"; if (n == 4) return "Shift"; if (n == 8) return "Win";
            if (n == 3) return "Ctrl+Alt"; if (n == 6) return "Ctrl+Shift"; return "Ctrl";
        }
        private int ModifierValue(string n)
        {
            if (n == "Alt") return 1; if (n == "Shift") return 4; if (n == "Win") return 8;
            if (n == "Ctrl+Alt") return 3; if (n == "Ctrl+Shift") return 6; return 2;
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
            return new Label { Text = text, Left = x, Top = y, Width = w, Height = h, ForeColor = Color.White, Font = new Font("Microsoft YaHei UI", size, bold ? FontStyle.Bold : FontStyle.Regular) };
        }
        private Button B(string text, int x, int y, int w, int h)
        {
            Button b = new Button { Text = text, Left = x, Top = y, Width = w, Height = h, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(52,68,94), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(68, 92, 132);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(42, 64, 104);
            return b;
        }
        private ComboBox C(int x, int y, int w)
        {
            return new ComboBox { Left = x, Top = y, Width = w, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(40,48,66), ForeColor = Color.White };
        }
    }

    static class Startup
    {
        public static void Set(bool enabled)
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) {
                if (enabled) k.SetValue("OrbitWheel-Lite", "\"" + Application.ExecutablePath + "\"");
                else k.DeleteValue("OrbitWheel-Lite", false);
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
            tray = new NotifyIcon { Icon = IconFactory.AppIcon(), Text = "OrbitWheel-Lite", Visible = true };
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
                tray.ShowBalloonTip(3000, "OrbitWheel-Lite", "快捷键已被其他程序占用，请在设置中更换。", ToolTipIcon.Warning);
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
                MessageBox.Show("设置页面打开失败：\n" + ex.Message, "OrbitWheel-Lite");
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
            using (System.Threading.Mutex mutex = new System.Threading.Mutex(true, "OrbitWheel-Lite.SingleInstance", out created)) {
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
