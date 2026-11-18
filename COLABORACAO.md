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
| 2026-11-17 | Claude | claude/hub-revenda | Revenda dos itens do hub limitada a 1 kama (36 poções davam lucro) | Managers/ShopHub.cs, Handlers/NpcHandler.cs, ShopHubTests.cs | aguardando revisão do Codex; não instalada |
