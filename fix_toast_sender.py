path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Infrastructure\Windows\ToastNotificationService.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """        public event Action<string, bool>? OnNotificationReceived;"""
good = """        public event Action<string, bool, string>? OnNotificationReceived;"""
c = c.replace(bad, good)

bad2 = """                        bool isCall = false;
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
good2 = """                        bool isCall = false;
                        string senderName = string.Empty;
                        try 
                        {
                            var textNodes = nova.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric)?.GetTextElements();
                            if (textNodes != null && textNodes.Count > 0)
                            {
                                senderName = textNodes[0].Text; // Geralmente o primeiro texto é o nome do remetente
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

                        OnNotificationReceived?.Invoke(appName, isCall, senderName);"""
c = c.replace(bad2, good2)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
