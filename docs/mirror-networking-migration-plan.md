# Plano de migração para Mirror Networking

## Objetivo

Migrar a camada de gameplay multiplayer de **Unity Netcode for GameObjects** para **Mirror Networking**, preservando o máximo possível de gameplay existente: personagens, inventário, armas, loot, matchmaking e servidor dedicado.

## Escopo recomendado

### Dentro do escopo inicial

- Substituir `NetworkBehaviour`, `ServerRpc`, `ClientRpc`, `NetworkObject`, `NetworkTransform` e `NetworkManager.Singleton` equivalentes do Netcode por APIs Mirror.
- Adaptar spawn/despawn de personagens e objetos de gameplay.
- Migrar sincronização de ações de personagem, inventário, armas e itens.
- Configurar prefabs com `NetworkIdentity` e componentes Mirror.
- Manter build cliente/servidor dedicado.
- Manter DevelopersHub RealtimeNetworking temporariamente como backend de autenticação/matchmaking, se possível.

### Fora do escopo inicial

- Reescrever todo o backend `Server Manager/`.
- Trocar banco/persistência.
- Refatorar todo o gameplay para arquitetura nova.
- Rebalancear armas, movimentação, animações ou UI.

## Estratégia recomendada

Migrar em fases, criando primeiro uma camada de compatibilidade para reduzir risco.

A abordagem mais segura é:

1. **Preparar o projeto**: documentar prefabs, cenas, RPCs e fluxo atual.
2. **Introduzir Mirror em paralelo** em uma branch de migração.
3. **Criar adaptação de sessão** para Mirror antes de mexer em todo o gameplay.
4. **Migrar spawn/conexão** com personagem mínimo.
5. **Migrar ações de personagem** por grupos: movimento/mira, armas, inventário, itens.
6. **Remover Netcode** quando a paridade estiver validada.
7. **Atualizar Server Manager/DevelopersHub** para nomenclatura genérica ou Mirror.

## Mapeamento Netcode → Mirror

| Netcode for GameObjects | Mirror Networking | Observações |
| --- | --- | --- |
| `Unity.Netcode.NetworkBehaviour` | `Mirror.NetworkBehaviour` | Base dos scripts de rede. |
| `NetworkObject` | `NetworkIdentity` | Deve estar nos prefabs/objetos de rede. |
| `NetworkManager.Singleton` | `NetworkManager.singleton` ou referência própria | Mirror usa `NetworkManager` diferente. |
| `StartServer()` | `NetworkManager.StartServer()` | Também há `StartHost()` e `StartClient()`. |
| `StartClient()` | `NetworkManager.StartClient()` | Configurar endereço/porta no transport. |
| `[ServerRpc]` | `[Command]` | Chamado do cliente para servidor; por padrão exige authority. Usar `requiresAuthority = false` quando necessário. |
| `[ClientRpc]` | `[ClientRpc]` | Chamado do servidor para todos os clientes observadores. |
| `ClientRpcParams` alvo específico | `TargetRpc`/`NetworkConnectionToClient` | Para enviar a um cliente específico. |
| `SpawnWithOwnership(clientId)` | `NetworkServer.Spawn(obj, connection)` | Concede ownership ao cliente/conexão. |
| `OwnerClientId`/`clientId` ulong | `NetworkConnectionToClient.connectionId` ou conexão | Não assumir equivalência direta. Criar mapeamento. |
| `IsServer` | `isServer` ou `NetworkServer.active` | Em `NetworkBehaviour`, usar propriedades Mirror. |
| `IsOwner` | `isOwned`/`isLocalPlayer` | Depende da versão Mirror. |
| `NetworkTransform` | `Mirror.NetworkTransform*` | Escolher componente conforme autoridade e confiabilidade. |
| `NetworkVariable` | `[SyncVar]` | O projeto atual quase não usa NetworkVariable. |

## Fase 0 — Baseline e inventário técnico

**Objetivo:** ter uma fotografia confiável antes de alterar networking.

Tarefas:

- Criar branch específica, exemplo: `migration/mirror-networking`.
- Confirmar que o projeto abre no Unity `6000.0.23f1`.
- Rodar uma build/Play Mode cliente-servidor atual e registrar comportamento esperado.
- Listar todos os prefabs com `NetworkObject`/`NetworkTransform`.
- Listar todos os scripts com `Unity.Netcode`.
- Registrar cenas, build settings e fluxo de dedicated server.

