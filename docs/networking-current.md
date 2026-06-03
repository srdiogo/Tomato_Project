# Networking atual

## Tecnologias usadas

- **Unity Netcode for GameObjects** (`com.unity.netcode.gameobjects` `2.3.2`).
- **Unity Transport** (`Unity.Netcode.Transports.UTP.UnityTransport`).
- **DevelopersHub RealtimeNetworking** para backend, matchmaking e dados de runtime.
- **Server Manager** como aplicação C# externa para autenticação, matchmaking, persistência e orquestração de servidores dedicados.

## Papéis de runtime

`SessionManager.Role` define:

- `Server`: build dedicado/headless que roda a partida.
- `Client`: jogador normal.

`MenuManager` decide o papel conforme a plataforma:

- Windows/Linux/OSX Server => servidor dedicado.
- Demais plataformas => cliente.

## Fluxo de matchmaking e conexão

1. Cliente abre `Menu.unity`.
2. `MenuManager.ClientAwake()` conecta ao DevelopersHub.
3. Cliente autentica.
4. Cliente inicia matchmaking com `RealtimeNetworking.StartMatchmaking(0, 0, Data.Extension.NETCODE_SERVER)`.
5. Server Manager forma a partida e inicia um servidor Unity dedicado.
6. Servidor dedicado carrega `Playground` e inicia Netcode como server.
7. Servidor dedicado informa `RealtimeNetworking.NetcodeServerIsReady(_port)`.
8. Cliente recebe `OnNetcodeServerReady(int port, Data.RuntimeGame gameData)`.
9. Cliente desconecta do backend, define `SessionManager.port` e carrega `Playground`.
10. Em `Playground`, `SessionManager.StartClient()` conecta ao servidor Netcode.

## Fluxo de spawn de personagem

Arquivo: `Assets/Scripts/SessionManager.cs`.

1. Servidor registra `NetworkManager.Singleton.OnClientConnectedCallback`.
2. Ao conectar, envia `OnClientConnectedClientRpc` apenas para o cliente conectado.
3. Cliente chama `SpawnCharacterServerRpc(_accountID)`.
4. Servidor consulta `RealtimeNetworking.NetcodeGetGameData()`.
5. Servidor encontra o player pelo `accountID`.
6. Servidor escolhe prefab via `PrefabManager.singleton.GetCharacterPrefab(tag)`.
7. Instancia personagem e chama `NetworkObject.SpawnWithOwnership(senderClientId)`.
8. Inicializa inventário/equipamentos/itens no chão e replica via `InitializeClientRpc`.

## RPCs e sincronizações principais

### Em `Character.cs`

- Mira e alvo:
  - `OnAimTargetChangedServerRpc` / `OnAimTargetChangedClientRpc`
  - `OnAimingChangedServerRpc` / `OnAimingChangedClientRpc`
  - `OnAimingMoveChangedServerRpc` / `OnAimingMoveChangedClientRpc`
  - `OnMoveSpeedChangedServerRpc` / `OnMoveSpeedChangedClientRpc`

- Armas:
  - `EquipWeaponServerRpc` / `EquipWeaponClientRpc`
  - `HolsterWeaponServerRpc` / `HolsterWeaponClientRpc`
  - `ReloadServerRpc` / `ReloadClientRpc`
  - `ShootServerRpc` / `ShootClientRpc`

- Movimento/estado:
  - `JumpServerRpc` / `JumpClientRpc`

- Combate:
  - `ApplyDamageClientRpc`

- Inventário/loot:
  - `PickupItemServerRpc` / `PickupItemClientRpc`
  - `DropItemsServerRpc` / `DropItemsClientRpc`

### Em `SessionManager.cs`

- Conexão/spawn:
  - `OnClientConnectedClientRpc`
  - `SpawnCharacterServerRpc`

- Troca de inventário:
  - `TradeItemsBetweenCharactersServerRpc`
  - `TradeItemsBetweenCharactersClientRpc`

- Itens no chão:
  - `UpdateItemPositionClientRpc`

## Transform sync

`Assets/Scripts/Netcode/ClientNetworkTransform.cs` herda de `Unity.Netcode.Components.NetworkTransform`.

Ele provavelmente existe para permitir transform com autoridade do cliente. Na migração, deve ser substituído por `NetworkTransform`, `NetworkTransformReliable`/equivalente Mirror ou componente customizado de movimento autoritativo.

## Prefabs de rede

- `Assets/DefaultNetworkPrefabs.asset` contém lista de prefabs registrados no Netcode.
- Personagens, itens e projéteis precisam ser revisados no Inspector para mapear componentes `NetworkObject`, `NetworkTransform` e scripts dependentes de Netcode.

## Dependências acopladas ao Netcode

Arquivos diretamente dependentes:

- `Assets/Scripts/Character.cs`
- `Assets/Scripts/SessionManager.cs`
- `Assets/Scripts/Item.cs`
- `Assets/Scripts/Projectile.cs`
- `Assets/Scripts/Netcode/ClientNetworkTransform.cs`
- `Assets/Scripts/MenuManager.cs` indiretamente pelo fluxo `NetcodeServerReady`/`NETCODE_SERVER`.
- `Server Manager/Extensions/Netcode.cs` pela orquestração de servidores Netcode.

## Riscos atuais para migração

- RPCs são numerosos e espalhados principalmente em `Character.cs`.
- Dados são serializados em JSON nos RPCs; Mirror aceita strings, mas a validação e o tamanho de mensagens devem ser revisados.
- O backend DevelopersHub está semanticamente acoplado a “Netcode” em nomes de métodos e extensão.
- O servidor dedicado escolhe porta via `FindFreeTcpPort`, mas transports Mirror podem exigir configuração diferente.
- IDs de item usam GUID/string próprios; isso ajuda a migração, mas precisa coexistir com `netId`/identity do Mirror.
