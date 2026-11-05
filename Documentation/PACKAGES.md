# Pacotes e ferramentas

O projeto usa **Unity 6.0 LTS 6000.0.85f1**. As dependências diretas são declaradas em `Packages/manifest.json`.

| Dependência | Versão declarada | Uso |
| --- | --- | --- |
| Universal Render Pipeline | 17.0.4 | Renderers separados do FPS 3D e do roguelike 2D |
| Input System | 1.11.2 | Teclado, mouse e Gamepad |
| Test Framework | 1.4.5 | Test Runner e testes de integração |
| Módulos nativos de áudio, IMGUI e JSON | 1.0.0 | AudioListener, interface e estado dos testes |
| Módulo nativo Physics | 1.0.0 | CharacterController, colisores e consultas 3D do FPS |

Os manifests consultados dos três pacotes principais estão em `PackageManifests/`. Eles descrevem o que os pacotes publicam; a resolução final e as dependências transitivas precisam ser verificadas pelo Unity Package Manager. O lockfile transitivo deve ser gerado pelo UPM real, sem inventar versões ou checksums.

O roguelike 2D calcula movimento e colisão no próprio runtime. O protótipo FPS usa o módulo Physics do Unity para CharacterController, colisores, raycasts e consultas de volume. A câmera é controlada pelo código do jogo. O UPM pode resolver módulos adicionais exigidos pelos pacotes restantes.

Para validação fora do Editor, `Tools/setup-cloud.sh` prepara o SDK **.NET 8.0.427** e os pacotes Python fixados em `Tools/requirements-validation.txt`. O script executa apenas as verificações deste jogo. Ele não instala nem licencia o Unity. As instalações mantêm verificação TLS e não usam credenciais de aplicação.

Os testes devem usar as APIs disponíveis no NUnit personalizado fornecido pelo Test Framework da Unity. Os testes de corrotina do Editor são executados pelo Unity Test Runner; não dependem de atributos de paralelismo de outras distribuições do NUnit.