Entregáveis:

- Checklist de prefabs de rede.
- Vídeo/log curto do fluxo atual funcionando.
- Lista de RPCs atuais por sistema.

Critério de saída:

- Equipe consegue reproduzir uma partida no Netcode antes de migrar.

## Fase 1 — Instalar e configurar Mirror

**Objetivo:** adicionar Mirror sem quebrar o projeto atual.

Tarefas:

- Adicionar Mirror pelo método escolhido do projeto: Unity Package Manager/Git URL/Asset Store.
- Criar cena ou prefab de teste isolado com `NetworkManager` Mirror.
- Escolher transport Mirror:
  - `KcpTransport` é a escolha comum para jogos realtime.
  - Avaliar transporte conforme target de deploy e servidor dedicado.
- Configurar porta e endereço equivalentes ao fluxo atual.
- Validar host/client local em uma cena mínima.

Entregáveis:

- Mirror instalado e compilando.
- Cena/prefab de teste Mirror funcional.

Critério de saída:

- Cliente conecta a servidor Mirror mínimo localmente.

## Fase 2 — Adapter de sessão

**Objetivo:** reduzir acoplamento direto a Netcode/Mirror.

Tarefas:

- Criar uma camada de sessão, por exemplo:
  - `INetworkSessionService`
  - `MirrorSessionManager`
  - opcionalmente `NetcodeSessionManager` temporário para comparação.
- Encapsular operações:
  - iniciar servidor;
  - iniciar cliente;
  - fechar servidor;
  - configurar porta/endereço;
  - evento de cliente conectado/desconectado;
  - spawn com ownership.
- Corrigir bug suspeito em `SessionManager.accountID`, cujo getter retorna `_port` em vez de `_accountID`.

Entregáveis:

- Código de sessão com menor dependência direta do transport/API.
- `SessionManager` preparado para versão Mirror.

Critério de saída:

- O fluxo atual ainda compila; a versão Mirror tem ponto claro de integração.

## Fase 3 — Migrar conexão e spawn de personagem

**Objetivo:** conectar cliente/servidor Mirror e spawnar personagem básico.

Tarefas:

- Substituir `NetworkObject` por `NetworkIdentity` nos prefabs de personagem.
- Registrar prefabs no `NetworkManager` Mirror.
- Migrar `SessionManager`:
  - `NetworkManager.Singleton.StartServer()` → Mirror start server.
  - `NetworkManager.Singleton.StartClient()` → Mirror start client.
  - callbacks de conexão → callbacks Mirror.
  - `SpawnCharacterServerRpc` → `[Command(requiresAuthority = false)]` ou mensagem customizada.
  - `SpawnWithOwnership` → `NetworkServer.Spawn(character, conn)`.
- Criar mapeamento `connectionId/accountID/Character` no servidor.
- Enviar dados iniciais ao cliente via `TargetRpc` quando necessário.

Entregáveis:

- Cliente entra na cena `Playground` e recebe personagem próprio.
- Outros clientes enxergam personagem spawnado.

Critério de saída:

- Dois clientes locais conectam ao servidor Mirror e veem personagens distintos.

## Fase 4 — Migrar movimentação, mira e transform

**Objetivo:** restaurar movimentação e estados visuais essenciais.

Tarefas:

- Substituir `ClientNetworkTransform` por componente Mirror equivalente ou solução customizada.
- Decidir autoridade de movimento:
  - cliente autoritativo com validação mínima, mais próximo do atual;
  - servidor autoritativo, mais robusto, porém maior refatoração.
- Migrar RPCs de mira/movimento:
  - `OnAimTargetChangedServerRpc` → `CmdSetAimTarget`.
  - `OnAimTargetChangedClientRpc` → `RpcSetAimTarget` ou `SyncVar` com hook.
  - repetir para `aiming`, `aimedMoveSpeed`, `moveSpeed`, `jump`.
- Evitar enviar RPC todo frame se possível; aplicar threshold/rate limit.

Entregáveis:

- Movimento remoto visível.
- Mira/animação remota com paridade aceitável.

Critério de saída:

