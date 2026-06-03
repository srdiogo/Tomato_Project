# Tomato Project — documentação do projeto

Este diretório documenta o projeto Unity **Tomato Project**, incluindo estrutura, arquitetura, sistemas de gameplay, networking atual e planejamento de migração para **Mirror Networking**.

## Índice

- [Setup e execução](setup.md)
- [Estrutura do projeto](project-structure.md)
- [Arquitetura técnica](architecture.md)
- [Sistemas de gameplay](gameplay-systems.md)
- [Networking atual](networking-current.md)
- [Plano de migração para Mirror Networking](mirror-networking-migration-plan.md)

## Resumo executivo

O projeto é um jogo Unity com cenas de menu e gameplay, personagens 3D, armas, munição, inventário, loot, matchmaking e servidor dedicado. A implementação atual combina:

- **Unity 6000.0.23f1**.
- **Universal Render Pipeline 17.0.3**.
- **Unity Input System** e **Starter Assets Third Person Controller**.
- **Unity Netcode for GameObjects 2.3.2** com **Unity Transport** para a sessão de gameplay.
- **DevelopersHub RealtimeNetworking** para autenticação, matchmaking, dados de conta/personagem/equipamento e ciclo de servidor Netcode.
- Um servidor C# separado em `Server Manager/` para serviços de backend, matchmaking e orquestração de partidas.

O plano de migração para Mirror deve separar claramente duas camadas:

1. **Camada de sessão/gameplay**: migrar de Netcode for GameObjects para Mirror.
2. **Camada de backend/matchmaking**: decidir se DevelopersHub RealtimeNetworking continua apenas como backend/orquestrador ou se também será substituído.

A recomendação inicial é migrar em fases, mantendo DevelopersHub como backend enquanto a camada de gameplay muda para Mirror.
