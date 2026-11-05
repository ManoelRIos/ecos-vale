# Protótipo em primeira pessoa

`Assets/_Game/Scenes/HauntFps.unity` é a nova entrada padrão de **Ecos da Meia-Noite**, em Unity 6000.0.85f1. O objetivo desta versão é experimentar combate e atmosfera macabra em um cenário 3D, com câmera em primeira pessoa, texturas pixeladas e objetos de formas cartoon gerados em C#.

## Abrir e completar o encontro

1. Extraia o projeto em uma pasta nova e curta e abra sua raiz pelo Unity Hub. Aguarde os pacotes e a compilação.
2. Com Play parado, use **Ecos → Abrir protótipo FPS** e pressione **Play**. O atalho `ABRIR_ECOS_DA_MEIA_NOITE.cmd` também solicita essa cena.
3. Inicie, clique na janela Game e mova-se pela arena. Atire nos Esqueletos e nas Lanternas, aproveitando os pilares como cobertura. Barris explosivos também atingem o jogador.
4. O encontro libera três ondas sucessivas. Ao eliminar a última, aproxime-se do portal e use **E / Quadrado** para concluir.
5. **Enter / X** reinicia após morte ou vitória. **Esc / Options** pausa e libera o cursor. Os botões dos menus também permitem reiniciar e abrir o roguelike 2D.

## Controles

| Ação | Teclado / mouse | Controle PS4 reconhecido pelo Input System |
| --- | --- | --- |
| Mover | WASD / setas | Analógico esquerdo |
| Olhar e mirar | Mouse | Analógico direito |
| Atirar | Botão esquerdo | R2 |
| Recarregar | R | Triângulo |
| Esquivar | Alt esquerdo / Ctrl esquerdo / botão direito | Círculo |
| Saltar | Espaço | X |
| Usar portal | E | Quadrado |
| Pausar e retomar | Esc | Options |
| Iniciar / reiniciar após morte ou vitória | Enter | X |
| Alternar áudio | M / botão do menu | Botão do menu |

Os menus liberam o cursor. Após retomar, o gatilho precisa ser solto antes de voltar a disparar. O funcionamento no DualShock 4 físico e em seus drivers ainda precisa de validação no PC.

## Conteúdo desta versão

A sala de **28 × 24 unidades** recebe paredes elevadas, piso pixelado, pilares sólidos, ossuário, lápides, grades, velas, abóboras, neblina e luzes com oscilação suave. A câmera é perspectiva e o movimento usa `CharacterController` no espaço 3D. A mira permite olhar para cima e para baixo.

O revólver tem **oito balas por carregador**, reserva infinita e recarga manual. O disparo consulta o primeiro contato físico no mundo; clarão, rastro, recuo e cápsulas acompanham o acerto. A esquiva dura 0,22 s, com proteção nos primeiros 60% e recuperação vulnerável nos 40% finais. O salto é uma ação separada.

**Esqueletos** perseguem e atacam de perto. **Lanternas** mantêm distância e anunciam seus tiros. Os dois recebem flash, recuo e efeitos ao serem feridos. As ondas variam entre três, quatro e cinco criaturas, misturando os dois tipos. Barris possuem volume 3D, pavio, explosão com dano radial, empurrão e trauma, incluindo reação em cadeia.

Música de ambiente e efeitos são sintetizados pelo próprio jogo. Há sons para arma, recarga, passos, ataques, acertos, esquiva, explosão, vitória e morte. Pausa, foco da janela e o botão de som controlam a reprodução.

## Módulos

| Arquivo | Responsabilidade |
| --- | --- |
| `Fps/HauntFpsGame.cs` e `.UI.cs` | Inicialização, estado, ondas, dano, tiros, menus e conclusão |
| `Fps/HauntFpsPlayer.cs` | Entrada, câmera, movimento, salto, esquiva, arma e cápsulas |
| `Fps/HauntFpsWorld.cs` | Geometria, texturas, cobertura, decoração, luzes, neblina e portal |
| `Fps/HauntFpsEnemy.cs` | Esqueleto, Lanterna, preparação de ataques e feedback |
| `Fps/HauntFpsBarrel.cs` | Barris destrutíveis e reação em cadeia |
| `Fps/HauntFpsAudio.cs` | Clipes originais, vozes de áudio e música |
| `Fps/HauntFpsLit.shader` | Material pixelado com iluminação |
| `Core/FpsPrototypeRules.cs` | Regras de ondas, esquiva, alcance e dano radial independentes do motor |
| `Editor/HauntFpsProject.cs` | Menu, preparação do renderer e smoke check de importação |

Os caminhos são relativos a `Assets/_Game/Haunt`. `Resources/FpsPalette.mat` conserva a referência do shader. O Editor gera `Resources/FpsPipeline.asset` e `Resources/FpsRenderer.asset`, separados dos recursos da cena 2D. O FPS aplica seu pipeline durante a execução e restaura o anterior ao encerrar.

## Convivência com o roguelike 2D

O build contém `HauntFps.unity` primeiro e `Haunt.unity` em seguida, ambas habilitadas. **Ecos → Abrir roguelike** continua abrindo a versão 2D com Play parado. Ela conserva quatro personagens, armas, loja, chefes, salas procedurais e suas folhas de animação. O FPS apresenta sua própria arena, arma e dois inimigos; seleção de personagens, catálogo de armas, loja, chefes e multiplayer ainda dependem de expansão específica desse protótipo.

## Verificar no Editor

**Este ambiente não possui Editor Unity.** Os checks Python e C# independentes verificam fontes, regras e entrega; não executam a física, os shaders, o áudio ou a entrada do motor.

Com um Editor licenciado e `UNITY_EDITOR_PATH` definido:

```bash
bash Tools/unity-check.sh import
bash Tools/unity-check.sh fps-smoke
bash Tools/unity-check.sh smoke
bash Tools/unity-check.sh tests
bash Tools/unity-check.sh build
```

`fps-smoke` verifica cena, bootstrap, material e renderer importados; `smoke` também confere a cena 2D. Nenhum desses dois comandos entra em Play Mode. A importação, a compilação Unity, o resultado renderizado e o build continuam pendentes até serem executados no motor.

No Play, confira captura/liberação do cursor, colisão de chão/paredes/pilares, mira vertical, oito tiros e R, salto, esquiva, acertos em cobertura, três ondas, explosões, portal e reinício. Pause durante recarga e ataques; alterne foco e som; verifique liberação de recursos ao encerrar. Repita com controle físico. Depois abra a cena 2D e confira que sua apresentação permanece funcional. Veja também [VALIDATION.md](VALIDATION.md).
