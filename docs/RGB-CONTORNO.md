# Ajuste visual do contorno RGB


## Contorno RGB mais definido (2026-10-05)

- Halo reforçado: espessura de 3 para 5 DIP e desfoque de 14 para 8 DIP. Paleta saturada com oito pontos incluindo rosa, violeta, azul, ciano, verde e amarelo.
- Borda nítida de 2 DIP desenhada acima da superfície da dock, compartilhando o pincel animado do halo. Altura e alinhamento acompanham a barra, inclusive a área reservada à magnificação dos ícones.
- Sem timer ou storyboard adicional; conserva a suspensão por visibilidade real e preferências de animação. As duas camadas não capturam cliques.
- Build Release: zero erros, um aviso CS0067 preexistente em fake de teste. Aplicativo e instalador win-x64 publicados; app.zip e release/GigaDock-Setup.exe atualizados.
- Limites: aparência em execução, diferentes fundos e escalas de tela ainda requerem conferência manual; não houve medição de GPU/CPU nem teste visual nesta etapa. Próximo passo: conferir contraste e espessura em 100%, 125%, 150% e 200%.
