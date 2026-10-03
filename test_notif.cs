using System;
using System.Threading.Tasks;

class Program {
    static async Task Main() {
        try {
            var listener = Windows.UI.Notifications.Management.UserNotificationListener.Current;
            var access = await listener.RequestAccessAsync();
            Console.WriteLine("Access: " + access);
        } catch (Exception ex) {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
