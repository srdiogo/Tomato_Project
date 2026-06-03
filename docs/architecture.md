# Arquitetura técnica

## Visão geral

A arquitetura atual é dividida entre:

1. **Cliente Unity**: menu, UI, seleção de personagem, gameplay local, renderização e lógica de inventário/combate.
2. **Servidor dedicado Unity**: build headless/server que roda a cena de gameplay e é autoridade da sessão Netcode.
3. **Backend DevelopersHub RealtimeNetworking**: autenticação, matchmaking, dados persistidos e orquestração da partida.
4. **Server Manager**: aplicação C# que aceita conexões TCP/UDP, gerencia contas/dados/matchmaking e inicia/encerra servidores de jogo.

## Fluxo de alto nível

```text
Cliente Unity
  └─ MenuManager
      ├─ conecta/autentica no DevelopersHub RealtimeNetworking
      ├─ busca/seleciona personagens e equipamentos
      ├─ inicia matchmaking
      └─ recebe porta/dados do servidor de jogo

Server Manager
  ├─ aceita conexões TCP/UDP
  ├─ autentica e gerencia dados persistidos
  ├─ agrupa jogadores em partida
  └─ inicia processo/build dedicado Unity

Servidor Unity dedicado
  └─ SessionManager como Server
      ├─ inicia NetworkManager/UnityTransport
      ├─ inicializa itens do mapa
      ├─ informa backend que está pronto
      └─ spawna personagens quando clientes conectam

Cliente Unity em gameplay
  └─ SessionManager como Client
      ├─ inicia NetworkManager/UnityTransport
      ├─ solicita spawn do personagem
      └─ sincroniza ações via ServerRpc/ClientRpc
```

## Componentes centrais

### `MenuManager`

Arquivo: `Assets/Scripts/MenuManager.cs`

Responsável por:

- Detectar se a build está rodando como cliente ou servidor dedicado.
- Inicializar conexão com DevelopersHub no cliente.
- Gerenciar autenticação, reconexão, matchmaking e botões de UI.
- Carregar `Playground` quando a partida estiver pronta.
- No servidor dedicado, carregar a cena da partida a partir dos dados de runtime.

### `SessionManager`

Arquivo: `Assets/Scripts/SessionManager.cs`

Responsável por:

- Configurar `UnityTransport` com IP/porta.
- Iniciar servidor ou cliente Netcode.
- Controlar timeout/encerramento automático do servidor dedicado.
- Inicializar itens do mapa no servidor.
- Avisar o backend que o servidor está pronto.
- Spawnar personagens com ownership por cliente.
- Sincronizar trocas de inventário entre personagens.
- Sincronizar posição de itens no chão.

### `Character`

Arquivo: `Assets/Scripts/Character.cs`

Responsável por:

- Estado do personagem: vida, inventário, arma, munição, mira, movimento e animação.
- Inicialização local/remota do personagem.
- Controle de armas: equipar, guardar, recarregar e atirar.
- Combate e dano.
- Loot, pickup, drop e sincronização de inventário.
- RPCs de Netcode para replicar ações e estados.

### `Item`, `Weapon`, `Ammo`, `Consumable`, `Miscellaneous`

Arquivos:

- `Assets/Scripts/Item.cs`
- `Assets/Scripts/Weapon.cs`
- `Assets/Scripts/Ammo.cs`
- `Assets/Scripts/Consumable.cs`
- `Assets/Scripts/Miscellaneous.cs`

Responsáveis por:

- Modelo básico de item com `id`, `networkID`, quantidade e estado no chão.
- Armas com dano, cadência, munição, projétil, muzzle, flash e dados de rig.
- Munições/consumíveis/miscelâneos como especializações de item.

### `PrefabManager`

Arquivo: `Assets/Scripts/PrefabManager.cs`

Responsável por centralizar referências de prefabs usados por ID/tag, incluindo personagens e itens.

### UI e menu de personagens

Arquivos:

- `Assets/Scripts/CanvasManager.cs`
- `Assets/Scripts/InventoryItem.cs`
- `Assets/Scripts/CharactersManager.cs`
- `Assets/Scripts/MenuCharacterItem.cs`
- `Assets/Scripts/MenuWeaponItem.cs`

Responsáveis por inventário, loot, seleção de personagem e equipamentos.

### Movimento/câmera/rig

Arquivos:

- `Assets/Scripts/CameraManager.cs`
- `Assets/Scripts/RigManager.cs`
- `Assets/Scripts/Animator/*.cs`

Responsáveis por mira, alvo da câmera, rig de mãos/armas e eventos de animação.

## Modelo de autoridade atual

A sessão de gameplay é majoritariamente server-authoritative:

- O servidor inicia objetos e itens.
- Clientes solicitam ações via `ServerRpc`.
- O servidor replica resultado com `ClientRpc`.
- Personagens são spawnados com ownership do cliente correspondente.
- Vários estados usam IDs próprios (`networkID` em string) para correlacionar itens, independentemente do ID interno do Netcode.

## Pontos de atenção arquitetural

- `SessionManager.accountID` tem getter retornando `_port` em vez de `_accountID`; isso parece bug e deve ser revisado antes ou durante a migração.
- `Character.cs` concentra muitas responsabilidades: input/estado/animação/inventário/combate/networking. A migração para Mirror ficará mais segura se essa classe for fatiada ou se a camada de networking for isolada por adaptadores.
- `Item` depende diretamente de `NetworkManager.Singleton.IsServer`; isso deverá ser substituído por `NetworkServer.active`, `isServer` ou uma abstração própria no Mirror.
- A integração DevelopersHub usa nomenclatura e métodos específicos de Netcode (`NetcodeServerIsReady`, `NetcodeGetGameData`, `Data.Extension.NETCODE_SERVER`). Isso precisa de decisão explícita no plano de migração.
