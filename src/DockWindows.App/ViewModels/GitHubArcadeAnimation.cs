using System.Collections.Generic;

namespace DockWindows.App.ViewModels;

// Simulações decorativas nas grades 7 × 7 e 36 × 7; não alteram dados salvos do GitHub.
internal sealed class GitHubArcadeAnimation
{
    private int _x, _y, _dx, _dy;
    private readonly HashSet<int> _revelados = new();
    private readonly HashSet<int> _minas = new();
    public void Reset(int colunas = 13)
    {
        _x = colunas / 2; _y = 5; _dx = 1; _dy = -1;
        _revelados.Clear(); _minas.Clear();
    }

    public void Tick(EstiloAnimacaoGitHub estilo, int quadro, IList<ContribuicaoDia> dias, int colunas = 13)
    {
        if (dias.Count < colunas * 7) return;
        foreach (var dia in dias) dia.MarcaArcade = 0;
        void Marcar(int x, int y, int marca)
        {
            if (x >= 0 && x < colunas && y >= 0 && y < 7) dias[y * colunas + x].MarcaArcade = marca;
        }
        switch (estilo)
        {
            case EstiloAnimacaoGitHub.Breakout:
                // Bola rebate nas paredes, consome tijolos e volta à raquete.
                if (_x + _dx < 0 || _x + _dx >= colunas) _dx = -_dx;
                if (_y + _dy is < 0 or > 5) _dy = -_dy;
                _x += _dx; _y += _dy;
                if (dias[_y * colunas + _x].Nivel > 0)
                {
                    dias[_y * colunas + _x].Nivel = 0; _dy = -_dy;
                }
                Marcar(_x, _y, 1);
                for (int p = -1; p <= 1; p++) Marcar(_x + p, 6, 2);
                break;
            case EstiloAnimacaoGitHub.Galaga:
                // Nave percorre a base; disparos sobem em direção aos invasores.
                int percurso = (colunas - 1) * 2;
                int nave = quadro % percurso; if (nave >= colunas) nave = percurso - nave;
                Marcar(nave, 6, 2); Marcar(nave - 1, 6, 2); Marcar(nave + 1, 6, 2);
                int deslocamento = quadro / 4 % 3;
                for (int i = 0; i < colunas / 2; i++)
                {
                    int ex = (i * 2 + deslocamento) % colunas;
                    if (!_revelados.Contains(i)) { Marcar(ex, 1, 3); Marcar(ex, 0, 4); }
                }
                int tiroX = (quadro / 6 * 6) % percurso;
                if (tiroX >= colunas) tiroX = percurso - tiroX;
                int tiroY = 5 - quadro % 6;
                Marcar(tiroX, tiroY, 1);
                if (tiroY <= 1)
                    for (int i = 0; i < colunas / 2; i++)
                        if ((i * 2 + deslocamento) % colunas == tiroX) { _revelados.Add(i); dias[colunas + tiroX].Nivel = 0; }
                if (_revelados.Count == colunas / 2) _revelados.Clear();
                break;
            case EstiloAnimacaoGitHub.PuzzleBobble:
                // Bolhas coloridas e disparo diagonal; grupos atingidos desaparecem.
                for (int y = 0; y < 3; y++)
                    for (int x = 0; x < colunas; x++)
                        if (!_revelados.Contains(y * colunas + x)) Marcar(x, y, 2 + (x / 3 + y) % 3);
                int fase = quadro % 8;
                int alvo = quadro / 8 % colunas;
                int bolhaX = colunas / 2 + (alvo - colunas / 2) * fase / 7;
                Marcar(colunas / 2, 6, 2); Marcar(bolhaX, 6 - fase * 4 / 7, 4);
                if (fase == 7)
                    for (int x = alvo - 1; x <= alvo + 1; x++)
                        if (x >= 0 && x < colunas)
                            for (int y = 0; y < 3; y++) { _revelados.Add(y * colunas + x); dias[y * colunas + x].Nivel = 0; }
                if (_revelados.Count >= colunas * 3) _revelados.Clear();
                break;
            case EstiloAnimacaoGitHub.Bomberman:
                int passo = quadro / 12;
                int bx = passo * 3 % colunas, by = passo * 2 % 7;
                int tempo = quadro % 12;
                if (tempo < 5) Marcar(bx, by, 2);
                else if (tempo < 8) { Marcar(bx, by, 3); Marcar(bx + 1, by, 2); }
                else
                    for (int d = -2; d <= 2; d++)
                    {
                        Marcar(bx + d, by, 5); Marcar(bx, by + d, 5);
                        if (bx + d >= 0 && bx + d < colunas) dias[by * colunas + bx + d].Nivel = 0;
                        if (by + d >= 0 && by + d < 7) dias[(by + d) * colunas + bx].Nivel = 0;
                    }
                break;
            case EstiloAnimacaoGitHub.Minesweeper:
                if (quadro == 0)
                    for (int i = 0; i < colunas * 7; i++) if (dias[i].Nivel >= 4) _minas.Add(i);
                _revelados.Add(quadro % (colunas * 7));
                foreach (int i in _revelados)
                {
                    if (_minas.Contains(i)) { dias[i].MarcaArcade = 6; continue; }
                    int vizinhas = 0;
                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                        {
                            int nx = i % colunas + x, ny = i / colunas + y;
                            if ((x != 0 || y != 0) && nx >= 0 && nx < colunas && ny >= 0 && ny < 7 && _minas.Contains(ny * colunas + nx)) vizinhas++;
                        }
                    dias[i].MarcaArcade = 10 + vizinhas;
                }
                break;
        }
    }
}
