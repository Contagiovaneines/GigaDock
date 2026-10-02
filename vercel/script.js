document.addEventListener('DOMContentLoaded', () => {
    const dockItems = document.querySelectorAll('.dock-item');
    const dock = document.getElementById('dock');
    
    // Configurações do efeito de lupa
    const maxScale = 1.4;
    const neighborScale = 1.15;
    
    // Como a lupa completa é complexa em JS puro e pode causar jitter com os widgets flexíveis,
    // o CSS atual já faz 90% do trabalho lindo.
    // Vamos adicionar interatividade aos botões.

    dockItems.forEach(item => {
        item.addEventListener('click', () => {
            // Efeito de pulo ao clicar (Bouncing)
            item.style.transform = 'scale(1.2) translateY(-20px)';
            setTimeout(() => {
                item.style.transform = '';
            }, 200);
            
            const tooltip = item.getAttribute('data-tooltip');
            if(tooltip !== 'Ajustes do GigaDock') {
                console.log('Abrindo ' + tooltip);
            }
        });
    });
    
    // Pequeno efeito de seguimento do mouse no container da dock
    dock.addEventListener('mousemove', (e) => {
        const rect = dock.getBoundingClientRect();
        const x = e.clientX - rect.left; // posição x dentro da dock
        
        // Pode ser usado para brilhos dinâmicos no futuro
    });
});
