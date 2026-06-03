# Estrutura do projeto

## Raiz

- `Assets/` — projeto Unity: cenas, scripts, prefabs, materiais, modelos, animações e plugins locais.
- `Packages/` — manifesto e lockfile de pacotes Unity.
- `ProjectSettings/` — configurações do projeto Unity.
- `Server Manager/` — aplicação C# de servidor/backend da solução DevelopersHub RealtimeNetworking.
- `docs/` — documentação criada para o projeto.

## Assets principais

### Cenas

- `Assets/Scenes/Menu.unity` — menu inicial, conexão/autenticação/matchmaking e seleção de personagem.
- `Assets/Scenes/Playground.unity` — cena de gameplay/partida.

### Scripts

- `Assets/Scripts/` — gameplay, UI, inventário, armas, personagens e sessão multiplayer.
- `Assets/Scripts/Animator/` — callbacks de state machine para equipar, guardar e recarregar armas.
- `Assets/Scripts/Editor/` — ferramentas de editor, incluindo ajuste de armas.
- `Assets/Scripts/Netcode/` — extensões específicas do Netcode atual.

### Prefabs

- `Assets/Prefabs/Characters/` — personagens jogáveis/dummies: `Bot`, `Moletom`, `Thomas`, `Tomatina`, `teuprimo`.
- `Assets/Prefabs/Weapons/` — armas: `AKM`, `AWP`, `TomatoGun`.
- `Assets/Prefabs/Ammo/` — munição: `Ammo_Tomato`.
- `Assets/Prefabs/Projectiles/` — projéteis: `Municao_Tomate`, `Projectile`.
- `Assets/Prefabs/Consumables/` — consumíveis: `Health Potion`.
- `Assets/Prefabs/UI/` — itens de UI: `CharacterItem`, `InventoryItem`, `WeaponItem`.
- `Assets/Prefabs/Managers.prefab` e `Assets/Prefabs/PrefabManager.prefab` — objetos de gerenciamento.
- `Assets/Prefabs/Network/Characters.asset` e `Assets/DefaultNetworkPrefabs.asset` — assets ligados a prefabs de rede.

### Assets de gameplay e visual

- `Assets/Clips/` — animações FBX para movimento, mira, recarga, corrida e manipulação de rifle.
- `Assets/Animation/UpperBody.mask` — máscara de animação para camadas do corpo superior.
- `Assets/Resources/` — modelos, materiais e assets carregáveis por recurso.
- `Assets/Settings/` — URP, renderers, input actions e perfis de volume.
- `Assets/StarterAssets/` — pacote Third Person Controller e documentação.

## Server Manager

`Server Manager/` contém uma aplicação de servidor C# em namespace `DevelopersHub.RealtimeNetworking.Server`.

Arquivos principais:

- `Program.cs` — entrada da aplicação.
- `Terminal.cs` — comandos/logs administrativos.
- `Scripts/Server.cs` — servidor TCP/UDP.
- `Scripts/Client.cs` — representação de cliente conectado.
- `Scripts/Manager.cs` — regras de alto nível, matchmaking e fluxo de jogo.
- `Extensions/Netcode.cs` — extensão que cria/monitora servidores Netcode dedicados.
- `Scripts/Database.cs`, `Sqlite.cs`, `Data.cs` — persistência e modelos de dados.
- `Scripts/Sender.cs`, `Receiver.cs`, `Packet.cs` — protocolo de comunicação.

## Configurações Unity relevantes

- `ProjectSettings/EditorBuildSettings.asset` — cenas do build.
- `ProjectSettings/TagManager.asset` — inclui camada/tag relacionada a `NetworkPlayer`.
- `ProjectSettings/ProjectVersion.txt` — versão do editor.
- `Packages/manifest.json` — dependências.
