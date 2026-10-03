path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Infrastructure\Windows\ToastNotificationService.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        public event Action<string>? OnNotificationReceived;"""
good = """        public event Action<string, bool>? OnNotificationReceived;"""
c = c.replace(bad, good)

bad2 = """                        string appName = nova.AppInfo.DisplayInfo.DisplayName ?? string.Empty;
                        OnNotificationReceived?.Invoke(appName);"""
good2 = """                        string appName = nova.AppInfo.DisplayInfo.DisplayName ?? string.Empty;
                        
                        bool isCall = false;
                        try 
                        {
                            var textNodes = nova.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric)?.GetTextElements();
                            if (textNodes != null)
                            {
                                foreach(var node in textNodes)
                                {
                                    string t = node.Text.ToLowerInvariant();
                                    if (t.Contains("chamada") || t.Contains("ligando") || t.Contains("call") || t.Contains("calling"))
                                    {
                                        isCall = true;
                                        break;
                                    }
                                }
                            }
                        } catch { }

                        OnNotificationReceived?.Invoke(appName, isCall);"""
c = c.replace(bad2, good2)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
