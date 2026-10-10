using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GigaDock.Services.Persistence;

namespace DockWindows.App.Views;

public sealed class TarefasPanel : StackPanel
{
    public TarefasPanel(string environment)
    {
        Width = 310;
        var store = new LocalTaskStore(System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DockWindows"));
        var list = new StackPanel(); var status = new TextBlock { Foreground = Brushes.Salmon, TextWrapping = TextWrapping.Wrap };
        void Draw()
        {
            list.Children.Clear();
            foreach (var task in store.Read(environment))
            {
                var row = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
                var remove = new Button { Content = "×", ToolTip = "Excluir tarefa " + task.Title, Margin = new Thickness(4, 0, 0, 0) };
                System.Windows.Automation.AutomationProperties.SetName(remove, "Excluir tarefa " + task.Title);
                DockPanel.SetDock(remove, Dock.Right); row.Children.Add(remove);
                var check = new CheckBox { IsChecked = task.Completed, Foreground = Brushes.White,
                    Content = new TextBlock { Text = task.Title, TextWrapping = TextWrapping.Wrap, MaxWidth = 245 } };
                System.Windows.Automation.AutomationProperties.SetName(check, task.Title);
                void Complete(object sender, RoutedEventArgs e)
                {
                    try { store.Complete(environment, task.Id, check.IsChecked == true); status.Text = ""; }
                    catch (Exception error) { status.Text = error.Message; }
                    Reload();
                }
                check.Checked += Complete; check.Unchecked += Complete;
                remove.Click += (_, _) => { try { store.Remove(environment, task.Id); status.Text = ""; } catch (Exception error) { status.Text = error.Message; } Reload(); };
                row.Children.Add(check); list.Children.Add(row);
            }
        }
        void Reload() { try { Draw(); } catch (Exception error) { status.Text = error.Message; } }
        Children.Add(new TextBlock { Text = "Tarefas deste ambiente", FontSize = 18, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 12) });
        Children.Add(new ScrollViewer { MaxHeight = 280, Content = list });
        var input = new TextBox { MaxLength = 200, Margin = new Thickness(0, 8, 0, 8) };
        System.Windows.Automation.AutomationProperties.SetName(input, "Título da nova tarefa"); Children.Add(input);
        var add = new Button { Content = "Adicionar tarefa" };
        add.Click += (_, _) => { try { store.Add(environment, input.Text); input.Clear(); status.Text = ""; Reload(); } catch (Exception error) { status.Text = error.Message; } };
        Children.Add(add); Children.Add(status); Reload();
    }
}
