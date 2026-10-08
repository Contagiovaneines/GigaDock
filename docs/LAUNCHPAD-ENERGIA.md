# Botões de energia no Launchpad

Etapa concluída em 8 de outubro de 2026: adicionados Desligar PC e Reiniciar PC em uma segunda linha do rodapé, preservando os botões existentes. Os controles têm nomes acessíveis e usam botões WPF com navegação por teclado.

Cada ação fecha o Launchpad e pede confirmação com Não selecionado por padrão. A execução usa o caminho absoluto de shutdown.exe no diretório de sistema e argumentos fixos: /s /t 0 ou /r /t 0. Não há /f; aplicativos podem bloquear o encerramento para salvar arquivos. Falhas ao iniciar o comando e códigos de saída diferentes de zero são exibidos em português.

Referência oficial: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/shutdown

Validação executada: dotnet build DockWindows.slnx --no-restore passou com zero avisos e erros; git diff --check passou. Não foram executados desligamento/reinício reais nem validação visual ou de teclado. Não foram adicionados testes que executem ações de energia nesta máquina.

Próximos passos: conferir layout em diferentes escalas, confirmar que Não cancela ambas as ações e validar desligamento/reinício em uma máquina de teste com arquivos salvos.
