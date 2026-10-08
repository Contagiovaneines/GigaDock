# Pokédex de mascotes — 2026-10-08

A seção Mascotes agora oferece uma Pokédex de Kanto com os 151 Pokémon em cartões, número, miniatura, indicação de seleção e painel do companheiro escolhido. A busca local aceita nomes parciais, diferenças de caixa e acentos, pontuação e números como `25` e `#025`. Há contador, botão Limpar e estado vazio. Os cartões permitem seleção pelo teclado, com foco visível e nomes acessíveis.

A escolha continua vinculada ao ambiente em edição. Filtrar a galeria não apaga o Pokémon salvo. O painel principal mostra a espécie escolhida, inclusive com o mascote desativado; o rodapé identifica separadamente a forma atual na dock ativa, que pode ter evoluído.

As 151 miniaturas PNG estão incorporadas como recursos WPF e totalizam 125.426 bytes. A galeria e a busca funcionam sem rede. O mascote na dock mantém os GIFs e o cache implementados anteriormente. Origem: [PokeAPI/sprites](https://github.com/PokeAPI/sprites), com licença original preservada em `src/DockWindows.App/Assets/Pokemon/LICENCE.txt`. URLs, tamanhos e hashes estão em `docs/pokedex-assets.json`.

Validação: solução Release compilada; confira o resultado final em `docs/STATUS.md`. Os testes de busca cobrem números, nomes, acentos, pontuação, Nidoran e ausência de resultados. A execução local dos testes está sujeita ao bloqueio existente do Smart App Control; compilação não equivale a teste visual. Próximo passo: conferir seleção, busca, teclado e redimensionamento na aplicação em execução permitida.
