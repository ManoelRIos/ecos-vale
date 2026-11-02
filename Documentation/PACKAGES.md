# Dependências verificadas

As versões diretas estão fixadas em `Packages/manifest.json`. Evidências dos manifests consultados estão em `PackageManifests/`.

| Pacote | Versão | Evidência |
| --- | --- | --- |
| URP | 17.0.4 | `Unity-Technologies/Graphics`, branch `6000.0/staging`, `Packages/com.unity.render-pipelines.universal/package.json`, Unity mínima 6000.0; uso também confirmado em `arfoundation-samples/6.0` (Editor 6000.0.70f1) |
| Input System | 1.11.2 | `Unity-Technologies/InputSystem`, tag `1.11.2`, manifest oficial |
| Cinemachine | 3.1.0 | espelho público `needle-mirror/com.unity.cinemachine`, tag `3.1.0`, Unity mínima 2022.3 |
| Netcode for GameObjects | 2.0.0 | `Unity-Technologies/com.unity.netcode.gameobjects`, tag `ngo/2.0.0`, Unity mínima 6000.0; requer Transport 2.3.0 |
| Unity Transport | 2.3.0 | espelho público `needle-mirror/com.unity.transport`, tag `2.3.0`, Unity mínima 2022.3 |
| Test Framework | 1.4.5 | espelho público `needle-mirror/com.unity.test-framework`, tag `1.4.5` |

As fontes oficiais estão em `https://raw.githubusercontent.com/` com os caminhos acima; os espelhos preservam manifests publicados, mas não substituem a resolução pelo registro da Unity. As versões existem nos manifests consultados e atendem às versões mínimas declaradas. **A resolução conjunta e compilação ainda precisam do Unity Package Manager/Editor.**

URP depende de Core/Shader Graph 17.0.4 e Universal Config 17.0.3. Cinemachine depende de Splines 2.0.0. Transport depende de Collections 2.2.1, Burst 1.8.12 e Mathematics 1.3.1. O UPM deverá gerar o lockfile transitivo real.

Os GUIDs de Lit e ParticlesUnlit foram conferidos nos respectivos `.shader.meta` da branch oficial Graphics `6000.0/staging`; `Tools/external_guids.json` é a lista explícita de referências externas permitidas na checagem estática. Os GUIDs locais são únicos e acompanhados de `.meta`.

O patch de referência do projeto foi atualizado para 6000.0.85f1 por causa da CVE-2025-59489. Evidências e fontes estão em [UnityCompatibility.json](UnityCompatibility.json); a abertura ainda depende de validação real no Editor.

Para NGO 2.0.0, o projeto habilita ambos os módulos nativos `com.unity.modules.physics` e `com.unity.modules.physics2d`, versão 1.0.0. O log do usuário confirmou `CS1069` quando Physics 2D estava ausente; `NetworkRigidBodyBase` referencia tipos dos dois módulos mesmo em um jogo 3D. A fonte correspondente é `Runtime/Components/NetworkRigidBodyBase.cs` na tag oficial `ngo/2.0.0`.
