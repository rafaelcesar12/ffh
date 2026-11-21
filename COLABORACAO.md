# Colaboração Claude + Codex neste repositório

Repositório local do servidor Jondo de testes do Rafael. Base: `Keka-Bron/JondoEmu`, commit
`412f66ad` (v1.0.9). O remoto `upstream` aponta para o projeto original; `origin` é o
repositório privado https://github.com/rafaelcesar12/jondo-local (só o Rafael tem acesso).
Comece cada tarefa com `git pull` e envie a branch com `git push -u origin <branch>`.

As regras gerais (não tocar na instalação oficial da Ankama, sem mods, preservar contas,
confirmar antes de reiniciar, sem senhas) estão no manual
`outputs/Manual-projeto-Jondo-para-Claude.md` da pasta do projeto e continuam valendo.

## Regras de trabalho

1. **Uma branch por tarefa**, com prefixo do assistente: `claude/<tarefa>` ou `codex/<tarefa>`.
   A `main` só recebe trabalho revisado.
2. **Antes de começar**, anote a tarefa na tabela abaixo (quem, branch, arquivos). Não edite
   arquivos listados numa tarefa "em andamento" do outro assistente.
3. **Commits pequenos**, mensagem em português dizendo o quê e por quê. O autor do commit
   identifica quem fez: `Claude <claude@jondo.local>` ou `Codex <codex@jondo.local>`.
4. **Revisão cruzada**: quem não implementou revisa o diff (`git diff main...<branch>`) antes do merge.
5. **Instalação** (`work/author-native-v1.0.9/JondoEmu`) não é versionada. Mudanças só chegam
   lá depois do merge, com confirmação do Rafael e registro do que foi copiado.
   Se alguém editar a instalação direto (como aconteceu com a célula 408), traga a mesma mudança
   para cá num commit logo em seguida.
6. **Nunca versionar**: `bases/`, `config/`, `logs/`, `.env`, binários e dados do cliente.
   O `.gitignore` original já cobre isso; não force (`git add -f`) nesses caminhos.

## Registro de tarefas

| Data | Quem | Branch | Tarefa | Arquivos | Estado |
|---|---|---|---|---|---|
| 2026-11-16 | Codex | main | Hub de testes: runas/poções a 1 kama e guia de calabouços | ver commit f882b659 | concluída, instalada |
| 2026-11-17 | Codex | — | Vendedor de runas 269 → 408 (direto na instalação) | content/npcs/spawns.json | instalada |
| 2026-11-17 | Claude | main | Reconciliar célula 408 no código; criar este repositório | content/npcs/spawns.json, COLABORACAO.md | concluída |
| 2026-11-17 | Claude | claude/hub-revenda | Revenda dos itens do hub limitada a 1 kama (36 poções davam lucro) | Managers/ShopHub.cs, Handlers/NpcHandler.cs, ShopHubTests.cs | concluída e instalada (revisão do Codex dispensada pelo Rafael) |
| 2026-11-17 | Claude | claude/ffh-e-forjamagia | Nome FFH (janela, bots, textos, servidor 290) e slot de runas com várias unidades | UI/ServerWindow.cs, UI/ServerTheme.cs, Bots/SimulatedPlayers.cs, Handlers/WorkshopHandler.cs, Handlers/CommissionHandler.cs, Managers/DatabaseManager.cs e outros | instalada (exe sha256 D3B2C930…); backup em outputs/Backup-antes-FFH-2026-11-17; falta teste no jogo |
| 2026-11-18 | Claude | claude/loja-recursos | Loja de recursos: 11 vendedores no mapa [-1,1] (88212758), 3.534 recursos a 1 kama, revenda travada em 1 kama | tools/gerar_loja_recursos.py, content/npcs/{shops,spawns,shop_hub}.json, Managers/ShopHub.cs, ShopHubTests.cs | instalada 2026-11-18 (exe sha256 1FD3B603…); backup em outputs/Backup-antes-loja-recursos-2026-11-18 |
| 2026-11-21 | Claude | claude/upstream-v1.0.15 | Junção dos 188 commits do JondoEmu original (até v1.0.15+); bots renomeados para Ffhbot-Aa (a regra de nomes recusa dígitos) | 586 arquivos; conflitos em NpcHandler, ConnectionProtocol, WorkshopHandler, SimulatedPlayers, ServerSettingsTexts, shops.json, spawns.json | instalada (exe sha256 E2E5D9DE…, + 21 arquivos de content/ e datos/); testada antes numa cópia da base; backup em outputs/Backup-antes-upstream-2026-11-20; falta teste no jogo |
| 2026-11-21 | Claude | claude/teto-pa-pm-alcance | Teto do equipamento: 12 PA, 6 PM e 6 de alcance (novo MaxRange); buffs de feitiço em luta somam por cima. O teto de alcance fica só em config/server_settings.json (o campo não cabia na janela em 1280x720) | ServerSettings.cs, Characters/CombatCharacteristics.cs, Network/CharacteristicsMessages.cs, Handlers/FightHandler.cs, CharacteristicParityTests.cs | instalada (exe sha256 A879F0DF…; config MaxActionPoints 12, MaxMovementPoints 6, MaxRange 6); backup em outputs/Backup-antes-teto-2026-11-21; falta teste no jogo |
