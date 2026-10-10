# Site estático

O site está em `vercel/`, com `index.html`, `style.css`, `script.js` e assets locais. Não exige instalação de pacotes nem backend.

Para visualizar da raiz:

```bash
python -m http.server 8080 --directory vercel
```

Abra `http://localhost:8080`. A galeria possui abas Windows/Linux/Guia e navegação por teclado. Capturas públicas selecionadas ficam em `vercel/assets/screenshots/` e também são usadas no README.

Na hospedagem estática, use `vercel/` como pasta raiz e deixe o comando de build vazio. Confira os links de GitHub e releases antes de publicar. A página distingue o pacote Linux gerado localmente dos downloads efetivamente publicados.

Não envie `.vercel/`, tokens ou dados de conta. A publicação deve ser feita pelo mantenedor após revisar o resultado. Nenhum deploy foi executado nesta organização do repositório.
