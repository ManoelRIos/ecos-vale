# Editor, segurança e erros de abertura

## Alerta confirmado e correção

A primeira entrega fixava **6000.0.40f1**, que está na faixa afetada pela **CVE-2025-59489**. O registro CVE informa que, na linha 6.0 LTS, a correção começa em **6000.0.58f2**. O alerta reportado é consistente com essa versão antiga; não deve ser ignorado.

O projeto agora fixa **6000.0.85f1**, cuja existência foi conferida na tag oficial `Unity-Technologies/UnityCsReference/6000.0.85f1`. Isso trata essa vulnerabilidade conhecida; não é uma certificação de ausência de outras falhas. Alterar ProjectVersion.txt não atualiza o Editor instalado e não corrige executáveis já produzidos: esses exigem rebuild com um Editor corrigido.

Fontes verificadas:

- [Registro CVE](https://github.com/CVEProject/cvelistV5/blob/main/cves/2025/59xxx/CVE-2025-59489.json)
- [Referência oficial do Editor 6000.0.85f1](https://github.com/Unity-Technologies/UnityCsReference/blob/6000.0.85f1/README.md)
- [Aviso do fornecedor](https://unity.com/security/sept-2025-01), referenciado pelo registro CVE. O acesso direto a esse domínio foi bloqueado pela política de rede deste ambiente; não foi usado para simular uma consulta bem-sucedida.

## Como abrir a versão corrigida

1. Feche o Unity e baixe novamente o ZIP da branch `codex/ecos-do-vale-vertical-slice`.
2. Extraia em uma **pasta nova**, preservando sua cópia anterior caso tenha feito alterações. Isso evita reutilizar caches de uma importação feita com outra versão.
3. No Unity Hub, instale **6000.0.85f1**, da linha **Unity 6.0 LTS**. Se necessário, use o [arquivo oficial de releases](https://unity.com/releases/editor/archive). Instale Windows Build Support para gerar o executável.
4. Em Projects, adicione a pasta que contém `Assets`, `Packages` e `ProjectSettings`; selecione explicitamente o Editor 6000.0.85f1. Não selecione a pasta externa que apenas contém o projeto extraído.
5. Aguarde a resolução dos pacotes/importação. Em seguida, use **Ecos → Abrir cena principal**, abra `Valley.unity` e entre em Play Mode.

Não é necessário apagar `Assets`, alterar DLLs, desabilitar validação TLS ou forçar versões aleatórias dos pacotes.

## O que foi corrigido adicionalmente

A inicialização URP usava criação genérica de ScriptableObject e alterava a lista interna do renderer. Ela agora usa a fábrica pública `UniversalRenderPipelineAsset.Create(renderer)`, que também inicializa recursos do pacote. A configuração automática foi reforçada para reagendar durante compilação/importação, reparar assets faltantes/configuração parcial e criar os dois prefabs independentemente.

O URP permanece em **17.0.4**. Além do manifest-fonte, foi conferido um projeto oficial concreto: `Unity-Technologies/arfoundation-samples`, branch 6.0, usa URP 17.0.4 com Editor 6000.0.70f1. Essa é evidência adicional de uso na linha 6.0, mas não substitui a resolução UPM e os testes deste projeto. As referências estão em `UnityCompatibility.json`.

O script `Tools/unity-check.sh` verifica o Editor selecionado antes de importar/testar/buildar. A validação estática também rejeita um pin na faixa afetada da CVE ou fora da linha suportada.

## Erros de compilação ainda exigem o Console

O aviso de segurança **não demonstra a causa de cada erro de importação/compilação**. O usuário confirmou Editor 6000.0.40f1 e a janela genérica de Safe Mode. Neste ambiente não há Editor instalado e ainda não foram fornecidas as primeiras mensagens vermelhas do Console. Portanto, não foi reproduzido nem confirmado o erro exato que bloqueou sua abertura.

Se continuar falhando, os dados úteis são a versão exibida no Hub e a primeira mensagem completa do Console, incluindo código CSxxxx/nome do pacote e caminho do arquivo. No Windows, o log do Editor fica em `%LOCALAPPDATA%\Unity\Editor\Editor.log`. Mensagens posteriores podem ser apenas consequências do primeiro erro.

Se o erro for de pacote não encontrado ou download bloqueado, confira a conexão/autenticação do Package Manager; no ambiente cloud os domínios `packages.unity.com` e `download.packages.unity.com` estão salvos no rascunho, mas ainda dependem de aplicação das configurações. Isso é diferente da conectividade da sua máquina local.

## Limite da verificação

Foram executados testes de política de versão, análise sintática dos scripts e testes do núcleo C#. Os testes EditMode, a importação de assets, shaders, Play Mode e o build continuam sem execução aqui. Consulte `VALIDATION.md`; não considere o projeto compilado somente porque os checks estáticos passaram.
