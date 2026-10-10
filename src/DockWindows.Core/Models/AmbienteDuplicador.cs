using System.Text.Json;

namespace DockWindows.Core.Models;

/// <summary>Duplica os dados locais sem reutilizar identidades nem registros de atividade.</summary>
public static class AmbienteDuplicador
{
    public static Ambiente Duplicar(Ambiente original, string nome)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > 60 || nome.Any(char.IsControl))
            throw new ArgumentException("Informe um nome de até 60 caracteres.");
        var copia = JsonSerializer.Deserialize<Ambiente>(JsonSerializer.Serialize(original))!;
        copia.Id = Guid.NewGuid().ToString();
        copia.Nome = nome.Trim();
        var novosIds = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var item in copia.Itens)
        {
            var anterior = item.Id; item.Id = Guid.NewGuid().ToString(); novosIds[anterior] = item.Id;
        }
        copia.OrdemAplicativosDock = (copia.OrdemAplicativosDock ?? []).Select(id => novosIds.GetValueOrDefault(id, id)).ToList();
        foreach (var colecao in copia.Colecoes)
        {
            colecao.Id = Guid.NewGuid().ToString();
            colecao.EhGlobal = false;
            foreach (var item in colecao.Itens) item.Id = Guid.NewGuid().ToString();
        }
        foreach (var widget in copia.WidgetsInstalados)
        {
            widget.Id = Guid.NewGuid().ToString();
            widget.Configuracao.Remove("UltimoRegistro");
        }
        return copia;
    }
}
