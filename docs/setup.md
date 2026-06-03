# Setup e execução

## Versão do Unity

O projeto foi inspecionado com as seguintes configurações:

- Editor: `Unity 6000.0.23f1` (`ProjectSettings/ProjectVersion.txt`).
- Render pipeline: URP `17.0.3` (`Packages/manifest.json`).

Abra o projeto pela raiz do repositório no Unity Hub.

## Pacotes principais

Declarados em `Packages/manifest.json`:

- `com.unity.netcode.gameobjects` `2.3.2` — networking atual de gameplay.
- `com.unity.transport` aparece como dependência transitiva via Netcode/UTP; o script usa `UnityTransport`.
- `com.unity.inputsystem` `1.11.2`.
- `com.unity.render-pipelines.universal` `17.0.3`.
- `com.unity.animation.rigging` `1.3.0`.
- `com.unity.cinemachine` `2.10.1`.
- `com.unity.ai.navigation` `2.0.4`.
- `com.unity.ugui` `2.0.0`.
- `com.unity.test-framework` `1.4.5`.

Além dos pacotes Unity, o projeto contém código/plugin em `Assets/Packages/DevelopersHub/RealtimeNetworking/` e `Assets/Packages/LitJson/`.

## Cenas configuradas no build

`ProjectSettings/EditorBuildSettings.asset` declara:

1. `Assets/Scenes/Menu.unity`
2. `Assets/Scenes/Playground.unity`

O fluxo atual usa o menu para autenticação/matchmaking e carrega `Playground` quando a partida está pronta.

## Execução cliente

Fluxo esperado em build normal de cliente:

1. `MenuManager` detecta plataforma não-servidor.
2. Define `SessionManager.role = Client`.
3. Conecta ao servidor DevelopersHub via `RealtimeNetworking.Connect()`.
4. Autentica com `RealtimeNetworking.Authenticate()`.
5. Libera seleção de personagem e matchmaking.
6. Ao receber servidor de partida pronto, desconecta do backend, define `SessionManager.port` e carrega a cena `Playground`.
7. `SessionManager` inicia o cliente Netcode com `NetworkManager.Singleton.StartClient()`.

Arquivos relevantes:

- `Assets/Scripts/MenuManager.cs`
- `Assets/Scripts/SessionManager.cs`
- `Assets/Packages/DevelopersHub/RealtimeNetworking/Scripts/Client.cs`

## Execução servidor dedicado

`MenuManager` detecta plataformas server:

- `RuntimePlatform.WindowsServer`
- `RuntimePlatform.LinuxServer`
- `RuntimePlatform.OSXServer`

Nessa condição:

1. Define `SessionManager.role = Server`.
2. Lê dados da partida com `RealtimeNetworking.NetcodeGetGameData()`.
3. Carrega `Playground` para `mapID == 0`.
4. Em `SessionManager.Start()`, escolhe uma porta livre e chama `StartServer()`.
5. Inicializa itens do mapa e informa ao backend que o servidor Netcode está pronto.

Arquivos relevantes:

- `Assets/Scripts/MenuManager.cs`
- `Assets/Scripts/SessionManager.cs`
- `Server Manager/Extensions/Netcode.cs`
- `Server Manager/Scripts/Manager.cs`

## Observações de build

- O projeto depende de prefabs com componentes de `NetworkObject`/Netcode e de um `NetworkManager` configurado em cena/prefab.
- `Assets/DefaultNetworkPrefabs.asset` registra prefabs de rede usados pelo Netcode atual.
- A camada de servidor externo em `Server Manager/` precisa ser tratada como aplicação C# separada.
