# Transformação inspirada no GIF

Inspecionados 102 quadros do GIF local enviado pelo usuário. A referência mostra a forma inicial, uma esfera branca com energia azul circulando e a revelação da próxima espécie.

O efeito WPF reproduz essa sequência durante 3 segundos usando sprites locais: silhueta clareando e encolhendo, esfera branca, três arcos azuis/brancos giratórios, partículas e nova forma surgindo da luz. Fundo transparente, área de 64 × 56 e altura temporária de 60 na faixa do mascote. O GIF completo não é incorporado, pois contém espécies e fundo fixos.

O efeito vale tanto para Eevee por clima quanto para evoluções por uma/três horas. Usa o temporizador visual existente; respeita animações desativadas, cancelamento ao ocultar/trocar e encerramento. A escolha final da forma é confirmada somente após completar a transformação.

Validação: 23 testes de recursos, clima e relógio aprovados; um teste adicional de renderização WPF aprovado, verificando transparência, esfera central branca e distinção entre espécie inicial/final. Prévia de seis quadros renderizados em pokemon-evolution-preview.png, inspecionada visualmente. O teste inicial precisou de UpdateLayout para renderizar a invalidação do controle sem janela; corrigido e executado novamente. Não houve inspeção da animação em tempo real na dock nem instalação manual.
