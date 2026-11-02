# Renderização e primeira revisão visual

A captura enviada pelo usuário confirma HUD e personagem em execução, mas mostra objetos magenta e terreno pouco/não visível. O usuário não observou erros vermelhos no Console. Isso é compatível com materiais URP usados sob um pipeline incompatível ou com recursos importados incompletamente; a captura não identifica sozinha qual estado estava ativo.

A revisão do código encontrou falhas concretas de preparação: aceitava qualquer pipeline não nulo, não validava shaders dos materiais existentes e podia pular o setup ao entrar em Play antes do callback inicial. Essas condições foram corrigidas; não foram tratadas como prova de que o render final já funciona na máquina do usuário.

## Mudanças implementadas

- O Editor seleciona `ValleyURP` em Graphics e em todos os níveis de qualidade, verificando o renderer 3D do próprio projeto.
- Antes de entrar em Play, uma preparação pendente é concluída em Edit Mode. Em runtime, `RenderingSetup` também aplica o pipeline configurado; trocar qualidade mantém essa seleção.
- Materiais existentes com shader incorreto são reparados preservando GUID e cor base. A fábrica usa shaders explícitos e informa shader ausente com uma mensagem de ação; não troca para um fallback para esconder erros.
- **Ecos → Reparar renderização URP** recria os recursos gerados do renderer/pipeline em seus assets, preservando GUID e valores básicos de qualidade. Esse comando restaura os recursos padrão e pode remover personalizações de renderer features feitas nesses assets gerados.
- **Ecos → Diagnosticar renderização** informa pipeline atual/Graphics/qualidade, shader encontrado, `isSupported` e mensagens do compilador de shaders. O estado suportado pode mudar após a troca de pipeline; o diagnóstico é explícito, sem desativar validações.
- Material emissivo em Resources mantém a variante de emissão referenciada para builds; o build Windows ainda não foi validado.
- Novo céu com gradiente, disco solar/lunar e transição pelo relógio do mundo. Névoa menos densa e ajuste de luz ambiente para leitura do terreno.
- Personagem com chapéu, lenço, mochila, botas, luvas, pivôs de animação e lanterna visual.
- Árvores com seis silhuetas temáticas; pinheiros com neve, galhos retorcidos, raízes e copas facetadas. LOD distante mantém copas/neve em vez de exibir somente o tronco.
- Meshes compartilhados para cone, tronco, copa e cogumelo; rochas facetadas e abóboras com rostos luminosos. Novos meshes também são persistidos ao gerar prefabs no Editor.
- Densidade padrão aumentada de 24 para 32 props/chunk. O custo em FPS ainda não foi medido.
- HUD com tipografia maior na Game View baixa, menu com rolagem e mapa ajustado à altura disponível.

## Como conferir no Unity

1. Atualize a cópia do projeto, preservando alterações próprias, e mantenha o caminho curto `C:\Dev\EcosDoVale`.
2. Fora do Play Mode, use **Ecos → Reparar renderização URP** e aguarde a preparação.
3. Use **Ecos → Abrir cena principal**, entre em Play e escolha Novo jogo.
4. Confira primeiro o chão e as cores do personagem/árvores; depois teste câmera, movimento, transição de horário e interface.
5. Se algo continuar magenta, use **Ecos → Diagnosticar renderização** e consulte as mensagens de pipeline/shader dessa sessão.

Os prefabs de referência já existentes são preservados. As novas formas do jogador/vegetação são usadas pela fábrica runtime; o reparo de renderização não sobrescreve prefabs editados pelo usuário.

## Verificação realizada e limites

Análise sintática/referências locais e os testes Python passaram. A geometria numérica dos novos perfis foi verificada quanto a faces não degeneradas e orientação externa. Foram adicionados testes EditMode para reparo de material, atualização de recursos e geometria dos meshes; eles ainda não foram executados.

**Não há Unity Editor neste ambiente.** Não foram executados compilação HLSL/ShaderLab pela Unity, compilação completa dos novos componentes, renderização, testes EditMode, Play Mode ou benchmark. O resultado visual precisa ser confirmado no Editor; a imagem anterior não foi alterada nem substituída por uma imagem gerada.
