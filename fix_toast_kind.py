path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Infrastructure\Windows\ToastNotificationService.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

c = c.replace("args.ChangeKind == UserNotificationChangedKind.Added || args.ChangeKind == UserNotificationChangedKind.Modified", "args.ChangeKind == UserNotificationChangedKind.Added")

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
