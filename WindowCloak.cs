using System.Diagnostics;
using System.Reflection;

namespace WindowCloak;

public class WindowCloak : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly Config _config;
    private readonly Dictionary<nint, int> _hiddenWindows;

    public WindowCloak()
    {
        _config = Config.Load();
        _hiddenWindows = new Dictionary<nint, int>();
        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
            Text = "WindowCloak",
            Visible = true
        };

        _notifyIcon.MouseUp += OnClick;
        _ = Task.Run(WindowLoop);
    }
    
    private async Task WindowLoop()
    {
        // used to keep track of windows ceasing to exist
        var wnds = new List<nint>(); 
        
        while (true)
        {
            try
            {
                if (!_config.Enabled)
                {
                    await Task.Delay(1);
                    continue;
                }
                
                wnds.Clear();
            
                foreach (var kvp in WindowHandler.GetVisibleWindows())
                {
                    wnds.Add(kvp.Key);
                
                    var shouldHide = (!_config.Windows.ContainsKey(kvp.Value) || !_config.Windows[kvp.Value]) != _config.AllowByDefault;
                    var newAffinity = shouldHide ? (_config.FullyHideWindows ? 0x11 : 0x01) : 0x00;
                
                    var shouldUpdate = !_hiddenWindows.ContainsKey(kvp.Key) || _hiddenWindows[kvp.Key] != newAffinity;
                    if(!shouldUpdate)
                        continue;

                    // hack: for some reason toggling the setting doesn't work nicely unless we do this
                    if(_hiddenWindows.TryGetValue(kvp.Key, out var value) && ((value == 0x11 && newAffinity == 0x01) || value == 0x01 && newAffinity == 0x11))
                        WindowHandler.SetWindowDisplayAffinity(kvp.Key, 0x00);
                    
                    WindowHandler.SetWindowDisplayAffinity(kvp.Key, newAffinity);
                    _hiddenWindows[kvp.Key] = newAffinity;
                    
#if DEBUG
                    Console.WriteLine("Updated window visibility for {0} ({1}): {2}", kvp.Key, kvp.Value, shouldHide);
#endif
                }
            
                // Clear windows that aren't present anymore
                for (var i = _hiddenWindows.Count - 1; i >= 0; i--)
                {
                    var id = _hiddenWindows.Keys.ElementAt(i);
                    if (!wnds.Contains(id))
                    {
                        _hiddenWindows.Remove(id);
#if DEBUG
                        Console.WriteLine("Window has closed: {0}", id);
#endif
                    }
                }
            }
            catch (Exception exception)
            {
#if DEBUG
                Console.WriteLine("Exception in WindowLoop: {0}", exception);
#endif
            }
            
            await Task.Delay(1);
        }
    }

    private void ToggleFullyHideWindows(object? sender, EventArgs e)
    {
        _config.FullyHideWindows = !_config.FullyHideWindows;
        _config.Save();
    }
    
    private void ToggleAllowByDefault(object? sender, EventArgs e)
    {
        _config.AllowByDefault = !_config.AllowByDefault;
        _config.Save();
    }

    private void ToggleEnabled(object? sender, EventArgs e)
    {
        _config.Enabled = !_config.Enabled;

        if (!_config.Enabled)
            RevealAllWindows();
        
        _config.Save();
    }

    private void Exit(object? sender, EventArgs e)
    {
        _notifyIcon.Visible = false;
        RevealAllWindows();
        Application.Exit();
    }

    private void RevealAllWindows()
    {
        _config.Enabled = false;
        _hiddenWindows.Clear();
        
        foreach (var kvp in WindowHandler.GetVisibleWindows())
        {
            WindowHandler.SetWindowDisplayAffinity(kvp.Key, 0x00);
        }
    }

    private void OnClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right) 
            return;
        
        _notifyIcon.ContextMenuStrip = CreateContextMenuStrip();
        
        // note: I stole this weird hack off StackOverflow, for some weird reason unless I do this, the context menu
        // on the tray icon will just not update correctly. I have no idea why and don't care to find out.
        var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        if(mi != null) mi.Invoke(_notifyIcon, null);
    }

    
    private ContextMenuStrip CreateContextMenuStrip()
    {
        var windows = new ToolStripMenuItem("Windows");
        var processes = new List<string>();
        
        foreach (var process in Process.GetProcesses())
        {
            if (process.MainWindowHandle <= 0 || processes.Contains(process.ProcessName))
                continue;

            processes.Add(process.ProcessName);
            
            // this is kinda ugly I don't like C#
            windows.DropDownItems.Add(new ToolStripMenuItem(process.ProcessName, null, (_, _) =>
            {
                if (_config.Windows.TryAdd(process.ProcessName, true))
                    return;
                        
                _config.Windows[process.ProcessName] = !_config.Windows[process.ProcessName];
                _config.Save();
            })
            {
                Checked = _config.Windows.ContainsKey(process.ProcessName) && _config.Windows[process.ProcessName],
                CheckOnClick = true
            });
        }
        
        return new ContextMenuStrip
        {
            Items =
            {
                new ToolStripMenuItem("Enable Cloak", null, ToggleEnabled)
                {
                    Checked = _config.Enabled,
                    CheckOnClick = true,
                },
                new ToolStripMenuItem("Allow by default", null, ToggleAllowByDefault)
                {
                    Checked = _config.AllowByDefault,
                    CheckOnClick = true,
                },
                new ToolStripMenuItem("Fully hide windows", null, ToggleFullyHideWindows)
                {
                    Checked = _config.FullyHideWindows,
                    CheckOnClick = true,
                },
                new ToolStripSeparator(),
                windows,
                new ToolStripSeparator(),
                new ToolStripMenuItem("Exit", null, Exit)
            }
        };
    }
}