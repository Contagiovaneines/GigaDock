path = r"c:\Users\giovane\Documents\dockwindows\src\DockWindows.Infrastructure\Windows\TeamsIntegrationService.cs"
with open(path, "r", encoding="utf-8") as f:
    c = f.read()

bad = """            else if (teamsProcs.Any())
            {
                // Teams aberto mas sem chamada - mostrar apenas "Disponível"
                newStatus = "Disponível";
                newCor = "#23A736";
                newContext = string.Empty; // sem texto extra quando disponivel
            }"""

good = """            else if (teamsProcs.Any())
            {
                // Como não temos acesso à API do Graph (para manter 100% offline/local),
                // não podemos ler o status real de "Ocupado" ou "Ausente".
                // Portanto, mostramos um status neutro para não mentir.
                newStatus = "Aberto";
                newCor = "#8E8E93"; // Cinza neutro
                newContext = string.Empty;
            }"""

# Handle encoding issues with 'Disponível' by regex if exact match fails
import re
c = re.sub(r'else if \(teamsProcs\.Any\(\)\)\s*\{\s*// Teams aberto.*?\n\s*newStatus = "Dispon.vel";\s*newCor = "#23A736";\s*newContext = string\.Empty;.*?\n\s*\}', good, c, flags=re.DOTALL)

with open(path, "w", encoding="utf-8") as f:
    f.write(c)
