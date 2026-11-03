# Ecos da Meia-Noite

Primeira versão do novo **roguelike 2D em pixel art para Unity 6.0 LTS 6000.0.85f1**. Uma cripta de Halloween substitui a exploração em terceira pessoa como experiência principal: entrar em salas, enfrentar criaturas e projéteis, derrotar o chefe e visitar um mercado seguro antes de descer ao próximo andar.

O projeto mantém a infraestrutura Unity existente. A cena antiga `Valley.unity` continua disponível para referência e não entra no build padrão. Os recursos visuais do novo jogo são sprites originais gerados por código, com filtro de ponto; esta etapa não usa arte de Enter the Gungeon nem depende de assets pagos.

**Validação nesta revisão:** passaram 23 testes Python e 99.935 asserções do núcleo C# do roguelike, incluindo 1.624 andares gerados por seed. A análise estática verifica sintaxe e referências, sem substituir a compilação Unity. O Editor Unity não está instalado aqui: importação, compilação completa Unity, Play Mode, apresentação visual e build Windows ainda precisam de execução no Editor. Não há um executável Windows validado desta versão.

## Abrir e jogar

[**Baixar o projeto atualizado em ZIP**](https://github.com/ManoelRIos/ecos-vale/raw/refs/heads/codex/ecos-do-vale-vertical-slice/Distribution/EcosDaMeiaNoite.zip)

1. Instale **Unity 6000.0.85f1** pelo Unity Hub. Para gerar o executável Windows, inclua **Windows Build Support**.
2. Extraia **todo** o ZIP atualizado em uma pasta nova e curta, por exemplo `C:\Dev\EcosDoVale`, preservando sua cópia anterior e alterações próprias. A pasta deve conter `Assets`, `Packages`, `ProjectSettings` e `ABRIR_ECOS_DA_MEIA_NOITE.cmd`.
3. Feche o Editor que estiver com a cópia antiga aberta. Execute **ABRIR_ECOS_DA_MEIA_NOITE.cmd** nessa pasta nova. O atalho confere se o código e a cena do roguelike estão presentes e solicita a abertura de `Haunt.unity` após a importação. Se o Editor estiver fora do caminho padrão do Hub, adicione essa pasta pelo Hub e use **Ecos → Abrir roguelike**, com o Play parado.
4. Aguarde a resolução dos pacotes e a compilação, pressione **Play** e inicie uma nova partida pelo menu. A seed permite repetir a disposição das salas; uma nova partida pode gerar outra seed.

O atalho antigo `ABRIR_ECOS_DO_VALE.cmd` encaminha para o novo, preservando os argumentos. `Distribution/EcosDaMeiaNoite.zip` e `Distribution/EcosDoVale.zip` agora contêm a mesma versão atualizada para que links antigos não entreguem o vale anterior. O log dessa abertura fica em `Logs/MeiaNoite-Editor.log`. A verificação sem abrir o Editor pode ser executada com `ABRIR_ECOS_DA_MEIA_NOITE.cmd /check`. Os atalhos receberam testes de contrato estático; ainda não foram executados no Windows.

O log enviado mostrou a importação e abertura de `Valley.unity`, sem referências ao novo modo `Haunt` e sem erros de compilação C#. Isso indica uma abertura da cópia anterior. O aviso de ausência de AudioListener, isoladamente, não identifica uma falha do roguelike. Se não aparecer **Ecos → Abrir roguelike**, confirme que o Hub está abrindo a pasta recém-extraída e que ela contém `Assets/_Game/Scenes/Haunt.unity`.

## A primeira versão

- Salas conectadas em uma grade, sem sobreposição, com disposição determinada pela seed. A entrada começa segura; salas de combate fecham as saídas até todos os inimigos morrerem.
- Movimento em oito direções, mira pelo mouse, disparos e esquiva com invulnerabilidade temporária.
- Quatro inimigos: abóbora perseguidora, fantasma atirador, cultista com rajadas e morcego veloz.
- Um chefe por andar, com padrões de projéteis. Três variações alternam conforme a progressão; dificuldade e recompensas aumentam nos andares seguintes.
- Ao vencer o chefe, um portal leva a uma área de compras separada. Os botões da loja podem ser usados de qualquer ponto do mercado. Moedas compram até 3 pontos de cura, +1 de dano, intervalo entre tiros 15% menor ou +2 de vida máxima. As compras têm custo, limite por visita e não consomem dinheiro se falharem.
- Sair da loja inicia outro andar, preservando vida, moedas e melhorias. Morrer encerra a partida e permite começar outra.
- HUD de vida, moedas, andar, chefe e mapa de salas; pausa e reinício pelo menu.

| Controle | Ação |
| --- | --- |
| WASD | Mover |
| Mouse | Mirar |
| Botão esquerdo | Atirar |
| Espaço / botão direito | Esquivar |
| Caminhar até uma saída aberta | Trocar de sala |
| E | Usar portal / sair da loja |
| M | Alternar mapa |
| Esc | Pausar / retomar |

Esta é uma base de combate e progressão para evoluir. As armas e melhorias são iniciais; arte, animação, áudio, variedade de salas e balanceamento ainda precisam de refinamento. As partidas novas são locais, sem multiplayer e sem persistência de uma run entre sessões. Os saves do vale antigo não são convertidos nem apagados pelo novo modo.

## Verificações de desenvolvimento

```bash
python3 -m pip install -r Tools/requirements-validation.txt
python3 Tools/validate_project.py
python3 -m unittest discover -s Tools -p 'test_*.py'
dotnet run --project Tools/HauntChecks.csproj
dotnet run --project Tools/CoreChecks.csproj
```

`HauntChecks` compila os arquivos reais do núcleo do roguelike, sem stubs Unity. Verifica conectividade, determinismo e variação de mapas, posição do chefe, compras, recompensas, morte e passagem entre andares. `CoreChecks` cobre o vale legado. Nenhum desses comandos verifica o render ou o comportamento em Play Mode.

Os testes Unity ficam em `Assets/_Game/Haunt/Tests/Editor`: cena inicial e configuração do build, integridade dos sprites, reinício após morte, combate/recompensa sem respawn, proteção da esquiva e abertura segura durante Play. Eles verificam objetos e estado, sem validar imagens renderizadas. Os testes de runtime isolam uma cena vazia e restauram a configuração anterior; preservam alterações de cena não salvas, pulando esse grupo com motivo explícito se necessário. Execute pelo Test Runner do Editor. Para rodar checks em lote com um Editor licenciado:

```bash
export UNITY_EDITOR_PATH='/caminho/para/Unity'
bash Tools/unity-check.sh import
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

O smoke de importação confirma recursos, bootstrap da cena e geração inicial; ele não entra em Play Mode. Confira os logs e o XML atual da execução dos testes em `/tmp`. Saída padrão do build: `Builds/Windows/EcosDaMeiaNoite.exe`. O lockfile transitivo de pacotes deve ser gerado pelo UPM real durante a importação.

Antes de considerar a versão jogável validada, teste no Editor: iniciar e repetir uma seed; entrar em combate; esquivar atravessando projéteis; derrotar cada tipo de inimigo e um chefe; atravessar o portal; comprar na loja; avançar de andar; pausar; morrer e reiniciar. Verifique ainda colisão com paredes/obstáculos, mira em resoluções diferentes e legibilidade das cores.

## Organização

- `Assets/_Game/Haunt/Core`: geração de andares e estado de partida em C# puro.
- `Assets/_Game/Haunt/Runtime`: combate, apresentação e interface do novo modo.
- `Assets/_Game/Haunt/Art`: sprites procedurais e shader sem iluminação.
- `Assets/_Game/Haunt/Editor`: abertura segura da cena e smoke de importação.
- `Assets/_Game/Scenes/Haunt.unity`: entrada principal do jogo.
- Demais pastas de `_Game`: vale legado, preservado.

Para consultar a etapa anterior, veja `Documentation/ARCHITECTURE.md` e os documentos do vale. Para recriar uma distribuição de fontes, inclua os arquivos novos no índice Git antes de executar `python3 Tools/package_project.py`; a ferramenta empacota o índice e não gera executáveis.
