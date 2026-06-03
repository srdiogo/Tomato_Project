# Sistemas de gameplay

## Personagens

Implementação principal: `Assets/Scripts/Character.cs`.

Cada personagem possui:

- Identificador de tipo (`id`).
- Vida (`health`).
- Inventário de `Item`.
- Arma equipada e munição equipada.
- Estado de mira, sprint, caminhada, grounded/free fall e velocidade.
- Rig de mãos/armas via `RigManager`.
- Ragdoll básico controlado por rigidbodies/colliders filhos.
- Identidade de cliente (`clientID`) recebida do networking.

## Inventário e loot

Arquivos principais:

- `Character.cs`
- `CanvasManager.cs`
- `InventoryItem.cs`
- `Item.cs`
- `SessionManager.cs`

Operações suportadas:

- Inicialização de inventário a partir dos dados do personagem/equipamentos.
- Pickup de item do chão.
- Merge/split de stacks por `networkID` e quantidade.
- Drop de um ou vários itens.
- Loot de outro personagem.
- Troca de itens entre personagens via `SessionManager.TradeItemsBetweenCharacters`.

A serialização de dados usa `LitJson` para transferir listas de itens e estados por RPC.

## Itens

`Item` é a base para todos os objetos coletáveis.

Campos/conceitos importantes:

- `id`: identifica o tipo de item/prefab.
- `networkID`: GUID/string usado para identificar uma instância lógica de item na sessão.
- Quantidade: encapsulada por subtipo (`Ammo.amount`, `Consumable.amount`, `Miscellaneous.amount`, `Weapon.ammo`).
- Estado no chão: controla rigidbody/collider e se pode ser coletado.

No servidor, itens do mapa são inicializados por `SessionManager.StartServer()` chamando `Item.ServerInitialize()`.

## Armas, munição e tiro

Arquivos principais:

- `Weapon.cs`
- `Ammo.cs`
- `Projectile.cs`
- `Character.cs`

Uma arma define:

- Tipo de empunhadura (`OneHanded`/`TwoHanded`).
- ID de munição compatível.
- Dano.
- Cadência de tiro.
- Tamanho do pente.
- Kick de mão/corpo.
- Dados de rig por personagem.
- Prefab de projétil.
- Muzzle e flash.

O tiro é iniciado pelo personagem, validado/aplicado pela arma e instancia um projétil localmente. O projétil aplica dano ao colidir, com lógica condicionada ao servidor.

## Movimento, mira e animações

Arquivos principais:

- `Character.cs`
- `CameraManager.cs`
- `RigManager.cs`
- `Assets/Scripts/Animator/EquipStateMachine.cs`
- `Assets/Scripts/Animator/HolsterStateMachine.cs`
- `Assets/Scripts/Animator/ReloadStateMachine.cs`

O personagem integra conceitos do Starter Assets e sincroniza estados relevantes:

- mira (`aiming`), alvo de mira (`aimTarget`);
- velocidade de movimento e movimento enquanto mira;
- equipar/guardar/recarregar arma;
- eventos de animação para concluir transições.

## UI

Arquivos principais:

- `MenuManager.cs` — conexão, autenticação, matchmaking.
- `CharactersManager.cs` — listagem e seleção de personagens/equipamentos.
- `CanvasManager.cs` — inventário e loot em gameplay.
- `MenuCharacterItem.cs`, `MenuWeaponItem.cs`, `InventoryItem.cs` — itens clicáveis de UI.

## Backend e progressão

`CharactersManager` usa `DevelopersHub.RealtimeNetworking.Client.RealtimeNetworking` para:

- buscar personagens;
- buscar equipamentos;
- selecionar personagem ativo;
- equipar/desequipar equipamentos no personagem.

Esses dados são usados depois por `SessionManager` para spawnar o personagem com inventário inicial.
