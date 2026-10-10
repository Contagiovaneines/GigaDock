using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GigaDock.Services.Persistence;

public sealed record LocalTask(string Id, string Title, bool Completed);

/// <summary>Lista local por ambiente; salva antes de devolver o novo estado à interface.</summary>
public sealed class LocalTaskStore(string dataRoot)
{
    private string FileFor(string environment)
    {
        if (string.IsNullOrWhiteSpace(environment)) throw new ArgumentException("Escolha um ambiente.");
        if (!Path.IsPathFullyQualified(dataRoot)) throw new ArgumentException("A pasta de dados deve ser absoluta.");
        return Path.Combine(dataRoot, "tasks", Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(environment))) + ".json");
    }

    public IReadOnlyList<LocalTask> Read(string environment)
    {
        var path = FileFor(environment);
        if (!File.Exists(path)) return [];
        if (new FileInfo(path).Length > 1024 * 1024) throw new IOException("A lista de tarefas excede o limite de tamanho.");
        var tasks = JsonSerializer.Deserialize<List<LocalTask>>(File.ReadAllText(path))
            ?? throw new IOException("A lista de tarefas não é válida.");
        Validate(tasks); return tasks;
    }

    public IReadOnlyList<LocalTask> Add(string environment, string title)
    {
        var tasks = Read(environment).ToList();
        tasks.Add(new LocalTask(Guid.NewGuid().ToString("N"), title.Trim(), false));
        Save(environment, tasks); return tasks;
    }

    public IReadOnlyList<LocalTask> Complete(string environment, string id, bool completed)
    {
        var tasks = Read(environment).ToList();
        var index = tasks.FindIndex(task => task.Id == id);
        if (index < 0) throw new ArgumentException("A tarefa não existe.");
        tasks[index] = tasks[index] with { Completed = completed };
        Save(environment, tasks); return tasks;
    }

    public IReadOnlyList<LocalTask> Remove(string environment, string id)
    {
        var tasks = Read(environment).Where(task => task.Id != id).ToList();
        Save(environment, tasks); return tasks;
    }

    private static void Validate(IReadOnlyList<LocalTask> tasks)
    {
        if (tasks.Count > 500 || tasks.Any(task => task is null || string.IsNullOrWhiteSpace(task.Id) ||
            string.IsNullOrWhiteSpace(task.Title) || task.Title.Length > 200 || task.Title.Any(char.IsControl)) ||
            tasks.Select(task => task.Id).Distinct(StringComparer.Ordinal).Count() != tasks.Count)
            throw new ArgumentException("Use até 500 tarefas, com títulos de 1 a 200 caracteres sem quebras de linha.");
    }

    private void Save(string environment, IReadOnlyList<LocalTask> tasks)
    {
        Validate(tasks);
        var path = FileFor(environment); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(tasks), new UTF8Encoding(false));
            if (OperatingSystem.IsLinux()) File.SetUnixFileMode(temporary, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