- Dois clientes conseguem se mover e mirar, vendo o estado um do outro.

## Fase 5 — Migrar armas, tiro, dano e recarga

**Objetivo:** restaurar combate multiplayer.

Tarefas:

- Migrar equipar/guardar arma:
  - `EquipWeaponServerRpc/ClientRpc` → `CmdEquipWeapon/RpcEquipWeapon`.
  - `HolsterWeaponServerRpc/ClientRpc` → `CmdHolsterWeapon/RpcHolsterWeapon`.
- Migrar recarga:
  - `ReloadServerRpc/ClientRpc` → `CmdReload/RpcReload`.
- Migrar tiro:
  - `ShootServerRpc/ClientRpc` → `CmdShoot/RpcShoot`.
- Revisar `Projectile`:
  - Se projétil precisa existir em rede, adicionar `NetworkIdentity` e spawn server-side.
  - Se for apenas efeito local e hit server-side, separar efeito visual de validação de dano.
- Migrar dano:
  - `ApplyDamageClientRpc` → `RpcApplyDamage` ou `TargetRpc`/`SyncVar health` com hook.

Entregáveis:

- Equipar, recarregar, atirar e causar dano funcionam em multiplayer.

Critério de saída:

- Cliente A consegue acertar Cliente B e ambos veem vida/animações coerentes.

## Fase 6 — Migrar inventário, pickup, drop e loot

**Objetivo:** restaurar persistência de estado de itens em partida.

Tarefas:

- Migrar `PickupItemServerRpc/ClientRpc`.
- Migrar `DropItemsServerRpc/ClientRpc`.
- Migrar `TradeItemsBetweenCharactersServerRpc/ClientRpc`.
- Migrar `UpdateItemPositionClientRpc`.
- Decidir representação de itens no chão:
  - objetos Mirror spawnados com `NetworkIdentity`; ou
  - objetos locais replicados via mensagens/RPC usando `networkID` atual.
- Manter `networkID` string como ID lógico para não depender de `netId` Mirror.
- Validar limites de tamanho de JSON em RPCs Mirror; se necessário, trocar listas grandes por mensagens paginadas/structs serializáveis.

Entregáveis:

- Pickup/drop/loot/troca de itens funcionam para dois clientes.

Critério de saída:

- Estado de inventário permanece consistente após múltiplas operações simultâneas simples.

## Fase 7 — Integrar backend/Server Manager com Mirror

**Objetivo:** preservar matchmaking e dedicated server com nomenclatura genérica/Mirror.

Opção recomendada inicialmente: **manter DevelopersHub como backend**, alterando somente o tipo de servidor de gameplay.

Tarefas:

- Revisar `Data.Extension.NETCODE_SERVER` e criar extensão equivalente, por exemplo `MIRROR_SERVER` ou `GAME_SERVER`.
- Adaptar chamadas do cliente:
  - `OnNetcodeServerReady` → evento genérico ou Mirror-specific.
  - `NetcodeGetGameData` → `GetGameData` genérico.
  - `NetcodeServerIsReady` → `GameServerIsReady`/`MirrorServerIsReady`.
  - `NetcodeCloseServer` → `GameServerClose`/`MirrorCloseServer`.
- Atualizar `Server Manager/Extensions/Netcode.cs` ou criar `Server Manager/Extensions/Mirror.cs`.
- Garantir que a porta escolhida corresponde ao transport Mirror configurado.
- Validar argumentos de linha de comando para build dedicado.

Entregáveis:

- Matchmaking inicia servidor Mirror dedicado.
- Cliente recebe IP/porta e conecta à sessão Mirror.

Critério de saída:

- Fluxo completo menu → matchmaking → servidor dedicado → gameplay funciona com Mirror.

## Fase 8 — Remover Netcode e limpar projeto

**Objetivo:** concluir migração sem dependências antigas desnecessárias.

Tarefas:

- Remover `using Unity.Netcode` e APIs Netcode restantes.
- Remover `Assets/Scripts/Netcode/ClientNetworkTransform.cs` se não for mais usado.
- Remover componentes `NetworkObject`/Netcode dos prefabs.
- Remover `Assets/DefaultNetworkPrefabs.asset` se não houver uso.
- Remover `com.unity.netcode.gameobjects` do `Packages/manifest.json` quando nenhum código/prefab depender dele.
- Atualizar documentação e nomes de APIs internas para não usar “Netcode” quando significar servidor de gameplay genérico.

