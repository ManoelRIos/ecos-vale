# Ecos da Meia-Noite

Roguelike 2D em pixel art para **Unity 6.0 LTS 6000.0.85f1**. Explore uma cripta de Halloween, enfrente criaturas e projéteis, derrote o guardião do andar e visite o Mercado das Almas antes de continuar.

O projeto contém somente este jogo: cena `Haunt.unity`, combate, geração de salas, loja, interface e ferramentas de desenvolvimento. Os sprites são originais, gerados por código e exibidos com filtro de ponto.

[**Baixar o projeto em ZIP**](https://github.com/ManoelRIos/ecos-vale/raw/refs/heads/codex/ecos-do-vale-vertical-slice/Distribution/EcosDaMeiaNoite.zip)

## Abrir e jogar

1. Instale **Unity 6000.0.85f1** pelo Unity Hub. Inclua **Windows Build Support** se quiser gerar um executável Windows.
2. Extraia todo o ZIP em uma **pasta nova e curta**, por exemplo `C:\Dev\EcosDaMeiaNoite`. A pasta deve conter `Assets`, `Packages`, `ProjectSettings` e `ABRIR_ECOS_DA_MEIA_NOITE.cmd`.
3. Feche o Editor aberto em outra cópia e execute **ABRIR_ECOS_DA_MEIA_NOITE.cmd**. O atalho confere os arquivos e solicita a abertura de `Haunt.unity` após a importação. Se a instalação do Editor estiver fora do caminho padrão do Hub, adicione essa pasta pelo Hub e use **Ecos → Abrir roguelike**, com o Play parado.
4. Aguarde a resolução dos pacotes e a compilação, pressione **Play** e inicie uma incursão pelo menu. Uma semente numérica repete a disposição das salas; campo vazio gera outra semente.

Use uma extração nova para atualizar uma cópia recebida por ZIP: sobrepor arquivos não remove arquivos que deixaram de fazer parte do projeto. Preserve alterações próprias antes de trocar de pasta. Em um clone Git, a atualização da branch também aplica as remoções dos arquivos rastreados.

O log do atalho fica em `Logs/MeiaNoite-Editor.log`. `ABRIR_ECOS_DA_MEIA_NOITE.cmd /check` verifica a pasta sem abrir o Editor. O atalho recebeu verificação estática, sem execução Windows neste ambiente.

## Combate e progressão

- Andares com 7–11 salas conectadas por portas, determinadas pela semente. A entrada é segura; as saídas de uma sala de combate ficam seladas até todos os inimigos morrerem.
- Movimento em oito direções, mira pelo mouse, disparos e esquiva com invulnerabilidade temporária.
- Quatro criaturas: abóbora perseguidora, fantasma atirador, cultista com rajadas e morcego que prepara um bote.
- Um chefe por andar. Três variações de ataques alternam com a progressão; dificuldade e recompensas aumentam.
- Após vencer o chefe, um portal leva ao Mercado das Almas. Compre até 3 pontos de cura, +1 de dano, intervalo entre tiros 15% menor ou +2 de vida máxima. Cada item pode ser comprado uma vez por visita; uma compra recusada não consome moedas.
- O próximo andar preserva vida, moedas e melhorias. Morrer encerra a incursão e permite repetir a semente ou gerar outra.
- HUD de vida, moedas, andar e chefe; mapa das salas, pausa e reinício.

| Controle | Ação |
| --- | --- |
| WASD | Mover |
| Mouse | Mirar |
| Botão esquerdo | Atirar |
| Espaço / botão direito | Esquivar |
| Caminhar até uma saída aberta | Trocar de sala |
| E | Usar portal / sair da loja |
| Botões da loja | Comprar de qualquer ponto do mercado |
| M | Alternar mapa |
| Esc | Pausar / retomar |

## Desenvolvimento e validação

O jogo é local e não exige serviços externos, contas de API ou segredos. A configuração de renderização prepara somente o URP e os recursos usados pelo roguelike.

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools -p 'test_*.py'
dotnet run --project Tools/HauntChecks.csproj
```

`HauntChecks` compila o núcleo real em C# puro, sem stubs Unity, e verifica mapas, progressão, compras e colisões de projéteis. Os checks Python verificam a estrutura, os metadados, as versões e o empacotamento. Eles não substituem a compilação ou a execução no Editor.

Com um Editor licenciado, execute os testes em `Assets/_Game/Haunt/Tests/Editor` pelo Test Runner, ou use:

```bash
export UNITY_EDITOR_PATH='/caminho/para/Unity'
bash Tools/unity-check.sh import
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

O build Windows usa `Builds/Windows/EcosDaMeiaNoite.exe`. Para empacotar as fontes, inclua os arquivos novos no índice Git e execute `python3 Tools/package_project.py`; o resultado é `Distribution/EcosDaMeiaNoite.zip`, com raiz `EcosDaMeiaNoite/`.

**Estado da verificação:** o Editor Unity não está instalado neste ambiente. A importação desta limpeza, a compilação completa Unity, o Play Mode, os shaders e o build ainda precisam ser executados no Editor. Os testes Unity verificam objetos e estado, sem certificar a aparência renderizada. Consulte [VALIDATION.md](Documentation/VALIDATION.md) para o roteiro de verificação e [UNITY_TROUBLESHOOTING.md](Documentation/UNITY_TROUBLESHOOTING.md) se a abertura falhar.

Esta é uma base de combate e progressão. Armas, áudio, animação, arte e balanceamento ainda precisam de refinamento. As incursões não têm persistência entre sessões nem multiplayer.

## Organização

- `Assets/_Game/Haunt/Core`: geração de andares, estado da incursão e geometria em C# puro.
- `Assets/_Game/Haunt/Runtime`: combate, renderização e interface.
- `Assets/_Game/Haunt/Art`: sprites procedurais e shader.
- `Assets/_Game/Haunt/Editor`: configuração URP, abertura da cena e build.
- `Assets/_Game/Haunt/Tests/Editor`: testes de integração no Unity.
- `Assets/_Game/Scenes/Haunt.unity`: única cena do jogo e entrada do build.
- `Assets/_Game/Resources`: material de sprites e recursos URP usados pelo jogo.

Veja [ARCHITECTURE.md](Documentation/ARCHITECTURE.md) e [PACKAGES.md](Documentation/PACKAGES.md).