Entregáveis:

- Projeto compila sem Netcode for GameObjects.
- Documentação atualizada.

Critério de saída:

- Busca por `Unity.Netcode`, `ServerRpc`, `NetworkObject` e `Netcode` não retorna dependências ativas, exceto histórico/documentação/migração.

## Arquivos com maior impacto

Prioridade alta:

- `Assets/Scripts/SessionManager.cs`
- `Assets/Scripts/Character.cs`
- `Assets/Scripts/Item.cs`
- `Assets/Scripts/Projectile.cs`
- `Assets/Scripts/Netcode/ClientNetworkTransform.cs`
- `Assets/Scripts/MenuManager.cs`
- `Server Manager/Extensions/Netcode.cs`
- `Server Manager/Scripts/Manager.cs`

Prefabs/cenas:

- `Assets/Prefabs/Characters/*.prefab`
- `Assets/Prefabs/Weapons/*.prefab`
- `Assets/Prefabs/Ammo/*.prefab`
- `Assets/Prefabs/Projectiles/*.prefab`
- `Assets/Prefabs/Managers.prefab`
- `Assets/Scenes/Menu.unity`
- `Assets/Scenes/Playground.unity`

Configuração:

- `Packages/manifest.json`
- `ProjectSettings/EditorBuildSettings.asset`
- `Assets/DefaultNetworkPrefabs.asset`

## Plano de testes por fase

### Testes manuais mínimos

- Abrir projeto sem erros de compilação.
- Rodar servidor e cliente local.
- Conectar dois clientes.
- Selecionar personagem/equipamento.
- Spawnar personagens.
- Movimento/mira visíveis entre clientes.
- Equipar arma.
- Recarregar.
- Atirar.
- Aplicar dano.
- Pegar item do chão.
- Dropar item.
- Loot/troca entre personagens.
- Desconectar cliente e verificar cleanup.
- Encerrar servidor sem clientes.

### Testes automatizáveis recomendados

- Testes EditMode para serialização/deserialização de `Character.Data` e `Item.Data`.
- Testes de lógica de inventário: merge, split, pickup, drop e trade.
- Testes de `PrefabManager`: IDs/tags resolvem prefabs esperados.
- Testes de configuração: todos os prefabs de rede Mirror têm `NetworkIdentity`.

## Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| APIs Mirror diferem semanticamente de Netcode RPCs | Alto | Migrar por grupos e validar a cada fase. |
| `Character.cs` é muito grande e acoplado | Alto | Criar wrappers/adapters antes de grandes mudanças. |
| DevelopersHub usa nomes e fluxo Netcode | Médio/Alto | Manter backend inicialmente e renomear para API genérica em fase própria. |
| Transform/client authority pode mudar feel do jogo | Médio | Começar com comportamento equivalente e só depois endurecer autoridade. |
| JSON em RPCs pode ser pesado | Médio | Medir payloads; se necessário, trocar por mensagens/structs Mirror. |
| Prefabs de rede podem ficar inconsistentes | Alto | Criar checklist de prefab e validação automatizada no editor. |
| Dedicated server pode usar porta/transport incorreto | Alto | Testar headless cedo com KCP/transport escolhido. |

## Ordem recomendada de commits

1. Documentação e checklist da migração.
2. Instalação Mirror e cena mínima de teste.
3. Adapter de sessão e correção de `accountID`.
4. Spawn básico Mirror.
5. Movimento/mira/transform.
6. Armas/combate.
7. Inventário/itens.
8. Backend/Server Manager Mirror ou API genérica.
9. Remoção Netcode e limpeza final.

## Critério final de conclusão

A migração pode ser considerada concluída quando:

- O projeto compila sem Netcode for GameObjects.
- Mirror conecta cliente/servidor local e dedicado.
- Matchmaking mantém o fluxo menu → partida.
- Dois clientes conseguem jogar uma sessão com spawn, movimento, armas, dano, inventário, pickup/drop e desconexão.
- Prefabs de rede estão registrados no Mirror.
- Documentação e procedimentos de build foram atualizados.
